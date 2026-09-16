using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using DO.OneAccess.Infrastructure.Extensions;
using DO.OneAccess.Server.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Register persistence foundation for EF Core tooling and DI
builder.Services.AddInfrastructurePersistence(builder.Configuration);

// Register authentication services and password hashing
builder.Services.AddInfrastructureAuthentication(builder.Configuration);

// Register application services
builder.Services.AddApplicationServices();

// Configure JWT Bearer authentication
var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Configuration 'Jwt:Key' is required and cannot be null, empty, or whitespace.");
}

var jwtKeyBytes = Encoding.UTF8.GetBytes(jwtKey);
if (jwtKeyBytes.Length < 32)
{
    throw new InvalidOperationException(
        "Configuration 'Jwt:Key' must be at least 256 bits (32 bytes).");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "DO.OneAccess",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "DO.OneAccess.API",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SystemAdministratorOnly", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole("SYSTEM_ADMINISTRATOR") ||
            ctx.User.HasClaim(c => (c.Type == ClaimTypes.Role || c.Type == "role") && c.Value == "SYSTEM_ADMINISTRATOR")));

    options.AddPolicy("AdministratorOrAbove", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole("SYSTEM_ADMINISTRATOR") ||
            ctx.User.IsInRole("ADMINISTRATOR") ||
            ctx.User.HasClaim(c => (c.Type == ClaimTypes.Role || c.Type == "role") && (c.Value == "SYSTEM_ADMINISTRATOR" || c.Value == "ADMINISTRATOR"))));

    options.AddPolicy("AnyAuthenticatedUser", policy =>
        policy.RequireAuthenticatedUser());
});

// Configure CORS for Blazor WebAssembly Client
var rawCorsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? throw new InvalidOperationException(
        "Configuration 'Cors:AllowedOrigins' is required.");

var corsOrigins = rawCorsOrigins
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim())
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (corsOrigins.Length == 0)
{
    throw new InvalidOperationException(
        "Configuration 'Cors:AllowedOrigins' must contain at least one valid, non-empty origin.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClientPolicy", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure Rate Limiting for Setup Endpoint
var setupRateLimitPermit = builder.Configuration
    .GetValue<int?>("Security:SetupRateLimitPermitLimit")
    ?? throw new InvalidOperationException(
        "Configuration 'Security:SetupRateLimitPermitLimit' is required.");

if (setupRateLimitPermit <= 0)
{
    throw new InvalidOperationException(
        "Configuration 'Security:SetupRateLimitPermitLimit' must be greater than zero.");
}

var setupRateLimitWindowSeconds = builder.Configuration
    .GetValue<int?>("Security:SetupRateLimitWindowSeconds")
    ?? throw new InvalidOperationException(
        "Configuration 'Security:SetupRateLimitWindowSeconds' is required.");

if (setupRateLimitWindowSeconds <= 0)
{
    throw new InvalidOperationException(
        "Configuration 'Security:SetupRateLimitWindowSeconds' must be greater than zero.");
}

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("SetupEndpointPolicy", httpContext =>
    {
        var clientIp = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            clientIp,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = setupRateLimitPermit,
                Window = TimeSpan.FromSeconds(setupRateLimitWindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("BlazorClientPolicy");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Initialize Bootstrap Token Service with system initialization state
using (var scope = app.Services.CreateScope())
{
    var bootstrapTokenService = scope.ServiceProvider.GetRequiredService<DO.OneAccess.Application.Common.Interfaces.IBootstrapTokenService>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<DO.OneAccess.Application.Common.Interfaces.IApplicationDbContext>();
        var isInitialized = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
            context.Users, u => u.RoleId == 1);
        bootstrapTokenService.Initialize(isInitialized);
    }
    catch
    {
        bootstrapTokenService.Initialize(isSystemInitialized: false);
    }
}

app.Run();

public partial class Program { }

