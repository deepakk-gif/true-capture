using System.Net;

namespace TrueCapture.Shared.Services;

public class Result
{
    public bool                    IsSuccess  { get; protected init; }
    public HttpStatusCode          StatusCode { get; protected init; }
    public IReadOnlyList<string>   Errors     { get; protected init; } = [];

    public static Result Success()                              => new() { IsSuccess = true,  StatusCode = HttpStatusCode.OK };
    public static Result NotFound(string msg)                   => new() { IsSuccess = false, StatusCode = HttpStatusCode.NotFound,            Errors = [msg] };
    public static Result Conflict(string msg)                   => new() { IsSuccess = false, StatusCode = HttpStatusCode.Conflict,            Errors = [msg] };
    public static Result Validation(IEnumerable<string> errs)   => new() { IsSuccess = false, StatusCode = HttpStatusCode.UnprocessableEntity, Errors = [..errs] };
    public static Result PayloadTooLarge(string msg)            => new() { IsSuccess = false, StatusCode = HttpStatusCode.RequestEntityTooLarge, Errors = [msg] };
    public static Result Forbidden(string? msg = null)          => new() { IsSuccess = false, StatusCode = HttpStatusCode.Forbidden,           Errors = [msg ?? "Access denied."] };
    public static Result Unauthorized(string? msg = null)       => new() { IsSuccess = false, StatusCode = HttpStatusCode.Unauthorized,        Errors = [msg ?? "Authentication required."] };
    public static Result Failure(string msg)                    => new() { IsSuccess = false, StatusCode = HttpStatusCode.InternalServerError, Errors = [msg] };
}

public sealed class Result<T> : Result
{
    public T? Value { get; private init; }

    public static Result<T> Success(T value)                         => new() { IsSuccess = true,  StatusCode = HttpStatusCode.OK,                   Value  = value };
    public new static Result<T> NotFound(string msg)                 => new() { IsSuccess = false, StatusCode = HttpStatusCode.NotFound,             Errors = [msg] };
    public new static Result<T> Conflict(string msg)                 => new() { IsSuccess = false, StatusCode = HttpStatusCode.Conflict,             Errors = [msg] };
    public new static Result<T> Validation(IEnumerable<string> errs) => new() { IsSuccess = false, StatusCode = HttpStatusCode.UnprocessableEntity,  Errors = [..errs] };
    public new static Result<T> PayloadTooLarge(string msg)          => new() { IsSuccess = false, StatusCode = HttpStatusCode.RequestEntityTooLarge, Errors = [msg] };
    public new static Result<T> Forbidden(string? msg = null)        => new() { IsSuccess = false, StatusCode = HttpStatusCode.Forbidden,            Errors = [msg ?? "Access denied."] };
    public new static Result<T> Unauthorized(string? msg = null)     => new() { IsSuccess = false, StatusCode = HttpStatusCode.Unauthorized,         Errors = [msg ?? "Authentication required."] };
    public new static Result<T> Failure(string msg)                  => new() { IsSuccess = false, StatusCode = HttpStatusCode.InternalServerError,  Errors = [msg] };
}
