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
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtService _jwtService;
        private readonly SkillSnapContext _context;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtService jwtService,
            SkillSnapContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponseDto 
                    { 
                        IsSuccess = false, 
                        Message = "Invalid input data" 
                    });
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return BadRequest(new AuthResponseDto 
                    { 
                        IsSuccess = false, 
                        Message = "User with this email already exists" 
                    });
                }

                // Create new user
                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    EmailConfirmed = true // For simplicity, auto-confirm emails
                };

                var result = await _userManager.CreateAsync(user, request.Password);

                if (!result.Succeeded)
                {
                    return BadRequest(new AuthResponseDto 
                    { 
                        IsSuccess = false, 
                        Message = string.Join(", ", result.Errors.Select(e => e.Description)) 
                    });
                }

                // Public registration only allows User role
                await _userManager.AddToRoleAsync(user, "User");

                // Create portfolio for the user
                var portfolio = new PortfolioUser
                {
                    Name = $"{user.FirstName} {user.LastName}",
                    Bio = "Welcome to SkillSnap! Start building your portfolio by adding your skills and projects.",
                    ProfileImageUrl = "https://via.placeholder.com/150?text=User",
                    ApplicationUserId = user.Id
                };

                _context.PortfolioUsers.Add(portfolio);
                await _context.SaveChangesAsync();

                // Update user with portfolio reference
                user.PortfolioUserId = portfolio.Id;
                await _userManager.UpdateAsync(user);

                // Generate JWT token
                var token = await _jwtService.GenerateTokenAsync(user);

                return Ok(new AuthResponseDto
                {
                    IsSuccess = true,
                    Message = "Registration successful",
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName ?? "",
                        LastName = user.LastName ?? "",
                        PortfolioUserId = user.PortfolioUserId
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new AuthResponseDto 
                { 
                    IsSuccess = false, 
                    Message = "An error occurred during registration" 
                });
            }
        }

        [HttpPost("create-user")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<AuthResponseDto>> CreateUser(RegisterDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponseDto 
                    { 
                        IsSuccess = false, 
                        Message = "Invalid input data" 
                    });
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return BadRequest(new AuthResponseDto 
                    { 
                        IsSuccess = false, 
                        Message = "User with this email already exists" 
                    });
                }

                // Create new user
                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    EmailConfirmed = true // For simplicity, auto-confirm emails
                };

                var result = await _userManager.CreateAsync(user, request.Password);

                if (!result.Succeeded)
                {
                    return BadRequest(new AuthResponseDto 
                    { 
                        IsSuccess = false, 
                        Message = string.Join(", ", result.Errors.Select(e => e.Description)) 
                    });
                }

                // Validate and assign role (Admin can assign any role)
                var requestedRole = request.Role ?? "User";
                var validRoles = new[] { "Admin", "Manager", "User" };
                
                if (!validRoles.Contains(requestedRole))
                {
                    return BadRequest(new AuthResponseDto 
                    { 
                        IsSuccess = false, 
                        Message = "Invalid role specified. Valid roles are: Admin, Manager, User" 
                    });
                }

                // Assign the requested role
                await _userManager.AddToRoleAsync(user, requestedRole);

                // Create portfolio for the user
                var portfolio = new PortfolioUser
                {
                    Name = $"{user.FirstName} {user.LastName}",
                    Bio = "Welcome to SkillSnap! Start building your portfolio by adding your skills and projects.",
                    ProfileImageUrl = "https://via.placeholder.com/150?text=User",
                    ApplicationUserId = user.Id
                };

                _context.PortfolioUsers.Add(portfolio);
                await _context.SaveChangesAsync();

                // Update user with portfolio reference
                user.PortfolioUserId = portfolio.Id;
                await _userManager.UpdateAsync(user);

                return Ok(new AuthResponseDto
                {
                    IsSuccess = true,
                    Message = $"User created successfully with {requestedRole} role",
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName ?? "",
                        LastName = user.LastName ?? "",
                        PortfolioUserId = user.PortfolioUserId
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new AuthResponseDto 
                { 
                    IsSuccess = false, 
                    Message = "An error occurred during user creation" 
                });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponseDto 
                    { 
                        IsSuccess = false, 
                        Message = "Invalid input data" 
                    });
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return BadRequest(new AuthResponseDto 
                    { 
                        IsSuccess = false, 
                        Message = "Invalid email or password" 
                    });
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
                if (!result.Succeeded)
                {
                    return BadRequest(new AuthResponseDto 
                    { 
                        IsSuccess = false, 
                        Message = "Invalid email or password" 
                    });
                }

                // Generate JWT token
                var token = await _jwtService.GenerateTokenAsync(user);

                return Ok(new AuthResponseDto
                {
                    IsSuccess = true,
                    Message = "Login successful",
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email!,
                        FirstName = user.FirstName ?? "",
                        LastName = user.LastName ?? "",
                        PortfolioUserId = user.PortfolioUserId
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new AuthResponseDto 
                { 
                    IsSuccess = false, 
                    Message = "An error occurred during login" 
                });
            }
        }

        [HttpPost("create-portfolio")]
        public async Task<ActionResult<AuthResponseDto>> CreatePortfolioUser([FromBody] PortfolioUser portfolioRequest)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new AuthResponseDto 
                    { 
                        IsSuccess = false, 
                        Message = "User not authenticated" 
                    });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new AuthResponseDto 
                    { 
                        IsSuccess = false, 
                        Message = "User not found" 
                    });
                }

                // Check if user already has a portfolio
                if (user.PortfolioUserId.HasValue)
                {
                    return BadRequest(new AuthResponseDto 
                    { 
                        IsSuccess = false, 
                        Message = "User already has a portfolio" 
                    });
                }

                // Create portfolio user
                var portfolioUser = new PortfolioUser
                {
                    Name = portfolioRequest.Name ?? $"{user.FirstName} {user.LastName}",
                    Bio = portfolioRequest.Bio,
                    ProfileImageUrl = portfolioRequest.ProfileImageUrl,
                    ApplicationUserId = userId
                };

                _context.PortfolioUsers.Add(portfolioUser);
                await _context.SaveChangesAsync();

                // Update ApplicationUser with PortfolioUserId
                user.PortfolioUserId = portfolioUser.Id;
                await _userManager.UpdateAsync(user);

                // Generate new token with updated portfolio information
                var token = await _jwtService.GenerateTokenAsync(user);

                return Ok(new AuthResponseDto
                {
                    IsSuccess = true,
                    Message = "Portfolio created successfully",
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email!,
                        FirstName = user.FirstName ?? "",
                        LastName = user.LastName ?? "",
                        PortfolioUserId = user.PortfolioUserId
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new AuthResponseDto 
                { 
                    IsSuccess = false, 
                    Message = "An error occurred while creating portfolio" 
                });
            }
        }

        [HttpGet("roles")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAvailableRoles()
        {
            return Ok(new { Roles = new[] { "Admin", "Manager", "User" } });
        }
    }
}
