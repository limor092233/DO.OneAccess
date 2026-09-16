using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Access;
using DO.OneAccess.Application.DTOs.Systems;

namespace DO.OneAccess.Application.Common.Interfaces;

/// <summary>
/// Resolves user system usage access and manages UserSystemAccess overrides.
/// This is entirely distinct from administrator system-management scope.
/// </summary>
public interface ISystemAccessService
{
    /// <summary>
    /// Determines whether the specified user may access the specified system.
    /// Applies: User.IsActive → System.IsActive → UserSystemAccess override → System.DefaultAccess.
    /// </summary>
    Task<SystemAccessResult> ResolveAccessAsync(Guid userId, Guid systemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all active systems the specified user currently has access to.
    /// </summary>
    Task<IReadOnlyList<SystemDto>> GetUserAccessibleSystemsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns UserSystemAccess override records visible to the actor.
    /// SysAdmin sees all; Administrator sees only records within their scope.
    /// </summary>
    Task<PagedResult<UserSystemAccessDto>> GetUserSystemAccessOverridesAsync(Guid actorUserId, Guid? filterUserId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates (upserts) a UserSystemAccess record.
    /// Administrator: requires division scope on target user AND system-management grant.
    /// SystemAdministrator: unrestricted.
    /// </summary>
    Task<UserSystemAccessDto> SetUserSystemAccessAsync(SetUserSystemAccessDto dto, Guid actorUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a UserSystemAccess override record.
    /// </summary>
    Task RevokeUserSystemAccessAsync(long userSystemAccessId, Guid actorUserId, CancellationToken cancellationToken = default);
}
