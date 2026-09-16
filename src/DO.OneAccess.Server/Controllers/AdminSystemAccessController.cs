using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.DTOs.Access;

namespace DO.OneAccess.Server.Controllers;

[Authorize(Policy = "SystemAdministratorOnly")]
[Route("api/admin-system-access")]
public class AdminSystemAccessController : ApiControllerBase
{
    private readonly IAdminScopeService _adminScopeService;

    public AdminSystemAccessController(IAdminScopeService adminScopeService)
    {
        _adminScopeService = adminScopeService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminSystemAccessDto>>> GetAllGrants(CancellationToken cancellationToken)
    {
        var result = await _adminScopeService.GetAllAdminSystemAccessGrantsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<AdminSystemAccessDto>> GrantAccess(
        [FromBody] GrantAdminSystemAccessDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _adminScopeService.GrantAdminSystemAccessAsync(dto, GetActorUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> RevokeAccess(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        await _adminScopeService.RevokeAdminSystemAccessAsync(id, GetActorUserId(), cancellationToken);
        return NoContent();
    }
}
