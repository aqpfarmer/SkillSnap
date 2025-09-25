using Microsoft.Extensions.Options;

namespace SkillSnap.Backend.Middleware
{
    /// <summary>
    /// Security configuration options
    /// </summary>
    public class SecurityOptions
    {
        public bool EnableContentSecurityPolicy { get; set; } = true;
        public bool EnableHsts { get; set; } = true;
        public bool EnableXFrameOptions { get; set; } = true;
        public bool EnableXContentTypeOptions { get; set; } = true;
        public bool EnableReferrerPolicy { get; set; } = true;
        public bool EnablePermissionsPolicy { get; set; } = true;
        public int HstsMaxAge { get; set; } = 31536000; // 1 year
        public string ContentSecurityPolicy { get; set; } = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; connect-src 'self'; font-src 'self'; object-src 'none'; media-src 'self'; frame-src 'none';";
    }

    /// <summary>
    /// Middleware for adding security headers and protecting against common attacks
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SecurityOptions _options;
        private readonly ILogger<SecurityHeadersMiddleware> _logger;

        public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityOptions> options, ILogger<SecurityHeadersMiddleware> logger)
        {
            _next = next;
            _options = options.Value;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Add security headers before processing the request
                AddSecurityHeaders(context);

                // Process the request
                await _next(context);

                // Additional post-processing if needed
                RemoveSensitiveHeaders(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in security headers middleware");
                throw;
            }
        }

        private void AddSecurityHeaders(HttpContext context)
        {
            var response = context.Response;

            // Content Security Policy
            if (_options.EnableContentSecurityPolicy && !response.Headers.ContainsKey("Content-Security-Policy"))
            {
                response.Headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
            }

            // HTTP Strict Transport Security
            if (_options.EnableHsts && context.Request.IsHttps && !response.Headers.ContainsKey("Strict-Transport-Security"))
            {
                response.Headers["Strict-Transport-Security"] = $"max-age={_options.HstsMaxAge}; includeSubDomains; preload";
            }

            // X-Frame-Options (prevent clickjacking)
            if (_options.EnableXFrameOptions && !response.Headers.ContainsKey("X-Frame-Options"))
            {
                response.Headers["X-Frame-Options"] = "DENY";
            }

            // X-Content-Type-Options (prevent MIME type sniffing)
            if (_options.EnableXContentTypeOptions && !response.Headers.ContainsKey("X-Content-Type-Options"))
            {
                response.Headers["X-Content-Type-Options"] = "nosniff";
            }

            // Referrer Policy
            if (_options.EnableReferrerPolicy && !response.Headers.ContainsKey("Referrer-Policy"))
            {
                response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            }

            // Permissions Policy (formerly Feature Policy)
            if (_options.EnablePermissionsPolicy && !response.Headers.ContainsKey("Permissions-Policy"))
            {
                response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), payment=(), usb=(), magnetometer=(), gyroscope=(), accelerometer=()";
            }

            // X-XSS-Protection (legacy but still useful for older browsers)
            if (!response.Headers.ContainsKey("X-XSS-Protection"))
            {
                response.Headers["X-XSS-Protection"] = "1; mode=block";
            }

            // Remove or mask server information
            if (!response.Headers.ContainsKey("Server"))
            {
                response.Headers["Server"] = "SkillSnap";
            }
        }

        private void RemoveSensitiveHeaders(HttpContext context)
        {
            var response = context.Response;

            // Remove sensitive headers that might leak information
            response.Headers.Remove("X-Powered-By");
            response.Headers.Remove("X-AspNet-Version");
            response.Headers.Remove("X-AspNetMvc-Version");
        }
    }

    /// <summary>
    /// Extension methods for registering security middleware
    /// </summary>
    public static class SecurityMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }

        public static IServiceCollection AddSecurityHeaders(this IServiceCollection services, Action<SecurityOptions>? configure = null)
        {
            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<SecurityOptions>(options => { }); // Use defaults
            }

            return services;
        }
    }
}