using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillSnap.Backend.Services;

namespace SkillSnap.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Only admins can access cache management
    public class CacheController : ControllerBase
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<CacheController> _logger;

        public CacheController(ICacheService cacheService, ILogger<CacheController> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// Clear all cache entries (Admin only)
        /// </summary>
        [HttpPost("clear")]
        public ActionResult ClearCache()
        {
            _cacheService.Clear();
            _logger.LogInformation("Cache cleared by admin user: {UserId}", User.Identity?.Name);
            return Ok(new { message = "Cache cleared successfully", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Clear cache entries by prefix (Admin only)
        /// </summary>
        [HttpPost("clear-prefix/{prefix}")]
        public ActionResult ClearCacheByPrefix(string prefix)
        {
            _cacheService.RemoveByPrefix(prefix);
            _logger.LogInformation("Cache cleared by prefix '{Prefix}' by admin user: {UserId}", prefix, User.Identity?.Name);
            return Ok(new { message = $"Cache entries with prefix '{prefix}' cleared successfully", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Get cache status and performance info (Admin only)
        /// </summary>
        [HttpGet("status")]
        public ActionResult GetCacheStatus()
        {
            return Ok(new
            {
                message = "Cache service is running",
                timestamp = DateTime.UtcNow,
                configuration = new
                {
                    defaultExpiration = "15 minutes",
                    slidingExpiration = "5 minutes",
                    maxEntries = 1024,
                    compactionPercentage = "20%"
                },
                availablePrefixes = new[]
                {
                    CacheKeys.PORTFOLIO_USERS_PREFIX,
                    CacheKeys.SKILLS_PREFIX,
                    CacheKeys.PROJECTS_PREFIX,
                    CacheKeys.USER_ROLES_PREFIX
                }
            });
        }
    }
}