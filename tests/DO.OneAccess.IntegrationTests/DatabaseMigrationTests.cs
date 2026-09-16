using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Infrastructure.Persistence;
using Xunit;

namespace DO.OneAccess.IntegrationTests;

[Collection("IntegrationTests")]
public class DatabaseMigrationTests : IDisposable
{
    private const string TestDatabaseName = "DO_OneAccess_Test";
    private const string ConnectionString = $"Server=localhost\\SQLEXPRESS;Database={TestDatabaseName};Trusted_Connection=True;TrustServerCertificate=True;";
    private readonly AppDbContext _context;

    public DatabaseMigrationTests()
    {
        // Enforce strict separation guard: Ensure tests NEVER touch development or production databases
        var builder = new SqlConnectionStringBuilder(ConnectionString);
        if (!string.Equals(builder.InitialCatalog, TestDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Security Guard: Integration tests must strictly target '{TestDatabaseName}', but targeted '{builder.InitialCatalog}'.");
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        _context = new AppDbContext(options);

        // Reset test database to clean state
        _context.Database.EnsureDeleted();
        _context.Database.Migrate();
    }

    [Fact]
    public async Task Migration_ShouldCreateAll12CanonicalTables()
    {
        var expectedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Roles",
            "Divisions",
            "Sections",
            "Users",
            "Systems",
            "SystemSettings",
            "UserSystemAccess",
            "AdministratorScopes",
            "AdministratorSystemAccess",
            "RefreshTokens",
            "LoginHistories",
            "AuditLogs"
        };

        var actualTables = new List<string>();

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using (var command = new SqlCommand(
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME != '__EFMigrationsHistory'", 
            connection))
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                actualTables.Add(reader.GetString(0));
            }
        }

        Assert.Equal(expectedTables.Count, actualTables.Count);
        foreach (var table in expectedTables)
        {
            Assert.Contains(table, actualTables, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Migration_ShouldSeedExactlyThreeRoles()
    {
        var roles = await _context.Roles.OrderBy(r => r.RoleId).ToListAsync();

        Assert.Equal(3, roles.Count);

        var sysAdmin = roles[0];
        Assert.Equal((short)1, sysAdmin.RoleId);
        Assert.Equal("SYSTEM_ADMINISTRATOR", sysAdmin.Code);
        Assert.Equal("System Administrator", sysAdmin.Name);
        Assert.True(sysAdmin.IsActive);

        var admin = roles[1];
        Assert.Equal((short)2, admin.RoleId);
        Assert.Equal("ADMINISTRATOR", admin.Code);
        Assert.Equal("Administrator", admin.Name);
        Assert.True(admin.IsActive);

        var user = roles[2];
        Assert.Equal((short)3, user.RoleId);
        Assert.Equal("USER", user.Code);
        Assert.Equal("User", user.Name);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task Migration_ShouldCreateExpectedForeignKeysInSqlServer()
    {
        var expectedFks = new[]
        {
            "FK_Sections_Divisions_DivisionId",
            "FK_Users_Sections_SectionId",
            "FK_Users_Roles_RoleId",
            "FK_SystemSettings_Systems_SystemId",
            "FK_UserSystemAccess_Systems_SystemId",
            "FK_UserSystemAccess_Users_UserId",
            "FK_AdministratorScopes_Divisions_DivisionId",
            "FK_AdministratorScopes_Users_AdministratorUserId",
            "FK_AdministratorSystemAccess_Systems_SystemId",
            "FK_AdministratorSystemAccess_Users_AdministratorUserId",
            "FK_RefreshTokens_RefreshTokens_ReplacedByTokenId",
            "FK_RefreshTokens_Users_UserId",
            "FK_LoginHistories_Users_UserId",
            "FK_AuditLogs_Users_UserId"
        };

        var actualFks = new List<string>();

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using (var command = new SqlCommand("SELECT name FROM sys.foreign_keys", connection))
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                actualFks.Add(reader.GetString(0));
            }
        }

        foreach (var expected in expectedFks)
        {
            Assert.Contains(expected, actualFks, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Migration_ShouldEnforceUniqueConstraintsInSqlServer()
    {
        var expectedUniqueIndexes = new[]
        {
            "IX_Roles_Code",
            "IX_Divisions_Code",
            "IX_Sections_DivisionId_Code",
            "IX_Users_EmployeeNumber",
            "IX_Users_Username",
            "IX_Systems_SystemCode",
            "IX_SystemSettings_SystemId_SettingKey",
            "IX_UserSystemAccess_UserId_SystemId",
            "IX_AdministratorScopes_AdministratorUserId",
            "IX_AdministratorSystemAccess_AdministratorUserId_SystemId"
        };

        var actualUniqueIndexes = new List<string>();

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using (var command = new SqlCommand(
            "SELECT name FROM sys.indexes WHERE is_unique = 1 AND is_primary_key = 0", 
            connection))
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                actualUniqueIndexes.Add(reader.GetString(0));
            }
        }

        foreach (var expected in expectedUniqueIndexes)
        {
            Assert.Contains(expected, actualUniqueIndexes, StringComparer.OrdinalIgnoreCase);
        }
    }

    public void Dispose()
    {
        try
        {
            _context.Database.EnsureDeleted();
        }
        catch
        {
            // Best effort cleanup
        }
        _context.Dispose();
    }
}
