using DO.OneAccess.Application.DTOs.Divisions;

namespace DO.OneAccess.Client.Services.Api;

public interface IDivisionApiClient
{
    Task<IReadOnlyList<DivisionDto>> GetAllDivisionsAsync(CancellationToken ct = default);
    Task<DivisionDto> GetDivisionByIdAsync(int divisionId, CancellationToken ct = default);
    Task<DivisionDto> CreateDivisionAsync(CreateDivisionDto dto, CancellationToken ct = default);
    Task<DivisionDto> UpdateDivisionAsync(int divisionId, UpdateDivisionDto dto, CancellationToken ct = default);
    Task DeactivateDivisionAsync(int divisionId, CancellationToken ct = default);
}
