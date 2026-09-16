using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Interfaces.Persistence;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Persistence.Repositories;

public class UserSystemAccessRepository : IUserSystemAccessRepository
{
    private readonly AppDbContext _context;

    public UserSystemAccessRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<UserSystemAccess?> GetByIdAsync(long userSystemAccessId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSystemAccess
            .Include(a => a.System)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.UserSystemAccessId == userSystemAccessId, cancellationToken);
    }

    public async Task<UserSystemAccess?> GetByUserAndSystemAsync(Guid userId, Guid systemId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSystemAccess
            .Include(a => a.System)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.UserId == userId && a.SystemId == systemId, cancellationToken);
    }

    public async Task<IReadOnlyList<UserSystemAccess>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSystemAccess
            .Include(a => a.System)
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.System.SystemName)
            .ToListAsync(cancellationToken);
    }

    public void Add(UserSystemAccess access)
    {
        _context.UserSystemAccess.Add(access);
    }

    public void Update(UserSystemAccess access)
    {
        _context.UserSystemAccess.Update(access);
    }

    public void Remove(UserSystemAccess access)
    {
        _context.UserSystemAccess.Remove(access);
    }
}
