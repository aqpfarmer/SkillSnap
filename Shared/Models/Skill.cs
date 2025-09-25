using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkillSnap.Shared.Attributes;

namespace SkillSnap.Shared.Models
{
    public class Skill
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Skill name is required")]
        [SafeText(MaxLength = 100)]
        public string? Name { get; set; }

        [SafeText(MaxLength = 50)]
        public String? Level { get; set; }

        [ForeignKey("PortfolioUser")]
        public int PortfolioUserId { get; set; }
        public PortfolioUser? PortfolioUser { get; set; }
    }
}