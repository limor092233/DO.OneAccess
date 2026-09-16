using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.DTOs.Access;
using DO.OneAccess.Application.Services;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Infrastructure.Persistence;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class AdminScopeServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly AuditService _auditService;
    private readonly AdminScopeService _adminScopeService;

    private readonly Guid _sysAdminId = Guid.NewGuid();
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _newAdminId = Guid.NewGuid();
    private readonly Guid _systemId = Guid.NewGuid();

    public AdminScopeServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("AdminScopeServiceTestDb_" + Guid.NewGuid())
            .Options;

        _context = new AppDbContext(options);
        _auditService = new AuditService(_context);
        var adminScopeRepo = new DO.OneAccess.Infrastructure.Persistence.Repositories.AdminScopeRepository(_context);
        var adminSystemAccessRepo = new DO.OneAccess.Infrastructure.Persistence.Repositories.AdminSystemAccessRepository(_context);
        var userRepo = new DO.OneAccess.Infrastructure.Persistence.Repositories.UserRepository(_context);
        var divRepo = new DO.OneAccess.Infrastructure.Persistence.Repositories.DivisionRepository(_context);
        var sysRepo = new DO.OneAccess.Infrastructure.Persistence.Repositories.SystemRepository(_context);
        var refreshRepo = new DO.OneAccess.Infrastructure.Persistence.Repositories.RefreshTokenRepository(_context);
        var uow = new DO.OneAccess.Infrastructure.Persistence.UnitOfWork(_context);
        _adminScopeService = new AdminScopeService(
            adminScopeRepo, adminSystemAccessRepo, userRepo, divRepo, sysRepo, refreshRepo, uow, _auditService, _context);

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        _context.Roles.AddRange(
            new Role { RoleId = 1, Code = "SYSTEM_ADMINISTRATOR", Name = "SysAdmin", IsActive = true },
            new Role { RoleId = 2, Code = "ADMINISTRATOR", Name = "Admin", IsActive = true }
        );

        _context.Divisions.Add(new Division { DivisionId = 1, Code = "D1", Name = "Division 1", IsActive = true, CreatedAt = DateTime.UtcNow });

        _context.Users.AddRange(
            new User { UserId = _sysAdminId, EmployeeNumber = "E1", FirstName = "A", LastName = "B", Email = "a@b.com", RoleId = 1, Username = "sa", PasswordHash = "h", IsActive = true, CreatedAt = DateTime.UtcNow },
            new User { UserId = _adminId, EmployeeNumber = "E2", FirstName = "C", LastName = "D", Email = "c@d.com", RoleId = 2, Username = "adm1", PasswordHash = "h", IsActive = true, CreatedAt = DateTime.UtcNow },
            new User { UserId = _newAdminId, EmployeeNumber = "E3", FirstName = "E", LastName = "F", Email = "e@f.com", RoleId = 2, Username = "adm2", PasswordHash = "h", IsActive = true, CreatedAt = DateTime.UtcNow }
        );

        _context.AdministratorScopes.Add(new AdministratorScope
        {
            AdministratorScopeId = 1,
            AdministratorUserId = _adminId,
            DivisionId = 1,
            CreatedAt = DateTime.UtcNow
        });

        _context.Systems.Add(new Domain.Entities.System
        {
            SystemId = _systemId,
            SystemCode = "SYS",
            SystemName = "System",
            DefaultAccess = "Restricted",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        _context.SaveChanges();
    }

    [Fact]
    public async Task AssignDivisionScope_DuplicateScope_ThrowsConflict()
    {
        await Assert.ThrowsAsync<ConflictException>(() =>
            _adminScopeService.AssignDivisionScopeAsync(new AssignAdminScopeDto
            {
                AdministratorUserId = _adminId,
                DivisionId = 1
            }, _sysAdminId));
    }

    [Fact]
    public async Task AssignDivisionScope_NewScope_Succeeds()
    {
        var result = await _adminScopeService.AssignDivisionScopeAsync(new AssignAdminScopeDto
        {
            AdministratorUserId = _newAdminId,
            DivisionId = 1
        }, _sysAdminId);

        Assert.NotNull(result);
        Assert.Equal(_newAdminId, result.AdministratorUserId);
        Assert.Equal(1, result.DivisionId);
    }

    [Fact]
    public async Task AssignDivisionScope_NonSysAdmin_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _adminScopeService.AssignDivisionScopeAsync(new AssignAdminScopeDto
            {
                AdministratorUserId = _newAdminId,
                DivisionId = 1
            }, _adminId));
    }

    [Fact]
    public async Task GrantAdminSystemAccess_SysAdmin_Succeeds()
    {
        var result = await _adminScopeService.GrantAdminSystemAccessAsync(new GrantAdminSystemAccessDto
        {
            AdministratorUserId = _adminId,
            SystemId = _systemId
        }, _sysAdminId);

        Assert.NotNull(result);
        Assert.Equal(_systemId, result.SystemId);
    }

    [Fact]
    public async Task GrantAdminSystemAccess_Duplicate_ThrowsConflict()
    {
        await _adminScopeService.GrantAdminSystemAccessAsync(new GrantAdminSystemAccessDto
        {
            AdministratorUserId = _adminId,
            SystemId = _systemId
        }, _sysAdminId);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _adminScopeService.GrantAdminSystemAccessAsync(new GrantAdminSystemAccessDto
            {
                AdministratorUserId = _adminId,
                SystemId = _systemId
            }, _sysAdminId));
    }

    [Fact]
    public async Task RevokeDivisionScope_SysAdmin_SucceedsAndAtomicallyRevokesRefreshTokens()
    {
        // Add active refresh token for _adminId
        var token = new RefreshToken
        {
            UserId = _adminId,
            TokenHash = "token-hash-revoke",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        // Add active token for unrelated user _newAdminId
        var unrelatedToken = new RefreshToken
        {
            UserId = _newAdminId,
            TokenHash = "unrelated-token-hash",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.AddRange(token, unrelatedToken);
        await _context.SaveChangesAsync();

        await _adminScopeService.RevokeDivisionScopeAsync(1, _sysAdminId);

        var scope = await _context.AdministratorScopes.FirstOrDefaultAsync(s => s.AdministratorScopeId == 1);
        Assert.Null(scope);

        // Verify affected admin's token was revoked
        var updatedToken = await _context.RefreshTokens.FindAsync(token.RefreshTokenId);
        Assert.NotNull(updatedToken);
        Assert.NotNull(updatedToken.RevokedAt);
        Assert.Equal("ADMIN_SCOPE_REVOKED", updatedToken.RevokedByIp);

        // Verify unrelated user's token was NOT revoked
        var updatedUnrelated = await _context.RefreshTokens.FindAsync(unrelatedToken.RefreshTokenId);
        Assert.NotNull(updatedUnrelated);
        Assert.Null(updatedUnrelated.RevokedAt);
    }

    [Fact]
    public async Task AssignDivisionScope_AtomicallyRevokesExistingRefreshTokensForAdmin()
    {
        var token = new RefreshToken
        {
            UserId = _newAdminId,
            TokenHash = "token-hash-assign",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();

        await _adminScopeService.AssignDivisionScopeAsync(new AssignAdminScopeDto
        {
            AdministratorUserId = _newAdminId,
            DivisionId = 1
        }, _sysAdminId);

        var updatedToken = await _context.RefreshTokens.FindAsync(token.RefreshTokenId);
        Assert.NotNull(updatedToken);
        Assert.NotNull(updatedToken.RevokedAt);
        Assert.Equal("ADMIN_SCOPE_ASSIGNED", updatedToken.RevokedByIp);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
