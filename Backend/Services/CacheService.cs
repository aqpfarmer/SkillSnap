using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace SkillSnap.Backend.Services
{
    /// <summary>
    /// Comprehensive caching service for SkillSnap application
    /// Provides memory-based caching with configurable expiration and cache invalidation
    /// </summary>
    public interface ICacheService
    {
        T? Get<T>(string key) where T : class;
        void Set<T>(string key, T value, TimeSpan? expiration = null) where T : class;
        void Remove(string key);
        void RemoveByPrefix(string prefix);
        void Clear();
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null) where T : class;
        bool IsCircuitBreakerOpen();
        void ResetCircuitBreaker();
        (int consecutiveFailures, DateTime lastFailureTime, bool isOpen) GetCircuitBreakerStatus();
    }

    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheService> _logger;
        private readonly IMetricsService? _metricsService;
        private readonly HashSet<string> _cacheKeys;
        private readonly SemaphoreSlim _semaphore;

        // Circuit breaker for cache failures
        private int _consecutiveFailures = 0;
        private DateTime _lastFailureTime = DateTime.MinValue;
        private readonly object _circuitBreakerLock = new object();
        private const int CircuitBreakerThreshold = 5;
        private static readonly TimeSpan CircuitBreakerTimeout = TimeSpan.FromMinutes(1);

        // Default cache expiration times
        private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan ShortExpiration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan LongExpiration = TimeSpan.FromHours(1);

        public CacheService(IMemoryCache memoryCache, ILogger<CacheService> logger, IMetricsService? metricsService = null)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _metricsService = metricsService;
            _cacheKeys = new HashSet<string>();
            _semaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Get an item from cache
        /// </summary>
        public T? Get<T>(string key) where T : class
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            if (IsCacheCircuitOpen())
            {
                _logger.LogDebug("Cache circuit is open, skipping cache for key: {Key}", key);
                stopwatch.Stop();
                _metricsService?.TrackCacheMiss(key, stopwatch.Elapsed);
                return null;
            }

            try
            {
                if (_memoryCache.TryGetValue(key, out var cachedValue))
                {
                    stopwatch.Stop();
                    _logger.LogDebug("Cache hit for key: {Key}", key);
                    _metricsService?.TrackCacheHit(key, stopwatch.Elapsed);
                    RecordCacheSuccess();
                    return cachedValue as T;
                }

                stopwatch.Stop();
                _logger.LogDebug("Cache miss for key: {Key}", key);
                _metricsService?.TrackCacheMiss(key, stopwatch.Elapsed);
                return null;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error retrieving from cache for key: {Key}", key);
                _metricsService?.TrackCacheMiss(key, stopwatch.Elapsed);
                RecordCacheFailure();
                return null;
            }
        }

        /// <summary>
        /// Check if the circuit breaker is open (cache is temporarily disabled due to failures)
        /// </summary>
        private bool IsCacheCircuitOpen()
        {
            lock (_circuitBreakerLock)
            {
                if (_consecutiveFailures >= CircuitBreakerThreshold)
                {
                    if (DateTime.UtcNow - _lastFailureTime > CircuitBreakerTimeout)
                    {
                        // Reset the circuit breaker
                        _consecutiveFailures = 0;
                        _logger.LogInformation("Cache circuit breaker reset after timeout");
                        return false;
                    }
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Record a cache operation success
        /// </summary>
        private void RecordCacheSuccess()
        {
            lock (_circuitBreakerLock)
            {
                if (_consecutiveFailures > 0)
                {
                    _consecutiveFailures = 0;
                    _logger.LogInformation("Cache circuit breaker reset after successful operation");
                }
            }
        }

        /// <summary>
        /// Record a cache operation failure
        /// </summary>
        private void RecordCacheFailure()
        {
            lock (_circuitBreakerLock)
            {
                _consecutiveFailures++;
                _lastFailureTime = DateTime.UtcNow;
                
                _metricsService?.TrackCircuitBreakerAction("FAILURE_RECORDED", $"Consecutive failures: {_consecutiveFailures}");
                
                if (_consecutiveFailures >= CircuitBreakerThreshold)
                {
                    _logger.LogWarning("Cache circuit breaker opened after {FailureCount} consecutive failures. Cache will be bypassed for {Timeout}.", 
                        _consecutiveFailures, CircuitBreakerTimeout);
                    
                    _metricsService?.TrackCircuitBreakerAction("CIRCUIT_OPENED", $"Threshold reached: {_consecutiveFailures} failures");
                }
            }
        }

        /// <summary>
        /// Set an item in cache with optional expiration
        /// </summary>
        public void Set<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            if (value == null)
            {
                _logger.LogWarning("Attempted to cache null value for key: {Key}", key);
                return;
            }

            if (IsCacheCircuitOpen())
            {
                _logger.LogDebug("Cache circuit is open, skipping cache set for key: {Key}", key);
                return;
            }

            try
            {
                _semaphore.Wait();

                var options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration,
                    SlidingExpiration = TimeSpan.FromMinutes(5), // Extend cache if accessed within 5 minutes
                    Priority = CacheItemPriority.Normal,
                    Size = 1 // Each cache entry counts as 1 unit towards the SizeLimit
                };

                // Add eviction callback for cleanup
                options.RegisterPostEvictionCallback((key, value, reason, state) =>
                {
                    _logger.LogDebug("Cache item evicted: {Key}, Reason: {Reason}", key, reason);
                    lock (_cacheKeys)
                    {
                        _cacheKeys.Remove(key.ToString() ?? string.Empty);
                    }
                });

                _memoryCache.Set(key, value, options);
                
                lock (_cacheKeys)
                {
                    _cacheKeys.Add(key);
                }

                RecordCacheSuccess();
                _logger.LogDebug("Cache set for key: {Key}, Expiration: {Expiration}", key, expiration ?? DefaultExpiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache for key: {Key}. Cache operation will be skipped, but application will continue.", key);
                RecordCacheFailure();
                // Don't throw - caching failure shouldn't break the application
            }
            finally
            {
                try
                {
                    _semaphore.Release();
                }
                catch (ObjectDisposedException)
                {
                    // Semaphore was disposed, which is fine during shutdown
                    _logger.LogDebug("Semaphore was disposed during cache set operation for key: {Key}", key);
                }
            }
        }

        /// <summary>
        /// Remove a specific item from cache
        /// </summary>
        public void Remove(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning("Attempted to remove cache entry with null or empty key");
                return;
            }

            try
            {
                _semaphore.Wait();
                
                _memoryCache.Remove(key);
                
                lock (_cacheKeys)
                {
                    _cacheKeys.Remove(key);
                }

                _logger.LogDebug("Cache removed for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache for key: {Key}. Cache may still contain stale data.", key);
                // Don't throw - cache removal failure shouldn't break the application
            }
            finally
            {
                try
                {
                    _semaphore.Release();
                }
                catch (ObjectDisposedException)
                {
                    _logger.LogDebug("Semaphore was disposed during cache remove operation for key: {Key}", key);
                }
            }
        }

        /// <summary>
        /// Remove all cache items with a specific prefix
        /// </summary>
        public void RemoveByPrefix(string prefix)
        {
            try
            {
                _semaphore.Wait();

                var keysToRemove = new List<string>();
                
                lock (_cacheKeys)
                {
                    keysToRemove = _cacheKeys.Where(k => k.StartsWith(prefix)).ToList();
                }

                foreach (var key in keysToRemove)
                {
                    _memoryCache.Remove(key);
                }

                lock (_cacheKeys)
                {
                    foreach (var key in keysToRemove)
                    {
                        _cacheKeys.Remove(key);
                    }
                }

                _logger.LogDebug("Removed {Count} cache items with prefix: {Prefix}", keysToRemove.Count, prefix);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache items with prefix: {Prefix}", prefix);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Clear all cache items
        /// </summary>
        public void Clear()
        {
            try
            {
                _semaphore.Wait();

                var keysToRemove = new List<string>();
                
                lock (_cacheKeys)
                {
                    keysToRemove = _cacheKeys.ToList();
                }

                foreach (var key in keysToRemove)
                {
                    _memoryCache.Remove(key);
                }

                lock (_cacheKeys)
                {
                    _cacheKeys.Clear();
                }

                _logger.LogInformation("Cache cleared - removed {Count} items", keysToRemove.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Get an item from cache, or set it if it doesn't exist
        /// </summary>
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null) where T : class
        {
            if (IsCacheCircuitOpen())
            {
                _logger.LogDebug("Cache circuit is open, bypassing cache for key: {Key}", key);
                return await getItem();
            }

            try
            {
                // First, try to get from cache
                var cachedItem = Get<T>(key);
                if (cachedItem != null)
                {
                    return cachedItem;
                }

                // Cache miss - get from source with retry logic
                var item = await GetWithFallbackAsync(getItem, key);
                
                // Try to cache the result, but don't fail if caching fails or circuit is open
                try
                {
                    if (item != null && !IsCacheCircuitOpen())
                    {
                        Set(key, item, expiration);
                    }
                }
                catch (Exception cacheEx)
                {
                    _logger.LogWarning(cacheEx, "Failed to cache item for key: {Key}, but continuing with uncached result", key);
                    RecordCacheFailure();
                }

                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOrSetAsync failed for key: {Key}, attempting fallback", key);
                RecordCacheFailure();
                
                // Final fallback - try to get data directly without caching
                try
                {
                    return await getItem();
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback also failed for key: {Key}", key);
                    throw; // Re-throw the original exception
                }
            }
        }

        /// <summary>
        /// Check if the circuit breaker is currently open
        /// </summary>
        public bool IsCircuitBreakerOpen()
        {
            return IsCacheCircuitOpen();
        }

        /// <summary>
        /// Manually reset the circuit breaker (useful for admin operations)
        /// </summary>
        public void ResetCircuitBreaker()
        {
            lock (_circuitBreakerLock)
            {
                _consecutiveFailures = 0;
                _lastFailureTime = DateTime.MinValue;
                _logger.LogInformation("Cache circuit breaker manually reset");
            }
        }

        /// <summary>
        /// Get detailed circuit breaker status information
        /// </summary>
        public (int consecutiveFailures, DateTime lastFailureTime, bool isOpen) GetCircuitBreakerStatus()
        {
            lock (_circuitBreakerLock)
            {
                return (_consecutiveFailures, _lastFailureTime, IsCacheCircuitOpen());
            }
        }

        /// <summary>
        /// Get data with retry and fallback logic
        /// </summary>
        private async Task<T> GetWithFallbackAsync<T>(Func<Task<T>> getItem, string key) where T : class
        {
            const int maxRetries = 3;
            const int baseDelayMs = 100;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var result = await getItem();
                    if (attempt > 1)
                    {
                        _logger.LogInformation("Successfully retrieved data for key: {Key} on attempt {Attempt}", key, attempt);
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries)
                    {
                        _logger.LogError(ex, "Failed to retrieve data for key: {Key} after {MaxRetries} attempts", key, maxRetries);
                        throw;
                    }

                    var delay = TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, attempt - 1)); // Exponential backoff
                    _logger.LogWarning(ex, "Attempt {Attempt} failed for key: {Key}, retrying in {Delay}ms", attempt, key, delay.TotalMilliseconds);
                    
                    await Task.Delay(delay);
                }
            }

            // This should never be reached, but just in case
            throw new InvalidOperationException($"Unexpected state in GetWithFallbackAsync for key: {key}");
        }
    }

    /// <summary>
    /// Cache key constants for consistent cache key management
    /// </summary>
    public static class CacheKeys
    {
        // Portfolio Users
        public const string ALL_PORTFOLIO_USERS = "portfolio_users:all";
        public const string PORTFOLIO_USER_BY_ID = "portfolio_user:id:{0}";
        public const string PORTFOLIO_USER_BY_APP_USER_ID = "portfolio_user:app_user:{0}";
        
        // Skills
        public const string ALL_SKILLS = "skills:all";
        public const string SKILLS_BY_USER_ID = "skills:user:{0}";
        public const string SKILL_BY_ID = "skill:id:{0}";
        
        // Projects
        public const string ALL_PROJECTS = "projects:all";
        public const string PROJECTS_BY_USER_ID = "projects:user:{0}";
        public const string PROJECT_BY_ID = "project:id:{0}";
        
        // User Roles
        public const string USER_ROLE = "user_role:portfolio_user:{0}";

        // Cache prefixes for bulk operations
        public const string PORTFOLIO_USERS_PREFIX = "portfolio_user";
        public const string SKILLS_PREFIX = "skill";
        public const string PROJECTS_PREFIX = "project";
        public const string USER_ROLES_PREFIX = "user_role";
    }
}