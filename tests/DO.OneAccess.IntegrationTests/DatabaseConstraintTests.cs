using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Infrastructure.Persistence;
using Xunit;

namespace DO.OneAccess.IntegrationTests;

[Collection("IntegrationTests")]
public class DatabaseConstraintTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestDatabaseName = "DO_OneAccess_Test";
    private const string ConnectionString = $"Server=localhost\\SQLEXPRESS;Database={TestDatabaseName};Trusted_Connection=True;TrustServerCertificate=True;";

    private readonly WebApplicationFactory<Program> _factory;

    public DatabaseConstraintTests(WebApplicationFactory<Program> factory)
    {
        var builder = new SqlConnectionStringBuilder(ConnectionString);
        if (!string.Equals(builder.InitialCatalog, TestDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Security Guard Violation: Targeted '{builder.InitialCatalog}'.");
        }

        _factory = factory.WithWebHostBuilder(hostBuilder =>
        {
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
            throw new InvalidOperationException("ISOLATION VIOLATION in DatabaseConstraintTests");
        }

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        var div = new Division { Code = "CONST_D1", Name = "Constraint Div", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Divisions.Add(div);
        context.SaveChanges();

        var sec = new Section { DivisionId = div.DivisionId, Code = "CONST_S1", Name = "Constraint Sec", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Sections.Add(sec);
        context.SaveChanges();

        var user = new User
        {
            UserId = Guid.NewGuid(),
            EmployeeNumber = "EMP_CONST_01",
            FirstName = "F",
            LastName = "L",
            Email = "c1@c.com",
            Position = "P",
            SectionId = sec.SectionId,
            RoleId = 3,
            Username = "const_user_01",
            PasswordHash = "AQAAAAIAAYagAAAAE...",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        context.SaveChanges();
    }

    [Fact]
    public async Task UniqueConstraint_DuplicateUsername_ThrowsDbUpdateException()
    {
        // Database uniqueness constraint on Users.Username
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sec = await context.Sections.FirstAsync();
        var duplicateUser = new User
        {
            UserId = Guid.NewGuid(),
            EmployeeNumber = "EMP_CONST_02",
            FirstName = "F2",
            LastName = "L2",
            Email = "c2@c.com",
            Position = "P",
            SectionId = sec.SectionId,
            RoleId = 3,
            Username = "const_user_01", // Duplicate username!
            PasswordHash = "AQAAAAIAAYagAAAAE...",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(duplicateUser);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task UniqueConstraint_DuplicateEmployeeNumber_ThrowsDbUpdateException()
    {
        // Database uniqueness constraint on Users.EmployeeNumber
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sec = await context.Sections.FirstAsync();
        var duplicateUser = new User
        {
            UserId = Guid.NewGuid(),
            EmployeeNumber = "EMP_CONST_01", // Duplicate EmployeeNumber!
            FirstName = "F3",
            LastName = "L3",
            Email = "c3@c.com",
            Position = "P",
            SectionId = sec.SectionId,
            RoleId = 3,
            Username = "const_user_03",
            PasswordHash = "AQAAAAIAAYagAAAAE...",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(duplicateUser);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task CompoundUnique_SectionCodeWithinDivision_ThrowsDbUpdateException()
    {
        // Database compound uniqueness constraint on Sections(DivisionId, Code)
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var div = await context.Divisions.FirstAsync();
        var duplicateSection = new Section
        {
            DivisionId = div.DivisionId,
            Code = "CONST_S1", // Duplicate code in same division!
            Name = "Duplicate Section",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Sections.Add(duplicateSection);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task UniqueConstraint_UserSystemAccess_ThrowsDbUpdateException()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await context.Users.FirstAsync();
        var sys = new Domain.Entities.System { SystemId = Guid.NewGuid(), SystemCode = "CONST_SYS1", SystemName = "Const Sys 1", DefaultAccess = "Restricted", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Systems.Add(sys);
        await context.SaveChangesAsync();

        context.UserSystemAccess.Add(new UserSystemAccess { UserId = user.UserId, SystemId = sys.SystemId, AccessType = "Allow", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        // Duplicate
        context.UserSystemAccess.Add(new UserSystemAccess { UserId = user.UserId, SystemId = sys.SystemId, AccessType = "Deny", CreatedAt = DateTime.UtcNow });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task UniqueConstraint_AdministratorSystemAccess_ThrowsDbUpdateException()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await context.Users.FirstAsync();
        var sys = new Domain.Entities.System { SystemId = Guid.NewGuid(), SystemCode = "CONST_SYS2", SystemName = "Const Sys 2", DefaultAccess = "Restricted", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Systems.Add(sys);
        await context.SaveChangesAsync();

        context.AdministratorSystemAccess.Add(new AdministratorSystemAccess { AdministratorUserId = user.UserId, SystemId = sys.SystemId, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        // Duplicate
        context.AdministratorSystemAccess.Add(new AdministratorSystemAccess { AdministratorUserId = user.UserId, SystemId = sys.SystemId, CreatedAt = DateTime.UtcNow });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task ForeignKey_Users_SectionId_InvalidSection_ThrowsDbUpdateException()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var invalidSectionUser = new User
        {
            UserId = Guid.NewGuid(),
            EmployeeNumber = "EMP_INVALID_SEC",
            FirstName = "Inv",
            LastName = "Sec",
            Email = "invsec@c.com",
            SectionId = 999999, // Non-existent SectionId
            RoleId = 3,
            Username = "const_user_invalid_sec",
            PasswordHash = "AQAAAAIAAYagAAAAE...",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(invalidSectionUser);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task UniqueConstraint_Roles_Code_ThrowsDbUpdateException()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var duplicateRole = new Role
        {
            RoleId = 99,
            Code = "USER", // Duplicate code of existing canonical role!
            Name = "Duplicate User Role",
            Description = "Duplicate"
        };
        context.Roles.Add(duplicateRole);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task UniqueConstraint_Divisions_Code_ThrowsDbUpdateException()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var duplicateDiv = new Division
        {
            Code = "CONST_D1", // Duplicate code of existing division!
            Name = "Duplicate Div",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Divisions.Add(duplicateDiv);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task UniqueConstraint_Systems_SystemCode_ThrowsDbUpdateException()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sys1 = new Domain.Entities.System
        {
            SystemId = Guid.NewGuid(),
            SystemCode = "SYS_UNIQUE_CODE",
            SystemName = "System 1",
            DefaultAccess = "Restricted",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Systems.Add(sys1);
        await context.SaveChangesAsync();

        var sys2 = new Domain.Entities.System
        {
            SystemId = Guid.NewGuid(),
            SystemCode = "SYS_UNIQUE_CODE", // Duplicate SystemCode!
            SystemName = "System 2",
            DefaultAccess = "Restricted",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Systems.Add(sys2);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task UniqueConstraint_SystemSettings_SystemId_SettingKey_ThrowsDbUpdateException()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sys = new Domain.Entities.System
        {
            SystemId = Guid.NewGuid(),
            SystemCode = "SYS_SETTINGS_TEST",
            SystemName = "Settings Test System",
            DefaultAccess = "Restricted",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Systems.Add(sys);
        await context.SaveChangesAsync();

        var setting1 = new SystemSetting
        {
            SystemId = sys.SystemId,
            SettingKey = "OAUTH_CLIENT_ID",
            SettingValue = "val1",
            CreatedAt = DateTime.UtcNow
        };
        context.SystemSettings.Add(setting1);
        await context.SaveChangesAsync();

        var setting2 = new SystemSetting
        {
            SystemId = sys.SystemId,
            SettingKey = "OAUTH_CLIENT_ID", // Duplicate compound key!
            SettingValue = "val2",
            CreatedAt = DateTime.UtcNow
        };
        context.SystemSettings.Add(setting2);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task UniqueConstraint_AdministratorScopes_AdministratorUserId_ThrowsDbUpdateException()
    {
        Guid userId;
        int divisionId;

        using (var scope1 = _factory.Services.CreateScope())
        {
            var ctx1 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await ctx1.Users.FirstAsync();
            var div = await ctx1.Divisions.FirstAsync();
            userId = user.UserId;
            divisionId = div.DivisionId;

            ctx1.AdministratorScopes.Add(new AdministratorScope
            {
                AdministratorUserId = userId,
                DivisionId = divisionId,
                CreatedAt = DateTime.UtcNow
            });
            await ctx1.SaveChangesAsync();
        }

        using (var scope2 = _factory.Services.CreateScope())
        {
            var ctx2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            ctx2.AdministratorScopes.Add(new AdministratorScope
            {
                AdministratorUserId = userId, // Duplicate AdministratorUserId
                DivisionId = divisionId,
                CreatedAt = DateTime.UtcNow
            });

            await Assert.ThrowsAsync<DbUpdateException>(() => ctx2.SaveChangesAsync());
        }
    }
}
