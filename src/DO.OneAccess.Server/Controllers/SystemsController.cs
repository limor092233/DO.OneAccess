using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.DTOs.Systems;

namespace DO.OneAccess.Server.Controllers;

[Route("api/systems")]
public class SystemsController : ApiControllerBase
{
    private readonly ISystemService _systemService;
    private readonly ISystemAccessService _systemAccessService;

    public SystemsController(
        ISystemService systemService,
        ISystemAccessService systemAccessService)
    {
        _systemService = systemService;
        _systemAccessService = systemAccessService;
    }

    [HttpGet]
    [Authorize(Policy = "AnyAuthenticatedUser")]
    public async Task<ActionResult<IReadOnlyList<SystemDto>>> GetAccessibleSystems(CancellationToken cancellationToken)
    {
        var result = await _systemAccessService.GetUserAccessibleSystemsAsync(GetActorUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("all")]
    [Authorize(Policy = "SystemAdministratorOnly")]
    public async Task<ActionResult<IReadOnlyList<SystemDto>>> GetAllSystems(CancellationToken cancellationToken)
    {
        var result = await _systemService.GetRegisteredSystemsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{systemId:guid}")]
    [Authorize(Policy = "AdministratorOrAbove")]
    public async Task<ActionResult<SystemDto>> GetSystemById(
        [FromRoute] Guid systemId,
        CancellationToken cancellationToken)
    {
        var result = await _systemService.GetSystemByIdAsync(systemId, GetActorUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "SystemAdministratorOnly")]
    public async Task<ActionResult<SystemDto>> RegisterSystem(
        [FromBody] RegisterSystemDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _systemService.RegisterSystemAsync(dto, GetActorUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetSystemById), new { systemId = result.SystemId }, result);
    }

    [HttpPut("{systemId:guid}")]
    [Authorize(Policy = "SystemAdministratorOnly")]
    public async Task<ActionResult<SystemDto>> UpdateSystem(
        [FromRoute] Guid systemId,
        [FromBody] UpdateSystemDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _systemService.UpdateSystemAsync(systemId, dto, GetActorUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{systemId:guid}")]
    [Authorize(Policy = "SystemAdministratorOnly")]
    public async Task<IActionResult> DeactivateSystem(
        [FromRoute] Guid systemId,
        CancellationToken cancellationToken)
    {
        await _systemService.DeactivateSystemAsync(systemId, GetActorUserId(), cancellationToken);
        return NoContent();
    }
}
