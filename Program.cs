using Microsoft.EntityFrameworkCore;
using UserService.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Đọc connection string từ ENV (Render) hoặc appsettings.json (local)
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
Console.WriteLine($"--- DATABASE_URL from ENV: '{(string.IsNullOrEmpty(connectionString) ? "NOT FOUND" : "FOUND")}' ---");

if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine($"--- Fallback to appsettings: '{(string.IsNullOrEmpty(connectionString) ? "NOT FOUND" : "FOUND")}' ---");
}

if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("❌ Không tìm thấy connection string! Set DATABASE_URL trên Render.");

Console.WriteLine($"--- Connection string starts with: '{connectionString[..Math.Min(30, connectionString.Length)]}...' ---");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Cấu hình JWT Authentication
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

// 3. Tự động migrate database
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        Console.WriteLine("--- ✅ Database Migration Successful! ---");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"--- ❌ Migration Error: {ex.Message} ---");
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Auth API V1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();