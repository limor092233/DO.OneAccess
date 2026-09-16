namespace DO.OneAccess.Application.Common.Interfaces.Persistence;

using DO.OneAccess.Application.DTOs.Divisions;

public interface IDivisionQueries
{
    Task<IReadOnlyList<DivisionDto>> GetAllAsync(
        int? scopedDivisionId = null,
        CancellationToken cancellationToken = default);

    Task<DivisionDto?> GetByIdAsync(
        int divisionId,
        CancellationToken cancellationToken = default);
}
