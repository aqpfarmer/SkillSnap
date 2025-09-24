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
    public class ProjectsController : ControllerBase
    {
        private readonly SkillSnapContext _context;

        public ProjectsController(SkillSnapContext context)
        {
            _context = context;
        }

        // GET: api/Projects
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
        {
            return await _context.Projects.Include(p => p.PortfolioUser).ToListAsync();
        }

        // GET: api/Projects/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Project>> GetProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);

            if (project == null)
            {
                return NotFound();
            }

            return project;
        }

        // GET: api/Projects/my
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<Project>>> GetMyProjects()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var projects = await _context.Projects
                .Include(p => p.PortfolioUser)
                .Where(p => p.PortfolioUser != null && p.PortfolioUser.ApplicationUserId == userId)
                .ToListAsync();

            return Ok(projects);
        }

        // PUT: api/Projects/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProject(int id, Project project)
        {
            if (id != project.Id)
            {
                return BadRequest();
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isManagerOrAdmin = User.IsInRole("Admin") || User.IsInRole("Manager");
            
            // Allow managers/admins to edit any project, or users to edit projects in their own portfolio
            if (!isManagerOrAdmin)
            {
                var existingProject = await _context.Projects
                    .Include(p => p.PortfolioUser)
                    .FirstOrDefaultAsync(p => p.Id == id);
                
                if (existingProject?.PortfolioUser?.ApplicationUserId != userId)
                {
                    return Forbid("You can only edit projects in your own portfolio");
                }
            }

            _context.Entry(project).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProjectExists(id))
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

        // POST: api/Projects
        [HttpPost]
        public async Task<ActionResult<Project>> PostProject(Project project)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isManagerOrAdmin = User.IsInRole("Admin") || User.IsInRole("Manager");
            
            // Allow managers/admins to create projects for any portfolio, or users to create projects for their own portfolio
            if (!isManagerOrAdmin)
            {
                var targetPortfolioUser = await _context.PortfolioUsers.FindAsync(project.PortfolioUserId);
                if (targetPortfolioUser?.ApplicationUserId != userId)
                {
                    return Forbid("You can only create projects for your own portfolio");
                }
            }

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProject", new { id = project.Id }, project);
        }

        // DELETE: api/Projects/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isManagerOrAdmin = User.IsInRole("Admin") || User.IsInRole("Manager");
            
            var project = await _context.Projects
                .Include(p => p.PortfolioUser)
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (project == null)
            {
                return NotFound();
            }

            // Allow managers/admins to delete any project, or users to delete projects from their own portfolio
            if (!isManagerOrAdmin && project.PortfolioUser?.ApplicationUserId != userId)
            {
                return Forbid("You can only delete projects from your own portfolio");
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id);
        }
    }
}
