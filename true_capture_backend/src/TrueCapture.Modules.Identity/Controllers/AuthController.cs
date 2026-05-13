using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Identity.Services;
using TrueCapture.Shared.Authorization;
using TrueCapture.Shared.Constants;
using TrueCapture.Shared.Controllers;

namespace TrueCapture.Modules.Identity.Controllers;

[Route("api/auth")]
public sealed class AuthController(IAuthService svc, IOtpService otps) : BaseController
{
    [HttpPost("register")]
    [AllowAnonymous]
    [RequireCaptcha]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct = default)
        => Ok(await svc.RegisterAsync(dto, ct));

    [HttpPost("login")]
    [AllowAnonymous]
    [RequireCaptcha]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct = default)
        => Ok(await svc.LoginAsync(dto, Request.Headers.UserAgent.ToString(),
                                       HttpContext.Connection.RemoteIpAddress?.ToString(), ct));

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> Refresh([FromBody] RefreshDto dto, CancellationToken ct = default)
        => Ok(await svc.RefreshAsync(dto, Request.Headers.UserAgent.ToString(),
                                          HttpContext.Connection.RemoteIpAddress?.ToString(), ct));

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshDto dto, CancellationToken ct = default)
        => Ok(await svc.LogoutAsync(dto.RefreshToken, ct));

    [HttpPost("send-otp")]
    [AllowAnonymous]
    [RequireCaptcha]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest req, CancellationToken ct = default)
        => Ok(await otps.SendAsync(new OtpSendRequest(req.Email, req.Purpose), ct));

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    [RequireCaptcha]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpAndIssueDto dto, CancellationToken ct = default)
        => Ok(await svc.VerifyOtpAndIssueAsync(dto, Request.Headers.UserAgent.ToString(),
                                                    HttpContext.Connection.RemoteIpAddress?.ToString(), ct));

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [RequireCaptcha]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto, CancellationToken ct = default)
        => Ok(await svc.ForgotPasswordAsync(dto, ct));

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [RequireCaptcha]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken ct = default)
        => Ok(await svc.ResetPasswordAsync(dto, ct));

    [HttpPost("google")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> Google([FromBody] GoogleSignInDto dto, CancellationToken ct = default)
        => Ok(await svc.GoogleSignInAsync(dto, Request.Headers.UserAgent.ToString(),
                                               HttpContext.Connection.RemoteIpAddress?.ToString(), ct));
}

public sealed record SendOtpRequest(string Email, OtpPurpose Purpose);
