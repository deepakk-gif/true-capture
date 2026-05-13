using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Infrastructure.Services;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Identity.Infrastructure;
using TrueCapture.Modules.Identity.Services;
using TrueCapture.Shared.Services;

namespace TrueCapture.Tests.Unit.Modules.Identity;

public sealed class AuthServiceExtensionsTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext     _db;
    private readonly AuthService      _sut;
    private readonly OtpService       _otps;
    private readonly IEmailSender     _email;

    public AuthServiceExtensionsTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opts, [new IdentityModelConfigurator()]);
        _db.Database.EnsureCreated();

        var baseSvc = new BaseService<AppDbContext>(_db, Substitute.For<IErrorLogger>());
        _email = Substitute.For<IEmailSender>();
        _otps  = new OtpService(_db, baseSvc, _email);

        var jwt = Options.Create(new JwtOptions
        {
            Issuer = "test", Audience = "test", SigningKey = new string('k', 64),
            AccessMinutes = 15, RefreshDays = 30,
        });
        var googleOpt = Options.Create(new GoogleAuthOptions { ClientId = "" });

        _sut = new AuthService(_db, baseSvc, new TokenService(jwt), _otps, googleOpt);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_ReturnsSuccess_NoEnumerationLeak()
    {
        var result = await _sut.ForgotPasswordAsync(
            new ForgotPasswordDto("nobody@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _email.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        (await _db.Set<OtpCode>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ForgotPassword_KnownEmail_PersistsOtpAndSendsEmail()
    {
        await _sut.RegisterAsync(
            new RegisterDto("reset@example.com", "resetter", "OldPass1!"), CancellationToken.None);

        var result = await _sut.ForgotPasswordAsync(
            new ForgotPasswordDto("reset@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _email.Received(1).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        (await _db.Set<OtpCode>()
            .Where(o => o.Email == "reset@example.com" && o.Purpose == OtpPurpose.PasswordReset)
            .CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ResetPassword_ValidOtp_UpdatesHashAndRevokesAllRefreshTokens()
    {
        // Register a user → one refresh token row exists
        var reg = await _sut.RegisterAsync(
            new RegisterDto("rotate@example.com", "rotater", "OldPass1!"), CancellationToken.None);
        reg.IsSuccess.Should().BeTrue();

        // Issue forgot-password OTP
        await _sut.ForgotPasswordAsync(new ForgotPasswordDto("rotate@example.com"), CancellationToken.None);

        // Peek the most-recent OTP row's plaintext is unknown; replace it with a known code.
        const string code = "111222";
        var row = await _db.Set<OtpCode>().OrderByDescending(o => o.Id).FirstAsync();
        row.CodeHash = HashHelper.Sha256Hex(code);
        await _db.SaveChangesAsync();

        var reset = await _sut.ResetPasswordAsync(
            new ResetPasswordDto("rotate@example.com", code, "NewPass2!"), CancellationToken.None);

        reset.IsSuccess.Should().BeTrue();

        var user = await _db.Set<User>().FirstAsync(u => u.Email == "rotate@example.com");
        BCrypt.Net.BCrypt.Verify("NewPass2!", user.PasswordHash).Should().BeTrue();

        // Every refresh token must now be revoked
        var anyActive = await _db.Set<RefreshToken>()
            .AnyAsync(t => t.UserId == user.Id && t.RevokedAtUtc == null);
        anyActive.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPassword_BadOtp_ReturnsUnauthorized_DoesNotChangeHash()
    {
        await _sut.RegisterAsync(
            new RegisterDto("nochange@example.com", "nc", "OrigPass1!"), CancellationToken.None);

        var reset = await _sut.ResetPasswordAsync(
            new ResetPasswordDto("nochange@example.com", "999999", "ShouldNotApply"), CancellationToken.None);

        reset.IsSuccess.Should().BeFalse();
        reset.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

        var user = await _db.Set<User>().FirstAsync(u => u.Email == "nochange@example.com");
        BCrypt.Net.BCrypt.Verify("OrigPass1!", user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task GoogleSignIn_UnconfiguredClientId_ReturnsFailure()
    {
        // Default fixture sets ClientId="" — this exercises the guard branch.
        var result = await _sut.GoogleSignInAsync(
            new GoogleSignInDto("any.token"), null, null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task VerifyOtpAndIssue_ValidVerifyEmailOtp_IssuesTokensAndFlipsEmailVerified()
    {
        await _sut.RegisterAsync(
            new RegisterDto("verifyflow@example.com", "vf", "Pass1234!"), CancellationToken.None);

        // Issue a VerifyEmail OTP for this user (use OtpService directly)
        var sendResult = await _otps.SendAsync(
            new OtpSendRequest("verifyflow@example.com", OtpPurpose.VerifyEmail), CancellationToken.None);
        sendResult.IsSuccess.Should().BeTrue();

        // Replace hash with known code so the test can finish the flow deterministically
        const string code = "424242";
        var row = await _db.Set<OtpCode>().OrderByDescending(o => o.Id).FirstAsync();
        row.CodeHash = HashHelper.Sha256Hex(code);
        await _db.SaveChangesAsync();

        var issued = await _sut.VerifyOtpAndIssueAsync(
            new VerifyOtpAndIssueDto("verifyflow@example.com", code, OtpPurpose.VerifyEmail),
            null, null, CancellationToken.None);

        issued.IsSuccess.Should().BeTrue();
        issued.Value!.AccessToken.Should().NotBeNullOrEmpty();

        var user = await _db.Set<User>().FirstAsync(u => u.Email == "verifyflow@example.com");
        user.EmailVerified.Should().BeTrue();
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }
}
