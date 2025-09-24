using Microsoft.AspNetCore.Identity;

namespace SkillSnap.Shared.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property to Portfolio Users (one-to-one relationship)
        public PortfolioUser? PortfolioUser { get; set; }
        public int? PortfolioUserId { get; set; }
    }
}