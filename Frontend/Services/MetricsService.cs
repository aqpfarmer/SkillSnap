using System.Text.Json;
using System.Text;

namespace Frontend.Services
{
    public interface IMetricsService
    {
        Task<MetricsData?> GetMetricsAsync();
        Task<CacheSummary?> GetCacheSummaryAsync();
        Task<bool> ResetMetricsAsync();
        Task<bool> SimulateLoadAsync(int requests = 10);
        Task<bool> TestCircuitBreakerAsync();
    }

    public class MetricsService : IMetricsService
    {
        private readonly AuthenticatedHttpClientService _httpClientService;
        private readonly JsonSerializerOptions _jsonOptions;

        public MetricsService(AuthenticatedHttpClientService httpClientService)
        {
            _httpClientService = httpClientService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<MetricsData?> GetMetricsAsync()
        {
            try
            {
                var response = await _httpClientService.GetAsync("api/metrics");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<MetricsData>(content, _jsonOptions);
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting metrics: {ex.Message}");
                return null;
            }
        }

        public async Task<CacheSummary?> GetCacheSummaryAsync()
        {
            try
            {
                var response = await _httpClientService.GetAsync("api/metrics/cache-summary");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<CacheSummary>(content, _jsonOptions);
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting cache summary: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> ResetMetricsAsync()
        {
            try
            {
                var response = await _httpClientService.PostAsync("api/metrics/reset", new StringContent(""));
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting metrics: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SimulateLoadAsync(int requests = 10)
        {
            try
            {
                var response = await _httpClientService.PostAsync($"api/metrics/simulate-load?requests={requests}", new StringContent(""));
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error simulating load: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> TestCircuitBreakerAsync()
        {
            try
            {
                var response = await _httpClientService.PostAsync("api/metrics/test-circuit-breaker", new StringContent(""));
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing circuit breaker: {ex.Message}");
                return false;
            }
        }
    }

    // Data models for metrics
    public class MetricsData
    {
        public TimeSpan Uptime { get; set; }
        public List<CacheMetric> CacheMetrics { get; set; } = new();
        public List<QueryMetric> QueryMetrics { get; set; } = new();
        public List<CircuitBreakerEvent> CircuitBreakerEvents { get; set; } = new();
        public CircuitBreakerStatus CircuitBreakerStatus { get; set; } = new();
        public SystemInfo SystemInfo { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class CacheMetric
    {
        public string Category { get; set; } = "";
        public int Hits { get; set; }
        public int Misses { get; set; }
        public TimeSpan TotalHitTime { get; set; }
        public TimeSpan TotalMissTime { get; set; }
        public double HitRate { get; set; }
        public double AverageHitTime { get; set; }
        public double AverageMissTime { get; set; }
        public int TotalRequests { get; set; }
    }

    public class QueryMetric
    {
        public string Operation { get; set; } = "";
        public int Count { get; set; }
        public TimeSpan TotalTime { get; set; }
        public int TotalResults { get; set; }
        public double AverageTime { get; set; }
        public double AverageResults { get; set; }
    }

    public class CircuitBreakerEvent
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = "";
        public string Reason { get; set; } = "";
    }

    public class CircuitBreakerStatus
    {
        public bool IsOpen { get; set; }
        public int ConsecutiveFailures { get; set; }
        public DateTime LastFailureTime { get; set; }
        public string Status { get; set; } = "";
    }

    public class SystemInfo
    {
        public string MachineName { get; set; } = "";
        public int ProcessorCount { get; set; }
        public long WorkingSet { get; set; }
        public long GCMemory { get; set; }
    }

    public class CacheSummary
    {
        public int TotalRequests { get; set; }
        public int TotalHits { get; set; }
        public int TotalMisses { get; set; }
        public double OverallHitRate { get; set; }
        public double AverageHitTime { get; set; }
        public double AverageMissTime { get; set; }
        public List<CategoryHitRate> CategoriesWithBestHitRate { get; set; } = new();
    }

    public class CategoryHitRate
    {
        public string Category { get; set; } = "";
        public double HitRate { get; set; }
    }
}