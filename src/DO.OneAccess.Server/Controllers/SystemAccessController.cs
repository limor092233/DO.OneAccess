using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Access;

namespace DO.OneAccess.Server.Controllers;

[Authorize(Policy = "AdministratorOrAbove")]
[Route("api/system-access")]
public class SystemAccessController : ApiControllerBase
{
    private readonly ISystemAccessService _systemAccessService;

    public SystemAccessController(ISystemAccessService systemAccessService)
    {
        _systemAccessService = systemAccessService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UserSystemAccessDto>>> GetOverrides(
        [FromQuery] Guid? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _systemAccessService.GetUserSystemAccessOverridesAsync(
            GetActorUserId(), userId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<UserSystemAccessDto>> SetAccess(
        [FromBody] SetUserSystemAccessDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _systemAccessService.SetUserSystemAccessAsync(dto, GetActorUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> RevokeAccess(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        await _systemAccessService.RevokeUserSystemAccessAsync(id, GetActorUserId(), cancellationToken);
        return NoContent();
    }
}
