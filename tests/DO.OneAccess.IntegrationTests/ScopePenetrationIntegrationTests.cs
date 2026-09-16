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
public class ScopePenetrationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string TestDatabaseName = "DO_OneAccess_Test";
    private const string ConnectionString = $"Server=localhost\\SQLEXPRESS;Database={TestDatabaseName};Trusted_Connection=True;TrustServerCertificate=True;";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private readonly Guid _sysAdminId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private readonly Guid _admin1Id = Guid.Parse("30000000-0000-0000-0000-000000000002");
    private readonly Guid _admin2Id = Guid.Parse("30000000-0000-0000-0000-000000000003");
    private readonly Guid _user1Id = Guid.Parse("30000000-0000-0000-0000-000000000004");
    private readonly Guid _user2Id = Guid.Parse("30000000-0000-0000-0000-000000000005");

    private readonly Guid _system1Id = Guid.Parse("30000000-0000-0000-0000-000000000011");
    private readonly Guid _system2Id = Guid.Parse("30000000-0000-0000-0000-000000000012");

    private int _div1Id;
    private int _div2Id;
    private int _sec1Id;
    private int _sec2Id;

    private string _sysAdminToken = string.Empty;
    private string _admin1Token = string.Empty;
    private string _user1Token = string.Empty;

    public ScopePenetrationIntegrationTests(WebApplicationFactory<Program> factory)
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
            throw new InvalidOperationException("ISOLATION VIOLATION in ScopePenetrationIntegrationTests");
        }

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        var div1 = new Division { Code = "PEN_D1", Name = "Penetration Div 1", IsActive = true, CreatedAt = DateTime.UtcNow };
        var div2 = new Division { Code = "PEN_D2", Name = "Penetration Div 2", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Divisions.AddRange(div1, div2);
        context.SaveChanges();

        _div1Id = div1.DivisionId;
        _div2Id = div2.DivisionId;

        var sec1 = new Section { DivisionId = _div1Id, Code = "PEN_S1", Name = "Penetration Sec 1", IsActive = true, CreatedAt = DateTime.UtcNow };
        var sec2 = new Section { DivisionId = _div2Id, Code = "PEN_S2", Name = "Penetration Sec 2", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Sections.AddRange(sec1, sec2);
        context.SaveChanges();

        _sec1Id = sec1.SectionId;
        _sec2Id = sec2.SectionId;

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var sysAdmin = new User
        {
            UserId = _sysAdminId,
            EmployeeNumber = "P_E01",
            FirstName = "Sys",
            LastName = "Admin",
            Email = "sa@pen.com",
            Position = "SA",
            SectionId = null,
            RoleId = 1,
            Username = "pensysadmin",
            PasswordHash = hasher.HashPassword(new User(), "Pass#123"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var admin1 = new User
        {
            UserId = _admin1Id,
            EmployeeNumber = "P_E02",
            FirstName = "Admin",
            LastName = "One",
            Email = "a1@pen.com",
            Position = "Admin",
            SectionId = null,
            RoleId = 2,
            Username = "penadmin1",
            PasswordHash = hasher.HashPassword(new User(), "Pass#123"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var admin2 = new User
        {
            UserId = _admin2Id,
            EmployeeNumber = "P_E03",
            FirstName = "Admin",
            LastName = "Two",
            Email = "a2@pen.com",
            Position = "Admin",
            SectionId = null,
            RoleId = 2,
            Username = "penadmin2",
            PasswordHash = hasher.HashPassword(new User(), "Pass#123"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var user1 = new User
        {
            UserId = _user1Id,
            EmployeeNumber = "P_E04",
            FirstName = "User",
            LastName = "One",
            Email = "u1@pen.com",
            Position = "Staff",
            SectionId = _sec1Id,
            RoleId = 3,
            Username = "penuser1",
            PasswordHash = hasher.HashPassword(new User(), "Pass#123"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var user2 = new User
        {
            UserId = _user2Id,
            EmployeeNumber = "P_E05",
            FirstName = "User",
            LastName = "Two",
            Email = "u2@pen.com",
            Position = "Staff",
            SectionId = _sec2Id,
            RoleId = 3,
            Username = "penuser2",
            PasswordHash = hasher.HashPassword(new User(), "Pass#123"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.AddRange(sysAdmin, admin1, admin2, user1, user2);

        // Admin 1 scoped to Division 1; Admin 2 scoped to Division 2
        context.AdministratorScopes.Add(new AdministratorScope { AdministratorUserId = _admin1Id, DivisionId = _div1Id, CreatedAt = DateTime.UtcNow });
        context.AdministratorScopes.Add(new AdministratorScope { AdministratorUserId = _admin2Id, DivisionId = _div2Id, CreatedAt = DateTime.UtcNow });

        // Admin 1 has management grant for System 1 only
        context.AdministratorSystemAccess.Add(new AdministratorSystemAccess { AdministratorUserId = _admin1Id, SystemId = _system1Id, CreatedAt = DateTime.UtcNow });

        var sys1 = new Domain.Entities.System { SystemId = _system1Id, SystemCode = "PEN_SYS1", SystemName = "Pen System 1", DefaultAccess = "Restricted", IsActive = true, CreatedAt = DateTime.UtcNow };
        var sys2 = new Domain.Entities.System { SystemId = _system2Id, SystemCode = "PEN_SYS2", SystemName = "Pen System 2", DefaultAccess = "All", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Systems.AddRange(sys1, sys2);

        context.SaveChanges();

        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        _sysAdminToken = jwt.GenerateAccessToken(sysAdmin, "SYSTEM_ADMINISTRATOR");
        _admin1Token = jwt.GenerateAccessToken(admin1, "ADMINISTRATOR");
        _user1Token = jwt.GenerateAccessToken(user1, "USER");
    }

    private void SetToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task Admin_AccessingCrossDivisionSection_Returns403Forbidden()
    {
        // Admin 1 is Division 1; Section 2 is Division 2
        SetToken(_admin1Token);
        var response = await _client.GetAsync($"/api/sections/{_sec2Id}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_UpdatingCrossDivisionSection_Returns403Forbidden()
    {
        SetToken(_admin1Token);
        var response = await _client.PutAsJsonAsync($"/api/sections/{_sec2Id}", new UpdateSectionDto
        {
            Name = "Tampered Section",
            Description = "Tampered Description"
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_DeactivatingCrossDivisionSection_Returns403Forbidden()
    {
        SetToken(_admin1Token);
        var response = await _client.DeleteAsync($"/api/sections/{_sec2Id}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CreatingCrossDivisionUser_Returns403Forbidden()
    {
        // Administrator attempting to create a user in another division's section
        SetToken(_admin1Token);
        var response = await _client.PostAsJsonAsync("/api/users", new CreateUserDto
        {
            EmployeeNumber = "EMP_TAMPER_01",
            FirstName = "Tamper",
            LastName = "Employee",
            Email = "tamper@pen.com",
            Position = "Specialist",
            SectionId = _sec2Id, // Cross-division section
            RoleId = 3,
            Username = "tamper_user",
            Password = "SecurePassword123!"
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CreatingAdminUser_Returns403Forbidden()
    {
        // Administrator attempting to provision an Administrator account
        SetToken(_admin1Token);
        var response = await _client.PostAsJsonAsync("/api/users", new CreateUserDto
        {
            EmployeeNumber = "P_E09A",
            FirstName = "Esc",
            LastName = "Admin",
            Email = "escadmin@pen.com",
            SectionId = _sec1Id,
            RoleId = 2, // ADMINISTRATOR
            Username = "escalated_admin",
            Password = "SecurePassword123!"
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CreatingSysAdminUser_Returns403Forbidden()
    {
        // Administrator attempting to provision a System Administrator account via normal endpoint
        SetToken(_admin1Token);
        var response = await _client.PostAsJsonAsync("/api/users", new CreateUserDto
        {
            EmployeeNumber = "P_E09B",
            FirstName = "Esc",
            LastName = "SysAdmin",
            Email = "escsys@pen.com",
            RoleId = 1, // SYSTEM_ADMINISTRATOR
            Username = "escalated_sysadmin",
            Password = "SecurePassword123!"
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CallingTransferSystemAdministratorEndpoint_Returns403Forbidden()
    {
        // Administrator attempting to call SysAdmin transfer endpoint
        SetToken(_admin1Token);
        var response = await _client.PostAsJsonAsync("/api/users/system-administrator/transfer", new TransferSystemAdministratorDto
        {
            TargetUserId = _user1Id,
            PreviousAdminNewRoleId = 2,
            PreviousAdminDivisionId = _div1Id
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Old creation endpoint must be completely gone (404)
        var oldResponse = await _client.PostAsJsonAsync("/api/users/system-administrator", new { });
        Assert.Equal(HttpStatusCode.NotFound, oldResponse.StatusCode);
    }

    [Fact]
    public async Task User_SelfDeactivation_Returns403Forbidden()
    {
        SetToken(_user1Token);
        var response = await _client.DeleteAsync($"/api/users/{_user1Id}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_SelfDeactivation_Returns403Forbidden()
    {
        SetToken(_admin1Token);
        var response = await _client.DeleteAsync($"/api/users/{_admin1Id}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SysAdmin_SelfDeactivation_Returns403Forbidden()
    {
        SetToken(_sysAdminToken);
        var response = await _client.DeleteAsync($"/api/users/{_sysAdminId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SysAdmin_SelfRoleChange_Returns403Forbidden()
    {
        SetToken(_sysAdminToken);
        var response = await _client.PutAsJsonAsync($"/api/users/{_sysAdminId}/role", new ChangeRoleDto
        {
            RoleId = 2 // Attempting to demote self
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_SystemAccess_WithoutAdminManagementGrant_Returns403Forbidden()
    {
        // Admin 1 has grant for System 1, but NOT System 2
        SetToken(_admin1Token);
        var response = await _client.PostAsJsonAsync("/api/system-access", new SetUserSystemAccessDto
        {
            UserId = _user1Id,
            SystemId = _system2Id,
            AccessType = "Allow"
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Actor_WhenDeactivatedInDb_Returns403OnMutatingCall()
    {
        // 1. Deactivate Admin 1 in database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var admin = await db.Users.FindAsync(_admin1Id);
            Assert.NotNull(admin);
            admin.IsActive = false;
            await db.SaveChangesAsync();
        }

        // 2. Admin 1 presents existing token
        SetToken(_admin1Token);
        var response = await _client.PostAsJsonAsync("/api/sections", new CreateSectionDto
        {
            DivisionId = _div1Id,
            Code = "INACTIVE_MUTATION",
            Name = "Should Fail"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
