using Microsoft.EntityFrameworkCore;
using SkillSnap.Shared.Models;

namespace SkillSnap.Backend.Data
{
    public class SkillSnapContext : DbContext
    {
        public SkillSnapContext(DbContextOptions<SkillSnapContext> options) : base(options)
        {
        }

        public DbSet<Skill> Skills { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<PortfolioUser> PortfolioUsers { get; set; }
    }
}
