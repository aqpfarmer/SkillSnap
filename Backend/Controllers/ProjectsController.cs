using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Backend.Data;
using SkillSnap.Backend.Services;
using SkillSnap.Shared.Models;
using static SkillSnap.Backend.Services.CacheKeys;

namespace SkillSnap.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Require authentication for all endpoints
    public class ProjectsController : ControllerBase
    {
        private readonly SkillSnapContext _context;
        private readonly ICacheService _cacheService;
        private readonly IMetricsService? _metricsService;

        public ProjectsController(SkillSnapContext context, ICacheService cacheService, IMetricsService? metricsService = null)
        {
            _context = context;
            _cacheService = cacheService;
            _metricsService = metricsService;
        }

        // GET: api/Projects
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
        {
            return await _cacheService.GetOrSetAsync(
                CacheKeys.ALL_PROJECTS,
                async () => {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var result = await _context.Projects
                        .AsNoTracking()
                        .Include(p => p.PortfolioUser)
                        .ToListAsync();
                    stopwatch.Stop();
                    _metricsService?.TrackDatabaseQuery("GetAllProjects", stopwatch.Elapsed, result.Count);
                    return result;
                },
                TimeSpan.FromMinutes(15)
            );
        }

        // GET: api/Projects/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Project>> GetProject(int id)
        {
            var cacheKey = string.Format(CacheKeys.PROJECT_BY_ID, id);
            
            var project = await _cacheService.GetOrSetAsync(
                cacheKey,
                async () => await _context.Projects
                    .AsNoTracking()
                    .Include(p => p.PortfolioUser)
                    .FirstOrDefaultAsync(p => p.Id == id),
                TimeSpan.FromMinutes(15)
            );

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

            var cacheKey = string.Format(CacheKeys.PROJECTS_BY_USER_ID, userId);
            
            var projects = await _cacheService.GetOrSetAsync(
                cacheKey,
                async () => await _context.Projects
                    .AsNoTracking()
                    .Include(p => p.PortfolioUser)
                    .Where(p => p.PortfolioUser != null && p.PortfolioUser.ApplicationUserId == userId)
                    .ToListAsync(),
                TimeSpan.FromMinutes(10) // Slightly shorter cache for user-specific data
            );

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
                
                // Invalidate related cache entries after successful update
                _cacheService.Remove(CacheKeys.ALL_PROJECTS);
                _cacheService.Remove(string.Format(CacheKeys.PROJECT_BY_ID, id));
                
                // Invalidate user-specific caches
                var updatedProject = await _context.Projects
                    .Include(p => p.PortfolioUser)
                    .FirstOrDefaultAsync(p => p.Id == id);
                    
                if (updatedProject?.PortfolioUser?.ApplicationUserId != null)
                {
                    _cacheService.Remove(string.Format(CacheKeys.PROJECTS_BY_USER_ID, updatedProject.PortfolioUser.ApplicationUserId));
                }
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

            // Invalidate related cache entries after successful creation
            _cacheService.Remove(CacheKeys.ALL_PROJECTS);
            
            // Invalidate user-specific cache if we know the portfolio user
            var createdProject = await _context.Projects
                .Include(p => p.PortfolioUser)
                .FirstOrDefaultAsync(p => p.Id == project.Id);
                
            if (createdProject?.PortfolioUser?.ApplicationUserId != null)
            {
                _cacheService.Remove(string.Format(CacheKeys.PROJECTS_BY_USER_ID, createdProject.PortfolioUser.ApplicationUserId));
            }

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

            // Invalidate related cache entries after successful deletion
            _cacheService.Remove(CacheKeys.ALL_PROJECTS);
            _cacheService.Remove(string.Format(CacheKeys.PROJECT_BY_ID, id));
            
            // Invalidate user-specific cache
            if (project.PortfolioUser?.ApplicationUserId != null)
            {
                _cacheService.Remove(string.Format(CacheKeys.PROJECTS_BY_USER_ID, project.PortfolioUser.ApplicationUserId));
            }

            return NoContent();
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.AsNoTracking().Any(e => e.Id == id);
        }
    }
}
