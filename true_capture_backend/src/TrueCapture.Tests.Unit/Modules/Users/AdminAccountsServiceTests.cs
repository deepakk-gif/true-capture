using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Infrastructure.Services;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Identity.Infrastructure;
using TrueCapture.Modules.Users.Models;
using TrueCapture.Modules.Users.Services;
using TrueCapture.Shared.Services;

namespace TrueCapture.Tests.Unit.Modules.Users;

public sealed class AdminAccountsServiceTests : IDisposable
{
    private readonly SqliteConnection      _conn;
    private readonly AppDbContext          _db;
    private readonly AdminAccountsService  _sut;

    public AdminAccountsServiceTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opts, [new IdentityModelConfigurator()]);
        _db.Database.EnsureCreated();

        var baseSvc = new BaseService<AppDbContext>(_db, Substitute.For<IErrorLogger>());
        _sut = new AdminAccountsService(_db, baseSvc);

        SeedRolesAndPermissionsAsync().GetAwaiter().GetResult();
    }

    private async Task SeedRolesAndPermissionsAsync()
    {
        var adminRole = new Role { Code = "admin", Name = "Administrator", Description = "", IsSystem = true };
        _db.Set<Role>().Add(adminRole);

        _db.Set<Permission>().AddRange(
            new Permission { Code = "Users.View",        Module = "Users",  Description = "" },
            new Permission { Code = "Posts.Moderate",    Module = "Posts",  Description = "" },
            new Permission { Code = "FakeVsReal.Publish", Module = "FvR",   Description = "" },
            new Permission { Code = "Users.CreateAdmin", Module = "Users",  Description = "super-admin only" });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task ListPermissionsAsync_ReturnsEveryRow_OrderedByModuleThenCode()
    {
        var result = await _sut.ListPermissionsAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(4);
        result.Value!.Select(p => p.Code).Should().Contain("Users.CreateAdmin");
    }

    [Fact]
    public async Task CreateAdminAsync_HappyPath_PersistsUser_RoleAndPermissions()
    {
        var req = new CreateAdminRequest(
            Email:           "moderator@example.com",
            Username:        "moderator",
            Password:        "ModPass1!",
            DisplayName:     "Mod One",
            PermissionCodes: new[] { "Users.View", "Posts.Moderate" });

        var result = await _sut.CreateAdminAsync(req, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.GrantedPermissionCodes.Should()
            .BeEquivalentTo(new[] { "Users.View", "Posts.Moderate" });

        var user = await _db.Set<User>().FirstAsync(u => u.Email == "moderator@example.com");
        user.IsAdmin.Should().BeTrue();
        user.EmailVerified.Should().BeTrue();
        user.IsActive.Should().BeTrue();
        user.DisplayName.Should().Be("Mod One");
        BCrypt.Net.BCrypt.Verify("ModPass1!", user.PasswordHash).Should().BeTrue();

        // Attached to the admin role
        var adminRoleAttached = await _db.Set<UserRole>()
            .Join(_db.Set<Role>(), ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Code })
            .AnyAsync(x => x.UserId == user.Id && x.Code == "admin");
        adminRoleAttached.Should().BeTrue();

        // Per-user permissions inserted
        var grants = await _db.Set<UserPermission>()
            .Where(up => up.UserId == user.Id)
            .Join(_db.Set<Permission>(), up => up.PermissionId, p => p.Id, (_, p) => p.Code)
            .ToListAsync();
        grants.Should().BeEquivalentTo(new[] { "Users.View", "Posts.Moderate" });

        // The created admin must NOT silently inherit Users.CreateAdmin —
        // it was not in the request, so it must not appear in their grants.
        grants.Should().NotContain("Users.CreateAdmin");
    }

    [Fact]
    public async Task CreateAdminAsync_UnknownPermissionCode_ReturnsValidation()
    {
        var req = new CreateAdminRequest(
            Email:           "bad@example.com",
            Username:        "bad",
            Password:        "BadPass1!",
            DisplayName:     null,
            PermissionCodes: new[] { "Users.View", "Does.Not.Exist" });

        var result = await _sut.CreateAdminAsync(req, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.UnprocessableEntity);
        (await _db.Set<User>().AnyAsync(u => u.Email == "bad@example.com")).Should().BeFalse();
    }

    [Fact]
    public async Task CreateAdminAsync_DuplicateEmail_ReturnsConflict()
    {
        _db.Set<User>().Add(new User
        {
            Email        = "taken@example.com",
            Username     = "taken",
            PasswordHash = "x",
            IsActive     = true,
        });
        await _db.SaveChangesAsync();

        var result = await _sut.CreateAdminAsync(
            new CreateAdminRequest("taken@example.com", "newname", "GoodPass1!", null, Array.Empty<string>()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateAdminAsync_ShortPassword_ReturnsValidation()
    {
        var result = await _sut.CreateAdminAsync(
            new CreateAdminRequest("short@example.com", "shorty", "abc", null, Array.Empty<string>()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.UnprocessableEntity);
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }
}
