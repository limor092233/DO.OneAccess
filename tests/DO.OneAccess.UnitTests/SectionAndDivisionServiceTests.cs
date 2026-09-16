using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.DTOs.Divisions;
using DO.OneAccess.Application.DTOs.Sections;
using DO.OneAccess.Application.Services;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Infrastructure.Persistence;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class SectionAndDivisionServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly AuditService _auditService;
    private readonly DivisionService _divisionService;
    private readonly SectionService _sectionService;

    private readonly Guid _sysAdminId = Guid.NewGuid();
    private readonly Guid _admin1Id = Guid.NewGuid();

    public SectionAndDivisionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("SecAndDivTestDb_" + Guid.NewGuid())
            .Options;

        _context = new AppDbContext(options);
        _auditService = new AuditService(_context);
        var divRepo = new DO.OneAccess.Infrastructure.Persistence.Repositories.DivisionRepository(_context);
        var uow = new DO.OneAccess.Infrastructure.Persistence.UnitOfWork(_context);
        var fakeQueries = new DO.OneAccess.UnitTests.Fakes.FakeDivisionQueries(new[]
        {
            new DivisionDto { DivisionId = 1, Code = "D1", Name = "Division 1", IsActive = true, CreatedAt = DateTime.UtcNow },
            new DivisionDto { DivisionId = 2, Code = "D2", Name = "Division 2", IsActive = true, CreatedAt = DateTime.UtcNow }
        });
        _divisionService = new DivisionService(divRepo, fakeQueries, uow, _auditService, _context);
        var secRepo = new DO.OneAccess.Infrastructure.Persistence.Repositories.SectionRepository(_context);
        _sectionService = new SectionService(secRepo, divRepo, uow, _auditService, _context);

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        _context.Roles.AddRange(
            new Role { RoleId = 1, Code = "SYSTEM_ADMINISTRATOR", Name = "SysAdmin", IsActive = true },
            new Role { RoleId = 2, Code = "ADMINISTRATOR", Name = "Admin", IsActive = true }
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
            new User { UserId = _sysAdminId, EmployeeNumber = "ES", FirstName = "S", LastName = "A", Email = "sa@t.com", RoleId = 1, Username = "sa", PasswordHash = "h", IsActive = true, CreatedAt = DateTime.UtcNow },
            new User { UserId = _admin1Id, EmployeeNumber = "EA", FirstName = "A", LastName = "B", Email = "ab@t.com", RoleId = 2, Username = "admin1", PasswordHash = "h", IsActive = true, CreatedAt = DateTime.UtcNow }
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
    public async Task CreateSection_AdminCreatingOutsideDivision_ThrowsForbidden()
    {
        // Admin 1 is scoped to Division 1; attempting to create section in Division 2
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sectionService.CreateSectionAsync(new CreateSectionDto
            {
                DivisionId = 2,
                Code = "SNEW",
                Name = "New Section"
            }, _admin1Id));
    }

    [Fact]
    public async Task CreateSection_AdminCreatingWithinDivision_Succeeds()
    {
        var result = await _sectionService.CreateSectionAsync(new CreateSectionDto
        {
            DivisionId = 1,
            Code = "SNEW1",
            Name = "New Section 1"
        }, _admin1Id);

        Assert.NotNull(result);
        Assert.Equal("SNEW1", result.Code);
        Assert.Equal(1, result.DivisionId);
    }

    [Fact]
    public async Task GetDivisionById_AdminRequestingAnotherDivision_ThrowsForbidden()
    {
        // Admin is in Division 1, requesting Division 2 -> 403 Forbidden
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _divisionService.GetDivisionByIdAsync(2, _admin1Id));
    }

    [Fact]
    public async Task GetDivisionById_AdminRequestingOwnDivision_Succeeds()
    {
        var result = await _divisionService.GetDivisionByIdAsync(1, _admin1Id);
        Assert.NotNull(result);
        Assert.Equal(1, result.DivisionId);
    }

    [Fact]
    public async Task CreateDivision_NonSysAdmin_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _divisionService.CreateDivisionAsync(new CreateDivisionDto
            {
                Code = "D3",
                Name = "Division 3"
            }, _admin1Id));
    }

    [Fact]
    public async Task UpdateSection_AdminUpdatingOutsideDivision_ThrowsForbidden()
    {
        // Section 2 is in Division 2
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sectionService.UpdateSectionAsync(2, new UpdateSectionDto { Name = "Hacked" }, _admin1Id));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
