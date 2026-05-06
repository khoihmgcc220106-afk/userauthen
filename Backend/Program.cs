using Microsoft.EntityFrameworkCore;
using UserService.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ====== 1. CẤU HÌNH CORS (PHẢI TRƯỚC builder.Build) ======
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()   // Cho phép tất cả các nguồn (Vue, React, v.v.)
              .AllowAnyMethod()   // Cho phép tất cả GET, POST, PUT, DELETE
              .AllowAnyHeader();  // Cho phép tất cả Header (bao gồm cả Authorization)
    });
});

// ====== HÀM CONVERT postgresql:// URI → Npgsql connection string ======
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

// ====== ĐỌC CONNECTION STRING ======
var rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
if (string.IsNullOrEmpty(rawConnectionString))
{
    rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

if (string.IsNullOrEmpty(rawConnectionString))
    throw new InvalidOperationException("❌ Không tìm thấy connection string!");

var connectionString = ConvertToNpgsqlConnectionString(rawConnectionString);

// ====== CẤU HÌNH DATABASE ======
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// ====== CẤU HÌNH JWT ======
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

// ====== 2. SỬ DỤNG CORS (THỨ TỰ RẤT QUAN TRỌNG) ======
// Phải đặt UseCors TRƯỚC UseAuthentication và UseAuthorization
app.UseCors("AllowAll"); 

// ====== TỰ ĐỘNG MIGRATE DATABASE ======
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"--- ❌ Migration Error: {ex.Message} ---");
    }
}

// ====== SWAGGER ======
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Auth API V1");
    c.RoutePrefix = "swagger";
});

// ====== MIDDLEWARE ======
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();