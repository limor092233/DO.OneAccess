using DO.OneAccess.Application.DTOs.Divisions;

namespace DO.OneAccess.Application.Common.Interfaces;

/// <summary>
/// Division management. All mutating operations are restricted to System Administrators.
/// </summary>
public interface IDivisionService
{
    Task<IReadOnlyList<DivisionDto>> GetAllDivisionsAsync(Guid actorUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single division. Administrator may only access their own assigned division.
    /// </summary>
    Task<DivisionDto> GetDivisionByIdAsync(int divisionId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<DivisionDto> CreateDivisionAsync(CreateDivisionDto dto, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<DivisionDto> UpdateDivisionAsync(int divisionId, UpdateDivisionDto dto, Guid actorUserId, CancellationToken cancellationToken = default);

    Task DeactivateDivisionAsync(int divisionId, Guid actorUserId, CancellationToken cancellationToken = default);
}
