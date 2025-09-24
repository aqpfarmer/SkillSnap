using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Backend.Data;
using SkillSnap.Backend.Services;
using SkillSnap.Shared.Models;

namespace SkillSnap.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Require authentication for all endpoints
    public class PortfolioUsersController : ControllerBase
    {
        private readonly SkillSnapContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICacheService _cacheService;

        public PortfolioUsersController(SkillSnapContext context, UserManager<ApplicationUser> userManager, ICacheService cacheService)
        {
            _context = context;
            _userManager = userManager;
            _cacheService = cacheService;
        }

        // GET: api/PortfolioUsers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PortfolioUser>>> GetPortfolioUsers()
        {
            return await _cacheService.GetOrSetAsync(
                CacheKeys.ALL_PORTFOLIO_USERS,
                async () => await _context.PortfolioUsers
                    .AsNoTracking()
                    .Include(p => p.Projects)
                    .Include(p => p.Skills)
                    .ToListAsync(),
                TimeSpan.FromMinutes(15)
            );
        }

        // GET: api/PortfolioUsers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PortfolioUser>> GetPortfolioUser(int id)
        {
            var cacheKey = string.Format(CacheKeys.PORTFOLIO_USER_BY_ID, id);
            
            var portfolioUser = await _cacheService.GetOrSetAsync(
                cacheKey,
                async () => await _context.PortfolioUsers
                    .AsNoTracking()
                    .Include(p => p.Projects)
                    .Include(p => p.Skills)
                    .FirstOrDefaultAsync(p => p.Id == id),
                TimeSpan.FromMinutes(15)
            );

            if (portfolioUser == null)
            {
                return NotFound();
            }

            return portfolioUser;
        }

        // GET: api/PortfolioUsers/me
        [HttpGet("me")]
        public async Task<ActionResult<PortfolioUser>> GetMyPortfolioUser()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var portfolioUser = await _context.PortfolioUsers
                .Include(p => p.Projects)
                .Include(p => p.Skills)
                .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

            if (portfolioUser == null)
            {
                // Auto-create portfolio if it doesn't exist
                var applicationUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (applicationUser == null)
                {
                    return Unauthorized("User not found");
                }

                // Create a new portfolio for the user
                portfolioUser = new PortfolioUser
                {
                    Name = $"{applicationUser.FirstName} {applicationUser.LastName}".Trim(),
                    Bio = "Welcome to SkillSnap! Start building your portfolio by adding your skills and projects.",
                    ProfileImageUrl = "https://via.placeholder.com/150?text=User",
                    ApplicationUserId = applicationUser.Id
                };

                _context.PortfolioUsers.Add(portfolioUser);
                await _context.SaveChangesAsync();

                // Update application user with portfolio reference
                applicationUser.PortfolioUserId = portfolioUser.Id;
                _context.Users.Update(applicationUser);
                await _context.SaveChangesAsync();

                // Reload with includes
                portfolioUser = await _context.PortfolioUsers
                    .Include(p => p.Projects)
                    .Include(p => p.Skills)
                    .FirstOrDefaultAsync(p => p.Id == portfolioUser.Id);
            }

            return portfolioUser!;
        }

        // PUT: api/PortfolioUsers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPortfolioUser(int id, PortfolioUser portfolioUser)
        {
            if (id != portfolioUser.Id)
            {
                return BadRequest();
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");
            
            // Allow users to edit their own portfolio, or admins to edit any portfolio
            if (!isAdmin)
            {
                var existingPortfolioUser = await _context.PortfolioUsers.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                if (existingPortfolioUser?.ApplicationUserId != userId)
                {
                    return Forbid("You can only edit your own portfolio");
                }
            }

            // Update the entity properly to avoid tracking conflicts
            var existingEntity = await _context.PortfolioUsers.FindAsync(id);
            if (existingEntity == null)
            {
                return NotFound();
            }

            // Update only the fields that can be modified
            existingEntity.Name = portfolioUser.Name;
            existingEntity.Bio = portfolioUser.Bio;
            existingEntity.ProfileImageUrl = portfolioUser.ProfileImageUrl;

            try
            {
                await _context.SaveChangesAsync();
                
                // Invalidate related cache entries after successful update
                _cacheService.Remove(CacheKeys.ALL_PORTFOLIO_USERS);
                _cacheService.Remove(string.Format(CacheKeys.PORTFOLIO_USER_BY_ID, id));
                if (existingEntity.ApplicationUserId != null)
                {
                    _cacheService.Remove(string.Format(CacheKeys.PORTFOLIO_USER_BY_APP_USER_ID, existingEntity.ApplicationUserId));
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PortfolioUserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/PortfolioUsers
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PortfolioUser>> PostPortfolioUser(PortfolioUser portfolioUser)
        {
            _context.PortfolioUsers.Add(portfolioUser);
            await _context.SaveChangesAsync();

            // Invalidate cache after creating new user
            _cacheService.Remove(CacheKeys.ALL_PORTFOLIO_USERS);

            return CreatedAtAction("GetPortfolioUser", new { id = portfolioUser.Id }, portfolioUser);
        }

        // DELETE: api/PortfolioUsers/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePortfolioUser(int id)
        {
            var portfolioUser = await _context.PortfolioUsers.FindAsync(id);
            if (portfolioUser == null)
            {
                return NotFound();
            }

            _context.PortfolioUsers.Remove(portfolioUser);
            await _context.SaveChangesAsync();

            // Invalidate related cache entries after successful deletion
            _cacheService.Remove(CacheKeys.ALL_PORTFOLIO_USERS);
            _cacheService.Remove(string.Format(CacheKeys.PORTFOLIO_USER_BY_ID, id));
            if (portfolioUser.ApplicationUserId != null)
            {
                _cacheService.Remove(string.Format(CacheKeys.PORTFOLIO_USER_BY_APP_USER_ID, portfolioUser.ApplicationUserId));
            }

            return NoContent();
        }

        // GET: api/PortfolioUsers/5/role
        [HttpGet("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<string>> GetUserRole(int id)
        {
            var portfolioUser = await _context.PortfolioUsers.FindAsync(id);
            if (portfolioUser == null || portfolioUser.ApplicationUserId == null)
            {
                return NotFound();
            }

            var applicationUser = await _userManager.FindByIdAsync(portfolioUser.ApplicationUserId);
            if (applicationUser == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(applicationUser);
            return Ok(roles.FirstOrDefault() ?? "User");
        }

        // PUT: api/PortfolioUsers/5/role
        [HttpPut("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] string newRole)
        {
            if (!new[] { "User", "Manager", "Admin" }.Contains(newRole))
            {
                return BadRequest("Invalid role. Must be User, Manager, or Admin.");
            }

            var portfolioUser = await _context.PortfolioUsers.FindAsync(id);
            if (portfolioUser == null || portfolioUser.ApplicationUserId == null)
            {
                return NotFound();
            }

            var applicationUser = await _userManager.FindByIdAsync(portfolioUser.ApplicationUserId);
            if (applicationUser == null)
            {
                return NotFound();
            }

            // Remove all existing roles
            var currentRoles = await _userManager.GetRolesAsync(applicationUser);
            await _userManager.RemoveFromRolesAsync(applicationUser, currentRoles);

            // Add the new role
            await _userManager.AddToRoleAsync(applicationUser, newRole);

            return Ok();
        }

        private bool PortfolioUserExists(int id)
        {
            return _context.PortfolioUsers.AsNoTracking().Any(e => e.Id == id);
        }
    }
}
