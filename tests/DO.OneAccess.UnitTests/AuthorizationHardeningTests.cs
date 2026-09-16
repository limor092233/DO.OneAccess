using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.Common.Security;
using DO.OneAccess.Application.DTOs.Access;
using DO.OneAccess.Application.DTOs.Users;
using DO.OneAccess.Application.Services;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Domain.Enums;
using DO.OneAccess.Infrastructure.Persistence;
using DO.OneAccess.Infrastructure.Security;
using Xunit;

namespace DO.OneAccess.UnitTests;

/// <summary>
/// Comprehensive authorization hardening tests verifying fine-grained 4-layer access control:
/// 1. Role Permission Check (WHAT)
/// 2. Administrator Scope Check (WHERE)
/// 3. Division / Section Active Status Check
/// 4. Resource Access
/// </summary>
public class AuthorizationHardeningTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly AuditService _auditService;
    private readonly PasswordHasher _passwordHasher;
    private readonly UserService _userService;
    private readonly AdminScopeService _adminScopeService;

    private readonly Guid _sysAdminId = Guid.NewGuid();
    private readonly Guid _validAdminId = Guid.NewGuid();
    private readonly Guid _adminWithoutScopeId = Guid.NewGuid();
    private readonly Guid _adminInactiveDivId = Guid.NewGuid();
    private readonly Guid _userDiv1Id = Guid.NewGuid();
    private readonly Guid _userWithResidualScopeId = Guid.NewGuid();

    public AuthorizationHardeningTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("AuthHardeningTestDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AppDbContext(options);
        _auditService = new AuditService(_context);
        _passwordHasher = new PasswordHasher();
        _userService = new UserService(_context, _auditService, _passwordHasher);
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
        // 1. Seed Roles
        _context.Roles.AddRange(
            new Role { RoleId = (short)RoleType.SystemAdministrator, Code = "SYSTEM_ADMINISTRATOR", Name = "System Administrator", IsActive = true },
            new Role { RoleId = (short)RoleType.Administrator, Code = "ADMINISTRATOR", Name = "Administrator", IsActive = true },
            new Role { RoleId = (short)RoleType.User, Code = "USER", Name = "User", IsActive = true }
        );

        // 2. Seed Divisions: Division 1 (Active), Division 2 (Active), Division 3 (Inactive)
        _context.Divisions.AddRange(
            new Division { DivisionId = 1, Code = "DIV1", Name = "Division 1 Active", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Division { DivisionId = 2, Code = "DIV2", Name = "Division 2 Active", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Division { DivisionId = 3, Code = "DIV3", Name = "Division 3 Inactive", IsActive = false, CreatedAt = DateTime.UtcNow }
        );

        // 3. Seed Sections:
        // Division 1: Section 1 (Active), Section 2 (Inactive)
        // Division 2: Section 3 (Active)
        // Division 3 (Inactive parent): Section 4 (Active flag but inactive parent division)
        _context.Sections.AddRange(
            new Section { SectionId = 1, DivisionId = 1, Code = "SEC1", Name = "D1 Active Section 1", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Section { SectionId = 2, DivisionId = 1, Code = "SEC2", Name = "D1 Inactive Section 2", IsActive = false, CreatedAt = DateTime.UtcNow },
            new Section { SectionId = 3, DivisionId = 2, Code = "SEC3", Name = "D2 Active Section 3", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Section { SectionId = 4, DivisionId = 3, Code = "SEC4", Name = "D3 Active Section under Inactive Div", IsActive = true, CreatedAt = DateTime.UtcNow }
        );

        // 4. Seed Users
        _context.Users.AddRange(
            new User
            {
                UserId = _sysAdminId,
                EmployeeNumber = "SA001",
                FirstName = "System",
                LastName = "Administrator",
                Email = "sysadmin@test.com",
                RoleId = (short)RoleType.SystemAdministrator,
                Username = "sysadmin",
                PasswordHash = "hash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                UserId = _validAdminId,
                EmployeeNumber = "AD001",
                FirstName = "Valid",
                LastName = "Admin",
                Email = "validadmin@test.com",
                RoleId = (short)RoleType.Administrator,
                Username = "validadmin",
                PasswordHash = "hash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                UserId = _adminWithoutScopeId,
                EmployeeNumber = "AD002",
                FirstName = "Scopeless",
                LastName = "Admin",
                Email = "scopeless@test.com",
                RoleId = (short)RoleType.Administrator,
                Username = "scopelessadmin",
                PasswordHash = "hash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                UserId = _adminInactiveDivId,
                EmployeeNumber = "AD003",
                FirstName = "InactiveDiv",
                LastName = "Admin",
                Email = "inactivediv@test.com",
                RoleId = (short)RoleType.Administrator,
                Username = "inactivedivadmin",
                PasswordHash = "hash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                UserId = _userDiv1Id,
                EmployeeNumber = "US001",
                FirstName = "Standard",
                LastName = "User",
                Email = "user1@test.com",
                SectionId = 1,
                RoleId = (short)RoleType.User,
                Username = "user1",
                PasswordHash = "hash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                UserId = _userWithResidualScopeId,
                EmployeeNumber = "US002",
                FirstName = "Residual",
                LastName = "ScopeUser",
                Email = "residual@test.com",
                SectionId = 1,
                RoleId = (short)RoleType.User,
                Username = "residualuser",
                PasswordHash = "hash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );

        // 5. Seed Scopes
        _context.AdministratorScopes.AddRange(
            // Valid admin scoped to Division 1 (Active)
            new AdministratorScope
            {
                AdministratorScopeId = 10,
                AdministratorUserId = _validAdminId,
                DivisionId = 1,
                CreatedAt = DateTime.UtcNow
            },
            // Admin scoped to Division 3 (Inactive)
            new AdministratorScope
            {
                AdministratorScopeId = 20,
                AdministratorUserId = _adminInactiveDivId,
                DivisionId = 3,
                CreatedAt = DateTime.UtcNow
            },
            // Standard User having a residual scope record
            new AdministratorScope
            {
                AdministratorScopeId = 30,
                AdministratorUserId = _userWithResidualScopeId,
                DivisionId = 1,
                CreatedAt = DateTime.UtcNow
            }
        );

        _context.SaveChanges();
    }

    /// <summary>
    /// Scenario 1: Administrator with valid active Division scope succeeds.
    /// </summary>
    [Fact]
    public async Task Scenario01_AdministratorWithValidActiveDivisionScope_Succeeds()
    {
        var scopedDivisionId = await AuthorizationHelper.RequireAdminOrAboveScopeAsync(_context, _validAdminId);
        Assert.NotNull(scopedDivisionId);
        Assert.Equal(1, scopedDivisionId.Value);

        // Scope check against their division succeeds without exception
        await AuthorizationHelper.ValidateDivisionScopeAsync(_context, _validAdminId, targetDivisionId: 1);
    }

    /// <summary>
    /// Scenario 2: Administrator without Division scope is denied (HTTP 403).
    /// </summary>
    [Fact]
    public async Task Scenario02_AdministratorWithoutDivisionScope_ThrowsForbidden()
    {
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            AuthorizationHelper.RequireAdminOrAboveScopeAsync(_context, _adminWithoutScopeId));

        Assert.Contains("Administrator scope not assigned", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Scenario 3: Administrator assigned to inactive Division is denied (HTTP 403).
    /// </summary>
    [Fact]
    public async Task Scenario03_AdministratorAssignedToInactiveDivision_ThrowsForbidden()
    {
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            AuthorizationHelper.RequireAdminOrAboveScopeAsync(_context, _adminInactiveDivId));

        Assert.Contains("Assigned division is inactive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Scenario 4: Administrator accessing another Division is denied (HTTP 403).
    /// </summary>
    [Fact]
    public async Task Scenario04_AdministratorAccessingAnotherDivision_ThrowsForbidden()
    {
        // _validAdminId is scoped to Division 1, attempts to access Division 2
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            AuthorizationHelper.ValidateDivisionScopeAsync(_context, _validAdminId, targetDivisionId: 2));

        Assert.Contains("outside administrator's assigned division scope", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Scenario 5: Administrator assigning a user to an inactive Section is rejected with validation error.
    /// </summary>
    [Fact]
    public async Task Scenario05_AdministratorAssigningUserToInactiveSection_ThrowsValidation()
    {
        // Section 2 is in Division 1 (permitted), but Section 2 is inactive
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.UpdateUserAsync(_userDiv1Id, new UpdateUserDto { SectionId = 2 }, _validAdminId));

        Assert.True(ex.Errors.ContainsKey(nameof(UpdateUserDto.SectionId)));
        Assert.Contains(ex.Errors[nameof(UpdateUserDto.SectionId)], err => err.Contains("inactive", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Scenario 6: Administrator assigning a user outside the permitted Division is rejected (HTTP 403).
    /// </summary>
    [Fact]
    public async Task Scenario06_AdministratorAssigningUserOutsidePermittedDivision_ThrowsForbidden()
    {
        // Section 3 is in Division 2, but _validAdminId is scoped only to Division 1
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            _userService.UpdateUserAsync(_userDiv1Id, new UpdateUserDto { SectionId = 3 }, _validAdminId));

        Assert.Contains("outside administrator's assigned division scope", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Scenario 7: Scope revocation followed by an access attempt revokes refresh tokens and denies access.
    /// </summary>
    [Fact]
    public async Task Scenario07_ScopeRevocation_FollowedByAccessAttempt_RevokesRefreshTokenAndThrowsForbidden()
    {
        // Add active refresh token for the valid administrator
        var token = new RefreshToken
        {
            UserId = _validAdminId,
            TokenHash = "valid-admin-token-hash",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();

        // 1. SysAdmin revokes the scope
        await _adminScopeService.RevokeDivisionScopeAsync(adminScopeId: 10, _sysAdminId);

        // 2. Verify token revocation occurred atomically for this administrator
        var updatedToken = await _context.RefreshTokens.FindAsync(token.RefreshTokenId);
        Assert.NotNull(updatedToken);
        Assert.NotNull(updatedToken.RevokedAt);
        Assert.Equal("ADMIN_SCOPE_REVOKED", updatedToken.RevokedByIp);

        // 3. Subsequent access attempt by the administrator fails immediately against database
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            AuthorizationHelper.RequireAdminOrAboveScopeAsync(_context, _validAdminId));

        Assert.Contains("Administrator scope not assigned", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Scenario 8: Role permission granted (Administrator) but Division scope missing is denied.
    /// </summary>
    [Fact]
    public async Task Scenario08_RolePermissionGranted_ButDivisionScopeMissing_ThrowsForbidden()
    {
        // User has RoleType.Administrator (Role permission passes WHAT check)
        var user = await _context.Users.FindAsync(_adminWithoutScopeId);
        Assert.NotNull(user);
        Assert.Equal((short)RoleType.Administrator, user.RoleId);

        // But AdministratorScope check fails (WHERE check fails)
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            AuthorizationHelper.RequireAdminOrAboveScopeAsync(_context, _adminWithoutScopeId));

        Assert.Contains("Administrator scope not assigned", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Scenario 9: Division scope granted but Role permission missing is denied.
    /// </summary>
    [Fact]
    public async Task Scenario09_DivisionScopeGranted_ButRolePermissionMissing_ThrowsForbidden()
    {
        // User has RoleType.User (Role permission fails WHAT check for admin operations)
        var user = await _context.Users.FindAsync(_userWithResidualScopeId);
        Assert.NotNull(user);
        Assert.Equal((short)RoleType.User, user.RoleId);

        // Even though user has a residual AdministratorScope record in DB
        var residualScope = await _context.AdministratorScopes
            .FirstOrDefaultAsync(s => s.AdministratorUserId == _userWithResidualScopeId);
        Assert.NotNull(residualScope);

        // Attempting admin-scoped operation fails at Role check
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            AuthorizationHelper.RequireAdminOrAboveScopeAsync(_context, _userWithResidualScopeId));

        Assert.Contains("Operation requires administrative privileges", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Scenario 10: Both Role Permission and Division Scope granted succeeds.
    /// </summary>
    [Fact]
    public async Task Scenario10_BothRolePermissionAndDivisionScopeGranted_Succeeds()
    {
        // User has RoleType.Administrator (WHAT)
        var user = await _context.Users.FindAsync(_validAdminId);
        Assert.NotNull(user);
        Assert.Equal((short)RoleType.Administrator, user.RoleId);

        // User has valid DivisionScope on active Division 1 (WHERE)
        var scope = await _context.AdministratorScopes
            .FirstOrDefaultAsync(s => s.AdministratorUserId == _validAdminId);
        Assert.NotNull(scope);
        Assert.Equal(1, scope.DivisionId);

        // Both checks pass -> returns division id
        var resolvedDivisionId = await AuthorizationHelper.RequireAdminOrAboveScopeAsync(_context, _validAdminId);
        Assert.Equal(1, resolvedDivisionId);
    }

    /// <summary>
    /// Scenario 11: Active Section under inactive parent Division is rejected with validation error.
    /// </summary>
    [Fact]
    public async Task Scenario11_ActiveSectionUnderInactiveParentDivision_ThrowsValidation()
    {
        // Section 4 has IsActive = true, but its parent Division 3 has IsActive = false
        var section = await _context.Sections.FindAsync(4);
        Assert.NotNull(section);
        Assert.True(section.IsActive);

        var division = await _context.Divisions.FindAsync(section.DivisionId);
        Assert.NotNull(division);
        Assert.False(division.IsActive);

        // SysAdmin attempting to assign Section 4 to a user fails due to inactive parent division
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.UpdateUserAsync(_userDiv1Id, new UpdateUserDto { SectionId = 4 }, _sysAdminId));

        Assert.True(ex.Errors.ContainsKey(nameof(UpdateUserDto.SectionId)));
        Assert.Contains(ex.Errors[nameof(UpdateUserDto.SectionId)], err => err.Contains("parent division is inactive", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
