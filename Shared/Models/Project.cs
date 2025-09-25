using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkillSnap.Shared.Attributes;

namespace SkillSnap.Shared.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Project title is required")]
        [SafeText(MaxLength = 200)]
        public string? Title { get; set; }

        [SafeText(MaxLength = 2000)]
        public String? Description { get; set; }

        [SafeUrl]
        public string? ProjectUrl { get; set; }

        [ForeignKey("PortfolioUser")]
        public int PortfolioUserId { get; set; }
        public PortfolioUser? PortfolioUser { get; set; }
    }
}