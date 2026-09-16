using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.DTOs.Auth;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Infrastructure.Persistence;
using Xunit;

namespace DO.OneAccess.IntegrationTests;

[Collection("IntegrationTests")]
public class AuthApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string TestDatabaseName = "DO_OneAccess_Test";
    private const string ConnectionString = $"Server=localhost\\SQLEXPRESS;Database={TestDatabaseName};Trusted_Connection=True;TrustServerCertificate=True;";
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public AuthApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Enforce strict safety guard: NEVER target DO_OneAccess, DO_OneAccess_DB, or DO_NSHP_APP_DB
        var builder = new SqlConnectionStringBuilder(ConnectionString);
        if (!string.Equals(builder.InitialCatalog, TestDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Security Guard: Integration tests must strictly target '{TestDatabaseName}', but targeted '{builder.InitialCatalog}'.");
        }

        if (string.Equals(builder.InitialCatalog, "DO_OneAccess", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(builder.InitialCatalog, "DO_OneAccess_DB", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(builder.InitialCatalog, "DO_NSHP_APP_DB", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Security Guard Violation: Attempted to target protected database '{builder.InitialCatalog}'.");
        }

        _factory = factory.WithWebHostBuilder(hostBuilder =>
        {
            // Use ConfigureAppConfiguration to inject the Jwt signing key and other config values
            hostBuilder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Jwt:Issuer", "DO.OneAccess" },
                    { "Jwt:Audience", "DO.OneAccess.API" },
                    { "Jwt:Key", "IntegrationTestingSecureSigningKey32BytesLong!" },
                    { "Security:UseHostCookiePrefix", "false" },
                    { "Security:RequireHttpsCookie", "false" }
                });
            });

            // Use ConfigureTestServices to REPLACE the DbContext registration.
            // ConfigureTestServices runs after ConfigureServices, so it overrides the
            // production AppDbContext registration. This is the ONLY reliable way to
            // guarantee DO_OneAccess_Test is used — config overrides do not reliably
            // propagate into eager DI registrations that read IConfiguration at startup.
            hostBuilder.ConfigureTestServices(services =>
            {
                // Remove the existing AppDbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Also remove the DbContext itself if registered as a concrete type
                var contextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(AppDbContext));
                if (contextDescriptor != null)
                {
                    services.Remove(contextDescriptor);
                }

                // Re-register AppDbContext explicitly targeting DO_OneAccess_Test
                // This is hardwired — not config-driven — so it cannot be overridden by accident
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(ConnectionString));
            });
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false // We inspect and manage cookies explicitly for testing
        });

        InitializeTestDatabase();
    }

    private void InitializeTestDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // CRITICAL SAFETY GUARD: Verify the context's actual connection string targets
        // DO_OneAccess_Test before allowing any destructive database operations.
        var actualConnection = context.Database.GetConnectionString() ?? string.Empty;
        var actualBuilder = new SqlConnectionStringBuilder(actualConnection);
        if (!string.Equals(actualBuilder.InitialCatalog, TestDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"ISOLATION VIOLATION: DbContext is connected to '{actualBuilder.InitialCatalog}' instead of '{TestDatabaseName}'. " +
                $"Refusing to run EnsureDeleted to protect the target database.");
        }

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        // Seed Division, Section, Employee, and User
        var division = new Division
        {
            Code = "TEST_DIV",
            Name = "Test Division",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Divisions.Add(division);

        var section = new Section
        {
            Division = division,
            Code = "TEST_SEC",
            Name = "Test Section",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Sections.Add(section);

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var user = new User
        {
            UserId = _testUserId,
            EmployeeNumber = "EMP_TEST_001",
            FirstName = "Test",
            LastName = "User",
            Email = "testuser@example.com",
            Position = "Tester",
            SectionId = null,
            RoleId = 1, // SYSTEM_ADMINISTRATOR
            Username = "testadmin",
            PasswordHash = passwordHasher.HashPassword(new User(), "SecureTestPassword#2026"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);

        context.SaveChanges();
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithJwtAndSecureCookie()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = "testadmin",
            Password = "SecureTestPassword#2026"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(content);
        Assert.False(string.IsNullOrWhiteSpace(content.AccessToken));
        Assert.Equal(900, content.ExpiresIn);
        Assert.Equal("Bearer", content.TokenType);

        // Verify cookies
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders));
        var cookieList = cookieHeaders.ToList();

        var refreshCookie = cookieList.FirstOrDefault(c => c.StartsWith("refreshToken="));
        Assert.NotNull(refreshCookie);
        Assert.Contains("httponly", refreshCookie, StringComparison.OrdinalIgnoreCase);
        // Non-__Host- refreshToken must use Path=/api/auth to scope it to auth endpoints only
        Assert.Contains("path=/api/auth", refreshCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", refreshCookie, StringComparison.OrdinalIgnoreCase);

        var xsrfCookie = cookieList.FirstOrDefault(c => c.StartsWith("XSRF-TOKEN="));
        Assert.NotNull(xsrfCookie);
        Assert.Contains("path=/", xsrfCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", xsrfCookie, StringComparison.OrdinalIgnoreCase);

        // Verify Database state
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await context.Users.FindAsync(_testUserId);
        Assert.NotNull(user!.LastLoginAt);

        var history = await context.LoginHistories.FirstOrDefaultAsync(h => h.UserId == _testUserId);
        Assert.NotNull(history);
        Assert.True(history.Success);

        var token = await context.RefreshTokens.FirstOrDefaultAsync(t => t.UserId == _testUserId);
        Assert.NotNull(token);
        Assert.Null(token.RevokedAt);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns401ProblemDetailsAndRecordsFailure()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = "testadmin",
            Password = "IncorrectPassword999!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // Verify DB recorded failed login history
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var history = await context.LoginHistories.FirstOrDefaultAsync(h => h.UsernameAttempted == "testadmin" && !h.Success);
        Assert.NotNull(history);
        Assert.Equal("Invalid credentials", history.FailureReason);
    }

    [Fact]
    public async Task Refresh_WithValidCookieAndCsrf_RotatesTokensSuccessfully()
    {
        // 1. Log in to obtain cookies
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = "testadmin",
            Password = "SecureTestPassword#2026"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var (refreshToken, xsrfToken) = ExtractTokensFromCookies(loginResponse);
        Assert.NotNull(refreshToken);
        Assert.NotNull(xsrfToken);

        // 2. Call refresh endpoint
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"refreshToken={refreshToken}; XSRF-TOKEN={xsrfToken}");
        request.Headers.Add("X-XSRF-TOKEN", xsrfToken);

        var refreshResponse = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<RefreshResponseDto>();
        Assert.NotNull(refreshResult);
        Assert.False(string.IsNullOrWhiteSpace(refreshResult.AccessToken));

        // 3. Verify cookie rotation in response
        var (newRefreshToken, _) = ExtractTokensFromCookies(refreshResponse);
        Assert.NotNull(newRefreshToken);
        Assert.NotEqual(refreshToken, newRefreshToken);
    }

    [Fact]
    public async Task Refresh_WithMissingCsrfHeader_Returns400BadRequest()
    {
        // 1. Log in
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = "testadmin",
            Password = "SecureTestPassword#2026"
        });
        var (refreshToken, xsrfToken) = ExtractTokensFromCookies(loginResponse);

        // 2. Attempt refresh with cookie but WITHOUT X-XSRF-TOKEN header
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"refreshToken={refreshToken}; XSRF-TOKEN={xsrfToken}");
        // Notice: Omitting X-XSRF-TOKEN header!

        var refreshResponse = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_WhenReplayed_RevokesAllUserTokensAndLogsSecurityAlert()
    {
        // 1. Log in
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = "testadmin",
            Password = "SecureTestPassword#2026"
        });
        var (token1, xsrf1) = ExtractTokensFromCookies(loginResponse);

        // 2. Legitimate refresh
        var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request1.Headers.Add("Cookie", $"refreshToken={token1}; XSRF-TOKEN={xsrf1}");
        request1.Headers.Add("X-XSRF-TOKEN", xsrf1);
        var refreshResponse = await _client.SendAsync(request1);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        // 3. Replay token1
        var replayRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        replayRequest.Headers.Add("Cookie", $"refreshToken={token1}; XSRF-TOKEN={xsrf1}");
        replayRequest.Headers.Add("X-XSRF-TOKEN", xsrf1);
        var replayResponse = await _client.SendAsync(replayRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, replayResponse.StatusCode);

        // 4. Verify DB: All tokens for test user are now revoked
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var activeTokens = await context.RefreshTokens
            .Where(t => t.UserId == _testUserId && t.RevokedAt == null)
            .ToListAsync();
        Assert.Empty(activeTokens);

        var auditLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.Action == "SecurityAlert_RefreshTokenReplay");
        Assert.NotNull(auditLog);
        Assert.Equal(_testUserId, auditLog.UserId);
    }

    [Fact]
    public async Task Logout_RevokesTokenAndClearsCookies()
    {
        // 1. Log in
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = "testadmin",
            Password = "SecureTestPassword#2026"
        });
        var (refreshToken, xsrfToken) = ExtractTokensFromCookies(loginResponse);

        // 2. Call logout (works without Bearer token!)
        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutRequest.Headers.Add("Cookie", $"refreshToken={refreshToken}; XSRF-TOKEN={xsrfToken}");
        logoutRequest.Headers.Add("X-XSRF-TOKEN", xsrfToken);

        var logoutResponse = await _client.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        // 3. Verify cookies deleted (expires in past)
        Assert.True(logoutResponse.Headers.TryGetValues("Set-Cookie", out var cookieHeaders));
        var cookies = cookieHeaders.ToList();

        var refreshCookie = cookies.FirstOrDefault(c => c.StartsWith("refreshToken="));
        Assert.NotNull(refreshCookie);
        Assert.Contains("expires=", refreshCookie, StringComparison.OrdinalIgnoreCase);

        // 4. Verify database token revoked and audit log written
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var token = await context.RefreshTokens.FirstOrDefaultAsync(t => t.UserId == _testUserId);
        Assert.NotNull(token!.RevokedAt);

        var auditLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.Action == "UserLogout");
        Assert.NotNull(auditLog);
    }

    private static (string? RefreshToken, string? XsrfToken) ExtractTokensFromCookies(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
        {
            return (null, null);
        }

        string? refreshToken = null;
        string? xsrfToken = null;

        foreach (var header in cookieHeaders)
        {
            var matchRefresh = Regex.Match(header, @"(?:__Host-refreshToken|refreshToken)=([^;]+)");
            if (matchRefresh.Success)
            {
                refreshToken = matchRefresh.Groups[1].Value;
            }

            var matchXsrf = Regex.Match(header, @"XSRF-TOKEN=([^;]+)");
            if (matchXsrf.Success)
            {
                xsrfToken = matchXsrf.Groups[1].Value;
            }
        }

        return (refreshToken, xsrfToken);
    }

    public void Dispose()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // SAFETY GUARD: Only delete the test database. If somehow the context
            // is connected to a different database at teardown, abort silently.
            var actualConnection = context.Database.GetConnectionString() ?? string.Empty;
            var actualBuilder = new SqlConnectionStringBuilder(actualConnection);
            if (string.Equals(actualBuilder.InitialCatalog, TestDatabaseName, StringComparison.OrdinalIgnoreCase))
            {
                context.Database.EnsureDeleted();
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }
}
