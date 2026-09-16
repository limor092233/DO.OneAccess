using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Interfaces.Persistence;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Domain.Enums;

namespace DO.OneAccess.Infrastructure.Persistence.Repositories;

public class SystemRepository : ISystemRepository
{
    private readonly AppDbContext _context;

    public SystemRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Domain.Entities.System?> GetByIdAsync(Guid systemId, CancellationToken cancellationToken = default)
    {
        return await _context.Systems
            .FirstOrDefaultAsync(s => s.SystemId == systemId, cancellationToken);
    }

    public async Task<Domain.Entities.System?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var trimmed = code.Trim();
        return await _context.Systems
            .FirstOrDefaultAsync(s => s.SystemCode == trimmed, cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Entities.System>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Systems
            .Where(s => s.IsActive)
            .OrderBy(s => s.SystemName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Entities.System>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Systems
            .OrderBy(s => s.SystemName)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, Guid? excludeSystemId = null, CancellationToken cancellationToken = default)
    {
        var trimmed = code.Trim();
        var query = _context.Systems.Where(s => s.SystemCode == trimmed);

        if (excludeSystemId.HasValue)
        {
            query = query.Where(s => s.SystemId != excludeSystemId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Entities.System>> GetAccessibleSystemsForUserAsync(
        Guid userId,
        short roleId,
        CancellationToken cancellationToken = default)
    {
        var activeSystems = await _context.Systems
            .Where(s => s.IsActive)
            .OrderBy(s => s.SystemName)
            .ToListAsync(cancellationToken);

        var overrides = await _context.UserSystemAccess
            .Where(a => a.UserId == userId)
            .ToDictionaryAsync(a => a.SystemId, a => a.AccessType, cancellationToken);

        var accessible = new List<Domain.Entities.System>();
        foreach (var sys in activeSystems)
        {
            var isAccessible = false;
            if (overrides.TryGetValue(sys.SystemId, out var accessType))
            {
                if (string.Equals(accessType, nameof(AccessOverride.Allow), StringComparison.OrdinalIgnoreCase))
                {
                    isAccessible = true;
                }
            }
            else
            {
                if (string.Equals(sys.DefaultAccess, nameof(DefaultAccess.All), StringComparison.OrdinalIgnoreCase))
                {
                    isAccessible = true;
                }
            }

            if (isAccessible)
            {
                accessible.Add(sys);
            }
        }

        return accessible;
    }

    public void Add(Domain.Entities.System system)
    {
        _context.Systems.Add(system);
    }

    public void Update(Domain.Entities.System system)
    {
        _context.Systems.Update(system);
    }
}
