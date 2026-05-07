using Microsoft.EntityFrameworkCore;
using UserService.Api.Data; // Đảm bảo namespace này khớp với project của bạn
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. CẤU HÌNH CORS (Cho phép Frontend từ Vercel truy cập)
// ============================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ============================================================
// 2. HÀM CHUYỂN ĐỔI DATABASE URL (Dành cho Render PostgreSQL)
// ============================================================
static string ConvertToNpgsqlConnectionString(string url)
{
    if (!url.StartsWith("postgresql://") && !url.StartsWith("postgres://"))
        return url;

    var uri = new Uri(url);
    var userInfo = uri.UserInfo.Split(':');
    var username = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";
    var host = uri.Host;
    var port = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');

    return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
}

// Đọc Connection String từ Environment (Render) hoặc AppSettings (Local)
var rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
                         ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(rawConnectionString))
    throw new InvalidOperationException("❌ Connection string 'DATABASE_URL' not found!");

var connectionString = ConvertToNpgsqlConnectionString(rawConnectionString);

// Cấu hình DbContext với PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// ============================================================
// 3. CẤU HÌNH AUTHENTICATION & JWT
// ============================================================
var key = Encoding.UTF8.GetBytes("Key_Bi_Mat_Sieu_Cap_Cua_Tui_123456"); // Phải khớp với key lúc tạo Token
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

// ============================================================
// 4. CẤU HÌNH SWAGGER (CÓ NÚT AUTHORIZE ĐỂ NHẬP TOKEN)
// ============================================================
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { 
        Title = "Advanced Microservices API", 
        Version = "v1",
        Description = "Hệ thống Auth & URL Shortener tích hợp"
    });

    // Định nghĩa loại bảo mật JWT cho Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Dán Token của bạn vào đây (Chỉ cần mã JWT, không cần chữ 'Bearer' vì hệ thống tự thêm)"
    });

    // Bắt Swagger áp dụng Token cho các request
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// ============================================================
// 5. CẤU HÌNH MIDDLEWARE (THỨ TỰ LÀ QUAN TRỌNG NHẤT)
// ============================================================

// 1. Dùng Swagger trước
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Microservices API V1");
    c.RoutePrefix = "swagger"; // Truy cập link: /swagger
});

// 2. Dùng CORS (Phải trước Auth)
app.UseCors("AllowAll");

// 3. Tự động Migrate Database khi khởi chạy
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        Console.WriteLine("✅ Database Migration Successful!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"--- ❌ Migration Error: {ex.Message} ---");
    }
}

// 4. Các Middleware xử lý request
app.UseAuthentication(); // Xác thực xem bạn là ai
app.UseAuthorization();  // Kiểm tra bạn có quyền làm gì

app.MapControllers();

app.Run();