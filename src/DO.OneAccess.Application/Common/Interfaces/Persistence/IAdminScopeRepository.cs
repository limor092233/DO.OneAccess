using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Application.Common.Interfaces.Persistence;

public interface IAdminScopeRepository
{
    Task<AdministratorScope?> GetByIdAsync(int administratorScopeId, CancellationToken cancellationToken = default);
    Task<AdministratorScope?> GetByAdminUserIdAsync(Guid administratorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdministratorScope>> GetAllAsync(CancellationToken cancellationToken = default);
    void Add(AdministratorScope scope);
    void Remove(AdministratorScope scope);
    Task RemoveByAdminUserIdAsync(Guid administratorUserId, CancellationToken cancellationToken = default);
}
