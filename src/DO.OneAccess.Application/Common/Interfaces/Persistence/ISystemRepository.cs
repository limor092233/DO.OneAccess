using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Application.Common.Interfaces.Persistence;

public interface ISystemRepository
{
    Task<Domain.Entities.System?> GetByIdAsync(Guid systemId, CancellationToken cancellationToken = default);
    Task<Domain.Entities.System?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Entities.System>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Entities.System>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, Guid? excludeSystemId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Entities.System>> GetAccessibleSystemsForUserAsync(Guid userId, short roleId, CancellationToken cancellationToken = default);
    void Add(Domain.Entities.System system);
    void Update(Domain.Entities.System system);
}
