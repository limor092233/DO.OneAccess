using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Domain.Enums;
using DO.OneAccess.Infrastructure.Persistence;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class ModelBuilderTests
{
    private readonly AppDbContext _context;

    public ModelBuilderTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "DO_OneAccess_UnitTest_Db_" + Guid.NewGuid())
            .Options;

        _context = new AppDbContext(options);
    }

    [Fact]
    public void ModelBuilder_ShouldRegisterAll12CanonicalTables()
    {
        var expectedTables = new[]
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

        var entityTypes = _context.Model.GetEntityTypes().ToList();
        var tableNames = entityTypes
            .Select(t => t.GetTableName())
            .Where(t => t != null)
            .Distinct()
            .ToList();

        Assert.Equal(12, expectedTables.Length);
        foreach (var expected in expectedTables)
        {
            Assert.Contains(expected, tableNames);
        }
    }

    [Fact]
    public void ModelBuilder_ShouldHaveTieredPrimaryKeys()
    {
        // smallint (short)
        var roleEntity = _context.Model.FindEntityType(typeof(Role));
        Assert.NotNull(roleEntity);
        var rolePk = roleEntity.FindPrimaryKey();
        Assert.NotNull(rolePk);
        Assert.Equal("RoleId", rolePk.Properties.Single().Name);
        Assert.Equal(typeof(short), rolePk.Properties.Single().ClrType);

        // int
        var divEntity = _context.Model.FindEntityType(typeof(Division));
        Assert.NotNull(divEntity);
        var divPk = divEntity.FindPrimaryKey();
        Assert.NotNull(divPk);
        Assert.Equal("DivisionId", divPk.Properties.Single().Name);
        Assert.Equal(typeof(int), divPk.Properties.Single().ClrType);

        var secEntity = _context.Model.FindEntityType(typeof(Section));
        Assert.NotNull(secEntity);
        var secPk = secEntity.FindPrimaryKey();
        Assert.NotNull(secPk);
        Assert.Equal("SectionId", secPk.Properties.Single().Name);
        Assert.Equal(typeof(int), secPk.Properties.Single().ClrType);

        // GUID (uniqueidentifier)
        var userEntity = _context.Model.FindEntityType(typeof(User));
        Assert.NotNull(userEntity);
        var userPk = userEntity.FindPrimaryKey();
        Assert.NotNull(userPk);
        Assert.Equal("UserId", userPk.Properties.Single().Name);
        Assert.Equal(typeof(Guid), userPk.Properties.Single().ClrType);

        var sysEntity = _context.Model.FindEntityType(typeof(Domain.Entities.System));
        Assert.NotNull(sysEntity);
        var sysPk = sysEntity.FindPrimaryKey();
        Assert.NotNull(sysPk);
        Assert.Equal("SystemId", sysPk.Properties.Single().Name);
        Assert.Equal(typeof(Guid), sysPk.Properties.Single().ClrType);

        // bigint (long)
        var bigintEntities = new Dictionary<Type, string>
        {
            { typeof(SystemSetting), "SystemSettingId" },
            { typeof(UserSystemAccess), "UserSystemAccessId" },
            { typeof(AdministratorScope), "AdministratorScopeId" },
            { typeof(AdministratorSystemAccess), "AdministratorSystemAccessId" },
            { typeof(RefreshToken), "RefreshTokenId" },
            { typeof(LoginHistory), "LoginHistoryId" },
            { typeof(AuditLog), "AuditLogId" }
        };

        foreach (var (type, pkName) in bigintEntities)
        {
            var entity = _context.Model.FindEntityType(type);
            Assert.NotNull(entity);
            var pk = entity.FindPrimaryKey();
            Assert.NotNull(pk);
            Assert.Equal(pkName, pk.Properties.Single().Name);
            Assert.Equal(typeof(long), pk.Properties.Single().ClrType);
        }
    }

    [Fact]
    public void ModelBuilder_ShouldEnforceUniqueConstraints()
    {
        // 1. Roles.Code
        var roleEntity = _context.Model.FindEntityType(typeof(Role))!;
        Assert.Contains(roleEntity.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "Code" }));

        // 2. Divisions.Code
        var divEntity = _context.Model.FindEntityType(typeof(Division))!;
        Assert.Contains(divEntity.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "Code" }));

        // 3. Sections(DivisionId, Code)
        var secEntity = _context.Model.FindEntityType(typeof(Section))!;
        Assert.Contains(secEntity.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "DivisionId", "Code" }));

        // 4. Users.EmployeeNumber (unique)
        var userEntity = _context.Model.FindEntityType(typeof(User))!;
        Assert.Contains(userEntity.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "EmployeeNumber" }));

        // 5. Users.Position and SectionId nullability
        Assert.True(userEntity.FindProperty("Position")!.IsNullable);
        Assert.True(userEntity.FindProperty("SectionId")!.IsNullable);

        // 6. Users.Username
        Assert.Contains(userEntity.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "Username" }));

        // 7. Systems.SystemCode
        var sysEntity = _context.Model.FindEntityType(typeof(Domain.Entities.System))!;
        Assert.Contains(sysEntity.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "SystemCode" }));

        // 8. SystemSettings(SystemId, SettingKey)
        var setEntity = _context.Model.FindEntityType(typeof(SystemSetting))!;
        Assert.Contains(setEntity.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "SystemId", "SettingKey" }));

        // 9. UserSystemAccess(UserId, SystemId)
        var usaEntity = _context.Model.FindEntityType(typeof(UserSystemAccess))!;
        Assert.Contains(usaEntity.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "UserId", "SystemId" }));

        // 10. AdministratorScopes.AdministratorUserId
        var scopeEntity = _context.Model.FindEntityType(typeof(AdministratorScope))!;
        Assert.Contains(scopeEntity.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "AdministratorUserId" }));

        // 11. AdministratorSystemAccess(AdministratorUserId, SystemId)
        var asaEntity = _context.Model.FindEntityType(typeof(AdministratorSystemAccess))!;
        Assert.Contains(asaEntity.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "AdministratorUserId", "SystemId" }));
    }

    [Fact]
    public void ModelBuilder_ForeignKeys_ShouldUseRestrictDeleteBehavior()
    {
        var entityTypes = _context.Model.GetEntityTypes();
        var allForeignKeys = entityTypes.SelectMany(e => e.GetForeignKeys()).ToList();

        Assert.NotEmpty(allForeignKeys);
        foreach (var fk in allForeignKeys)
        {
            Assert.True(
                fk.DeleteBehavior == DeleteBehavior.Restrict || fk.DeleteBehavior == DeleteBehavior.NoAction,
                $"FK {fk.PrincipalEntityType.DisplayName()} -> {fk.DeclaringEntityType.DisplayName()} has delete behavior {fk.DeleteBehavior} instead of Restrict/NoAction");
        }
    }

    [Fact]
    public void ModelBuilder_SeedData_ShouldContainOnlyThreeCanonicalRoles()
    {
        _context.Database.EnsureCreated();

        var roles = _context.Roles.OrderBy(r => r.RoleId).ToList();
        Assert.Equal(3, roles.Count);

        var sysAdmin = roles.Single(r => r.RoleId == 1);
        Assert.Equal("SYSTEM_ADMINISTRATOR", sysAdmin.Code);
        Assert.Equal("System Administrator", sysAdmin.Name);
        Assert.True(sysAdmin.IsActive);

        var admin = roles.Single(r => r.RoleId == 2);
        Assert.Equal("ADMINISTRATOR", admin.Code);
        Assert.Equal("Administrator", admin.Name);
        Assert.True(admin.IsActive);

        var user = roles.Single(r => r.RoleId == 3);
        Assert.Equal("USER", user.Code);
        Assert.Equal("User", user.Name);
        Assert.True(user.IsActive);
    }

    [Fact]
    public void Enums_ShouldMatchCanonicalDefinitions()
    {
        Assert.Equal(1, (short)RoleType.SystemAdministrator);
        Assert.Equal(2, (short)RoleType.Administrator);
        Assert.Equal(3, (short)RoleType.User);

        Assert.Equal("All", DefaultAccess.All.ToString());
        Assert.Equal("Restricted", DefaultAccess.Restricted.ToString());

        Assert.Equal("Allow", AccessOverride.Allow.ToString());
        Assert.Equal("Deny", AccessOverride.Deny.ToString());
    }
}
