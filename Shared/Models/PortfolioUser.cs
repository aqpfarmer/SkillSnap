using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkillSnap.Shared.Attributes;

namespace SkillSnap.Shared.Models
{
    public class PortfolioUser
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [SafeText(MaxLength = 100)]
        public string? Name { get; set; }

        [SafeText(MaxLength = 1000)]
        public String? Bio { get; set; }

        [SafeUrl]
        public string? ProfileImageUrl { get; set; }
        
        // Navigation properties
        public List<Project>? Projects { get; set; }
        public List<Skill>? Skills { get; set; }
        
        // Optional relationship to ApplicationUser (authentication)
        public string? ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser? ApplicationUser { get; set; }
    }
}