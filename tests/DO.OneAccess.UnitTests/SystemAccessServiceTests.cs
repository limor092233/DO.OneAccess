using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.DTOs.Access;
using DO.OneAccess.Application.Services;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Domain.Enums;
using DO.OneAccess.Infrastructure.Persistence;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class SystemAccessServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly AuditService _auditService;
    private readonly SystemAccessService _systemAccessService;

    private readonly Guid _sysAdminId = Guid.NewGuid();
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _inactiveAdminId = Guid.NewGuid();
    private readonly Guid _user1Id = Guid.NewGuid(); // Division 1
    private readonly Guid _user2Id = Guid.NewGuid(); // Division 2
    private readonly Guid _system1Id = Guid.NewGuid(); // Default Restricted
    private readonly Guid _system2Id = Guid.NewGuid(); // Default All
    private readonly Guid _inactiveSystemId = Guid.NewGuid();

    public SystemAccessServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("SystemAccessServiceTestDb_" + Guid.NewGuid())
            .Options;

        _context = new AppDbContext(options);
        _auditService = new AuditService(_context);
        _systemAccessService = new SystemAccessService(_context, _auditService);

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        var roles = new[]
        {
            new Role { RoleId = 1, Code = "SYSTEM_ADMINISTRATOR", Name = "SysAdmin", IsActive = true },
            new Role { RoleId = 2, Code = "ADMINISTRATOR", Name = "Admin", IsActive = true },
            new Role { RoleId = 3, Code = "USER", Name = "User", IsActive = true }
        };
        _context.Roles.AddRange(roles);

        var div1 = new Division { DivisionId = 1, Code = "DIV1", Name = "Division 1", IsActive = true, CreatedAt = DateTime.UtcNow };
        var div2 = new Division { DivisionId = 2, Code = "DIV2", Name = "Division 2", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Divisions.AddRange(div1, div2);

        var sec1 = new Section { SectionId = 1, DivisionId = 1, Code = "SEC1", Name = "Section 1", IsActive = true, CreatedAt = DateTime.UtcNow };
        var sec2 = new Section { SectionId = 2, DivisionId = 2, Code = "SEC2", Name = "Section 2", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Sections.AddRange(sec1, sec2);

        var sysAdmin = new User { UserId = _sysAdminId, EmployeeNumber = "E00", FirstName = "Sys", LastName = "Admin", Email = "sa@test.com", Position = "SA", SectionId = null, RoleId = 1, Username = "sysadmin", PasswordHash = "hash", IsActive = true, CreatedAt = DateTime.UtcNow };
        var admin = new User { UserId = _adminId, EmployeeNumber = "E01", FirstName = "Div1", LastName = "Admin", Email = "admin@test.com", Position = "Admin", SectionId = null, RoleId = 2, Username = "admin1", PasswordHash = "hash", IsActive = true, CreatedAt = DateTime.UtcNow };
        var inactiveAdmin = new User { UserId = _inactiveAdminId, EmployeeNumber = "E01B", FirstName = "Inactive", LastName = "Admin", Email = "inadmin@test.com", Position = "Admin", SectionId = null, RoleId = 2, Username = "inadmin", PasswordHash = "hash", IsActive = false, CreatedAt = DateTime.UtcNow };
        var user1 = new User { UserId = _user1Id, EmployeeNumber = "E02", FirstName = "User", LastName = "One", Email = "u1@test.com", Position = "Staff", SectionId = 1, RoleId = 3, Username = "user1", PasswordHash = "hash", IsActive = true, CreatedAt = DateTime.UtcNow };
        var user2 = new User { UserId = _user2Id, EmployeeNumber = "E03", FirstName = "User", LastName = "Two", Email = "u2@test.com", Position = "Staff", SectionId = 2, RoleId = 3, Username = "user2", PasswordHash = "hash", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Users.AddRange(sysAdmin, admin, inactiveAdmin, user1, user2);

        // Admin 1 scope: Division 1
        _context.AdministratorScopes.Add(new AdministratorScope
        {
            AdministratorScopeId = 1,
            AdministratorUserId = _adminId,
            DivisionId = 1,
            CreatedAt = DateTime.UtcNow
        });

        // Admin 1 system management grant for System 1
        _context.AdministratorSystemAccess.Add(new AdministratorSystemAccess
        {
            AdministratorSystemAccessId = 1,
            AdministratorUserId = _adminId,
            SystemId = _system1Id,
            CreatedAt = DateTime.UtcNow
        });

        var sys1 = new Domain.Entities.System { SystemId = _system1Id, SystemCode = "SYS1", SystemName = "System 1", DefaultAccess = "Restricted", IsActive = true, CreatedAt = DateTime.UtcNow };
        var sys2 = new Domain.Entities.System { SystemId = _system2Id, SystemCode = "SYS2", SystemName = "System 2", DefaultAccess = "All", IsActive = true, CreatedAt = DateTime.UtcNow };
        var sysInactive = new Domain.Entities.System { SystemId = _inactiveSystemId, SystemCode = "SYS3", SystemName = "System 3", DefaultAccess = "All", IsActive = false, CreatedAt = DateTime.UtcNow };
        _context.Systems.AddRange(sys1, sys2, sysInactive);

        _context.SaveChanges();
    }

    [Fact]
    public async Task ResolveAccess_InactiveUser_ReturnsDeny()
    {
        var result = await _systemAccessService.ResolveAccessAsync(_inactiveAdminId, _system2Id);
        Assert.False(result.IsGranted);
        Assert.Contains("inactive", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolveAccess_InactiveSystem_ReturnsDeny()
    {
        var result = await _systemAccessService.ResolveAccessAsync(_user1Id, _inactiveSystemId);
        Assert.False(result.IsGranted);
        Assert.Contains("inactive", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolveAccess_NoOverride_DefaultAccessAll_ReturnsGrant()
    {
        var result = await _systemAccessService.ResolveAccessAsync(_user1Id, _system2Id);
        Assert.True(result.IsGranted);
    }

    [Fact]
    public async Task ResolveAccess_NoOverride_DefaultAccessRestricted_ReturnsDeny()
    {
        var result = await _systemAccessService.ResolveAccessAsync(_user1Id, _system1Id);
        Assert.False(result.IsGranted);
    }

    [Fact]
    public async Task ResolveAccess_ExplicitAllowOverride_OverridesRestricted()
    {
        _context.UserSystemAccess.Add(new UserSystemAccess
        {
            UserId = _user1Id,
            SystemId = _system1Id,
            AccessType = "Allow",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _systemAccessService.ResolveAccessAsync(_user1Id, _system1Id);
        Assert.True(result.IsGranted);
    }

    [Fact]
    public async Task ResolveAccess_ExplicitDenyOverride_OverridesAll()
    {
        _context.UserSystemAccess.Add(new UserSystemAccess
        {
            UserId = _user1Id,
            SystemId = _system2Id,
            AccessType = "Deny",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _systemAccessService.ResolveAccessAsync(_user1Id, _system2Id);
        Assert.False(result.IsGranted);
    }

    [Fact]
    public async Task GetUserAccessibleSystems_ReturnsOnlyPermittedActiveSystems()
    {
        // Give explicit Allow to system 1 (default restricted)
        _context.UserSystemAccess.Add(new UserSystemAccess
        {
            UserId = _user1Id,
            SystemId = _system1Id,
            AccessType = "Allow",
            CreatedAt = DateTime.UtcNow
        });
        // Give explicit Deny to system 2 (default all)
        _context.UserSystemAccess.Add(new UserSystemAccess
        {
            UserId = _user1Id,
            SystemId = _system2Id,
            AccessType = "Deny",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var systems = await _systemAccessService.GetUserAccessibleSystemsAsync(_user1Id);
        Assert.Single(systems);
        Assert.Equal(_system1Id, systems[0].SystemId);
    }

    [Fact]
    public async Task SetUserSystemAccess_Upsert_UpdatesExistingRecord()
    {
        // First insert
        var created = await _systemAccessService.SetUserSystemAccessAsync(new SetUserSystemAccessDto
        {
            UserId = _user1Id,
            SystemId = _system1Id,
            AccessType = "Allow"
        }, _adminId);

        Assert.Equal("Allow", created.AccessType);

        // Upsert update
        var updated = await _systemAccessService.SetUserSystemAccessAsync(new SetUserSystemAccessDto
        {
            UserId = _user1Id,
            SystemId = _system1Id,
            AccessType = "Deny"
        }, _adminId);

        Assert.Equal("Deny", updated.AccessType);
        Assert.Equal(created.UserSystemAccessId, updated.UserSystemAccessId);

        // Verify database has exactly one record
        var count = await _context.UserSystemAccess.CountAsync(a => a.UserId == _user1Id && a.SystemId == _system1Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SetUserSystemAccess_AdminCrossingDivisionBoundary_ThrowsForbidden()
    {
        // Admin is Division 1; User 2 is Division 2
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _systemAccessService.SetUserSystemAccessAsync(new SetUserSystemAccessDto
            {
                UserId = _user2Id,
                SystemId = _system1Id,
                AccessType = "Allow"
            }, _adminId));
    }

    [Fact]
    public async Task SetUserSystemAccess_AdminWithoutSystemGrant_ThrowsForbidden()
    {
        // Admin has no grant for System 2
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _systemAccessService.SetUserSystemAccessAsync(new SetUserSystemAccessDto
            {
                UserId = _user1Id,
                SystemId = _system2Id,
                AccessType = "Allow"
            }, _adminId));
    }

    [Fact]
    public async Task SetUserSystemAccess_InactiveAdmin_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _systemAccessService.SetUserSystemAccessAsync(new SetUserSystemAccessDto
            {
                UserId = _user1Id,
                SystemId = _system1Id,
                AccessType = "Allow"
            }, _inactiveAdminId));
    }

    [Fact]
    public async Task SetUserSystemAccess_SysAdmin_GlobalScope_Succeeds()
    {
        // SysAdmin can manage User 2 (Division 2) on System 2 without explicit grants
        var result = await _systemAccessService.SetUserSystemAccessAsync(new SetUserSystemAccessDto
        {
            UserId = _user2Id,
            SystemId = _system2Id,
            AccessType = "Allow"
        }, _sysAdminId);

        Assert.NotNull(result);
        Assert.Equal("Allow", result.AccessType);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
