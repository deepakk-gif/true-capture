using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TrueCapture.Shared.Services;

namespace TrueCapture.Shared.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// The signed-in user's id from the JWT `sub` claim. Falls back to
    /// <see cref="ClaimTypes.NameIdentifier"/> in case claim mapping is ever on.
    /// </summary>
    protected long CurrentUserId
    {
        get
        {
            var raw = User.FindFirst("sub")?.Value
                      ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(raw, out var id) ? id : 0;
        }
    }

    protected IActionResult Ok<T>(Result<T> result) => result.ToActionResult();
    protected IActionResult Ok(Result    result)    => result.ToActionResult();
}
