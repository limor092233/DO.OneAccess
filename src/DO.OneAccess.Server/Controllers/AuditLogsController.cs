using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Audit;

namespace DO.OneAccess.Server.Controllers;

[Authorize(Policy = "SystemAdministratorOnly")]
[Route("api/audit-logs")]
public class AuditLogsController : ApiControllerBase
{
    private readonly IAuditService _auditService;

    public AuditLogsController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAuditLogs(
        [FromQuery] AuditLogQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _auditService.GetAuditLogsAsync(query, cancellationToken);
        return Ok(result);
    }
}
