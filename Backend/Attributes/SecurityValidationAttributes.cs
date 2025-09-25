using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SkillSnap.Backend.Attributes
{
    /// <summary>
    /// Validation attribute to prevent XSS attacks
    /// </summary>
    public class NoXssAttribute : ValidationAttribute
    {
        private readonly string[] _xssPatterns = {
            @"<script[^>]*>.*?</script>",
            @"javascript:",
            @"vbscript:",
            @"onload\s*=",
            @"onerror\s*=",
            @"onclick\s*=",
            @"onmouseover\s*=",
            @"<iframe[^>]*>",
            @"<object[^>]*>",
            @"<embed[^>]*>"
        };

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return ValidationResult.Success;

            var input = value.ToString()!;
            
            foreach (var pattern in _xssPatterns)
            {
                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                {
                    return new ValidationResult("Input contains potentially malicious content and is not allowed.");
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validation attribute to prevent SQL injection
    /// </summary>
    public class NoSqlInjectionAttribute : ValidationAttribute
    {
        private readonly string[] _sqlPatterns = {
            @"(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE){0,1}|INSERT( +INTO){0,1}|MERGE|SELECT|UPDATE|UNION( +ALL){0,1})\b)",
            @"(\b(AND|OR)\b.{1,6}?\b(=|>|<|\!=|<>|<=|>=)\b)",
            @"(\bUNION\b.*?\bSELECT\b)",
            @";\s*(--|#)",
            @"/\*.*?\*/",
            @"'[\s]*;[\s]*--"
        };

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return ValidationResult.Success;

            var input = value.ToString()!;
            
            foreach (var pattern in _sqlPatterns)
            {
                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                {
                    return new ValidationResult("Input contains potentially malicious SQL patterns and is not allowed.");
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validation attribute for safe URLs
    /// </summary>
    public class SafeUrlAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return ValidationResult.Success;

            var url = value.ToString()!;
            
            // Check if it's a valid URI
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                return new ValidationResult("Invalid URL format.");

            // Only allow HTTP and HTTPS
            if (uri.Scheme != "http" && uri.Scheme != "https")
                return new ValidationResult("Only HTTP and HTTPS URLs are allowed.");

            // Check for suspicious patterns
            var suspiciousPatterns = new[] { "javascript:", "vbscript:", "data:", "file:" };
            foreach (var pattern in suspiciousPatterns)
            {
                if (url.ToLower().Contains(pattern))
                    return new ValidationResult("URL contains suspicious content and is not allowed.");
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validation attribute for strong passwords
    /// </summary>
    public class StrongPasswordAttribute : ValidationAttribute
    {
        public int MinLength { get; set; } = 8;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireDigit { get; set; } = true;
        public bool RequireSpecialChar { get; set; } = true;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return new ValidationResult("Password is required.");

            var password = value.ToString()!;
            var errors = new List<string>();

            if (password.Length < MinLength)
                errors.Add($"Password must be at least {MinLength} characters long.");

            if (RequireUppercase && !Regex.IsMatch(password, @"[A-Z]"))
                errors.Add("Password must contain at least one uppercase letter.");

            if (RequireLowercase && !Regex.IsMatch(password, @"[a-z]"))
                errors.Add("Password must contain at least one lowercase letter.");

            if (RequireDigit && !Regex.IsMatch(password, @"[0-9]"))
                errors.Add("Password must contain at least one digit.");

            if (RequireSpecialChar && !Regex.IsMatch(password, @"[!@#$%^&*()_+=\[{\]};:<>|./?,-]"))
                errors.Add("Password must contain at least one special character.");

            // Check for common weak passwords
            var commonPasswords = new[] { 
                "password", "123456", "password123", "admin", "qwerty", 
                "letmein", "welcome", "monkey", "dragon", "master" 
            };
            
            if (commonPasswords.Contains(password.ToLower()))
                errors.Add("Password is too common and not allowed.");

            if (errors.Any())
                return new ValidationResult(string.Join(" ", errors));

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validation attribute for safe text input (prevents XSS and limits length)
    /// </summary>
    public class SafeTextAttribute : ValidationAttribute
    {
        public int MaxLength { get; set; } = 1000;
        public bool AllowHtml { get; set; } = false;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return ValidationResult.Success;

            var input = value.ToString()!;

            // Check length
            if (input.Length > MaxLength)
                return new ValidationResult($"Input exceeds maximum length of {MaxLength} characters.");

            // Check for XSS if HTML is not allowed
            if (!AllowHtml)
            {
                var xssPatterns = new[] {
                    @"<script[^>]*>.*?</script>",
                    @"javascript:",
                    @"vbscript:",
                    @"onload\s*=",
                    @"onerror\s*=",
                    @"onclick\s*=",
                    @"<iframe[^>]*>",
                    @"<object[^>]*>",
                    @"<embed[^>]*>"
                };

                foreach (var pattern in xssPatterns)
                {
                    if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                    {
                        return new ValidationResult("Input contains potentially malicious content and is not allowed.");
                    }
                }
            }

            // Check for control characters (except normal whitespace)
            if (Regex.IsMatch(input, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]"))
                return new ValidationResult("Input contains invalid control characters.");

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validation attribute for secure email addresses
    /// </summary>
    public class SecureEmailAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return ValidationResult.Success;

            var email = value.ToString()!;

            // Basic email validation
            var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            if (!emailRegex.IsMatch(email))
                return new ValidationResult("Invalid email format.");

            // Length check (RFC 5321 limit)
            if (email.Length > 254)
                return new ValidationResult("Email address is too long.");

            // Check for suspicious patterns
            var suspiciousPatterns = new[] { "<script", "javascript:", "vbscript:", "<iframe", "'" };
            foreach (var pattern in suspiciousPatterns)
            {
                if (email.ToLower().Contains(pattern))
                    return new ValidationResult("Email contains suspicious content and is not allowed.");
            }

            return ValidationResult.Success;
        }
    }
}