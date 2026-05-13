using Microsoft.AspNetCore.Mvc;
using TrueCapture.Shared.Services;

namespace TrueCapture.Shared.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected long CurrentUserId =>
        long.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : 0;

    protected IActionResult Ok<T>(Result<T> result) => result.ToActionResult();
    protected IActionResult Ok(Result    result)    => result.ToActionResult();
}
