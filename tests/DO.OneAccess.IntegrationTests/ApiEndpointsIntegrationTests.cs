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
using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Access;
using DO.OneAccess.Application.DTOs.Divisions;
using DO.OneAccess.Application.DTOs.Sections;
using DO.OneAccess.Application.DTOs.Systems;
using DO.OneAccess.Application.DTOs.Users;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Infrastructure.Persistence;
using Xunit;

namespace DO.OneAccess.IntegrationTests;

[Collection("IntegrationTests")]
public class ApiEndpointsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string TestDatabaseName = "DO_OneAccess_Test";
    private const string ConnectionString = $"Server=localhost\\SQLEXPRESS;Database={TestDatabaseName};Trusted_Connection=True;TrustServerCertificate=True;";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private readonly Guid _sysAdminId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private readonly Guid _admin1Id = Guid.Parse("40000000-0000-0000-0000-000000000002");
    private readonly Guid _user1Id = Guid.Parse("40000000-0000-0000-0000-000000000003");
    private readonly Guid _system1Id = Guid.Parse("40000000-0000-0000-0000-000000000020");

    private int _div1Id;
    private int _sec1Id;

    private string _sysAdminToken = string.Empty;
    private string _admin1Token = string.Empty;
    private string _user1Token = string.Empty;

    public ApiEndpointsIntegrationTests(WebApplicationFactory<Program> factory)
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
            throw new InvalidOperationException("ISOLATION VIOLATION in ApiEndpointsIntegrationTests");
        }

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        var div1 = new Division { Code = "API_D1", Name = "API Div 1", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Divisions.Add(div1);
        context.SaveChanges();
        _div1Id = div1.DivisionId;

        var sec1 = new Section { DivisionId = _div1Id, Code = "API_S1", Name = "API Sec 1", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Sections.Add(sec1);
        context.SaveChanges();
        _sec1Id = sec1.SectionId;

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var sysAdmin = new User
        {
            UserId = _sysAdminId,
            EmployeeNumber = "API_E01",
            FirstName = "Sys",
            LastName = "Admin",
            Email = "sa@api.com",
            Position = "SA",
            SectionId = null,
            RoleId = 1,
            Username = "apisysadmin",
            PasswordHash = hasher.HashPassword(new User(), "Pass#123"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var admin1 = new User
        {
            UserId = _admin1Id,
            EmployeeNumber = "API_E02",
            FirstName = "Admin",
            LastName = "One",
            Email = "a1@api.com",
            Position = "Admin",
            SectionId = null,
            RoleId = 2,
            Username = "apiadmin1",
            PasswordHash = hasher.HashPassword(new User(), "Pass#123"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var user1 = new User
        {
            UserId = _user1Id,
            EmployeeNumber = "API_E03",
            FirstName = "User",
            LastName = "One",
            Email = "u1@api.com",
            Position = "Staff",
            SectionId = _sec1Id,
            RoleId = 3,
            Username = "apiuser1",
            PasswordHash = hasher.HashPassword(new User(), "Pass#123"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.AddRange(sysAdmin, admin1, user1);

        context.AdministratorScopes.Add(new AdministratorScope { AdministratorUserId = _admin1Id, DivisionId = _div1Id, CreatedAt = DateTime.UtcNow });
        context.AdministratorSystemAccess.Add(new AdministratorSystemAccess { AdministratorUserId = _admin1Id, SystemId = _system1Id, CreatedAt = DateTime.UtcNow });

        var sys1 = new Domain.Entities.System { SystemId = _system1Id, SystemCode = "API_SYS1", SystemName = "API System 1", DefaultAccess = "Restricted", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Systems.Add(sys1);

        context.SaveChanges();

        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        _sysAdminToken = jwt.GenerateAccessToken(sysAdmin, "SYSTEM_ADMINISTRATOR");
        _admin1Token = jwt.GenerateAccessToken(admin1, "ADMINISTRATOR");
        _user1Token = jwt.GenerateAccessToken(user1, "USER");
    }

    private void SetToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private void ClearToken() =>
        _client.DefaultRequestHeaders.Authorization = null;

    // ==========================================
    // 1. PRIVILEGED SYSTEM ADMINISTRATOR ENDPOINT & TRANSFER
    // ==========================================

    [Fact]
    public async Task OldPrivilegedSysAdminCreationEndpoint_Returns404NotFound()
    {
        SetToken(_sysAdminToken);
        var response = await _client.PostAsJsonAsync("/api/users/system-administrator", new
        {
            Username = "should_not_exist",
            Password = "Password123!"
        });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TransferSysAdmin_Unauthenticated_Returns401()
    {
        ClearToken();
        var response = await _client.PostAsJsonAsync("/api/users/system-administrator/transfer", new TransferSystemAdministratorDto
        {
            TargetUserId = _user1Id,
            PreviousAdminNewRoleId = 2,
            PreviousAdminDivisionId = _div1Id
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TransferSysAdmin_UserRole_Returns403()
    {
        SetToken(_user1Token);
        var response = await _client.PostAsJsonAsync("/api/users/system-administrator/transfer", new TransferSystemAdministratorDto
        {
            TargetUserId = _user1Id,
            PreviousAdminNewRoleId = 2,
            PreviousAdminDivisionId = _div1Id
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TransferSysAdmin_AdminRole_Returns403()
    {
        SetToken(_admin1Token);
        var response = await _client.PostAsJsonAsync("/api/users/system-administrator/transfer", new TransferSystemAdministratorDto
        {
            TargetUserId = _user1Id,
            PreviousAdminNewRoleId = 2,
            PreviousAdminDivisionId = _div1Id
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TransferSysAdmin_SysAdmin_Succeeds()
    {
        SetToken(_sysAdminToken);
        var response = await _client.PostAsJsonAsync("/api/users/system-administrator/transfer", new TransferSystemAdministratorDto
        {
            TargetUserId = _user1Id,
            PreviousAdminNewRoleId = 2,
            PreviousAdminDivisionId = _div1Id
        });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify target is now SysAdmin in DB
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var targetUser = await context.Users.FindAsync(_user1Id);
        Assert.NotNull(targetUser);
        Assert.Equal((short)1, targetUser.RoleId);
        Assert.Null(targetUser.SectionId);

        // Verify previous SysAdmin stepped down to Administrator in Div1
        var prevUser = await context.Users.FindAsync(_sysAdminId);
        Assert.NotNull(prevUser);
        Assert.Equal((short)2, prevUser.RoleId);
        Assert.Null(prevUser.SectionId);
    }

    // ==========================================
    // 2. SECTIONS ENDPOINT
    // ==========================================

    [Fact]
    public async Task Sections_AdminListAndGet_Succeeds()
    {
        SetToken(_admin1Token);
        var listResponse = await _client.GetAsync($"/api/sections?divisionId={_div1Id}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/sections/{_sec1Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var sec = await getResponse.Content.ReadFromJsonAsync<SectionDto>();
        Assert.NotNull(sec);
        Assert.Equal("API_S1", sec.Code);
    }

    [Fact]
    public async Task Sections_AdminCreateUpdateDeactivate_Succeeds()
    {
        SetToken(_admin1Token);

        // Create
        var createResponse = await _client.PostAsJsonAsync("/api/sections", new CreateSectionDto
        {
            DivisionId = _div1Id,
            Code = "API_S_CRUD",
            Name = "Crud Section",
            Description = "Crud Section Desc"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<SectionDto>();
        Assert.NotNull(created);

        // Update
        var updateResponse = await _client.PutAsJsonAsync($"/api/sections/{created.SectionId}", new UpdateSectionDto
        {
            Name = "Updated Crud Section",
            Description = "Updated Desc"
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Deactivate
        var deleteResponse = await _client.DeleteAsync($"/api/sections/{created.SectionId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    // ==========================================
    // 3. USERS ENDPOINT
    // ==========================================

    [Fact]
    public async Task Users_AdminListAndGet_Succeeds()
    {
        SetToken(_admin1Token);
        var listResponse = await _client.GetAsync("/api/users?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/users/{_user1Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var u = await getResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(u);
        Assert.Equal("apiuser1", u.Username);
        Assert.Equal("API_E03", u.EmployeeNumber);
    }

    [Fact]
    public async Task Users_AdminCreateAndUpdate_Succeeds()
    {
        SetToken(_admin1Token);

        // Create User directly with profile data (One Employee = One User)
        var createResponse = await _client.PostAsJsonAsync("/api/users", new CreateUserDto
        {
            EmployeeNumber = "API_U_CRUD",
            FirstName = "User",
            LastName = "Candidate",
            Email = "candidate@api.com",
            Position = "Associate",
            SectionId = _sec1Id,
            RoleId = 3, // USER
            Username = "crud_new_user",
            Password = "Password#123"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(createdUser);
        Assert.Equal("API_U_CRUD", createdUser.EmployeeNumber);
        Assert.Equal("User", createdUser.FirstName);
        Assert.Equal(_sec1Id, createdUser.SectionId);

        // Update User
        var updateResponse = await _client.PutAsJsonAsync($"/api/users/{createdUser.UserId}", new UpdateUserDto
        {
            Username = "crud_updated_user",
            FirstName = "UpdatedUser",
            LastName = "Candidate",
            Email = "updated_candidate@api.com",
            Position = "Senior Associate"
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Deactivate User
        var deleteResponse = await _client.DeleteAsync($"/api/users/{createdUser.UserId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    // ==========================================
    // 4. DIVISIONS ENDPOINT
    // ==========================================

    [Fact]
    public async Task Divisions_AdminList_ReturnsScopedDivision()
    {
        SetToken(_admin1Token);
        var response = await _client.GetAsync("/api/divisions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<IReadOnlyList<DivisionDto>>();
        Assert.NotNull(list);
        Assert.Single(list);
        Assert.Equal(_div1Id, list[0].DivisionId);

        // Admin attempting to create division returns 403 Forbidden
        var createResponse = await _client.PostAsJsonAsync("/api/divisions", new CreateDivisionDto
        {
            Code = "ADMIN_DIV_FAIL",
            Name = "Fail Division"
        });
        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
    }

    [Fact]
    public async Task Divisions_SysAdminCrud_Succeeds()
    {
        SetToken(_sysAdminToken);

        // List
        var listResponse = await _client.GetAsync("/api/divisions");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        // Create
        var createResponse = await _client.PostAsJsonAsync("/api/divisions", new CreateDivisionDto
        {
            Code = "API_D_CRUD",
            Name = "Crud Division",
            Description = "Crud Division Desc"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<DivisionDto>();
        Assert.NotNull(created);

        // Get by ID
        var getResponse = await _client.GetAsync($"/api/divisions/{created.DivisionId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Update
        var updateResponse = await _client.PutAsJsonAsync($"/api/divisions/{created.DivisionId}", new UpdateDivisionDto
        {
            Name = "Updated Crud Division"
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Deactivate
        var deleteResponse = await _client.DeleteAsync($"/api/divisions/{created.DivisionId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    // ==========================================
    // 5. SYSTEMS ENDPOINT
    // ==========================================

    [Fact]
    public async Task Systems_UserRegister_Returns403()
    {
        SetToken(_user1Token);
        var response = await _client.PostAsJsonAsync("/api/systems", new RegisterSystemDto
        {
            SystemCode = "FORBIDDEN_SYS",
            SystemName = "Forbidden System"
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Systems_AnyAuthenticatedUser_GetsAccessibleSystems()
    {
        SetToken(_user1Token);
        var response = await _client.GetAsync("/api/systems");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Systems_SysAdmin_GetAllAndCrud_Succeeds()
    {
        SetToken(_sysAdminToken);

        // Get All
        var allResponse = await _client.GetAsync("/api/systems/all");
        Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);

        // Register
        var registerResponse = await _client.PostAsJsonAsync("/api/systems", new RegisterSystemDto
        {
            SystemCode = "API_SYS_CRUD",
            SystemName = "Crud Office System",
            DefaultAccess = "Restricted"
        });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registered = await registerResponse.Content.ReadFromJsonAsync<SystemDto>();
        Assert.NotNull(registered);

        // Get by ID
        var getResponse = await _client.GetAsync($"/api/systems/{registered.SystemId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Update
        var updateResponse = await _client.PutAsJsonAsync($"/api/systems/{registered.SystemId}", new UpdateSystemDto
        {
            SystemName = "Updated Office System"
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Deactivate
        var deleteResponse = await _client.DeleteAsync($"/api/systems/{registered.SystemId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    // ==========================================
    // 6. SCOPES & SYSTEM ACCESS ENDPOINTS
    // ==========================================

    [Fact]
    public async Task AdminScopes_SysAdminAssignAndRemove_Succeeds()
    {
        SetToken(_sysAdminToken);

        Guid extraAdminId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var extraAdmin = new User
            {
                UserId = Guid.NewGuid(),
                EmployeeNumber = "API_E_EXTRA",
                FirstName = "Ex",
                LastName = "Adm",
                Email = "ex@api.com",
                Position = "Adm",
                SectionId = null,
                RoleId = 2,
                Username = "extraadmin",
                PasswordHash = hasher.HashPassword(new User(), "P#1"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(extraAdmin);
            await db.SaveChangesAsync();
            extraAdminId = extraAdmin.UserId;
        }

        // Assign
        var assignResponse = await _client.PostAsJsonAsync("/api/admin-scopes", new AssignAdminScopeDto
        {
            AdministratorUserId = extraAdminId,
            DivisionId = _div1Id
        });
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);
        var assigned = await assignResponse.Content.ReadFromJsonAsync<AdminScopeDto>();
        Assert.NotNull(assigned);

        // Get all
        var getResponse = await _client.GetAsync("/api/admin-scopes");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Remove
        var deleteResponse = await _client.DeleteAsync($"/api/admin-scopes/{assigned.AdministratorScopeId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task AdminSystemAccess_SysAdminGrantAndRevoke_Succeeds()
    {
        SetToken(_sysAdminToken);

        // Create extra system to test grant/revoke
        var sysExtra = new Domain.Entities.System { SystemId = Guid.NewGuid(), SystemCode = "API_SYS_EXTRA", SystemName = "Extra Sys", DefaultAccess = "Restricted", IsActive = true, CreatedAt = DateTime.UtcNow };
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Systems.Add(sysExtra);
            await db.SaveChangesAsync();
        }

        // Grant system management authority to Admin 1
        var grantResponse = await _client.PostAsJsonAsync("/api/admin-system-access", new GrantAdminSystemAccessDto
        {
            AdministratorUserId = _admin1Id,
            SystemId = sysExtra.SystemId
        });
        Assert.Equal(HttpStatusCode.OK, grantResponse.StatusCode);
        var granted = await grantResponse.Content.ReadFromJsonAsync<AdminSystemAccessDto>();
        Assert.NotNull(granted);

        // List
        var listResponse = await _client.GetAsync("/api/admin-system-access");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        // Revoke
        var revokeResponse = await _client.DeleteAsync($"/api/admin-system-access/{granted.AdministratorSystemAccessId}");
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);
    }

    [Fact]
    public async Task SystemAccess_AdminOverrideAndRevoke_Succeeds()
    {
        SetToken(_admin1Token);

        // Set Access override
        var setResponse = await _client.PostAsJsonAsync("/api/system-access", new SetUserSystemAccessDto
        {
            UserId = _user1Id,
            SystemId = _system1Id,
            AccessType = "Allow"
        });
        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);
        var access = await setResponse.Content.ReadFromJsonAsync<UserSystemAccessDto>();
        Assert.NotNull(access);

        // List overrides
        var listResponse = await _client.GetAsync($"/api/system-access?userId={_user1Id}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        // Revoke override
        var revokeResponse = await _client.DeleteAsync($"/api/system-access/{access.UserSystemAccessId}");
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
