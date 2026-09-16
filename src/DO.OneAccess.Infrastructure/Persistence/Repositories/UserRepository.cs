using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Interfaces.Persistence;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Domain.Enums;

namespace DO.OneAccess.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<User?> GetByIdAsync(Guid userId, bool includeNavigations = true, CancellationToken cancellationToken = default)
    {
        if (!includeNavigations)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        }

        return await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Section)
                .ThenInclude(s => s!.Division)
            .Include(u => u.AdministratorScope)
                .ThenInclude(s => s!.Division)
            .Include(u => u.AdministratorSystemAccesses)
                .ThenInclude(a => a.System)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, bool includeNavigations = false, CancellationToken cancellationToken = default)
    {
        var trimmed = username.Trim();
        if (!includeNavigations)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == trimmed, cancellationToken);
        }

        return await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Section)
                .ThenInclude(s => s!.Division)
            .Include(u => u.AdministratorScope)
            .Include(u => u.AdministratorSystemAccesses)
            .FirstOrDefaultAsync(u => u.Username == trimmed, cancellationToken);
    }

    public async Task<User?> GetByEmployeeNumberAsync(string employeeNumber, CancellationToken cancellationToken = default)
    {
        var trimmed = employeeNumber.Trim();
        return await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Section)
                .ThenInclude(s => s!.Division)
            .FirstOrDefaultAsync(u => u.EmployeeNumber == trimmed, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
    }

    public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var trimmed = username.Trim();
        return await _context.Users
            .AnyAsync(u => u.Username == trimmed, cancellationToken);
    }

    public async Task<bool> ExistsByEmployeeNumberAsync(string employeeNumber, CancellationToken cancellationToken = default)
    {
        var trimmed = employeeNumber.Trim();
        return await _context.Users
            .AnyAsync(u => u.EmployeeNumber == trimmed, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await _context.Users
            .AnyAsync(u => u.Email == normalized, cancellationToken);
    }

    public async Task<bool> HasActiveSystemAdministratorAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.RoleId == (short)RoleType.SystemAdministrator && u.IsActive, cancellationToken);
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int? adminDivisionId,
        bool? isActive,
        int? sectionId,
        string? usernameContains,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users
            .Include(u => u.Role)
            .Include(u => u.Section)
                .ThenInclude(s => s!.Division)
            .AsNoTracking();

        if (adminDivisionId.HasValue)
        {
            query = query.Where(u => u.Section != null && u.Section.DivisionId == adminDivisionId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        if (sectionId.HasValue)
        {
            query = query.Where(u => u.SectionId == sectionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(usernameContains))
        {
            query = query.Where(u => u.Username.Contains(usernameContains.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(u =>
                u.Username.Contains(term) ||
                u.EmployeeNumber.Contains(term) ||
                u.FirstName.Contains(term) ||
                u.LastName.Contains(term) ||
                u.Email.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var p = page <= 0 ? 1 : page;
        var ps = pageSize <= 0 ? 20 : pageSize;

        var items = await query
            .OrderBy(u => u.Username)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public void Add(User user)
    {
        _context.Users.Add(user);
    }

    public void Update(User user)
    {
        _context.Users.Update(user);
    }
}
