using Microsoft.EntityFrameworkCore;
using UserService.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Database (PostgreSQL)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Cấu hình JWT Authentication (Để làm chức năng Login/Xác thực)
// Chú ý: Key này phải giống hệt key trong Controller lúc tạo Token
var key = Encoding.UTF8.GetBytes("Key_Bi_Mat_Sieu_Cap_Cua_Tui_123456");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Cấu hình Pipeline xử lý HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Thứ tự app.Use... rất quan trọng, không được đảo lộn
//app.UseHttpsRedirection();

app.UseAuthentication(); // Bật xác thực (Kiểm tra thẻ bài)
app.UseAuthorization();  // Bật phân quyền

app.MapControllers();

app.Run();