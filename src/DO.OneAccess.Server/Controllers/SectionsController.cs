using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.DTOs.Sections;

namespace DO.OneAccess.Server.Controllers;

[Authorize(Policy = "AdministratorOrAbove")]
[Route("api/sections")]
public class SectionsController : ApiControllerBase
{
    private readonly ISectionService _sectionService;

    public SectionsController(ISectionService sectionService)
    {
        _sectionService = sectionService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SectionDto>>> GetSections(
        [FromQuery] SectionQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _sectionService.GetSectionsAsync(query, GetActorUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{sectionId:int}")]
    public async Task<ActionResult<SectionDto>> GetSectionById(
        [FromRoute] int sectionId,
        CancellationToken cancellationToken)
    {
        var result = await _sectionService.GetSectionByIdAsync(sectionId, GetActorUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<SectionDto>> CreateSection(
        [FromBody] CreateSectionDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _sectionService.CreateSectionAsync(dto, GetActorUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetSectionById), new { sectionId = result.SectionId }, result);
    }

    [HttpPut("{sectionId:int}")]
    public async Task<ActionResult<SectionDto>> UpdateSection(
        [FromRoute] int sectionId,
        [FromBody] UpdateSectionDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _sectionService.UpdateSectionAsync(sectionId, dto, GetActorUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{sectionId:int}")]
    public async Task<IActionResult> DeactivateSection(
        [FromRoute] int sectionId,
        CancellationToken cancellationToken)
    {
        await _sectionService.DeactivateSectionAsync(sectionId, GetActorUserId(), cancellationToken);
        return NoContent();
    }
}
