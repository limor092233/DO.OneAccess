using DO.OneAccess.Application.DTOs.Access;

namespace DO.OneAccess.Client.Services.Api;

public interface IAdminScopeApiClient
{
    Task<IReadOnlyList<AdminScopeDto>> GetAllScopesAsync(CancellationToken ct = default);
    Task<AdminScopeDto> AssignDivisionScopeAsync(AssignAdminScopeDto dto, CancellationToken ct = default);
    Task RevokeDivisionScopeAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<AdminSystemAccessDto>> GetAllGrantsAsync(CancellationToken ct = default);
    Task<AdminSystemAccessDto> GrantAccessAsync(GrantAdminSystemAccessDto dto, CancellationToken ct = default);
    Task RevokeAccessAsync(long id, CancellationToken ct = default);
}
