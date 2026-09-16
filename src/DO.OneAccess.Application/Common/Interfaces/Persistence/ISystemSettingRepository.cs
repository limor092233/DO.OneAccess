using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Application.Common.Interfaces.Persistence;

public interface ISystemSettingRepository
{
    Task<SystemSetting?> GetBySystemIdAndKeyAsync(Guid systemId, string settingKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemSetting>> GetBySystemIdAsync(Guid systemId, CancellationToken cancellationToken = default);
    void Add(SystemSetting setting);
    void Update(SystemSetting setting);
    void Remove(SystemSetting setting);
}
