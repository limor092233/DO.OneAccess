using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Users;

namespace DO.OneAccess.Client.Services.Api;

public interface IUserApiClient
{
    Task<PagedResult<UserDto>> GetUsersAsync(UserQueryDto query, CancellationToken ct = default);
    Task<UserDto> GetUserByIdAsync(Guid userId, CancellationToken ct = default);
    Task<UserDto> CreateUserAsync(CreateUserDto dto, CancellationToken ct = default);
    Task TransferSystemAdministratorAsync(TransferSystemAdministratorDto dto, CancellationToken ct = default);
    Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserDto dto, CancellationToken ct = default);
    Task DeactivateUserAsync(Guid userId, CancellationToken ct = default);
    Task ChangeRoleAsync(Guid userId, ChangeRoleDto dto, CancellationToken ct = default);
}
