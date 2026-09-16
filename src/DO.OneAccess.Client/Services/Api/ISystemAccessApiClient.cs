using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Access;

namespace DO.OneAccess.Client.Services.Api;

public interface ISystemAccessApiClient
{
    Task<PagedResult<UserSystemAccessDto>> GetOverridesAsync(Guid? userId = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<UserSystemAccessDto> SetAccessAsync(SetUserSystemAccessDto dto, CancellationToken ct = default);
    Task RevokeAccessAsync(long id, CancellationToken ct = default);
}
