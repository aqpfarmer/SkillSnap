using SkillSnap.Backend.Constants;

namespace SkillSnap.Backend.Services
{
    /// <summary>
    /// Extensions for cache operations to reduce code duplication
    /// </summary>
    public static class CacheExtensions
    {
        /// <summary>
        /// Gets cached data with standard duration (15 minutes)
        /// </summary>
        public static Task<T> GetOrSetStandardAsync<T>(this ICacheService cache, string key, Func<Task<T>> factory) where T : class
        {
            return cache.GetOrSetAsync(key, factory, AppConstants.CacheDurations.Standard);
        }

        /// <summary>
        /// Gets cached data with short duration (10 minutes)
        /// </summary>
        public static Task<T> GetOrSetShortAsync<T>(this ICacheService cache, string key, Func<Task<T>> factory) where T : class
        {
            return cache.GetOrSetAsync(key, factory, AppConstants.CacheDurations.Short);
        }

        /// <summary>
        /// Gets cached data with long duration (1 hour)
        /// </summary>
        public static Task<T> GetOrSetLongAsync<T>(this ICacheService cache, string key, Func<Task<T>> factory) where T : class
        {
            return cache.GetOrSetAsync(key, factory, AppConstants.CacheDurations.Long);
        }

        /// <summary>
        /// Gets cached data with user-specific duration (10 minutes)
        /// </summary>
        public static Task<T> GetOrSetUserSpecificAsync<T>(this ICacheService cache, string key, Func<Task<T>> factory) where T : class
        {
            return cache.GetOrSetAsync(key, factory, AppConstants.CacheDurations.UserSpecific);
        }

        /// <summary>
        /// Invalidates multiple cache entries with logging
        /// </summary>
        public static void InvalidateMultiple(this ICacheService cache, IEnumerable<string> keys, ILogger logger, string context = "cache operation")
        {
            var invalidatedCount = 0;
            foreach (var key in keys)
            {
                try
                {
                    cache.Remove(key);
                    invalidatedCount++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to invalidate cache key: {CacheKey} during {Context}", key, context);
                }
            }
            logger.LogDebug("Invalidated {Count} cache entries for {Context}", invalidatedCount, context);
        }
    }
}