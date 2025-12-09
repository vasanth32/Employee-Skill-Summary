# Recreate EmployeeSkillPoc From Scratch (No Git)

Use these steps on a clean machine with .NET 8 and SQL Server installed.

## 1) Create solution and projects
```powershell
mkdir EmployeeSkillPoc
cd EmployeeSkillPoc
dotnet new sln -n EmployeeSkillPoc
dotnet new webapi -n ApiGateway -f net8.0
dotnet new webapi -n AuthService -f net8.0
dotnet new webapi -n EmployeeService -f net8.0
dotnet new webapi -n SkillService -f net8.0
dotnet new webapi -n SearchService -f net8.0
dotnet new classlib -n SharedModels -f net8.0
dotnet sln add ApiGateway/ApiGateway.csproj AuthService/AuthService.csproj EmployeeService/EmployeeService.csproj SkillService/SkillService.csproj SearchService/SearchService.csproj SharedModels/SharedModels.csproj
```

## 2) Add NuGet packages
Common packages for all services:
```powershell
$projects = @('ApiGateway','AuthService','EmployeeService','SkillService','SearchService')
$common = @(
  'Microsoft.EntityFrameworkCore',
  'Microsoft.EntityFrameworkCore.SqlServer',
  'Serilog.AspNetCore',
  'RabbitMQ.Client',
  'FluentValidation',
  'Swashbuckle.AspNetCore',
  'System.IdentityModel.Tokens.Jwt'
)
foreach ($p in $projects) { foreach ($pkg in $common) { dotnet add "$p/$p.csproj" package $pkg } }
```
Gateway package:
```powershell
dotnet add ApiGateway/ApiGateway.csproj package YARP.ReverseProxy
```
Auth-specific packages (matching net8):
```powershell
dotnet add AuthService/AuthService.csproj package Microsoft.AspNetCore.Authentication.JwtBearer -v 8.0.7
dotnet add AuthService/AuthService.csproj package Microsoft.EntityFrameworkCore.Design -v 8.0.7
dotnet add AuthService/AuthService.csproj package Microsoft.EntityFrameworkCore.Tools -v 8.0.7
dotnet add AuthService/AuthService.csproj package FluentValidation.AspNetCore -v 11.3.1
```

## 3) AuthService code
Replace `AuthService/appsettings.json` with:
```json
{
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
  },
  "ConnectionStrings": {
    "AuthDatabase": "Server=YOUR_SQL_SERVER;Database=AuthServiceDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "CHANGE_ME_TO_A_LONG_RANDOM_SECRET",
    "Issuer": "AuthService",
    "Audience": "EmployeeSkillPoc",
    "ExpiresMinutes": 60
  },
  "AllowedHosts": "*"
}
```

`Program.cs` minimal setup:
```csharp
using System.Text;
using AuthService.Data;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuthDatabase")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

Add models under `AuthService/Models/`:
- `User.cs`
```csharp
using System.ComponentModel.DataAnnotations;
namespace AuthService.Models;
public class User
{
    [Key] public Guid UserId { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = default!;
    [Required, MaxLength(255)] public string Email { get; set; } = default!;
    [Required] public string PasswordHash { get; set; } = default!;
    [Required, MaxLength(50)] public string Role { get; set; } = default!;
}
```
- `RegisterRequest.cs`
```csharp
namespace AuthService.Models;
public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}
```
- `LoginRequest.cs`
```csharp
namespace AuthService.Models;
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```
- `AuthResponse.cs`
```csharp
namespace AuthService.Models;
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
```

Add DbContext `AuthService/Data/AuthDbContext.cs`:
```csharp
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;
public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
    }
}
```

Password hashing helper `AuthService/Services/PasswordHasher.cs`:
```csharp
using System.Security.Cryptography;
namespace AuthService.Services;
public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }
    public static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);
        var inputHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, hash.Length);
        return CryptographicOperations.FixedTimeEquals(hash, inputHash);
    }
}
```

Validators under `AuthService/Validators/`:
- `RegisterRequestValidator.cs`
```csharp
using AuthService.Models;
using FluentValidation;
namespace AuthService.Validators;
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.Role).NotEmpty().MaximumLength(50);
    }
}
```
- `LoginRequestValidator.cs`
```csharp
using AuthService.Models;
using FluentValidation;
namespace AuthService.Validators;
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty();
    }
}
```

Controller `AuthService/Controllers/AuthController.cs`:
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Data;
using AuthService.Models;
using AuthService.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthController(AuthDbContext dbContext, IConfiguration configuration,
        IValidator<RegisterRequest> registerValidator, IValidator<LoginRequest> loginValidator)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var validation = await _registerValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        var existing = await _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (existing != null) return Conflict("A user with this email already exists.");

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            Role = request.Role
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(ct);

        var token = GenerateToken(user);
        return Ok(new AuthResponse { Token = token, Role = user.Role });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var validation = await _loginValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (user == null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials.");

        var token = GenerateToken(user);
        return Ok(new AuthResponse { Token = token, Role = user.Role });
    }

    private string GenerateToken(User user)
    {
        var jwt = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresMinutes = int.TryParse(jwt["ExpiresMinutes"], out var minutes) ? minutes : 60;

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

## 4) Apply migration and database update
```powershell
dotnet ef migrations add InitialCreate --project AuthService --startup-project AuthService
dotnet ef database update --project AuthService --startup-project AuthService
```

## 5) Run AuthService
```powershell
dotnet run --project AuthService
```
Default URLs: `https://localhost:5001`, `http://localhost:5000`.

## 6) Quick Postman check
- `POST /auth/register` with JSON body:
```json
{ "name": "Alice Admin", "email": "alice@example.com", "password": "Passw0rd!", "role": "Admin" }
```
- `POST /auth/login` with:
```json
{ "email": "alice@example.com", "password": "Passw0rd!" }
```
Use returned token as `Authorization: Bearer <token>` on protected endpoints.

## 7) Other services
- Repeat connection string setup and migrations per service when models are added.
- All services should use the same JWT issuer/audience/key to validate tokens from AuthService.

