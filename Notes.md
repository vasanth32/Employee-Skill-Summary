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

# Phase 3 — EmployeeService (from scratch, manual)
Goal: Employee DB + CRUD + publish `EmployeeCreatedEvent` to RabbitMQ.

## 1) Packages and reference
```powershell
dotnet add EmployeeService/EmployeeService.csproj package Microsoft.EntityFrameworkCore -v 8.0.7
dotnet add EmployeeService/EmployeeService.csproj package Microsoft.EntityFrameworkCore.SqlServer -v 8.0.7
dotnet add EmployeeService/EmployeeService.csproj package Microsoft.EntityFrameworkCore.Design -v 8.0.7
dotnet add EmployeeService/EmployeeService.csproj package Microsoft.EntityFrameworkCore.Tools -v 8.0.7
dotnet add EmployeeService/EmployeeService.csproj package FluentValidation.AspNetCore -v 11.3.1
dotnet add EmployeeService/EmployeeService.csproj package RabbitMQ.Client -v 6.8.1
dotnet add EmployeeService/EmployeeService.csproj reference SharedModels/SharedModels.csproj
```

## 2) appsettings.json
```json
"ConnectionStrings": {
  "EmployeeDatabase": "Server=YOUR_SQL_SERVER;Database=EmployeeServiceDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;"
},
"RabbitMQ": {
  "HostName": "localhost",
  "UserName": "guest",
  "Password": "guest",
  "Exchange": "employee.events",
  "ExchangeType": "fanout"
},
"AllowedHosts": "*"
```

## 3) Program.cs
```csharp
using EmployeeService.Data;
using EmployeeService.Messaging;
using EmployeeService.Repositories;
using EmployeeService.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<EmployeeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("EmployeeDatabase")));
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeAppService>();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

## 4) Models (`EmployeeService/Models`)
`Employee.cs`
```csharp
using System.ComponentModel.DataAnnotations;
namespace EmployeeService.Models;
public class Employee
{
    [Key] public Guid EmployeeId { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = default!;
    [Required, MaxLength(255)] public string Email { get; set; } = default!;
    [Required, MaxLength(50)] public string Role { get; set; } = default!;
}
```
`EmployeeCreateRequest.cs`
```csharp
namespace EmployeeService.Models;
public class EmployeeCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
```
`EmployeeUpdateRequest.cs`
```csharp
namespace EmployeeService.Models;
public class EmployeeUpdateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
```
`EmployeeResponse.cs`
```csharp
namespace EmployeeService.Models;
public class EmployeeResponse
{
    public Guid EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
```

## 5) DbContext (`EmployeeService/Data/EmployeeDbContext.cs`)
```csharp
using EmployeeService.Models;
using Microsoft.EntityFrameworkCore;
namespace EmployeeService.Data;
public class EmployeeDbContext : DbContext
{
    public EmployeeDbContext(DbContextOptions<EmployeeDbContext> options) : base(options) { }
    public DbSet<Employee> Employees => Set<Employee>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Employee>().HasIndex(e => e.Email).IsUnique();
    }
}
```

## 6) Validation (`EmployeeService/Validators`)
`EmployeeCreateRequestValidator.cs`
```csharp
using EmployeeService.Models;
using FluentValidation;
namespace EmployeeService.Validators;
public class EmployeeCreateRequestValidator : AbstractValidator<EmployeeCreateRequest>
{
    public EmployeeCreateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Role).NotEmpty().MaximumLength(50);
    }
}
```
`EmployeeUpdateRequestValidator.cs`
```csharp
using EmployeeService.Models;
using FluentValidation;
namespace EmployeeService.Validators;
public class EmployeeUpdateRequestValidator : AbstractValidator<EmployeeUpdateRequest>
{
    public EmployeeUpdateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Role).NotEmpty().MaximumLength(50);
    }
}
```

## 7) Repository (`EmployeeService/Repositories`)
`IEmployeeRepository.cs`
```csharp
using EmployeeService.Models;
namespace EmployeeService.Repositories;
public interface IEmployeeRepository
{
    Task<List<Employee>> GetAllAsync(CancellationToken cancellationToken);
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Employee employee, CancellationToken cancellationToken);
    Task UpdateAsync(Employee employee, CancellationToken cancellationToken);
    Task DeleteAsync(Employee employee, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
```
`EmployeeRepository.cs`
```csharp
using EmployeeService.Data;
using EmployeeService.Models;
using Microsoft.EntityFrameworkCore;
namespace EmployeeService.Repositories;
public class EmployeeRepository : IEmployeeRepository
{
    private readonly EmployeeDbContext _dbContext;
    public EmployeeRepository(EmployeeDbContext dbContext) { _dbContext = dbContext; }

    public Task<List<Employee>> GetAllAsync(CancellationToken ct) =>
        _dbContext.Employees.AsNoTracking().ToListAsync(ct);
    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _dbContext.Employees.FirstOrDefaultAsync(e => e.EmployeeId == id, ct);
    public async Task AddAsync(Employee employee, CancellationToken ct) =>
        await _dbContext.Employees.AddAsync(employee, ct);
    public Task UpdateAsync(Employee employee, CancellationToken ct)
    { _dbContext.Employees.Update(employee); return Task.CompletedTask; }
    public Task DeleteAsync(Employee employee, CancellationToken ct)
    { _dbContext.Employees.Remove(employee); return Task.CompletedTask; }
    public Task<bool> EmailExistsAsync(string email, Guid? excludeId, CancellationToken ct) =>
        _dbContext.Employees.AnyAsync(e => e.Email == email && (!excludeId.HasValue || e.EmployeeId != excludeId.Value), ct);
    public Task SaveChangesAsync(CancellationToken ct) => _dbContext.SaveChangesAsync(ct);
}
```

## 8) Service (`EmployeeService/Services`)
`IEmployeeService.cs`
```csharp
using EmployeeService.Models;
namespace EmployeeService.Services;
public interface IEmployeeService
{
    Task<List<EmployeeResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<EmployeeResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<EmployeeResponse> CreateAsync(EmployeeCreateRequest request, CancellationToken cancellationToken);
    Task<EmployeeResponse?> UpdateAsync(Guid id, EmployeeUpdateRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
```
`EmployeeAppService.cs`
```csharp
using EmployeeService.Models;
using EmployeeService.Messaging;
using EmployeeService.Repositories;
using SharedModels.Events;
namespace EmployeeService.Services;
public class EmployeeAppService : IEmployeeService
{
    private readonly IEmployeeRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    public EmployeeAppService(IEmployeeRepository repository, IEventPublisher eventPublisher)
    { _repository = repository; _eventPublisher = eventPublisher; }

    public async Task<List<EmployeeResponse>> GetAllAsync(CancellationToken ct) =>
        (await _repository.GetAllAsync(ct)).Select(Map).ToList();

    public async Task<EmployeeResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var employee = await _repository.GetByIdAsync(id, ct);
        return employee == null ? null : Map(employee);
    }

    public async Task<EmployeeResponse> CreateAsync(EmployeeCreateRequest request, CancellationToken ct)
    {
        if (await _repository.EmailExistsAsync(request.Email, null, ct))
            throw new InvalidOperationException("Email already exists.");

        var employee = new Employee
        {
            EmployeeId = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Role = request.Role
        };
        await _repository.AddAsync(employee, ct);
        await _repository.SaveChangesAsync(ct);

        await _eventPublisher.PublishEmployeeCreatedAsync(new EmployeeCreatedEvent
        {
            EmployeeId = employee.EmployeeId,
            Name = employee.Name,
            Email = employee.Email,
            Role = employee.Role
        }, ct);
        return Map(employee);
    }

    public async Task<EmployeeResponse?> UpdateAsync(Guid id, EmployeeUpdateRequest request, CancellationToken ct)
    {
        var employee = await _repository.GetByIdAsync(id, ct);
        if (employee == null) return null;
        if (await _repository.EmailExistsAsync(request.Email, id, ct))
            throw new InvalidOperationException("Email already exists.");

        employee.Name = request.Name;
        employee.Email = request.Email;
        employee.Role = request.Role;
        await _repository.UpdateAsync(employee, ct);
        await _repository.SaveChangesAsync(ct);
        return Map(employee);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var employee = await _repository.GetByIdAsync(id, ct);
        if (employee == null) return false;
        await _repository.DeleteAsync(employee, ct);
        await _repository.SaveChangesAsync(ct);
        return true;
    }

    private static EmployeeResponse Map(Employee e) => new()
    {
        EmployeeId = e.EmployeeId,
        Name = e.Name,
        Email = e.Email,
        Role = e.Role
    };
}
```

## 9) Messaging (`EmployeeService/Messaging`)
Shared event (`SharedModels/Events/EmployeeCreatedEvent.cs`)
```csharp
namespace SharedModels.Events;
public class EmployeeCreatedEvent
{
    public Guid EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
```
`IEventPublisher.cs`
```csharp
using SharedModels.Events;
namespace EmployeeService.Messaging;
public interface IEventPublisher
{
    Task PublishEmployeeCreatedAsync(EmployeeCreatedEvent message, CancellationToken cancellationToken);
}
```
`RabbitMqEventPublisher.cs`
```csharp
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using SharedModels.Events;
namespace EmployeeService.Messaging;
public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _exchange;

    public RabbitMqEventPublisher(IConfiguration configuration)
    {
        var rabbit = configuration.GetSection("RabbitMQ");
        _exchange = rabbit["Exchange"] ?? "employee.events";
        var exchangeType = rabbit["ExchangeType"] ?? "fanout";
        var factory = new ConnectionFactory
        {
            HostName = rabbit["HostName"] ?? "localhost",
            UserName = rabbit["UserName"] ?? "guest",
            Password = rabbit["Password"] ?? "guest"
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(exchange: _exchange, type: exchangeType, durable: true, autoDelete: false);
    }

    public Task PublishEmployeeCreatedAsync(EmployeeCreatedEvent message, CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        _channel.BasicPublish(exchange: _exchange, routingKey: string.Empty, basicProperties: null, body: body);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
```

## 10) Controller (`EmployeeService/Controllers/EmployeesController.cs`)
```csharp
using EmployeeService.Models;
using EmployeeService.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
namespace EmployeeService.Controllers;

[ApiController]
[Route("employees")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IValidator<EmployeeCreateRequest> _createValidator;
    private readonly IValidator<EmployeeUpdateRequest> _updateValidator;

    public EmployeesController(IEmployeeService employeeService,
        IValidator<EmployeeCreateRequest> createValidator,
        IValidator<EmployeeUpdateRequest> updateValidator)
    {
        _employeeService = employeeService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<ActionResult<List<EmployeeResponse>>> GetAll(CancellationToken ct) =>
        Ok(await _employeeService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeResponse>> GetById(Guid id, CancellationToken ct)
    {
        var employee = await _employeeService.GetByIdAsync(id, ct);
        if (employee == null) return NotFound();
        return Ok(employee);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeResponse>> Create([FromBody] EmployeeCreateRequest request, CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);
        try
        {
            var created = await _employeeService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.EmployeeId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EmployeeResponse>> Update(Guid id, [FromBody] EmployeeUpdateRequest request, CancellationToken ct)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);
        try
        {
            var updated = await _employeeService.UpdateAsync(id, request, ct);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _employeeService.DeleteAsync(id, ct);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
```

## 11) Migration + DB update
```powershell
dotnet ef migrations add InitialCreate --project EmployeeService --startup-project EmployeeService
dotnet ef database update --project EmployeeService --startup-project EmployeeService
```

## 12) Quick manual tests
- POST `/employees`:
```json
{ "name": "Jane Doe", "email": "jane@example.com", "role": "Manager" }
```
- PUT `/employees/{id}`:
```json
{ "name": "Jane D.", "email": "jane@example.com", "role": "Lead" }
```
- DELETE `/employees/{id}`: expect 204.
- Successful create should also publish `EmployeeCreatedEvent` to the configured RabbitMQ exchange.

Notes:
- Each service has its own DB (AuthServiceDb, EmployeeServiceDb, etc.).
- All services should share the same JWT signing key/issuer/audience (AuthService issues tokens).

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

