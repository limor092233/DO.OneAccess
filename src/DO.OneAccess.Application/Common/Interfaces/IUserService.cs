using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Users;

namespace DO.OneAccess.Application.Common.Interfaces;

/// <summary>
/// User account management. All operations pass actorUserId for scope enforcement and audit logging.
/// </summary>
public interface IUserService
{
    Task<PagedResult<UserDto>> GetUsersAsync(UserQueryDto query, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<UserDto> GetUserByIdAsync(Guid userId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<UserDto> CreateUserAsync(CreateUserDto dto, Guid actorUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transfers the System Administrator role to an existing active User or Administrator.
    /// Only the active SYSTEM_ADMINISTRATOR may call this.
    /// </summary>
    Task TransferSystemAdministratorAsync(TransferSystemAdministratorDto dto, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserDto dto, Guid actorUserId, CancellationToken cancellationToken = default);

    Task DeactivateUserAsync(Guid userId, Guid actorUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the role of the specified user. Only SYSTEM_ADMINISTRATOR may call this.
    /// </summary>
    Task ChangeRoleAsync(Guid userId, short roleId, Guid actorUserId, CancellationToken cancellationToken = default);
}
