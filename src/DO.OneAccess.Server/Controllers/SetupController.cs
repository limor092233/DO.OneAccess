using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.DTOs.Setup;

namespace DO.OneAccess.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SetupController : ApiControllerBase
{
    private readonly IFirstRunSetupService _setupService;
    private readonly IBootstrapTokenService _bootstrapTokenService;

    public SetupController(
        IFirstRunSetupService setupService,
        IBootstrapTokenService bootstrapTokenService)
    {
        _setupService = setupService;
        _bootstrapTokenService = bootstrapTokenService;
    }

    [HttpGet("status")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<SetupStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _setupService.GetStatusAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("SetupEndpointPolicy")]
    public async Task<ActionResult<FirstRunSetupResultDto>> Setup(
        [FromBody] FirstRunSetupDto dto,
        [FromHeader(Name = "X-Bootstrap-Token")] string? bootstrapToken,
        CancellationToken cancellationToken)
    {
        var status = await _setupService.GetStatusAsync(cancellationToken);
        if (status.IsInitialized)
        {
            return StatusCode(StatusCodes.Status409Conflict, new
            {
                message = "System is already initialized."
            });
        }

        if (!_bootstrapTokenService.IsConfigured)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Setup is temporarily unavailable. Bootstrap token has not been configured in the server environment."
            });
        }

        if (string.IsNullOrWhiteSpace(bootstrapToken) || !_bootstrapTokenService.ValidateToken(bootstrapToken))
        {
            return Unauthorized(new
            {
                message = "Invalid or missing bootstrap token. Provide a valid token in the 'X-Bootstrap-Token' header."
            });
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _setupService.SetupAsync(dto, clientIp, userAgent, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
