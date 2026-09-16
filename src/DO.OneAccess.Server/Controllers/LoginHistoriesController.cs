using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Audit;

namespace DO.OneAccess.Server.Controllers;

[Authorize(Policy = "SystemAdministratorOnly")]
[Route("api/login-histories")]
public class LoginHistoriesController : ApiControllerBase
{
    private readonly ILoginHistoryService _loginHistoryService;

    public LoginHistoriesController(ILoginHistoryService loginHistoryService)
    {
        _loginHistoryService = loginHistoryService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<LoginHistoryDto>>> GetLoginHistories(
        [FromQuery] LoginHistoryQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _loginHistoryService.GetLoginHistoriesAsync(query, cancellationToken);
        return Ok(result);
    }
}
