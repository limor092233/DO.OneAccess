using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.DTOs.Access;

namespace DO.OneAccess.Server.Controllers;

[Authorize(Policy = "SystemAdministratorOnly")]
[Route("api/admin-scopes")]
public class AdminScopesController : ApiControllerBase
{
    private readonly IAdminScopeService _adminScopeService;

    public AdminScopesController(IAdminScopeService adminScopeService)
    {
        _adminScopeService = adminScopeService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminScopeDto>>> GetAllScopes(CancellationToken cancellationToken)
    {
        var result = await _adminScopeService.GetAllScopesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<AdminScopeDto>> AssignDivisionScope(
        [FromBody] AssignAdminScopeDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _adminScopeService.AssignDivisionScopeAsync(dto, GetActorUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> RevokeDivisionScope(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        await _adminScopeService.RevokeDivisionScopeAsync(id, GetActorUserId(), cancellationToken);
        return NoContent();
    }
}
