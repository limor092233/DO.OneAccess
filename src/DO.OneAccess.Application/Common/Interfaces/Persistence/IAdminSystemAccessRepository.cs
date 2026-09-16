using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Application.Common.Interfaces.Persistence;

public interface IAdminSystemAccessRepository
{
    Task<AdministratorSystemAccess?> GetByIdAsync(int administratorSystemAccessId, CancellationToken cancellationToken = default);
    Task<AdministratorSystemAccess?> GetByAdminAndSystemAsync(Guid administratorUserId, Guid systemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdministratorSystemAccess>> GetByAdminUserIdAsync(Guid administratorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdministratorSystemAccess>> GetAllAsync(CancellationToken cancellationToken = default);
    void Add(AdministratorSystemAccess access);
    void Remove(AdministratorSystemAccess access);
    Task RemoveByAdminUserIdAsync(Guid administratorUserId, CancellationToken cancellationToken = default);
}
