using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserService.Api.Data;
using UserService.Api.Models;
using Microsoft.AspNetCore.Authorization;

namespace UserService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly string _jwtKey = "Key_Bi_Mat_Sieu_Cap_Cua_Tui_123456";

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/users/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult> Register(User user)
        {
            // Hash password before saving to database
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registration successful!", userId = user.Id });
        }

        // POST: api/users/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] User loginInfo)
        {
            // 1. Find user by username
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginInfo.Username);

            // 2. Verify hashed password
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginInfo.PasswordHash, user.PasswordHash))
                return Unauthorized(new { message = "Invalid username or password!" });

            // 3. Generate JWT Token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Ok(new { 
                Token = tokenHandler.WriteToken(token), 
                Username = user.Username 
            });
        }

        // GET: api/users/me
        [HttpGet("me")]
        [Authorize] // Requires valid JWT token
        public IActionResult GetMyProfile()
        {
            var userName = User.Identity?.Name;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Ok(new { 
                message = "Welcome to the protected area", 
                user = userName, 
                id = userId 
            });
        }
    }
}