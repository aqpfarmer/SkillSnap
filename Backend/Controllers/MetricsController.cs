using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillSnap.Backend.Services;
using System.Diagnostics;

namespace SkillSnap.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Manager")] // Restrict to Manager role only
    public class MetricsController : ControllerBase
    {
        private readonly IMetricsService _metricsService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<MetricsController> _logger;

        public MetricsController(
            IMetricsService metricsService, 
            ICacheService cacheService,
            ILogger<MetricsController> logger)
        {
            _metricsService = metricsService;
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// Get comprehensive performance metrics
        /// </summary>
        [HttpGet]
        public ActionResult<MetricsSnapshot> GetMetrics()
        {
            try
            {
                var metrics = _metricsService.GetMetrics();
                
                // Add circuit breaker status
                var (consecutiveFailures, lastFailureTime, isOpen) = _cacheService.GetCircuitBreakerStatus();
                
                var response = new
                {
                    metrics.Uptime,
                    metrics.CacheMetrics,
                    metrics.QueryMetrics,
                    metrics.CircuitBreakerEvents,
                    CircuitBreakerStatus = new
                    {
                        IsOpen = isOpen,
                        ConsecutiveFailures = consecutiveFailures,
                        LastFailureTime = lastFailureTime,
                        Status = isOpen ? "OPEN" : "CLOSED"
                    },
                    SystemInfo = new
                    {
                        MachineName = Environment.MachineName,
                        ProcessorCount = Environment.ProcessorCount,
                        WorkingSet = Environment.WorkingSet,
                        GCMemory = GC.GetTotalMemory(false)
                    },
                    metrics.GeneratedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve metrics");
                return StatusCode(500, "Failed to retrieve metrics");
            }
        }

        /// <summary>
        /// Reset all performance metrics
        /// </summary>
        [HttpPost("reset")]
        public IActionResult ResetMetrics()
        {
            try
            {
                _metricsService.ResetMetrics();
                _logger.LogInformation("Metrics reset successfully");
                return Ok(new { Message = "Metrics reset successfully", ResetAt = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset metrics");
                return StatusCode(500, "Failed to reset metrics");
            }
        }

        /// <summary>
        /// Get cache statistics summary
        /// </summary>
        [HttpGet("cache-summary")]
        public IActionResult GetCacheSummary()
        {
            try
            {
                var metrics = _metricsService.GetMetrics();
                var cacheMetrics = metrics.CacheMetrics;

                var summary = new
                {
                    TotalRequests = cacheMetrics.Sum(m => m.TotalRequests),
                    TotalHits = cacheMetrics.Sum(m => m.Hits),
                    TotalMisses = cacheMetrics.Sum(m => m.Misses),
                    OverallHitRate = cacheMetrics.Sum(m => m.TotalRequests) > 0 
                        ? (double)cacheMetrics.Sum(m => m.Hits) / cacheMetrics.Sum(m => m.TotalRequests) * 100 
                        : 0,
                    AverageHitTime = cacheMetrics.Where(m => m.Hits > 0).DefaultIfEmpty().Average(m => m?.AverageHitTime ?? 0),
                    AverageMissTime = cacheMetrics.Where(m => m.Misses > 0).DefaultIfEmpty().Average(m => m?.AverageMissTime ?? 0),
                    CategoriesWithBestHitRate = cacheMetrics
                        .Where(m => m.TotalRequests >= 5) // Only categories with meaningful data
                        .OrderByDescending(m => m.HitRate)
                        .Take(3)
                        .Select(m => new { m.Category, HitRate = Math.Round(m.HitRate, 2) })
                        .ToList()
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cache summary");
                return StatusCode(500, "Failed to get cache summary");
            }
        }

        /// <summary>
        /// Simulate load to generate metrics data
        /// </summary>
        [HttpPost("simulate-load")]
        public async Task<IActionResult> SimulateLoad([FromQuery] int requests = 10)
        {
            if (requests > 100) requests = 100; // Safety limit
            
            try
            {
                var tasks = new List<Task>();
                var random = new Random();

                for (int i = 0; i < requests; i++)
                {
                    var delay = random.Next(10, 100); // Random delay between 10-100ms
                    tasks.Add(Task.Run(async () =>
                    {
                        await Task.Delay(delay);
                        
                        // Simulate different types of operations
                        var operation = random.Next(1, 5);
                        var sw = Stopwatch.StartNew();

                        switch (operation)
                        {
                            case 1:
                                _metricsService.TrackDatabaseQuery("SimulatedProjectQuery", sw.Elapsed, random.Next(1, 20));
                                break;
                            case 2:
                                _metricsService.TrackDatabaseQuery("SimulatedSkillQuery", sw.Elapsed, random.Next(5, 50));
                                break;
                            case 3:
                                if (random.NextDouble() > 0.3) // 70% cache hit rate
                                    _metricsService.TrackCacheHit($"projects_cache_{random.Next(1, 5)}", sw.Elapsed);
                                else
                                    _metricsService.TrackCacheMiss($"projects_cache_{random.Next(1, 5)}", sw.Elapsed);
                                break;
                            case 4:
                                if (random.NextDouble() > 0.2) // 80% cache hit rate
                                    _metricsService.TrackCacheHit($"skills_cache_{random.Next(1, 5)}", sw.Elapsed);
                                else
                                    _metricsService.TrackCacheMiss($"skills_cache_{random.Next(1, 5)}", sw.Elapsed);
                                break;
                        }
                    }));
                }

                await Task.WhenAll(tasks);

                return Ok(new 
                { 
                    Message = $"Simulated {requests} operations successfully",
                    SimulatedAt = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to simulate load");
                return StatusCode(500, "Failed to simulate load");
            }
        }

        /// <summary>
        /// Test circuit breaker functionality
        /// </summary>
        [HttpPost("test-circuit-breaker")]
        public IActionResult TestCircuitBreaker()
        {
            try
            {
                // Simulate circuit breaker actions
                _metricsService.TrackCircuitBreakerAction("FAILURE_DETECTED", "Simulated cache failure for testing");
                _metricsService.TrackCircuitBreakerAction("THRESHOLD_REACHED", "Multiple consecutive failures detected");
                _metricsService.TrackCircuitBreakerAction("CIRCUIT_OPENED", "Circuit breaker opened to prevent cascade failures");
                
                return Ok(new 
                { 
                    Message = "Circuit breaker events simulated successfully",
                    SimulatedAt = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test circuit breaker");
                return StatusCode(500, "Failed to test circuit breaker");
            }
        }
    }
}