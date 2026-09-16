using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Interfaces.Persistence;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Persistence.Repositories;

public class AdminSystemAccessRepository : IAdminSystemAccessRepository
{
    private readonly AppDbContext _context;

    public AdminSystemAccessRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<AdministratorSystemAccess?> GetByIdAsync(int administratorSystemAccessId, CancellationToken cancellationToken = default)
    {
        return await _context.AdministratorSystemAccess
            .Include(a => a.AdministratorUser)
            .Include(a => a.System)
            .FirstOrDefaultAsync(a => a.AdministratorSystemAccessId == administratorSystemAccessId, cancellationToken);
    }

    public async Task<AdministratorSystemAccess?> GetByAdminAndSystemAsync(Guid administratorUserId, Guid systemId, CancellationToken cancellationToken = default)
    {
        return await _context.AdministratorSystemAccess
            .Include(a => a.AdministratorUser)
            .Include(a => a.System)
            .FirstOrDefaultAsync(a => a.AdministratorUserId == administratorUserId && a.SystemId == systemId, cancellationToken);
    }

    public async Task<IReadOnlyList<AdministratorSystemAccess>> GetByAdminUserIdAsync(Guid administratorUserId, CancellationToken cancellationToken = default)
    {
        return await _context.AdministratorSystemAccess
            .Include(a => a.System)
            .Where(a => a.AdministratorUserId == administratorUserId)
            .OrderBy(a => a.System.SystemName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdministratorSystemAccess>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AdministratorSystemAccess
            .Include(a => a.AdministratorUser)
            .Include(a => a.System)
            .OrderBy(a => a.AdministratorUser.Username)
            .ToListAsync(cancellationToken);
    }

    public void Add(AdministratorSystemAccess access)
    {
        _context.AdministratorSystemAccess.Add(access);
    }

    public void Remove(AdministratorSystemAccess access)
    {
        _context.AdministratorSystemAccess.Remove(access);
    }

    public async Task RemoveByAdminUserIdAsync(Guid administratorUserId, CancellationToken cancellationToken = default)
    {
        var accesses = await _context.AdministratorSystemAccess
            .Where(a => a.AdministratorUserId == administratorUserId)
            .ToListAsync(cancellationToken);

        if (accesses.Count > 0)
        {
            _context.AdministratorSystemAccess.RemoveRange(accesses);
        }
    }
}
