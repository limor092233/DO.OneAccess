using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.DTOs.Auth;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Infrastructure.Persistence;
using Xunit;

namespace DO.OneAccess.IntegrationTests;

[Collection("IntegrationTests")]
public class AuthHardeningIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string TestDatabaseName = "DO_OneAccess_Test";
    private const string ConnectionString = $"Server=localhost\\SQLEXPRESS;Database={TestDatabaseName};Trusted_Connection=True;TrustServerCertificate=True;";
    private const string JwtSecretKey = "IntegrationTestingSecureSigningKey32BytesLong!";
    private const string Issuer = "DO.OneAccess";
    private const string Audience = "DO.OneAccess.API";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Guid _testUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public AuthHardeningIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var builder = new SqlConnectionStringBuilder(ConnectionString);
        if (!string.Equals(builder.InitialCatalog, TestDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Security Guard Violation: Targeted '{builder.InitialCatalog}'.");
        }

        _factory = factory.WithWebHostBuilder(hostBuilder =>
        {
            hostBuilder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Jwt:Issuer", Issuer },
                    { "Jwt:Audience", Audience },
                    { "Jwt:Key", JwtSecretKey },
                    { "Security:UseHostCookiePrefix", "false" },
                    { "Security:RequireHttpsCookie", "false" }
                });
            });

            hostBuilder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                var contextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(AppDbContext));
                if (contextDescriptor != null) services.Remove(contextDescriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(ConnectionString));
            });
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        InitializeTestDatabase();
    }

    private void InitializeTestDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var actualConnection = context.Database.GetConnectionString() ?? string.Empty;
        var actualBuilder = new SqlConnectionStringBuilder(actualConnection);
        if (!string.Equals(actualBuilder.InitialCatalog, TestDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("ISOLATION VIOLATION in AuthHardeningIntegrationTests");
        }

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        var division = new Division { Code = "DIV_HARDEN", Name = "Harden Div", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Divisions.Add(division);

        var section = new Section { Division = division, Code = "SEC_HARDEN", Name = "Harden Sec", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Sections.Add(section);

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var user = new User
        {
            UserId = _testUserId,
            EmployeeNumber = "EMP_HARDEN_01",
            FirstName = "Harden",
            LastName = "User",
            Email = "harden@test.com",
            Position = "Specialist",
            Section = section,
            RoleId = 3, // USER
            Username = "hardenuser",
            PasswordHash = hasher.HashPassword(new User(), "SecurePass#123"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);

        context.SaveChanges();
    }

    private string GenerateCustomToken(Guid userId, string role, DateTime expires, string signingKey)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(signingKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "hardenuser"),
                new Claim(ClaimTypes.Role, role)
            }),
            NotBefore = expires.AddMinutes(-5),
            IssuedAt = expires.AddMinutes(-5),
            Expires = expires,
            Issuer = Issuer,
            Audience = Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static (string? RefreshToken, string? CsrfToken) ExtractCookies(HttpResponseMessage response)
    {
        string? refreshToken = null;
        string? csrfToken = null;

        if (response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
        {
            foreach (var header in cookieHeaders)
            {
                var match = Regex.Match(header, @"^([^=]+)=([^;]+)");
                if (match.Success)
                {
                    var name = match.Groups[1].Value.Trim();
                    var val = match.Groups[2].Value.Trim();
                    if (name.Contains("refreshToken", StringComparison.OrdinalIgnoreCase)) refreshToken = val;
                    else if (name.Equals("XSRF-TOKEN", StringComparison.OrdinalIgnoreCase)) csrfToken = val;
                }
            }
        }
        return (refreshToken, csrfToken);
    }

    [Fact]
    public async Task AccessToken_WhenExpired_Returns401WithWwwAuthenticateHeader()
    {
        // Category 1: Canonical specification requirement
        var expiredToken = GenerateCustomToken(_testUserId, "USER", DateTime.UtcNow.AddMinutes(-10), JwtSecretKey);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/systems");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(response.Headers.WwwAuthenticate.Any(), "Expected WWW-Authenticate challenge header.");
    }

    [Fact]
    public async Task AccessToken_WhenMalformed_Returns401Unauthorized()
    {
        // Category 1: Canonical specification requirement
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/systems");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.valid.jwt.token");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AccessToken_WhenInvalidSignature_Returns401Unauthorized()
    {
        // Category 1: Canonical specification requirement
        var wrongKeyToken = GenerateCustomToken(_testUserId, "USER", DateTime.UtcNow.AddMinutes(15), "CompletelyDifferentSigningKey32BytesLong!");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/systems");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", wrongKeyToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WhenUserInactive_RejectsWith401AndRevokesToken()
    {
        // Category 1: Canonical specification requirement
        // 1. Login to establish valid refresh session
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = "hardenuser",
            Password = "SecurePass#123"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var (refreshToken, csrfToken) = ExtractCookies(loginResponse);
        Assert.NotNull(refreshToken);
        Assert.NotNull(csrfToken);

        // 2. Deactivate the user in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var u = await db.Users.FindAsync(_testUserId);
            Assert.NotNull(u);
            u.IsActive = false;
            await db.SaveChangesAsync();
        }

        // 3. Attempt refresh
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"refreshToken={refreshToken}; XSRF-TOKEN={csrfToken}");
        refreshRequest.Headers.Add("X-XSRF-TOKEN", csrfToken);

        var refreshResponse = await _client.SendAsync(refreshRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);

        // 4. Verify token was revoked in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var hash = jwtService.HashRefreshToken(refreshToken);
            var tokenRecord = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
            Assert.NotNull(tokenRecord);
            Assert.NotNull(tokenRecord.RevokedAt);
        }
    }

    [Fact]
    public async Task RefreshToken_WhenReplayed_RevokesAllUserTokens()
    {
        // Category 3: Existing implementation behavior
        // 1. Login
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = "hardenuser",
            Password = "SecurePass#123"
        });
        var (token1, csrfToken) = ExtractCookies(loginResponse);
        Assert.NotNull(token1);
        Assert.NotNull(csrfToken);

        // 2. First refresh with token1 -> issues token2
        var refreshReq1 = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshReq1.Headers.Add("Cookie", $"refreshToken={token1}; XSRF-TOKEN={csrfToken}");
        refreshReq1.Headers.Add("X-XSRF-TOKEN", csrfToken);
        var refreshResp1 = await _client.SendAsync(refreshReq1);
        Assert.Equal(HttpStatusCode.OK, refreshResp1.StatusCode);
        var (token2, csrf2) = ExtractCookies(refreshResp1);
        Assert.NotNull(token2);
        Assert.NotEqual(token1, token2);

        // 3. Replay attack: present token1 again!
        var replayReq = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        replayReq.Headers.Add("Cookie", $"refreshToken={token1}; XSRF-TOKEN={csrf2 ?? csrfToken}");
        replayReq.Headers.Add("X-XSRF-TOKEN", csrf2 ?? csrfToken);
        var replayResp = await _client.SendAsync(replayReq);
        Assert.Equal(HttpStatusCode.Unauthorized, replayResp.StatusCode);

        // 4. Verify token2 is also revoked as part of replay defense
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var hash2 = jwtService.HashRefreshToken(token2);
        var tokenRecord2 = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash2);
        Assert.NotNull(tokenRecord2);
        Assert.NotNull(tokenRecord2.RevokedAt);

        // Verify audit log recorded for user
        var auditEntry = await db.AuditLogs.FirstOrDefaultAsync(a => a.UserId == _testUserId);
        Assert.NotNull(auditEntry);
    }

    [Fact]
    public async Task Refresh_WithoutCsrfHeader_Returns400BadRequest()
    {
        // Category 1: Canonical specification requirement
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = "hardenuser",
            Password = "SecurePass#123"
        });
        var (refreshToken, csrfToken) = ExtractCookies(loginResponse);

        var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshReq.Headers.Add("Cookie", $"refreshToken={refreshToken}; XSRF-TOKEN={csrfToken}");
        // Omit X-XSRF-TOKEN header
        var response = await _client.SendAsync(refreshReq);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutCsrfHeader_Returns400BadRequest()
    {
        // Category 1: Canonical specification requirement
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = "hardenuser",
            Password = "SecurePass#123"
        });
        var (refreshToken, csrfToken) = ExtractCookies(loginResponse);

        var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutReq.Headers.Add("Cookie", $"refreshToken={refreshToken}; XSRF-TOKEN={csrfToken}");
        // Omit X-XSRF-TOKEN header
        var response = await _client.SendAsync(logoutReq);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithValidCsrfAndCookie_RevokesTokenAndClearsCookies()
    {
        // Category 1: Canonical specification requirement
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = "hardenuser",
            Password = "SecurePass#123"
        });
        var (refreshToken, csrfToken) = ExtractCookies(loginResponse);
        Assert.NotNull(refreshToken);

        var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutReq.Headers.Add("Cookie", $"refreshToken={refreshToken}; XSRF-TOKEN={csrfToken}");
        logoutReq.Headers.Add("X-XSRF-TOKEN", csrfToken);

        var response = await _client.SendAsync(logoutReq);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify token revoked in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var hash = jwtService.HashRefreshToken(refreshToken);
        var tokenRecord = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        Assert.NotNull(tokenRecord);
        Assert.NotNull(tokenRecord.RevokedAt);
    }

    [Fact]
    public async Task Logout_WhenNoCookieProvided_ReturnsNoContentSafely()
    {
        // Category 3: Existing implementation behavior
        var dummyCsrf = "dummysessioncsrf12345";
        var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutReq.Headers.Add("Cookie", $"XSRF-TOKEN={dummyCsrf}");
        logoutReq.Headers.Add("X-XSRF-TOKEN", dummyCsrf);

        var response = await _client.SendAsync(logoutReq);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
