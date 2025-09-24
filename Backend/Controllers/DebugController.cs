using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Backend.Data;
using SkillSnap.Shared.Models;

namespace SkillSnap.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DebugController : ControllerBase
    {
        private readonly SkillSnapContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DebugController(SkillSnapContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("user-info")]
        public async Task<IActionResult> GetUserInfo()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userEmail = User.FindFirst("email")?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("No user ID in token");
            }

            var applicationUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var portfolioUser = await _context.PortfolioUsers.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

            return Ok(new {
                TokenUserId = userId,
                TokenUserEmail = userEmail,
                ApplicationUserFound = applicationUser != null,
                ApplicationUserEmail = applicationUser?.Email,
                ApplicationUserPortfolioId = applicationUser?.PortfolioUserId,
                PortfolioUserFound = portfolioUser != null,
                PortfolioUserId = portfolioUser?.Id,
                PortfolioUserName = portfolioUser?.Name
            });
        }

        [HttpGet("all-users")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users.Select(u => new {
                u.Id,
                u.Email,
                u.PortfolioUserId,
                u.FirstName,
                u.LastName
            }).ToListAsync();

            var portfolios = await _context.PortfolioUsers.Select(p => new {
                p.Id,
                p.ApplicationUserId,
                p.Name
            }).ToListAsync();

            return Ok(new { Users = users, Portfolios = portfolios });
        }

        [HttpPost("repair-portfolios")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RepairPortfolios()
        {
            try
            {
                // Find users without portfolio records
                var usersWithoutPortfolios = await _context.Users
                    .Where(u => u.PortfolioUserId == null)
                    .ToListAsync();

                var repairedCount = 0;

                foreach (var user in usersWithoutPortfolios)
                {
                    // Create portfolio for the user
                    var portfolio = new PortfolioUser
                    {
                        Name = $"{user.FirstName} {user.LastName}".Trim(),
                        Bio = "Welcome to SkillSnap! Start building your portfolio by adding your skills and projects.",
                        ProfileImageUrl = "https://via.placeholder.com/150?text=User",
                        ApplicationUserId = user.Id
                    };

                    _context.PortfolioUsers.Add(portfolio);
                    await _context.SaveChangesAsync();

                    // Update user with portfolio reference
                    user.PortfolioUserId = portfolio.Id;
                    _context.Users.Update(user);
                    repairedCount++;
                }

                await _context.SaveChangesAsync();

                return Ok(new { 
                    Message = $"Successfully repaired {repairedCount} user portfolios",
                    RepairedUsers = usersWithoutPortfolios.Select(u => new { u.Email, u.FirstName, u.LastName })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error repairing portfolios", Error = ex.Message });
            }
        }

        [HttpGet("portfolio-images")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPortfolioImages()
        {
            var portfolioUsers = await _context.PortfolioUsers
                .Select(p => new { 
                    p.Id, 
                    p.Name, 
                    p.ProfileImageUrl,
                    ApplicationUserId = p.ApplicationUserId
                })
                .ToListAsync();

            return Ok(new {
                TotalCount = portfolioUsers.Count,
                Users = portfolioUsers,
                Message = "Portfolio user image URLs retrieved successfully"
            });
        }
    }
}