using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace TrueCapture.Shared.Services;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess) return new OkResult();
        return result.StatusCode switch
        {
            HttpStatusCode.NotFound            => new NotFoundObjectResult(new { errors = result.Errors }),
            HttpStatusCode.Conflict            => new ConflictObjectResult(new { errors = result.Errors }),
            HttpStatusCode.UnprocessableEntity => new UnprocessableEntityObjectResult(new { errors = result.Errors }),
            HttpStatusCode.Forbidden           => new ForbidResult(),
            HttpStatusCode.Unauthorized        => new UnauthorizedObjectResult(new { errors = result.Errors }),
            _                                  => new ObjectResult(new { errors = result.Errors }) { StatusCode = (int)result.StatusCode },
        };
    }

    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess) return new OkObjectResult(result.Value);
        return ((Result)result).ToActionResult();
    }
}
