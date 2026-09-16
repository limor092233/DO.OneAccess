using DO.OneAccess.Application.DTOs.Systems;

namespace DO.OneAccess.Application.Common.Interfaces;

/// <summary>
/// System registry management. Mutation is restricted to System Administrators.
/// Querying by ID requires System Administrator or Administrator with system-management grant.
/// </summary>
public interface ISystemService
{
    Task<IReadOnlyList<SystemDto>> GetRegisteredSystemsAsync(CancellationToken cancellationToken = default);

    Task<SystemDto> GetSystemByIdAsync(Guid systemId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<SystemDto> RegisterSystemAsync(RegisterSystemDto dto, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<SystemDto> UpdateSystemAsync(Guid systemId, UpdateSystemDto dto, Guid actorUserId, CancellationToken cancellationToken = default);

    Task DeactivateSystemAsync(Guid systemId, Guid actorUserId, CancellationToken cancellationToken = default);
}
