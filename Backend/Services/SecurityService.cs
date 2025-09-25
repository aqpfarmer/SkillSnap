using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;
using System.Web;

namespace SkillSnap.Backend.Services
{
    /// <summary>
    /// Comprehensive security service for protecting against XSS, SQL injection, and other security threats
    /// Implements Azure security best practices for input validation and sanitization
    /// </summary>
    public interface ISecurityService
    {
        // XSS Protection
        bool IsXssSafe(string input);
        string SanitizeHtml(string input);
        
        // SQL Injection Protection
        bool IsSqlInjectionSafe(string input);
        
        // URL Validation
        bool IsUrlSafe(string url);
        
        // Password Security
        bool IsPasswordStrong(string password);
        
        // Rate Limiting
        bool CheckRateLimit(string clientId, string endpoint);
        void ResetRateLimit(string clientId, string endpoint);
        
        // Input Validation
        string SanitizeInput(string input);
        bool IsValidEmail(string email);
    }

    public class SecurityService : ISecurityService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<SecurityService> _logger;

        // XSS patterns - comprehensive list of dangerous patterns
        private static readonly Regex[] XssPatterns = {
            new(@"<script[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline),
            new(@"javascript:", RegexOptions.IgnoreCase),
            new(@"vbscript:", RegexOptions.IgnoreCase),
            new(@"on\w+\s*=", RegexOptions.IgnoreCase),
            new(@"<iframe[^>]*>.*?</iframe>", RegexOptions.IgnoreCase | RegexOptions.Singleline),
            new(@"<object[^>]*>.*?</object>", RegexOptions.IgnoreCase | RegexOptions.Singleline),
            new(@"<embed[^>]*>", RegexOptions.IgnoreCase),
            new(@"<link[^>]*>", RegexOptions.IgnoreCase),
            new(@"<meta[^>]*>", RegexOptions.IgnoreCase),
            new(@"<\s*\/?\s*(?:applet|base|basefont|bgsound|blink|body|embed|frame|frameset|head|html|ilayer|iframe|layer|link|meta|object|plaintext|script|style|title|xml)[^>]*>", RegexOptions.IgnoreCase),
            new(@"&#x?[0-9a-f]+;?", RegexOptions.IgnoreCase),
            new(@"eval\s*\(", RegexOptions.IgnoreCase),
            new(@"expression\s*\(", RegexOptions.IgnoreCase)
        };

        // SQL Injection patterns - common attack vectors
        private static readonly Regex[] SqlInjectionPatterns = {
            new(@"('|(\'')|(;)|(\-\-)|(\s+(or|and)\s+.*(=|like))", RegexOptions.IgnoreCase),
            new(@"\b(union|select|insert|delete|update|drop|create|alter|exec|execute|sp_|xp_)\b", RegexOptions.IgnoreCase),
            new(@"(\%27)|(\')|(\-\-)|(%23)|(#)", RegexOptions.IgnoreCase),
            new(@"((\%3D)|(=))[^\n]*((\%27)|(\')|(\-\-)|(%23)|(#))", RegexOptions.IgnoreCase),
            new(@"\b(cast|convert|char|nchar|varchar|nvarchar)\s*\(", RegexOptions.IgnoreCase),
            new(@"\b(waitfor|delay)\s", RegexOptions.IgnoreCase),
            new(@"(benchmark|sleep)\s*\(", RegexOptions.IgnoreCase)
        };

        // Rate limiting configuration
        private readonly Dictionary<string, (int limit, TimeSpan window)> _rateLimits = new()
        {
            { "login", (5, TimeSpan.FromMinutes(5)) },
            { "register", (3, TimeSpan.FromMinutes(10)) },
            { "api", (100, TimeSpan.FromMinutes(1)) },
            { "default", (50, TimeSpan.FromMinutes(1)) }
        };

        public SecurityService(IMemoryCache cache, ILogger<SecurityService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public bool IsXssSafe(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return true;

            try
            {
                // Check against all XSS patterns
                foreach (var pattern in XssPatterns)
                {
                    if (pattern.IsMatch(input))
                    {
                        _logger.LogWarning("XSS pattern detected in input: {Pattern}", pattern.ToString());
                        return false;
                    }
                }

                // Additional checks for encoded attacks
                var decodedInput = HttpUtility.HtmlDecode(HttpUtility.UrlDecode(input));
                foreach (var pattern in XssPatterns)
                {
                    if (pattern.IsMatch(decodedInput))
                    {
                        _logger.LogWarning("XSS pattern detected in decoded input: {Pattern}", pattern.ToString());
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking XSS safety for input");
                return false; // Fail secure
            }
        }

        public string SanitizeHtml(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            try
            {
                // HTML encode the input to neutralize any dangerous content
                var sanitized = HttpUtility.HtmlEncode(input);
                
                // Remove null bytes and control characters
                sanitized = Regex.Replace(sanitized, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", string.Empty);
                
                return sanitized;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sanitizing HTML input");
                return HttpUtility.HtmlEncode(input); // Fallback to simple encoding
            }
        }

        public bool IsSqlInjectionSafe(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return true;

            try
            {
                // Check against all SQL injection patterns
                foreach (var pattern in SqlInjectionPatterns)
                {
                    if (pattern.IsMatch(input))
                    {
                        _logger.LogWarning("SQL injection pattern detected in input: {Pattern}", pattern.ToString());
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking SQL injection safety for input");
                return false; // Fail secure
            }
        }

        public bool IsUrlSafe(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return true;

            try
            {
                // Check if it's a valid URI
                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                {
                    return false;
                }

                // Only allow HTTP and HTTPS schemes
                if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                {
                    _logger.LogWarning("Unsafe URL scheme detected: {Scheme}", uri.Scheme);
                    return false;
                }

                // Check for suspicious patterns in URL
                var suspiciousPatterns = new[]
                {
                    @"javascript:",
                    @"vbscript:",
                    @"data:",
                    @"file:",
                    @"ftp:"
                };

                foreach (var pattern in suspiciousPatterns)
                {
                    if (url.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("Suspicious pattern in URL: {Pattern}", pattern);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating URL safety");
                return false;
            }
        }

        public bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // Strong password criteria based on Azure security recommendations
            return password.Length >= 8 &&
                   Regex.IsMatch(password, @"[A-Z]") && // At least one uppercase
                   Regex.IsMatch(password, @"[a-z]") && // At least one lowercase
                   Regex.IsMatch(password, @"\d") &&    // At least one digit
                   Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"); // At least one special character
        }

        public bool CheckRateLimit(string clientId, string endpoint)
        {
            try
            {
                // Determine the rate limit for this endpoint
                var endpointKey = endpoint.ToLowerInvariant();
                if (!_rateLimits.TryGetValue(endpointKey, out var limit))
                {
                    limit = _rateLimits["default"];
                }

                var cacheKey = $"rate_limit:{clientId}:{endpointKey}";
                var requests = _cache.Get<List<DateTime>>(cacheKey) ?? new List<DateTime>();

                // Remove old requests outside the window
                var cutoff = DateTime.UtcNow - limit.window;
                requests.RemoveAll(r => r < cutoff);

                // Check if limit exceeded
                if (requests.Count >= limit.limit)
                {
                    _logger.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Endpoint}", clientId, endpoint);
                    return false;
                }

                // Add current request
                requests.Add(DateTime.UtcNow);
                _cache.Set(cacheKey, requests, limit.window);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rate limit for client {ClientId}", clientId);
                return true; // Fail open for rate limiting
            }
        }

        public void ResetRateLimit(string clientId, string endpoint)
        {
            try
            {
                var cacheKey = $"rate_limit:{clientId}:{endpoint.ToLowerInvariant()}";
                _cache.Remove(cacheKey);
                _logger.LogInformation("Rate limit reset for client {ClientId} on endpoint {Endpoint}", clientId, endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting rate limit for client {ClientId}", clientId);
            }
        }

        public string SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            try
            {
                // Remove null bytes and control characters
                var sanitized = Regex.Replace(input, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", string.Empty);
                
                // Trim whitespace
                sanitized = sanitized.Trim();
                
                // Limit length to prevent DoS
                if (sanitized.Length > 10000)
                {
                    sanitized = sanitized.Substring(0, 10000);
                    _logger.LogWarning("Input truncated due to excessive length");
                }

                return sanitized;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sanitizing input");
                return input.Trim(); // Fallback to simple trim
            }
        }

        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Use built-in email validation with additional security checks
                var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
                if (!emailRegex.IsMatch(email))
                    return false;

                // Additional security checks
                if (email.Length > 254) // RFC 5321 limit
                    return false;

                // Check for suspicious patterns
                var suspiciousPatterns = new[]
                {
                    @"\.{2,}", // Multiple consecutive dots
                    @"^\.|\.$", // Starting or ending with dot
                    @"[<>""']", // Dangerous characters
                };

                foreach (var pattern in suspiciousPatterns)
                {
                    if (Regex.IsMatch(email, pattern))
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating email");
                return false;
            }
        }
    }
}