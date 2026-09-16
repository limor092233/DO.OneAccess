using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Interfaces.Persistence;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Persistence.Repositories;

public class SystemSettingRepository : ISystemSettingRepository
{
    private readonly AppDbContext _context;

    public SystemSettingRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<SystemSetting?> GetBySystemIdAndKeyAsync(Guid systemId, string settingKey, CancellationToken cancellationToken = default)
    {
        var trimmed = settingKey.Trim();
        return await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.SystemId == systemId && s.SettingKey == trimmed, cancellationToken);
    }

    public async Task<IReadOnlyList<SystemSetting>> GetBySystemIdAsync(Guid systemId, CancellationToken cancellationToken = default)
    {
        return await _context.SystemSettings
            .Where(s => s.SystemId == systemId)
            .OrderBy(s => s.SettingKey)
            .ToListAsync(cancellationToken);
    }

    public void Add(SystemSetting setting)
    {
        _context.SystemSettings.Add(setting);
    }

    public void Update(SystemSetting setting)
    {
        _context.SystemSettings.Update(setting);
    }

    public void Remove(SystemSetting setting)
    {
        _context.SystemSettings.Remove(setting);
    }
}
