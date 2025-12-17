using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSection = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuthenticatedUser", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});

// Configure YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(transformBuilderContext =>
    {
        // Custom transform to forward user claims as headers for all routes
        transformBuilderContext.AddRequestTransform(transformContext =>
        {
            var user = transformContext.HttpContext.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                // Extract claims from JWT token
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                    ?? user.FindFirst("sub")?.Value 
                    ?? user.FindFirst(ClaimTypes.Name)?.Value;
                var userEmail = user.FindFirst(ClaimTypes.Email)?.Value 
                    ?? user.FindFirst("email")?.Value;
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value 
                    ?? user.FindFirst("role")?.Value;

                // Forward claims as custom headers
                if (!string.IsNullOrEmpty(userId))
                {
                    transformContext.ProxyRequest.Headers.TryAddWithoutValidation("X-User-Id", userId);
                }
                if (!string.IsNullOrEmpty(userEmail))
                {
                    transformContext.ProxyRequest.Headers.TryAddWithoutValidation("X-User-Email", userEmail);
                }
                if (!string.IsNullOrEmpty(userRole))
                {
                    transformContext.ProxyRequest.Headers.TryAddWithoutValidation("X-User-Role", userRole);
                }

                // Forward all claims as a JSON header (optional, for debugging)
                var allClaims = user.Claims.Select(c => new { c.Type, c.Value });
                var claimsJson = System.Text.Json.JsonSerializer.Serialize(allClaims);
                transformContext.ProxyRequest.Headers.TryAddWithoutValidation("X-User-Claims", claimsJson);
            }
            return default;
        });
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Authentication and Authorization must come before MapReverseProxy
app.UseAuthentication();
app.UseAuthorization();

// Map YARP reverse proxy
app.MapReverseProxy();

app.Run();
