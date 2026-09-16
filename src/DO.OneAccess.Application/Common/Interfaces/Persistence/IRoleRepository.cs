using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Application.Common.Interfaces.Persistence;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(short roleId, CancellationToken cancellationToken = default);
    Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}
