using DO.OneAccess.Application.DTOs.Access;

namespace DO.OneAccess.Application.Common.Interfaces;

/// <summary>
/// Manages administrator organizational scope (AdministratorScopes) and
/// administrator system-management grants (AdministratorSystemAccess).
/// All mutating operations are restricted to System Administrators.
/// </summary>
public interface IAdminScopeService
{
    Task<IReadOnlyList<AdminScopeDto>> GetAllScopesAsync(CancellationToken cancellationToken = default);

    Task<AdminScopeDto> AssignDivisionScopeAsync(AssignAdminScopeDto dto, Guid actorUserId, CancellationToken cancellationToken = default);

    Task RevokeDivisionScopeAsync(long adminScopeId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminSystemAccessDto>> GetAllAdminSystemAccessGrantsAsync(CancellationToken cancellationToken = default);

    Task<AdminSystemAccessDto> GrantAdminSystemAccessAsync(GrantAdminSystemAccessDto dto, Guid actorUserId, CancellationToken cancellationToken = default);

    Task RevokeAdminSystemAccessAsync(long adminSystemAccessId, Guid actorUserId, CancellationToken cancellationToken = default);
}
