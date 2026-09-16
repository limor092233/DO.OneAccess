using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.DTOs.Users;
using DO.OneAccess.Application.Services;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Infrastructure.Persistence;
using DO.OneAccess.Infrastructure.Security;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class UserServiceAuthorizationTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly AuditService _auditService;
    private readonly PasswordHasher _passwordHasher;
    private readonly UserService _userService;

    private readonly Guid _sysAdminId = Guid.NewGuid();
    private readonly Guid _admin1Id = Guid.NewGuid();
    private readonly Guid _userDiv1Id = Guid.NewGuid();
    private readonly Guid _userDiv2Id = Guid.NewGuid();

    public UserServiceAuthorizationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("UserServiceAuthTestDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AppDbContext(options);
        _auditService = new AuditService(_context);
        _passwordHasher = new PasswordHasher();
        _userService = new UserService(_context, _auditService, _passwordHasher);

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        _context.Roles.AddRange(
            new Role { RoleId = 1, Code = "SYSTEM_ADMINISTRATOR", Name = "SysAdmin", IsActive = true },
            new Role { RoleId = 2, Code = "ADMINISTRATOR", Name = "Admin", IsActive = true },
            new Role { RoleId = 3, Code = "USER", Name = "User", IsActive = true }
        );

        _context.Divisions.AddRange(
            new Division { DivisionId = 1, Code = "D1", Name = "Division 1", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Division { DivisionId = 2, Code = "D2", Name = "Division 2", IsActive = true, CreatedAt = DateTime.UtcNow }
        );

        _context.Sections.AddRange(
            new Section { SectionId = 1, DivisionId = 1, Code = "S1", Name = "Section 1", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Section { SectionId = 2, DivisionId = 2, Code = "S2", Name = "Section 2", IsActive = true, CreatedAt = DateTime.UtcNow }
        );

        _context.Users.AddRange(
            new User
            {
                UserId = _sysAdminId,
                EmployeeNumber = "ES",
                FirstName = "Sys",
                LastName = "Admin",
                Email = "sa@t.com",
                Position = null,
                SectionId = null,
                RoleId = 1,
                Username = "sa",
                PasswordHash = "h",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                UserId = _admin1Id,
                EmployeeNumber = "EA",
                FirstName = "Admin",
                LastName = "One",
                Email = "ab@t.com",
                Position = "Manager",
                SectionId = null,
                RoleId = 2,
                Username = "admin1",
                PasswordHash = "h",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                UserId = _userDiv1Id,
                EmployeeNumber = "EU1",
                FirstName = "User",
                LastName = "One",
                Email = "u1@t.com",
                Position = "Analyst",
                SectionId = 1,
                RoleId = 3,
                Username = "user1",
                PasswordHash = "h",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                UserId = _userDiv2Id,
                EmployeeNumber = "EU2",
                FirstName = "User",
                LastName = "Two",
                Email = "u2@t.com",
                Position = "Developer",
                SectionId = 2,
                RoleId = 3,
                Username = "user2",
                PasswordHash = "h",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );

        _context.AdministratorScopes.Add(new AdministratorScope
        {
            AdministratorScopeId = 1,
            AdministratorUserId = _admin1Id,
            DivisionId = 1,
            CreatedAt = DateTime.UtcNow
        });

        _context.SaveChanges();
    }

    [Fact]
    public async Task ChangeRole_AdminActor_ThrowsForbidden()
    {
        // Rule: Administrator cannot perform role changes. Only System Administrator may change roles.
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _userService.ChangeRoleAsync(_userDiv1Id, 2, _admin1Id));
    }

    [Fact]
    public async Task ChangeRole_SelfRoleChange_ThrowsForbidden()
    {
        // Rule: Self-role modification is not permitted
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _userService.ChangeRoleAsync(_sysAdminId, 2, _sysAdminId));
    }

    [Fact]
    public async Task ChangeRole_SysAdmin_Succeeds()
    {
        await _userService.ChangeRoleAsync(_userDiv1Id, 2, _sysAdminId);

        var user = await _context.Users.FindAsync(_userDiv1Id);
        Assert.NotNull(user);
        Assert.Equal((short)2, user.RoleId);
    }

    [Fact]
    public async Task CreateUser_AdminCreatingAdminRole_ThrowsForbidden()
    {
        // Rule: Administrator can only create User role
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _userService.CreateUserAsync(new CreateUserDto
            {
                EmployeeNumber = "ENew1",
                FirstName = "New",
                LastName = "Admin",
                Email = "newadmin@t.com",
                SectionId = 1,
                RoleId = 2, // Attempting Administrator role
                Username = "newadmin",
                Password = "Password123!"
            }, _admin1Id));
    }

    [Fact]
    public async Task CreateUser_NormalEndpointAttemptingSysAdmin_ThrowsForbidden()
    {
        // Rule: Normal POST /api/users must NEVER create a System Administrator
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _userService.CreateUserAsync(new CreateUserDto
            {
                EmployeeNumber = "ENewSA",
                FirstName = "New",
                LastName = "SA",
                Email = "newsa@t.com",
                RoleId = 1, // Attempting SysAdmin role
                Username = "newsysadmin",
                Password = "Password123!"
            }, _sysAdminId));
    }

    [Fact]
    public async Task CreateUser_SysAdminCreatingAdminRole_Succeeds()
    {
        var result = await _userService.CreateUserAsync(new CreateUserDto
        {
            EmployeeNumber = "EAdminNew",
            FirstName = "Sys",
            LastName = "CreatedAdmin",
            Email = "syscreatedadmin@t.com",
            RoleId = 2,
            DivisionId = 1,
            Username = "syscreatedadmin",
            Password = "Password123!"
        }, _sysAdminId);

        Assert.NotNull(result);
        Assert.Equal((short)2, result.RoleId);
        Assert.Null(result.SectionId);

        var scopes = await _context.AdministratorScopes.Where(s => s.AdministratorUserId == result.UserId).ToListAsync();
        Assert.Single(scopes);
        Assert.Equal(1, scopes[0].DivisionId);
    }

    [Fact]
    public async Task CreateUser_AdminCreatingUserOutsideDivision_ThrowsForbidden()
    {
        // Section 2 belongs to Division 2, Admin 1 is scoped to Division 1
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _userService.CreateUserAsync(new CreateUserDto
            {
                EmployeeNumber = "EDiv2User",
                FirstName = "Div2",
                LastName = "User",
                Email = "div2user@t.com",
                DivisionId = 2,
                SectionId = 2,
                RoleId = 3,
                Username = "newdiv2user",
                Password = "Password123!"
            }, _admin1Id));
    }

    [Fact]
    public async Task CreateUser_AdminCreatingUserInsideDivision_Succeeds()
    {
        // Section 1 belongs to Division 1
        var result = await _userService.CreateUserAsync(new CreateUserDto
        {
            EmployeeNumber = "EDiv1User",
            FirstName = "Div1",
            LastName = "User",
            Email = "div1user@t.com",
            DivisionId = 1,
            SectionId = 1,
            RoleId = 3,
            Username = "newdiv1user",
            Password = "Password123!"
        }, _admin1Id);

        Assert.NotNull(result);
        Assert.Equal("newdiv1user", result.Username);
        Assert.Equal(1, result.SectionId);
    }

    [Fact]
    public async Task CreateUser_UserRoleWithoutSection_ThrowsValidation()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.CreateUserAsync(new CreateUserDto
            {
                EmployeeNumber = "ENoSec",
                FirstName = "No",
                LastName = "Section",
                Email = "nosec@t.com",
                SectionId = null,
                RoleId = 3,
                Username = "nosecuser",
                Password = "Password123!"
            }, _sysAdminId));
    }

    [Fact]
    public async Task TransferSystemAdministrator_SysAdminActor_Succeeds()
    {
        await _userService.TransferSystemAdministratorAsync(new TransferSystemAdministratorDto
        {
            TargetUserId = _userDiv1Id,
            PreviousAdminNewRoleId = 2,
            PreviousAdminDivisionId = 1
        }, _sysAdminId);

        // Database validation: Target is SysAdmin
        var dbTarget = await _context.Users.FindAsync(_userDiv1Id);
        Assert.NotNull(dbTarget);
        Assert.Equal((short)1, dbTarget.RoleId);
        Assert.Null(dbTarget.SectionId);

        // Database validation: Previous SysAdmin is Administrator
        var dbPrevious = await _context.Users.FindAsync(_sysAdminId);
        Assert.NotNull(dbPrevious);
        Assert.Equal((short)2, dbPrevious.RoleId);
        Assert.Null(dbPrevious.SectionId);

        // Administrator scope assigned to previous SysAdmin
        var prevScopes = await _context.AdministratorScopes.Where(s => s.AdministratorUserId == _sysAdminId).ToListAsync();
        Assert.Single(prevScopes);
        Assert.Equal(1, prevScopes[0].DivisionId);

        // Invariant: Exactly one System Administrator in DB
        var sysAdminCount = await _context.Users.CountAsync(u => u.RoleId == 1);
        Assert.Equal(1, sysAdminCount);
    }

    [Fact]
    public async Task TransferSystemAdministrator_AdminActor_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _userService.TransferSystemAdministratorAsync(new TransferSystemAdministratorDto
            {
                TargetUserId = _userDiv1Id,
                PreviousAdminNewRoleId = 3,
                PreviousAdminDivisionId = 1,
                PreviousAdminSectionId = 1
            }, _admin1Id));
    }

    [Fact]
    public async Task TransferSystemAdministrator_TargetIsSelf_ThrowsValidation()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.TransferSystemAdministratorAsync(new TransferSystemAdministratorDto
            {
                TargetUserId = _sysAdminId,
                PreviousAdminNewRoleId = 2,
                PreviousAdminDivisionId = 1
            }, _sysAdminId));
    }

    [Fact]
    public async Task GetUserById_AdminRequestingUserInAnotherDivision_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _userService.GetUserByIdAsync(_userDiv2Id, _admin1Id));
    }

    [Fact]
    public async Task DeactivateUser_SelfDeactivation_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _userService.DeactivateUserAsync(_admin1Id, _admin1Id));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
