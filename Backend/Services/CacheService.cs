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
    }

    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheService> _logger;
        private readonly HashSet<string> _cacheKeys;
        private readonly SemaphoreSlim _semaphore;

        // Default cache expiration times
        private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan ShortExpiration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan LongExpiration = TimeSpan.FromHours(1);

        public CacheService(IMemoryCache memoryCache, ILogger<CacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _cacheKeys = new HashSet<string>();
            _semaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Get an item from cache
        /// </summary>
        public T? Get<T>(string key) where T : class
        {
            try
            {
                if (_memoryCache.TryGetValue(key, out var cachedValue))
                {
                    _logger.LogDebug("Cache hit for key: {Key}", key);
                    return cachedValue as T;
                }

                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving from cache for key: {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// Set an item in cache with optional expiration
        /// </summary>
        public void Set<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            try
            {
                _semaphore.Wait();

                var options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration,
                    SlidingExpiration = TimeSpan.FromMinutes(5), // Extend cache if accessed within 5 minutes
                    Priority = CacheItemPriority.Normal
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

                _logger.LogDebug("Cache set for key: {Key}, Expiration: {Expiration}", key, expiration ?? DefaultExpiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache for key: {Key}", key);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Remove a specific item from cache
        /// </summary>
        public void Remove(string key)
        {
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
                _logger.LogError(ex, "Error removing cache for key: {Key}", key);
            }
            finally
            {
                _semaphore.Release();
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
            var cachedItem = Get<T>(key);
            if (cachedItem != null)
            {
                return cachedItem;
            }

            var item = await getItem();
            Set(key, item, expiration);
            return item;
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