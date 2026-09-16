using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace DO.OneAccess.Server.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid GetActorUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var actorGuid))
        {
            throw new UnauthorizedAccessException("Actor identity claim is missing or invalid.");
        }

        return actorGuid;
    }
}
