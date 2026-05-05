using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserService.Api.Data;
using UserService.Api.Models;

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

        // 1. ĐĂNG KÝ (Tạo User mới)
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đăng ký thành công!", userId = user.Id });
        }

        // 2. ĐĂNG NHẬP (Cấp Token)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User loginInfo)
        {
            // Tìm user khớp cả username và password
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginInfo.Username && u.PasswordHash == loginInfo.PasswordHash);

            if (user == null)
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu!" });

            // Tạo mã JWT Token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(2), // Token hết hạn sau 2 tiếng
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new
            {
                Token = tokenString,
                Username = user.Username,
                UserId = user.Id
            });
        }

        // 3. LẤY DANH SÁCH USER (Để test)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }
    }
}