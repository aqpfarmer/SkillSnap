using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Backend.Data;
using SkillSnap.Shared.Models;

namespace SkillSnap.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PortfolioUsersController : ControllerBase
    {
        private readonly SkillSnapContext _context;

        public PortfolioUsersController(SkillSnapContext context)
        {
            _context = context;
        }

        // GET: api/PortfolioUsers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PortfolioUser>>> GetPortfolioUsers()
        {
            return await _context.PortfolioUsers.Include(p => p.Projects).Include(p => p.Skills).ToListAsync();
        }

        // GET: api/PortfolioUsers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PortfolioUser>> GetPortfolioUser(int id)
        {
            var portfolioUser = await _context.PortfolioUsers.Include(p => p.Projects).Include(p => p.Skills).FirstOrDefaultAsync(p => p.Id == id);

            if (portfolioUser == null)
            {
                return NotFound();
            }

            return portfolioUser;
        }

        // PUT: api/PortfolioUsers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPortfolioUser(int id, PortfolioUser portfolioUser)
        {
            if (id != portfolioUser.Id)
            {
                return BadRequest();
            }

            _context.Entry(portfolioUser).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
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
        public async Task<ActionResult<PortfolioUser>> PostPortfolioUser(PortfolioUser portfolioUser)
        {
            _context.PortfolioUsers.Add(portfolioUser);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPortfolioUser", new { id = portfolioUser.Id }, portfolioUser);
        }

        // DELETE: api/PortfolioUsers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePortfolioUser(int id)
        {
            var portfolioUser = await _context.PortfolioUsers.FindAsync(id);
            if (portfolioUser == null)
            {
                return NotFound();
            }

            _context.PortfolioUsers.Remove(portfolioUser);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PortfolioUserExists(int id)
        {
            return _context.PortfolioUsers.Any(e => e.Id == id);
        }
    }
}
