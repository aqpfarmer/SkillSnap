using Microsoft.AspNetCore.Identity;
using SkillSnap.Shared.Models;
using System;
using System.Linq;

namespace SkillSnap.Backend.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(SkillSnapContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            context.Database.EnsureCreated();

            // Create roles if they don't exist
            await CreateRolesAsync(roleManager);

            // Create admin user if it doesn't exist
            await CreateAdminUserAsync(userManager, context);

            // Seed portfolio data if needed
            await SeedPortfolioDataAsync(context, userManager);
        }

        private static async Task CreateRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roleNames = { "Admin", "Manager", "User" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        private static async Task CreateAdminUserAsync(UserManager<ApplicationUser> userManager, SkillSnapContext context)
        {
            // Check if admin user already exists
            var adminUser = await userManager.FindByEmailAsync("admin@skillsnap.com");
            if (adminUser == null)
            {
                // Create admin user
                adminUser = new ApplicationUser
                {
                    UserName = "admin@skillsnap.com",
                    Email = "admin@skillsnap.com",
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!"); // Strong password for admin

                if (result.Succeeded)
                {
                    // Assign Admin role
                    await userManager.AddToRoleAsync(adminUser, "Admin");

                    // Create portfolio for admin
                    var adminPortfolio = new PortfolioUser
                    {
                        Name = "SkillSnap Administrator",
                        Bio = "System administrator with full access to manage users, projects, and skills.",
                        ProfileImageUrl = "https://via.placeholder.com/150?text=Admin",
                        ApplicationUserId = adminUser.Id
                    };

                    context.PortfolioUsers.Add(adminPortfolio);
                    await context.SaveChangesAsync();

                    // Update admin user with portfolio reference
                    adminUser.PortfolioUserId = adminPortfolio.Id;
                    await userManager.UpdateAsync(adminUser);
                }
            }
        }

        private static async Task SeedPortfolioDataAsync(SkillSnapContext context, UserManager<ApplicationUser> userManager)
        {
            // Only seed if there are no portfolio users (excluding admin)
            if (context.PortfolioUsers.Count() > 1) // More than just admin
            {
                return; // DB has been seeded
            }

            // Create a sample regular user
            var sampleUser = await userManager.FindByEmailAsync("demo@skillsnap.com");
            if (sampleUser == null)
            {
                sampleUser = new ApplicationUser
                {
                    UserName = "demo@skillsnap.com",
                    Email = "demo@skillsnap.com",
                    FirstName = "Demo",
                    LastName = "User",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(sampleUser, "Demo123!"); // Strong password for demo

                if (result.Succeeded)
                {
                    // Assign User role
                    await userManager.AddToRoleAsync(sampleUser, "User");

                    // Create portfolio for demo user
                    var demoPortfolio = new PortfolioUser
                    {
                        Name = "Demo User",
                        Bio = "A full-stack developer with a passion for creating beautiful and functional web applications.",
                        ProfileImageUrl = "https://avatars.githubusercontent.com/u/1000000?v=4",
                        ApplicationUserId = sampleUser.Id
                    };

                    context.PortfolioUsers.Add(demoPortfolio);
                    await context.SaveChangesAsync();

                    // Update user with portfolio reference
                    sampleUser.PortfolioUserId = demoPortfolio.Id;
                    await userManager.UpdateAsync(sampleUser);

                    // Add sample skills for demo user
                    var skills = new Skill[]
                    {
                        new Skill{Name="C#", Level="Expert", PortfolioUserId = demoPortfolio.Id},
                        new Skill{Name="ASP.NET Core", Level="Expert", PortfolioUserId = demoPortfolio.Id},
                        new Skill{Name="Blazor", Level="Intermediate", PortfolioUserId = demoPortfolio.Id},
                        new Skill{Name="SQL", Level="Advanced", PortfolioUserId = demoPortfolio.Id},
                        new Skill{Name="Azure", Level="Intermediate", PortfolioUserId = demoPortfolio.Id}
                    };

                    foreach (Skill s in skills)
                    {
                        context.Skills.Add(s);
                    }
                    await context.SaveChangesAsync();

                    // Add sample projects for demo user
                    var projects = new Project[]
                    {
                        new Project{Title="E-commerce Website", Description="A full-featured e-commerce site built with ASP.NET Core and Blazor.", ProjectUrl="https://github.com", PortfolioUserId = demoPortfolio.Id},
                        new Project{Title="Task Management App", Description="A simple and intuitive task management application.", ProjectUrl="https://github.com", PortfolioUserId = demoPortfolio.Id},
                    };

                    foreach (Project p in projects)
                    {
                        context.Projects.Add(p);
                    }
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
