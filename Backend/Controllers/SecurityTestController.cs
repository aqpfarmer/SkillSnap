using Microsoft.AspNetCore.Mvc;
using SkillSnap.Backend.Services;

namespace SkillSnap.Backend.Controllers
{
    /// <summary>
    /// Test controller for verifying security features
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SecurityTestController : ControllerBase
    {
        private readonly ISecurityService _securityService;
        private readonly ILogger<SecurityTestController> _logger;

        public SecurityTestController(ISecurityService securityService, ILogger<SecurityTestController> logger)
        {
            _securityService = securityService;
            _logger = logger;
        }

        /// <summary>
        /// Test XSS protection
        /// </summary>
        [HttpPost("test-xss")]
        public IActionResult TestXss([FromBody] TestInputDto input)
        {
            try
            {
                var isXssSafe = _securityService.IsXssSafe(input.TestString);
                var sanitizedInput = _securityService.SanitizeHtml(input.TestString);

                return Ok(new
                {
                    original = input.TestString,
                    isXssSafe,
                    sanitized = sanitizedInput,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing XSS protection");
                return BadRequest("Error testing XSS protection");
            }
        }

        /// <summary>
        /// Test SQL injection protection
        /// </summary>
        [HttpPost("test-sql-injection")]
        public IActionResult TestSqlInjection([FromBody] TestInputDto input)
        {
            try
            {
                var isSqlSafe = _securityService.IsSqlInjectionSafe(input.TestString);
                var sanitizedInput = _securityService.SanitizeInput(input.TestString);

                return Ok(new
                {
                    original = input.TestString,
                    isSqlSafe,
                    sanitized = sanitizedInput,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing SQL injection protection");
                return BadRequest("Error testing SQL injection protection");
            }
        }

        /// <summary>
        /// Test URL validation
        /// </summary>
        [HttpPost("test-url")]
        public IActionResult TestUrl([FromBody] TestInputDto input)
        {
            try
            {
                var isUrlSafe = _securityService.IsUrlSafe(input.TestString);

                return Ok(new
                {
                    url = input.TestString,
                    isUrlSafe,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing URL validation");
                return BadRequest("Error testing URL validation");
            }
        }

        /// <summary>
        /// Test password strength
        /// </summary>
        [HttpPost("test-password")]
        public IActionResult TestPassword([FromBody] TestInputDto input)
        {
            try
            {
                var isPasswordStrong = _securityService.IsPasswordStrong(input.TestString);

                return Ok(new
                {
                    passwordLength = input.TestString?.Length ?? 0,
                    isPasswordStrong,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing password strength");
                return BadRequest("Error testing password strength");
            }
        }

        /// <summary>
        /// Test email validation
        /// </summary>
        [HttpPost("test-email")]
        public IActionResult TestEmail([FromBody] TestInputDto input)
        {
            try
            {
                var isValidEmail = _securityService.IsValidEmail(input.TestString);

                return Ok(new
                {
                    email = input.TestString,
                    isValidEmail,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing email validation");
                return BadRequest("Error testing email validation");
            }
        }

        /// <summary>
        /// Test rate limiting - call this multiple times to trigger rate limit
        /// </summary>
        [HttpGet("test-rate-limit")]
        public IActionResult TestRateLimit()
        {
            try
            {
                var clientId = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var endpoint = "test-rate-limit";
                
                var isAllowed = _securityService.CheckRateLimit(clientId, endpoint);

                return Ok(new
                {
                    clientId,
                    endpoint,
                    isAllowed,
                    timestamp = DateTime.UtcNow,
                    message = isAllowed ? "Request allowed" : "Rate limit exceeded"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing rate limit");
                return BadRequest("Error testing rate limit");
            }
        }

        /// <summary>
        /// Get comprehensive security status
        /// </summary>
        [HttpGet("security-status")]
        public IActionResult GetSecurityStatus()
        {
            try
            {
                return Ok(new
                {
                    securityServiceAvailable = true,
                    features = new
                    {
                        xssProtection = true,
                        sqlInjectionProtection = true,
                        urlValidation = true,
                        passwordValidation = true,
                        emailValidation = true,
                        rateLimiting = true
                    },
                    enhancedPasswordSettings = new
                    {
                        minimumLength = 8,
                        requiresDigit = true,
                        requiresUppercase = true,
                        requiresLowercase = true,
                        requiresNonAlphanumeric = true,
                        requiresUniqueChars = 4
                    },
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting security status");
                return BadRequest("Error getting security status");
            }
        }
    }

    /// <summary>
    /// DTO for test input
    /// </summary>
    public class TestInputDto
    {
        public string? TestString { get; set; }
    }
}