using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Backend.Data;
using SkillSnap.Shared.Models;

namespace SkillSnap.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Require authentication for all endpoints
    public class SkillsController : ControllerBase
    {
        private readonly SkillSnapContext _context;

        public SkillsController(SkillSnapContext context)
        {
            _context = context;
        }

        // GET: api/Skills
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Skill>>> GetSkills()
        {
            return await _context.Skills.Include(s => s.PortfolioUser).ToListAsync();
        }

        // GET: api/Skills/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Skill>> GetSkill(int id)
        {
            var skill = await _context.Skills.FindAsync(id);

            if (skill == null)
            {
                return NotFound();
            }

            return skill;
        }

        // PUT: api/Skills/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSkill(int id, Skill skill)
        {
            if (id != skill.Id)
            {
                return BadRequest();
            }

            // Users can only edit skills from their own portfolio
            if (User.IsInRole("User"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token.");
                }

                var applicationUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (applicationUser == null)
                {
                    return Unauthorized("User not found.");
                }

                // Auto-create portfolio if it doesn't exist
                if (applicationUser.PortfolioUserId == null)
                {
                    var portfolioUser = new PortfolioUser
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
                }

                // Get the existing skill to check ownership
                var existingSkill = await _context.Skills.FindAsync(id);
                if (existingSkill == null)
                {
                    return NotFound();
                }

                // Ensure the user can only edit skills from their own portfolio
                if (existingSkill.PortfolioUserId != applicationUser.PortfolioUserId)
                {
                    return Forbid("Users can only edit skills from their own portfolio.");
                }

                // Ensure the user is not trying to change the portfolio user ID
                if (skill.PortfolioUserId != applicationUser.PortfolioUserId)
                {
                    return Forbid("Users cannot change the portfolio user ID of their skills.");
                }
            }
            // Admin/Manager can edit any skills (no additional validation needed)

            _context.Entry(skill).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SkillExists(id))
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

        // POST: api/Skills
        [HttpPost]
        public async Task<ActionResult<Skill>> PostSkill(Skill skill)
        {
            // Users can only create skills for their own portfolio
            if (User.IsInRole("User"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token.");
                }

                var applicationUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (applicationUser == null)
                {
                    return Unauthorized("User not found.");
                }

                // Auto-create portfolio if it doesn't exist (same as PortfolioUsersController)
                if (applicationUser.PortfolioUserId == null)
                {
                    var portfolioUser = new PortfolioUser
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
                }

                // Ensure the user is only creating skills for their own portfolio
                if (skill.PortfolioUserId != applicationUser.PortfolioUserId)
                {
                    return Forbid("Users can only create skills for their own portfolio.");
                }
            }
            // Admin/Manager can create skills for any user (no additional validation needed)

            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSkill", new { id = skill.Id }, skill);
        }

        // DELETE: api/Skills/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSkill(int id)
        {
            var skill = await _context.Skills.FindAsync(id);
            if (skill == null)
            {
                return NotFound();
            }

            // Users can only delete skills from their own portfolio
            if (User.IsInRole("User"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token.");
                }

                var applicationUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (applicationUser == null)
                {
                    return Unauthorized("User not found.");
                }

                // Auto-create portfolio if it doesn't exist (though unlikely for DELETE)
                if (applicationUser.PortfolioUserId == null)
                {
                    var portfolioUser = new PortfolioUser
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
                }

                // Ensure the user can only delete skills from their own portfolio
                if (skill.PortfolioUserId != applicationUser.PortfolioUserId)
                {
                    return Forbid("Users can only delete skills from their own portfolio.");
                }
            }
            // Admin/Manager can delete any skills (no additional validation needed)

            _context.Skills.Remove(skill);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Skills/names/distinct
        [HttpGet("names/distinct")]
        public async Task<ActionResult<IEnumerable<string>>> GetDistinctSkillNames()
        {
            var distinctNames = await _context.Skills
                .Where(s => !string.IsNullOrEmpty(s.Name))
                .Select(s => s.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync();

            return Ok(distinctNames);
        }

        private bool SkillExists(int id)
        {
            return _context.Skills.Any(e => e.Id == id);
        }
    }
}
