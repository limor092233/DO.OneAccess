using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Application.Common.Interfaces.Persistence;

public interface IDivisionRepository
{
    Task<Division?> GetByIdAsync(int divisionId, CancellationToken cancellationToken = default);
    Task<Division?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Division>> GetAllAsync(int? scopedDivisionId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, int? excludeDivisionId = null, CancellationToken cancellationToken = default);
    void Add(Division division);
    void Update(Division division);
}
