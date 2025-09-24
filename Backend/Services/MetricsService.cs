using System.Collections.Concurrent;
using System.Diagnostics;

namespace SkillSnap.Backend.Services
{
    /// <summary>
    /// Service for tracking and collecting performance metrics
    /// </summary>
    public interface IMetricsService
    {
        void TrackCacheHit(string cacheKey, TimeSpan duration);
        void TrackCacheMiss(string cacheKey, TimeSpan duration);
        void TrackDatabaseQuery(string operation, TimeSpan duration, int resultCount = 0);
        void TrackCircuitBreakerAction(string action, string reason = "");
        MetricsSnapshot GetMetrics();
        void ResetMetrics();
    }

    public class MetricsService : IMetricsService
    {
        private readonly ConcurrentDictionary<string, CacheMetric> _cacheMetrics = new();
        private readonly ConcurrentDictionary<string, QueryMetric> _queryMetrics = new();
        private readonly List<CircuitBreakerEvent> _circuitBreakerEvents = new();
        private readonly object _lockObject = new object();
        private DateTime _startTime = DateTime.UtcNow;

        public void TrackCacheHit(string cacheKey, TimeSpan duration)
        {
            var category = GetCacheCategory(cacheKey);
            _cacheMetrics.AddOrUpdate(category, 
                new CacheMetric { Category = category, Hits = 1, TotalHitTime = duration },
                (key, existing) => new CacheMetric 
                { 
                    Category = category,
                    Hits = existing.Hits + 1,
                    Misses = existing.Misses,
                    TotalHitTime = existing.TotalHitTime.Add(duration),
                    TotalMissTime = existing.TotalMissTime
                });
        }

        public void TrackCacheMiss(string cacheKey, TimeSpan duration)
        {
            var category = GetCacheCategory(cacheKey);
            _cacheMetrics.AddOrUpdate(category,
                new CacheMetric { Category = category, Misses = 1, TotalMissTime = duration },
                (key, existing) => new CacheMetric
                {
                    Category = category,
                    Hits = existing.Hits,
                    Misses = existing.Misses + 1,
                    TotalHitTime = existing.TotalHitTime,
                    TotalMissTime = existing.TotalMissTime.Add(duration)
                });
        }

        public void TrackDatabaseQuery(string operation, TimeSpan duration, int resultCount = 0)
        {
            _queryMetrics.AddOrUpdate(operation,
                new QueryMetric { Operation = operation, Count = 1, TotalTime = duration, TotalResults = resultCount },
                (key, existing) => new QueryMetric
                {
                    Operation = operation,
                    Count = existing.Count + 1,
                    TotalTime = existing.TotalTime.Add(duration),
                    TotalResults = existing.TotalResults + resultCount
                });
        }

        public void TrackCircuitBreakerAction(string action, string reason = "")
        {
            lock (_lockObject)
            {
                _circuitBreakerEvents.Add(new CircuitBreakerEvent
                {
                    Timestamp = DateTime.UtcNow,
                    Action = action,
                    Reason = reason
                });

                // Keep only last 100 events to prevent memory issues
                if (_circuitBreakerEvents.Count > 100)
                {
                    _circuitBreakerEvents.RemoveRange(0, _circuitBreakerEvents.Count - 100);
                }
            }
        }

        public MetricsSnapshot GetMetrics()
        {
            var uptime = DateTime.UtcNow - _startTime;
            
            lock (_lockObject)
            {
                return new MetricsSnapshot
                {
                    Uptime = uptime,
                    CacheMetrics = _cacheMetrics.Values.ToList(),
                    QueryMetrics = _queryMetrics.Values.ToList(),
                    CircuitBreakerEvents = _circuitBreakerEvents.ToList(),
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }

        public void ResetMetrics()
        {
            _cacheMetrics.Clear();
            _queryMetrics.Clear();
            lock (_lockObject)
            {
                _circuitBreakerEvents.Clear();
            }
            _startTime = DateTime.UtcNow;
        }

        private string GetCacheCategory(string cacheKey)
        {
            if (cacheKey.StartsWith("projects", StringComparison.OrdinalIgnoreCase))
                return "Projects";
            if (cacheKey.StartsWith("skills", StringComparison.OrdinalIgnoreCase))
                return "Skills";
            if (cacheKey.StartsWith("users", StringComparison.OrdinalIgnoreCase))
                return "Users";
            if (cacheKey.StartsWith("portfoliousers", StringComparison.OrdinalIgnoreCase))
                return "PortfolioUsers";
            
            return "Other";
        }
    }

    public class MetricsSnapshot
    {
        public TimeSpan Uptime { get; set; }
        public List<CacheMetric> CacheMetrics { get; set; } = new();
        public List<QueryMetric> QueryMetrics { get; set; } = new();
        public List<CircuitBreakerEvent> CircuitBreakerEvents { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class CacheMetric
    {
        public string Category { get; set; } = "";
        public int Hits { get; set; }
        public int Misses { get; set; }
        public TimeSpan TotalHitTime { get; set; }
        public TimeSpan TotalMissTime { get; set; }
        
        public double HitRate => (Hits + Misses) > 0 ? (double)Hits / (Hits + Misses) * 100 : 0;
        public double AverageHitTime => Hits > 0 ? TotalHitTime.TotalMilliseconds / Hits : 0;
        public double AverageMissTime => Misses > 0 ? TotalMissTime.TotalMilliseconds / Misses : 0;
        public int TotalRequests => Hits + Misses;
    }

    public class QueryMetric
    {
        public string Operation { get; set; } = "";
        public int Count { get; set; }
        public TimeSpan TotalTime { get; set; }
        public int TotalResults { get; set; }
        
        public double AverageTime => Count > 0 ? TotalTime.TotalMilliseconds / Count : 0;
        public double AverageResults => Count > 0 ? (double)TotalResults / Count : 0;
    }

    public class CircuitBreakerEvent
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = "";
        public string Reason { get; set; } = "";
    }
}