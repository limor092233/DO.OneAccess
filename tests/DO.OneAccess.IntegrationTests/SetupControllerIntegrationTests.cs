using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.DTOs.Auth;
using DO.OneAccess.Application.DTOs.Setup;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Domain.Enums;
using DO.OneAccess.Infrastructure.Persistence;
using Xunit;

namespace DO.OneAccess.IntegrationTests;

[Collection("IntegrationTests")]
public class SetupControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string TestDatabaseName = "DO_OneAccess_Test";
    private const string ConnectionString = $"Server=localhost\\SQLEXPRESS;Database={TestDatabaseName};Trusted_Connection=True;TrustServerCertificate=True;";
    private const string TestBootstrapToken = "test-bootstrap-token-64chars-long-secret-key-1234567890abcdef123456";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SetupControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var builder = new SqlConnectionStringBuilder(ConnectionString);
        if (!string.Equals(builder.InitialCatalog, TestDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Security Guard Violation: Targeted '{builder.InitialCatalog}'.");
        }

        EnsureDatabaseMigrated();

        _factory = factory.WithWebHostBuilder(hostBuilder =>
        {
            hostBuilder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                    ["ASPNETCORE_ENVIRONMENT"] = "Development",
                    ["ONEACCESS_BOOTSTRAP_TOKEN"] = TestBootstrapToken
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

        ResetTestDatabase();
    }

    private static void EnsureDatabaseMigrated()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        using var context = new AppDbContext(options);
        context.Database.Migrate();
    }

    private void ResetTestDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var actualConnection = context.Database.GetConnectionString() ?? string.Empty;
        var actualBuilder = new SqlConnectionStringBuilder(actualConnection);
        if (!string.Equals(actualBuilder.InitialCatalog, TestDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("ISOLATION VIOLATION in SetupControllerIntegrationTests");
        }

        context.Database.Migrate();

        using var connection = new SqlConnection(ConnectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM AuditLogs;
            DELETE FROM LoginHistories;
            DELETE FROM RefreshTokens;
            DELETE FROM UserSystemAccess;
            DELETE FROM AdministratorScopes;
            DELETE FROM AdministratorSystemAccess;
            DELETE FROM SystemSettings;
            DELETE FROM Systems;
            DELETE FROM Users;
            DELETE FROM Sections;
            DELETE FROM Divisions;";
        cmd.ExecuteNonQuery();

        var bootstrapService = scope.ServiceProvider.GetRequiredService<IBootstrapTokenService>();
        bootstrapService.Initialize(isSystemInitialized: false);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private static FirstRunSetupDto CreateSetupDto(string suffix = "") => new()
    {
        EmployeeNumber = $"SA-0001{suffix}",
        FirstName = "FirstRun",
        MiddleName = "System",
        LastName = "Administrator",
        Email = $"sysadmin{suffix}@organization.gov",
        Position = "Lead Systems Administrator",
        Username = $"sysadmin{suffix}",
        Password = "CorrectHorseBatteryStaple123!",
        ConfirmPassword = "CorrectHorseBatteryStaple123!"
    };

    [Fact]
    public async Task GetStatus_CleanDatabase_ReturnsUninitialized()
    {
        var response = await _client.GetAsync("/api/setup/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SetupStatusDto>();
        Assert.NotNull(result);
        Assert.False(result.IsInitialized);
    }

    [Fact]
    public async Task Setup_MissingTokenHeader_Returns401Unauthorized()
    {
        var dto = CreateSetupDto();
        var response = await _client.PostAsJsonAsync("/api/setup", dto);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Setup_WrongTokenHeader_Returns401Unauthorized()
    {
        var dto = CreateSetupDto();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/setup")
        {
            Content = JsonContent.Create(dto)
        };
        request.Headers.Add("X-Bootstrap-Token", "wrong-secret-token");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Setup_ValidToken_SuccessfullyCreatesSysAdminAndAuditLog()
    {
        var dto = CreateSetupDto();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/setup")
        {
            Content = JsonContent.Create(dto)
        };
        request.Headers.Add("X-Bootstrap-Token", TestBootstrapToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<FirstRunSetupResultDto>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(dto.Username, result.Username);

        // Verify Database State
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Username == dto.Username);

        Assert.NotNull(user);
        Assert.Equal((short)RoleType.SystemAdministrator, user.RoleId);
        Assert.True(user.IsActive);
        Assert.Null(user.SectionId); // Preserves universal organizational scope
        Assert.Equal(dto.EmployeeNumber, user.EmployeeNumber);
        Assert.Equal(dto.FirstName, user.FirstName);
        Assert.Equal(dto.LastName, user.LastName);
        Assert.Equal(dto.Email, user.Email);

        // Verify Audit Log
        var audit = await context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "SYSTEM_BOOTSTRAP");

        Assert.NotNull(audit);
        Assert.Null(audit.UserId); // System provisioning event
        Assert.Equal("User", audit.EntityName);
        Assert.Equal(user.UserId.ToString(), audit.EntityId);
        Assert.Contains(dto.Username, audit.NewValues!);
        Assert.DoesNotContain(dto.Password, audit.NewValues!); // Passwords strictly excluded

        // Verify status now reports initialized
        var statusResp = await _client.GetAsync("/api/setup/status");
        var status = await statusResp.Content.ReadFromJsonAsync<SetupStatusDto>();
        Assert.NotNull(status);
        Assert.True(status.IsInitialized);
    }

    [Fact]
    public async Task Setup_SecondAttempt_Returns409Conflict()
    {
        // First successful setup
        var dto1 = CreateSetupDto();
        var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/setup") { Content = JsonContent.Create(dto1) };
        req1.Headers.Add("X-Bootstrap-Token", TestBootstrapToken);
        var res1 = await _client.SendAsync(req1);
        Assert.Equal(HttpStatusCode.Created, res1.StatusCode);

        // Second setup attempt
        var dto2 = CreateSetupDto("2");
        var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/setup") { Content = JsonContent.Create(dto2) };
        req2.Headers.Add("X-Bootstrap-Token", TestBootstrapToken);
        var res2 = await _client.SendAsync(req2);

        Assert.Equal(HttpStatusCode.Conflict, res2.StatusCode);
    }

    [Fact]
    public async Task Setup_NewlyCreatedSysAdmin_CanSuccessfullyAuthenticate()
    {
        var dto = CreateSetupDto();
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/setup") { Content = JsonContent.Create(dto) };
        req.Headers.Add("X-Bootstrap-Token", TestBootstrapToken);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        // Authenticate via standard Login API
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = dto.Username,
            Password = dto.Password
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginResult);
        Assert.False(string.IsNullOrWhiteSpace(loginResult.AccessToken));
        Assert.Equal("Bearer", loginResult.TokenType);

        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(loginResult.AccessToken);
        var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role");
        Assert.NotNull(roleClaim);
        Assert.Equal("SYSTEM_ADMINISTRATOR", roleClaim.Value);
    }

    [Fact]
    public async Task Setup_ParallelConcurrentRequests_OnlyOneSucceeds()
    {
        var dto1 = CreateSetupDto("conc1");
        var dto2 = CreateSetupDto("conc2");

        var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/setup") { Content = JsonContent.Create(dto1) };
        req1.Headers.Add("X-Bootstrap-Token", TestBootstrapToken);

        var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/setup") { Content = JsonContent.Create(dto2) };
        req2.Headers.Add("X-Bootstrap-Token", TestBootstrapToken);

        var task1 = _client.SendAsync(req1);
        var task2 = _client.SendAsync(req2);

        var responses = await Task.WhenAll(task1, task2);
        var statusCodes = responses.Select(r => r.StatusCode).ToList();

        // Exactly one should succeed with 201 Created, the other should receive 409 Conflict
        Assert.Contains(HttpStatusCode.Created, statusCodes);
        Assert.Contains(HttpStatusCode.Conflict, statusCodes);
    }

    [Fact]
    public async Task Setup_InactiveSysAdmin_StillMaintainsInitializedStateAndBlocksSetup()
    {
        // Seed an inactive SysAdmin directly
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = new User
            {
                UserId = Guid.NewGuid(),
                EmployeeNumber = "DEACT-01",
                FirstName = "Deactivated",
                LastName = "Admin",
                Email = "deactivated@org.gov",
                Position = "Admin",
                SectionId = null,
                RoleId = (short)RoleType.SystemAdministrator,
                Username = "deactivated_admin",
                PasswordHash = "fakehash",
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        // Check status
        var statusResp = await _client.GetAsync("/api/setup/status");
        var status = await statusResp.Content.ReadFromJsonAsync<SetupStatusDto>();
        Assert.NotNull(status);
        Assert.True(status.IsInitialized); // Inactive SysAdmin still means initialized

        // Setup attempt must be rejected with 409
        var dto = CreateSetupDto();
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/setup") { Content = JsonContent.Create(dto) };
        req.Headers.Add("X-Bootstrap-Token", TestBootstrapToken);
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Setup_ExceedingRateLimit_Returns429TooManyRequests()
    {
        const string rateLimitTestIp = "198.51.100.99";
        var dto = CreateSetupDto("ratelimit");

        HttpResponseMessage? lastResponse = null;

        // Exhaust the 10-permit limit for this IP
        for (var i = 0; i < 11; i++)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/setup")
            {
                Content = JsonContent.Create(dto)
            };
            req.Headers.Add("X-Forwarded-For", rateLimitTestIp);
            req.Headers.Add("X-Bootstrap-Token", "invalid-token-to-stay-uninitialized");

            lastResponse = await _client.SendAsync(req);
        }

        Assert.NotNull(lastResponse);
        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
    }
}
