using Microsoft.EntityFrameworkCore;
using UserService.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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
Console.WriteLine($"--- DATABASE_URL from ENV: '{(string.IsNullOrEmpty(rawConnectionString) ? "NOT FOUND" : "FOUND")}' ---");

if (string.IsNullOrEmpty(rawConnectionString))
{
    rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine($"--- Fallback to appsettings: '{(string.IsNullOrEmpty(rawConnectionString) ? "NOT FOUND" : "FOUND")}' ---");
}

if (string.IsNullOrEmpty(rawConnectionString))
    throw new InvalidOperationException("❌ Không tìm thấy connection string! Set DATABASE_URL trên Render.");

var connectionString = ConvertToNpgsqlConnectionString(rawConnectionString);
Console.WriteLine("--- ✅ Connection string converted OK ---");

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

// ====== TỰ ĐỘNG MIGRATE DATABASE ======
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