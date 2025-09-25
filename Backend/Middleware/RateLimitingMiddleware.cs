using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace SkillSnap.Backend.Middleware
{
    /// <summary>
    /// Rate limiting configuration options
    /// </summary>
    public class RateLimitOptions
    {
        public int MaxRequests { get; set; } = 100;
        public TimeSpan TimeWindow { get; set; } = TimeSpan.FromMinutes(1);
        public string Message { get; set; } = "Rate limit exceeded. Try again later.";
        public Dictionary<string, RateLimitRule> EndpointRules { get; set; } = new();
    }

    /// <summary>
    /// Rate limit rule for specific endpoints
    /// </summary>
    public class RateLimitRule
    {
        public int MaxRequests { get; set; }
        public TimeSpan TimeWindow { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Middleware for rate limiting to prevent abuse and DDoS attacks
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly RateLimitOptions _options;
        private readonly ILogger<RateLimitingMiddleware> _logger;

        public RateLimitingMiddleware(
            RequestDelegate next, 
            IMemoryCache cache, 
            RateLimitOptions options,
            ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _cache = cache;
            _options = options;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                var clientId = GetClientIdentifier(context);
                var endpoint = context.Request.Path.Value ?? "";
                
                // Get rate limit rules for this endpoint
                var rule = GetRateLimitRule(endpoint);
                
                if (await IsRateLimitExceeded(clientId, endpoint, rule))
                {
                    _logger.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Endpoint}", clientId, endpoint);
                    
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    context.Response.ContentType = "application/json";
                    
                    var response = new
                    {
                        error = "Rate limit exceeded",
                        message = rule.Message ?? _options.Message,
                        retryAfter = rule.TimeWindow.TotalSeconds
                    };
                    
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
                    return;
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in rate limiting middleware");
                await _next(context); // Continue processing on error
            }
        }

        private string GetClientIdentifier(HttpContext context)
        {
            // Try to get IP from X-Forwarded-For header (for load balancers/proxies)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',');
                if (ips.Length > 0)
                    return ips[0].Trim();
            }

            // Try to get IP from X-Real-IP header
            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
                return realIp;

            // Fall back to connection remote IP
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private RateLimitRule GetRateLimitRule(string endpoint)
        {
            // Check for specific endpoint rules
            foreach (var kvp in _options.EndpointRules)
            {
                if (endpoint.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }

            // Return default rule
            return new RateLimitRule
            {
                MaxRequests = _options.MaxRequests,
                TimeWindow = _options.TimeWindow,
                Message = _options.Message
            };
        }

        private Task<bool> IsRateLimitExceeded(string clientId, string endpoint, RateLimitRule rule)
        {
            var key = $"rate_limit:{clientId}:{endpoint}";
            
            if (_cache.TryGetValue(key, out List<DateTime>? requests))
            {
                // Remove expired requests
                var cutoff = DateTime.UtcNow - rule.TimeWindow;
                requests = requests!.Where(r => r > cutoff).ToList();
                
                if (requests.Count >= rule.MaxRequests)
                {
                    return Task.FromResult(true);
                }
                
                requests.Add(DateTime.UtcNow);
                _cache.Set(key, requests, rule.TimeWindow);
            }
            else
            {
                requests = new List<DateTime> { DateTime.UtcNow };
                _cache.Set(key, requests, rule.TimeWindow);
            }

            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Extension methods for rate limiting middleware
    /// </summary>
    public static class RateLimitingExtensions
    {
        public static IServiceCollection AddRateLimiting(this IServiceCollection services, Action<RateLimitOptions>? configure = null)
        {
            var options = new RateLimitOptions();
            configure?.Invoke(options);

            // Configure specific endpoint rules
            options.EndpointRules.TryAdd("/api/auth/login", new RateLimitRule 
            { 
                MaxRequests = 5, 
                TimeWindow = TimeSpan.FromMinutes(1),
                Message = "Too many login attempts. Please wait before trying again."
            });
            
            options.EndpointRules.TryAdd("/api/auth/register", new RateLimitRule 
            { 
                MaxRequests = 3, 
                TimeWindow = TimeSpan.FromMinutes(5),
                Message = "Too many registration attempts. Please wait before trying again."
            });

            services.AddSingleton(options);
            services.AddMemoryCache(); // Required for rate limiting storage
            return services;
        }

        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RateLimitingMiddleware>();
        }
    }
}