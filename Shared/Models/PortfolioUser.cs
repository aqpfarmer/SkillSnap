using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSnap.Shared.Models
{
    public class PortfolioUser
    {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }
        public String? Bio { get; set; }
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