using SkillSnap.Shared.Models;
using System;
using System.Linq;

namespace SkillSnap.Backend.Data
{
    public static class DbInitializer
    {
        public static void Initialize(SkillSnapContext context)
        {
            context.Database.EnsureCreated();

            // Look for any users.
            if (context.PortfolioUsers.Any())
            {
                return;   // DB has been seeded
            }

            var user = new PortfolioUser
            {
                Name = "Chris L",
                Bio = "A full-stack developer with a passion for creating beautiful and functional web applications.",
                ProfileImageUrl = "https://avatars.githubusercontent.com/u/1000000?v=4",
            };

            context.PortfolioUsers.Add(user);
            context.SaveChanges();

            var skills = new Skill[]
            {
                new Skill{Name="C#", Level="Expert", PortfolioUserId = user.Id},
                new Skill{Name="ASP.NET Core", Level="Expert", PortfolioUserId = user.Id},
                new Skill{Name="Blazor", Level="Intermediate", PortfolioUserId = user.Id},
                new Skill{Name="SQL", Level="Advanced", PortfolioUserId = user.Id},
                new Skill{Name="Azure", Level="Intermediate", PortfolioUserId = user.Id}
            };

            foreach (Skill s in skills)
            {
                context.Skills.Add(s);
            }
            context.SaveChanges();

            var projects = new Project[]
            {
                new Project{Title="E-commerce Website", Description="A full-featured e-commerce site built with ASP.NET Core and Blazor.", ProjectUrl="https://github.com", PortfolioUserId = user.Id},
                new Project{Title="Task Management App", Description="A simple and intuitive task management application.", ProjectUrl="https://github.com", PortfolioUserId = user.Id},
            };

            foreach (Project p in projects)
            {
                context.Projects.Add(p);
            }
            context.SaveChanges();
        }
    }
}
