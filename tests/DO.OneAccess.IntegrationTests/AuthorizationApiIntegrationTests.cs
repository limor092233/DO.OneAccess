using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.DTOs.Access;
using DO.OneAccess.Application.DTOs.Sections;
using DO.OneAccess.Application.DTOs.Users;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Infrastructure.Persistence;
using Xunit;

namespace DO.OneAccess.IntegrationTests;

[Collection("IntegrationTests")]
public class AuthorizationApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string TestDatabaseName = "DO_OneAccess_Test";
    private const string ConnectionString = $"Server=localhost\\SQLEXPRESS;Database={TestDatabaseName};Trusted_Connection=True;TrustServerCertificate=True;";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private readonly Guid _sysAdminId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private readonly Guid _admin1Id = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private readonly Guid _admin2Id = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private readonly Guid _user1Id = Guid.Parse("10000000-0000-0000-0000-000000000004");
    private readonly Guid _user2Id = Guid.Parse("10000000-0000-0000-0000-000000000005");

    private readonly Guid _system1Id = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private readonly Guid _system2Id = Guid.Parse("20000000-0000-0000-0000-000000000002");

    private int _div1Id;
    private int _div2Id;

    private string _sysAdminToken = string.Empty;
    private string _admin1Token = string.Empty;
    private string _user1Token = string.Empty;

    public AuthorizationApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Safety guard: ensure tests target ONLY DO_OneAccess_Test
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

            hostBuilder.ConfigureTestServices(services =>
            {
                services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
                    Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
                    options =>
                    {
                        var key = "IntegrationTestingSecureSigningKey32BytesLong!";
                        options.TokenValidationParameters.IssuerSigningKey =
                            new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key));
                    });

                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                var contextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(AppDbContext));
                if (contextDescriptor != null)
                {
                    services.Remove(contextDescriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(ConnectionString));
            });
        });

        _client = _factory.CreateClient();

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
            throw new InvalidOperationException(
                $"ISOLATION VIOLATION: DbContext is connected to '{actualBuilder.InitialCatalog}' instead of '{TestDatabaseName}'.");
        }

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        var div1 = new Division { Code = "DIV1", Name = "Division 1", IsActive = true, CreatedAt = DateTime.UtcNow };
        var div2 = new Division { Code = "DIV2", Name = "Division 2", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Divisions.AddRange(div1, div2);
        context.SaveChanges();

        _div1Id = div1.DivisionId;
        _div2Id = div2.DivisionId;

        var sec1 = new Section { DivisionId = _div1Id, Code = "SEC1", Name = "Section 1", IsActive = true, CreatedAt = DateTime.UtcNow };
        var sec2 = new Section { DivisionId = _div2Id, Code = "SEC2", Name = "Section 2", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Sections.AddRange(sec1, sec2);
        context.SaveChanges();

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var sysAdmin = new User { UserId = _sysAdminId, EmployeeNumber = "E01", FirstName = "Sys", LastName = "Admin", Email = "sa@test.com", Position = "SA", SectionId = null, RoleId = 1, Username = "sysadmin", PasswordHash = passwordHasher.HashPassword(new User(), "Pass#123"), IsActive = true, CreatedAt = DateTime.UtcNow };
        var admin1 = new User { UserId = _admin1Id, EmployeeNumber = "E02", FirstName = "Admin", LastName = "One", Email = "a1@test.com", Position = "Admin", SectionId = null, RoleId = 2, Username = "admin1", PasswordHash = passwordHasher.HashPassword(new User(), "Pass#123"), IsActive = true, CreatedAt = DateTime.UtcNow };
        var admin2 = new User { UserId = _admin2Id, EmployeeNumber = "E03", FirstName = "Admin", LastName = "Two", Email = "a2@test.com", Position = "Admin", SectionId = null, RoleId = 2, Username = "admin2", PasswordHash = passwordHasher.HashPassword(new User(), "Pass#123"), IsActive = true, CreatedAt = DateTime.UtcNow };
        var user1 = new User { UserId = _user1Id, EmployeeNumber = "E04", FirstName = "User", LastName = "One", Email = "u1@test.com", Position = "Staff", SectionId = sec1.SectionId, RoleId = 3, Username = "user1", PasswordHash = passwordHasher.HashPassword(new User(), "Pass#123"), IsActive = true, CreatedAt = DateTime.UtcNow };
        var user2 = new User { UserId = _user2Id, EmployeeNumber = "E05", FirstName = "User", LastName = "Two", Email = "u2@test.com", Position = "Staff", SectionId = sec2.SectionId, RoleId = 3, Username = "user2", PasswordHash = passwordHasher.HashPassword(new User(), "Pass#123"), IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Users.AddRange(sysAdmin, admin1, admin2, user1, user2);

        context.AdministratorScopes.Add(new AdministratorScope
        {
            AdministratorUserId = _admin1Id,
            DivisionId = _div1Id,
            CreatedAt = DateTime.UtcNow
        });

        context.AdministratorScopes.Add(new AdministratorScope
        {
            AdministratorUserId = _admin2Id,
            DivisionId = _div2Id,
            CreatedAt = DateTime.UtcNow
        });

        context.AdministratorSystemAccess.Add(new AdministratorSystemAccess
        {
            AdministratorUserId = _admin1Id,
            SystemId = _system1Id,
            CreatedAt = DateTime.UtcNow
        });

        var sys1 = new Domain.Entities.System { SystemId = _system1Id, SystemCode = "SYS1", SystemName = "System 1", DefaultAccess = "Restricted", IsActive = true, CreatedAt = DateTime.UtcNow };
        var sys2 = new Domain.Entities.System { SystemId = _system2Id, SystemCode = "SYS2", SystemName = "System 2", DefaultAccess = "All", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Systems.AddRange(sys1, sys2);

        context.SaveChanges();

        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        _sysAdminToken = jwtService.GenerateAccessToken(sysAdmin, "SYSTEM_ADMINISTRATOR");
        _admin1Token = jwtService.GenerateAccessToken(admin1, "ADMINISTRATOR");
        _user1Token = jwtService.GenerateAccessToken(user1, "USER");
    }

    private void SetBearerToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private void ClearBearerToken()
    {
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task UnauthenticatedRequest_Returns401()
    {
        ClearBearerToken();
        var response = await _client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UserRole_CallingAdminEndpoint_Returns403()
    {
        SetBearerToken(_user1Token);
        var response = await _client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserRole_CallingSystemsEndpoint_Returns200()
    {
        SetBearerToken(_user1Token);
        var response = await _client.GetAsync("/api/systems");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminRole_CallingSysAdminOnlyEndpoint_Returns403()
    {
        SetBearerToken(_admin1Token);
        var response = await _client.PostAsJsonAsync("/api/divisions", new { Code = "DIV_TEST", Name = "Test Division" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminRole_RequestingOwnDivision_Returns200()
    {
        SetBearerToken(_admin1Token);
        var response = await _client.GetAsync($"/api/divisions/{_div1Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminRole_RequestingAnotherDivision_Returns403()
    {
        // Admin 1 is Division 1; requesting Division 2 -> 403 Forbidden
        SetBearerToken(_admin1Token);
        var response = await _client.GetAsync($"/api/divisions/{_div2Id}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminRole_CreatingSectionInAnotherDivision_Returns403()
    {
        SetBearerToken(_admin1Token);
        var response = await _client.PostAsJsonAsync("/api/sections", new CreateSectionDto
        {
            DivisionId = _div2Id,
            Code = "SEC2_NEW",
            Name = "Section 2 New"
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminRole_CreatingSectionInOwnDivision_Returns201()
    {
        SetBearerToken(_admin1Token);
        var response = await _client.PostAsJsonAsync("/api/sections", new CreateSectionDto
        {
            DivisionId = _div1Id,
            Code = "SEC1_NEW",
            Name = "Section 1 New"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AdminRole_AccessingUserInDifferentDivision_Returns403()
    {
        SetBearerToken(_admin1Token);
        var response = await _client.GetAsync($"/api/users/{_user2Id}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminRole_ChangingRole_Returns403()
    {
        // Administrator cannot perform role changes
        SetBearerToken(_admin1Token);
        var response = await _client.PutAsJsonAsync($"/api/users/{_user1Id}/role", new ChangeRoleDto
        {
            RoleId = 2
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SysAdmin_SelfRoleChange_Returns403()
    {
        // Administrator cannot modify their own role
        SetBearerToken(_sysAdminToken);
        var response = await _client.PutAsJsonAsync($"/api/users/{_sysAdminId}/role", new ChangeRoleDto
        {
            RoleId = 2
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SysAdmin_ChangingOtherUserRole_Returns204()
    {
        SetBearerToken(_sysAdminToken);
        var response = await _client.PutAsJsonAsync($"/api/users/{_user1Id}/role", new ChangeRoleDto
        {
            RoleId = 2
        });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AdminRole_SystemAccess_WithoutSystemGrant_Returns403()
    {
        // Admin 1 has grant for System 1, but NOT for System 2
        SetBearerToken(_admin1Token);
        var response = await _client.PostAsJsonAsync("/api/system-access", new SetUserSystemAccessDto
        {
            UserId = _user1Id,
            SystemId = _system2Id,
            AccessType = "Allow"
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminRole_SystemAccess_WithGrantAndScope_Returns200()
    {
        // Admin 1 has grant for System 1, and User 1 is in Division 1 -> 200 OK
        SetBearerToken(_admin1Token);
        var response = await _client.PostAsJsonAsync("/api/system-access", new SetUserSystemAccessDto
        {
            UserId = _user1Id,
            SystemId = _system1Id,
            AccessType = "Allow"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SysAdmin_AuditLogsAndLoginHistories_Returns200()
    {
        SetBearerToken(_sysAdminToken);
        var auditResponse = await _client.GetAsync("/api/audit-logs");
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);

        var historyResponse = await _client.GetAsync("/api/login-histories");
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
