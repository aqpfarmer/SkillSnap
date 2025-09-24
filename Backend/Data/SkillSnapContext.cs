using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Shared.Models;

namespace SkillSnap.Backend.Data
{
    public class SkillSnapContext : IdentityDbContext<ApplicationUser>
    {
        public SkillSnapContext(DbContextOptions<SkillSnapContext> options) : base(options)
        {
        }

        public DbSet<Skill> Skills { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<PortfolioUser> PortfolioUsers { get; set; }
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // Configure ApplicationUser to PortfolioUser relationship (optional one-to-one)
            builder.Entity<ApplicationUser>()
                .HasOne(au => au.PortfolioUser)
                .WithOne(pu => pu.ApplicationUser)
                .HasForeignKey<ApplicationUser>(au => au.PortfolioUserId)
                .IsRequired(false);
        }
    }
}
