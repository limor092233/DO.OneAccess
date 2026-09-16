using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Users;

namespace DO.OneAccess.Server.Controllers;

[Authorize(Policy = "AdministratorOrAbove")]
[Route("api/users")]
public class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UserDto>>> GetUsers(
        [FromQuery] UserQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _userService.GetUsersAsync(query, GetActorUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserDto>> GetUserById(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserByIdAsync(userId, GetActorUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(
        [FromBody] CreateUserDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _userService.CreateUserAsync(dto, GetActorUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetUserById), new { userId = result.UserId }, result);
    }

    [HttpPost("system-administrator/transfer")]
    [Authorize(Policy = "SystemAdministratorOnly")]
    public async Task<IActionResult> TransferSystemAdministrator(
        [FromBody] TransferSystemAdministratorDto dto,
        CancellationToken cancellationToken)
    {
        await _userService.TransferSystemAdministratorAsync(dto, GetActorUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPut("{userId:guid}")]
    public async Task<ActionResult<UserDto>> UpdateUser(
        [FromRoute] Guid userId,
        [FromBody] UpdateUserDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateUserAsync(userId, dto, GetActorUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> DeactivateUser(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        await _userService.DeactivateUserAsync(userId, GetActorUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPut("{userId:guid}/role")]
    [Authorize(Policy = "SystemAdministratorOnly")]
    public async Task<IActionResult> ChangeRole(
        [FromRoute] Guid userId,
        [FromBody] ChangeRoleDto dto,
        CancellationToken cancellationToken)
    {
        await _userService.ChangeRoleAsync(userId, dto.RoleId, GetActorUserId(), cancellationToken);
        return NoContent();
    }
}
