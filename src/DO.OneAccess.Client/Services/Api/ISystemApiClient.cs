using DO.OneAccess.Application.DTOs.Systems;

namespace DO.OneAccess.Client.Services.Api;

public interface ISystemApiClient
{
    Task<IReadOnlyList<SystemDto>> GetAccessibleSystemsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SystemDto>> GetAllSystemsAsync(CancellationToken ct = default);
    Task<SystemDto> GetSystemByIdAsync(Guid systemId, CancellationToken ct = default);
    Task<SystemDto> RegisterSystemAsync(RegisterSystemDto dto, CancellationToken ct = default);
    Task<SystemDto> UpdateSystemAsync(Guid systemId, UpdateSystemDto dto, CancellationToken ct = default);
    Task DeactivateSystemAsync(Guid systemId, CancellationToken ct = default);
}
