using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.DTOs.Divisions;

namespace DO.OneAccess.Server.Controllers;

[Route("api/divisions")]
public class DivisionsController : ApiControllerBase
{
    private readonly IDivisionService _divisionService;

    public DivisionsController(IDivisionService divisionService)
    {
        _divisionService = divisionService;
    }

    [HttpGet]
    [Authorize(Policy = "AdministratorOrAbove")]
    public async Task<ActionResult<IReadOnlyList<DivisionDto>>> GetAllDivisions(CancellationToken cancellationToken)
    {
        var result = await _divisionService.GetAllDivisionsAsync(GetActorUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{divisionId:int}")]
    [Authorize(Policy = "AdministratorOrAbove")]
    public async Task<ActionResult<DivisionDto>> GetDivisionById(
        [FromRoute] int divisionId,
        CancellationToken cancellationToken)
    {
        var result = await _divisionService.GetDivisionByIdAsync(divisionId, GetActorUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "SystemAdministratorOnly")]
    public async Task<ActionResult<DivisionDto>> CreateDivision(
        [FromBody] CreateDivisionDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _divisionService.CreateDivisionAsync(dto, GetActorUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetDivisionById), new { divisionId = result.DivisionId }, result);
    }

    [HttpPut("{divisionId:int}")]
    [Authorize(Policy = "SystemAdministratorOnly")]
    public async Task<ActionResult<DivisionDto>> UpdateDivision(
        [FromRoute] int divisionId,
        [FromBody] UpdateDivisionDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _divisionService.UpdateDivisionAsync(divisionId, dto, GetActorUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{divisionId:int}")]
    [Authorize(Policy = "SystemAdministratorOnly")]
    public async Task<IActionResult> DeactivateDivision(
        [FromRoute] int divisionId,
        CancellationToken cancellationToken)
    {
        await _divisionService.DeactivateDivisionAsync(divisionId, GetActorUserId(), cancellationToken);
        return NoContent();
    }
}
