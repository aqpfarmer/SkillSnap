using System.ComponentModel.DataAnnotations;
using SkillSnap.Shared.Attributes;

namespace SkillSnap.Shared.Models
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Email is required")]
        [SecureEmail]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
        [NoXss]
        [NoSqlInjection]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterDto
    {
        [Required(ErrorMessage = "Email is required")]
        [SecureEmail]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StrongPassword]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password confirmation is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [SafeText(MaxLength = 50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [SafeText(MaxLength = 50)]
        public string LastName { get; set; } = string.Empty;

        [SafeText(MaxLength = 20)]
        public string? Role { get; set; } = "User"; // Default to User role
    }

    public class AuthResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public UserDto? User { get; set; }
    }

    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int? PortfolioUserId { get; set; }
    }
}