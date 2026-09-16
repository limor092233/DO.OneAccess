using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Application.Common.Interfaces.Persistence;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId, bool includeNavigations = true, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, bool includeNavigations = false, CancellationToken cancellationToken = default);
    Task<User?> GetByEmployeeNumberAsync(string employeeNumber, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmployeeNumberAsync(string employeeNumber, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> HasActiveSystemAdministratorAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int? adminDivisionId,
        bool? isActive,
        int? sectionId,
        string? usernameContains,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    void Add(User user);
    void Update(User user);
}
