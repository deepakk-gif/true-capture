using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Infrastructure.Services;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Identity.Infrastructure;
using TrueCapture.Modules.Identity.Services;
using TrueCapture.Shared.Services;

namespace TrueCapture.Tests.Unit.Modules.Identity;

public sealed class OtpServiceTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext     _db;
    private readonly IEmailSender     _email;
    private readonly OtpService       _sut;

    public OtpServiceTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_conn)
            .Options;

        _db = new AppDbContext(opts, [new IdentityModelConfigurator()]);
        _db.Database.EnsureCreated();

        var baseSvc = new BaseService<AppDbContext>(_db, Substitute.For<IErrorLogger>());
        _email = Substitute.For<IEmailSender>();
        _sut   = new OtpService(_db, baseSvc, _email, Substitute.For<IHostEnvironment>());
    }

    private async Task<User> SeedUserAsync(string email = "user@example.com")
    {
        var u = new User { Email = email, Username = "u", PasswordHash = "h", IsActive = true };
        _db.Set<User>().Add(u);
        await _db.SaveChangesAsync();
        return u;
    }

    [Fact]
    public async Task SendAsync_WithKnownEmail_DispatchesAndPersistsRow()
    {
        var u = await SeedUserAsync();

        var result = await _sut.SendAsync(new OtpSendRequest(u.Email, OtpPurpose.VerifyEmail), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _db.Set<OtpCode>().CountAsync()).Should().Be(1);
        await _email.Received(1).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_UnknownEmail_PasswordReset_DoesNotDispatch_NoEnumerationLeak()
    {
        var result = await _sut.SendAsync(
            new OtpSendRequest("ghost@example.com", OtpPurpose.PasswordReset), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _db.Set<OtpCode>().CountAsync()).Should().Be(0);
        await _email.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_RateLimit_Returns422AfterFifth()
    {
        var u = await SeedUserAsync("rate@example.com");
        for (var i = 0; i < 5; i++)
        {
            var ok = await _sut.SendAsync(new OtpSendRequest(u.Email, OtpPurpose.VerifyEmail), CancellationToken.None);
            ok.IsSuccess.Should().BeTrue();
        }

        var sixth = await _sut.SendAsync(new OtpSendRequest(u.Email, OtpPurpose.VerifyEmail), CancellationToken.None);

        sixth.IsSuccess.Should().BeFalse();
        sixth.StatusCode.Should().Be(System.Net.HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task VerifyAsync_WrongCode_ReturnsUnauthorized_IncrementsAttempt()
    {
        var u = await SeedUserAsync("verify@example.com");
        await _sut.SendAsync(new OtpSendRequest(u.Email, OtpPurpose.VerifyEmail), CancellationToken.None);

        var bad = await _sut.VerifyAsync(
            new OtpVerifyRequest(u.Email, "000000", OtpPurpose.VerifyEmail), CancellationToken.None);

        bad.IsSuccess.Should().BeFalse();
        bad.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

        var row = await _db.Set<OtpCode>().FirstAsync();
        row.AttemptCount.Should().Be(1);
        row.UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task VerifyAsync_ExpiredCode_ReturnsUnauthorized()
    {
        var u = await SeedUserAsync("exp@example.com");
        _db.Set<OtpCode>().Add(new OtpCode
        {
            UserId       = u.Id,
            Email        = u.Email,
            CodeHash     = HashHelper.Sha256Hex("123456"),
            Purpose      = OtpPurpose.VerifyEmail,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1),
        });
        await _db.SaveChangesAsync();

        var result = await _sut.VerifyAsync(
            new OtpVerifyRequest(u.Email, "123456", OtpPurpose.VerifyEmail), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task VerifyAsync_CorrectCode_MarksUsed_SetsEmailVerified()
    {
        var u = await SeedUserAsync("ok@example.com");
        const string code = "654321";
        _db.Set<OtpCode>().Add(new OtpCode
        {
            UserId       = u.Id,
            Email        = u.Email,
            CodeHash     = HashHelper.Sha256Hex(code),
            Purpose      = OtpPurpose.VerifyEmail,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
        });
        await _db.SaveChangesAsync();

        var result = await _sut.VerifyAsync(
            new OtpVerifyRequest(u.Email, code, OtpPurpose.VerifyEmail), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.User!.EmailVerified.Should().BeTrue();
        (await _db.Set<OtpCode>().FirstAsync()).UsedAtUtc.Should().NotBeNull();
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }
}

internal static class HashHelper
{
    public static string Sha256Hex(string s)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        return Convert.ToHexString(bytes);
    }
}
