using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Infrastructure.Services;
using TrueCapture.Modules.Identity.Infrastructure;
using TrueCapture.Modules.Identity.Services;
using TrueCapture.Shared.Services;

namespace TrueCapture.Tests.Unit.Modules.Identity;

public sealed class AuthServiceTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext     _db;
    private readonly AuthService      _sut;

    public AuthServiceTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_conn)
            .Options;

        _db = new AppDbContext(opts, [new IdentityModelConfigurator()]);
        _db.Database.EnsureCreated();

        var baseSvc = new BaseService<AppDbContext>(_db, Substitute.For<IErrorLogger>());

        var jwt = Options.Create(new JwtOptions
        {
            Issuer        = "test",
            Audience      = "test",
            SigningKey    = new string('k', 64),
            AccessMinutes = 15,
            RefreshDays   = 30,
        });
        var tokens = new TokenService(jwt);

        _sut = new AuthService(
            _db, baseSvc, tokens,
            Substitute.For<IOtpService>(),
            Substitute.For<IUserDeviceService>(),
            Options.Create(new GoogleAuthOptions { ClientId = "" }));
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_ReturnsTokens()
    {
        var result = await _sut.RegisterAsync(
            new RegisterDto("new@user.com", "newuser", "Password123!"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsConflict()
    {
        await _sut.RegisterAsync(new RegisterDto("dup@user.com", "user1", "Password123!"), CancellationToken.None);
        var second = await _sut.RegisterAsync(new RegisterDto("dup@user.com", "user2", "Password123!"), CancellationToken.None);

        second.IsSuccess.Should().BeFalse();
        second.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsUnauthorized()
    {
        await _sut.RegisterAsync(new RegisterDto("login@user.com", "loginuser", "RightPass1!"), CancellationToken.None);

        var login = await _sut.LoginAsync(new LoginDto("login@user.com", "WrongPass!"), null, null, CancellationToken.None);

        login.IsSuccess.Should().BeFalse();
        login.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }
}
