using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.DTOs.Systems;
using DO.OneAccess.Application.Services;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Infrastructure.Persistence;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class SystemServiceAuthorizationTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly AuditService _auditService;
    private readonly SystemService _systemService;

    private readonly Guid _sysAdminId = Guid.NewGuid();
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _systemGrantedId = Guid.NewGuid();
    private readonly Guid _systemNotGrantedId = Guid.NewGuid();

    public SystemServiceAuthorizationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("SystemServiceAuthTestDb_" + Guid.NewGuid())
            .Options;

        _context = new AppDbContext(options);
        _auditService = new AuditService(_context);
        _systemService = new SystemService(_context, _auditService);

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        _context.Roles.AddRange(
            new Role { RoleId = 1, Code = "SYSTEM_ADMINISTRATOR", Name = "SysAdmin", IsActive = true },
            new Role { RoleId = 2, Code = "ADMINISTRATOR", Name = "Admin", IsActive = true }
        );

        _context.Divisions.Add(new Division { DivisionId = 1, Code = "D1", Name = "Division 1", IsActive = true, CreatedAt = DateTime.UtcNow });
        _context.Sections.Add(new Section { SectionId = 1, DivisionId = 1, Code = "S1", Name = "Section 1", IsActive = true, CreatedAt = DateTime.UtcNow });

        _context.Users.AddRange(
            new User { UserId = _sysAdminId, EmployeeNumber = "ES", FirstName = "S", LastName = "A", Email = "sa@t.com", RoleId = 1, Username = "sa", PasswordHash = "h", IsActive = true, CreatedAt = DateTime.UtcNow },
            new User { UserId = _adminId, EmployeeNumber = "EA", FirstName = "A", LastName = "B", Email = "ab@t.com", RoleId = 2, Username = "adm", PasswordHash = "h", IsActive = true, CreatedAt = DateTime.UtcNow }
        );

        _context.AdministratorScopes.Add(new AdministratorScope
        {
            AdministratorScopeId = 1,
            AdministratorUserId = _adminId,
            DivisionId = 1,
            CreatedAt = DateTime.UtcNow
        });

        _context.AdministratorSystemAccess.Add(new AdministratorSystemAccess
        {
            AdministratorSystemAccessId = 1,
            AdministratorUserId = _adminId,
            SystemId = _systemGrantedId,
            CreatedAt = DateTime.UtcNow
        });

        _context.Systems.AddRange(
            new Domain.Entities.System { SystemId = _systemGrantedId, SystemCode = "SG", SystemName = "System Granted", DefaultAccess = "Restricted", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Domain.Entities.System { SystemId = _systemNotGrantedId, SystemCode = "SNG", SystemName = "System Not Granted", DefaultAccess = "Restricted", IsActive = true, CreatedAt = DateTime.UtcNow }
        );

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetSystemById_AdminWithoutSystemGrant_ThrowsForbidden()
    {
        // Administrator cannot manage Systems without AdministratorSystemAccess
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _systemService.GetSystemByIdAsync(_systemNotGrantedId, _adminId));
    }

    [Fact]
    public async Task GetSystemById_AdminWithSystemGrant_Succeeds()
    {
        var result = await _systemService.GetSystemByIdAsync(_systemGrantedId, _adminId);
        Assert.NotNull(result);
        Assert.Equal(_systemGrantedId, result.SystemId);
    }

    [Fact]
    public async Task GetSystemById_SysAdmin_GlobalScope_SucceedsWithoutGrant()
    {
        var result = await _systemService.GetSystemByIdAsync(_systemNotGrantedId, _sysAdminId);
        Assert.NotNull(result);
        Assert.Equal(_systemNotGrantedId, result.SystemId);
    }

    [Fact]
    public async Task RegisterSystem_NonSysAdmin_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _systemService.RegisterSystemAsync(new RegisterSystemDto
            {
                SystemCode = "NEW",
                SystemName = "New System",
                DefaultAccess = "Restricted"
            }, _adminId));
    }

    [Fact]
    public async Task RegisterSystem_SysAdmin_Succeeds()
    {
        var result = await _systemService.RegisterSystemAsync(new RegisterSystemDto
        {
            SystemCode = "NEW",
            SystemName = "New System",
            DefaultAccess = "Restricted"
        }, _sysAdminId);

        Assert.NotNull(result);
        Assert.Equal("NEW", result.SystemCode);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
