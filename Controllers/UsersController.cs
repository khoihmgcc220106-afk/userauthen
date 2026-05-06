using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserService.Api.Data;
using UserService.Api.Models;
using Microsoft.AspNetCore.Authorization; // Thêm cái này để dùng [Authorize]

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

        // --- PHÂN VÙNG 1: AI CŨNG VÀO ĐƯỢC (PUBLIC) ---

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult> Register(User user)
        {
            // MÃ HÓA MẬT KHẨU TRƯỚC KHI LƯU
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đăng ký thành công!", userId = user.Id });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] User loginInfo)
        {
            // 1. Chỉ tìm theo Username
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginInfo.Username);

            // 2. Kiểm tra mật khẩu đã mã hóa
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginInfo.PasswordHash, user.PasswordHash))
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu!" });

            // 3. Tạo Token (Thêm Claim Role để phân vùng)
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Username == "admin" ? "Admin" : "User") // Tạm thời set admin nếu tên là admin
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

        // --- PHÂN VÙNG 2: CHỈ DÀNH CHO NGƯỜI ĐÃ ĐĂNG NHẬP ---

        [HttpGet("me")]
        [Authorize] // Bắt buộc phải có Token mới lấy được thông tin cá nhân
        public IActionResult GetMyProfile()
        {
            var userName = User.Identity?.Name;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Ok(new { message = "Đây là khu vực bí mật", user = userName, id = userId });
        }

        // --- PHÂN VÙNG 3: CHỈ DÀNH CHO ADMIN (Leader sẽ thích cái này) ---

        [HttpGet("all-users")]
        [Authorize(Roles = "Admin")] // Chỉ tài khoản admin mới xem được hết danh sách
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }
    }
}