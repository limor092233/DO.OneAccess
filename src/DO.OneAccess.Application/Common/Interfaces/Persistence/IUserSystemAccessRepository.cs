using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Application.Common.Interfaces.Persistence;

public interface IUserSystemAccessRepository
{
    Task<UserSystemAccess?> GetByIdAsync(long userSystemAccessId, CancellationToken cancellationToken = default);
    Task<UserSystemAccess?> GetByUserAndSystemAsync(Guid userId, Guid systemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserSystemAccess>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(UserSystemAccess access);
    void Update(UserSystemAccess access);
    void Remove(UserSystemAccess access);
}
