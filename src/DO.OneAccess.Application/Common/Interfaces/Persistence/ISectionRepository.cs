using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Application.Common.Interfaces.Persistence;

public interface ISectionRepository
{
    Task<Section?> GetByIdAsync(int sectionId, bool includeDivision = false, CancellationToken cancellationToken = default);
    Task<Section?> GetByCodeAsync(int divisionId, string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Section>> GetByDivisionIdAsync(int divisionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Section>> GetListAsync(int? divisionId, bool? isActive, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(int divisionId, string code, int? excludeSectionId = null, CancellationToken cancellationToken = default);
    void Add(Section section);
    void Update(Section section);
}
