using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace TrueCapture.Tests.Integration;

/// <summary>
/// End-to-end coverage of every scenario in <c>specs/user-profile/spec.md</c>
/// (as amended 2026-05-21 — id-based routing, <c>PUT</c> edit, follow-request flow).
/// Requires Docker: <see cref="WebAppFixture"/> spins up a real PostgreSQL container.
/// </summary>
public sealed class UserProfileTests(WebAppFixture fixture) : IClassFixture<WebAppFixture>
{
    // ---- View own profile ----------------------------------------------

    [Fact]
    public async Task GetMe_WithoutToken_Returns401()
    {
        var client = fixture.CreateClient();
        var resp   = await client.GetAsync("/api/users/me");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithToken_ReturnsProfile()
    {
        var (_, client) = await NewUserAsync();

        var body = await GetJsonAsync(client, "/api/users/me");

        body.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
        body.GetProperty("value").GetProperty("username").GetString().Should().NotBeNullOrEmpty();
    }

    // ---- View another user's public profile ----------------------------

    [Fact]
    public async Task GetProfileById_Existing_ReturnsProfileWithFollowState()
    {
        var (_, viewer)   = await NewUserAsync();
        var (targetId, _) = await NewUserAsync();

        var body = await GetJsonAsync(viewer, $"/api/users/{targetId}");

        body.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
        body.GetProperty("value").GetProperty("followState").GetString().Should().Be("none");
        body.GetProperty("value").GetProperty("isMe").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task GetProfileById_Unknown_ReturnsNotFound()
    {
        var (_, client) = await NewUserAsync();

        var body = await GetJsonAsync(client, "/api/users/999999999");

        body.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        body.GetProperty("statusCode").GetInt32().Should().Be((int)HttpStatusCode.NotFound);
    }

    // ---- Edit profile --------------------------------------------------

    [Fact]
    public async Task UpdateProfile_ChangesDisplayNameAndBio()
    {
        var (_, client) = await NewUserAsync();

        var resp = await client.PutAsJsonAsync("/api/users/me",
            new { displayName = "Jordan Rivers", bio = "Coffee and code." });
        var body = await ReadJsonAsync(resp);

        body.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
        body.GetProperty("value").GetProperty("displayName").GetString().Should().Be("Jordan Rivers");
        body.GetProperty("value").GetProperty("bio").GetString().Should().Be("Coffee and code.");
    }

    // ---- Follow and unfollow -------------------------------------------

    [Fact]
    public async Task FollowPublicUser_CreatesAcceptedEdge_AndBumpsCounters()
    {
        var (followerId, follower) = await NewUserAsync();
        var (targetId, target)     = await NewUserAsync();

        var followBody = await ReadJsonAsync(
            await follower.PostAsync($"/api/users/{targetId}/follow", null));
        followBody.GetProperty("value").GetProperty("followState").GetString().Should().Be("following");

        // Target's denormalized followersCount and follower's followingCount both move.
        var targetProfile = await GetJsonAsync(target, "/api/users/me");
        targetProfile.GetProperty("value").GetProperty("followersCount").GetInt32().Should().Be(1);

        var followerProfile = await GetJsonAsync(follower, "/api/users/me");
        followerProfile.GetProperty("value").GetProperty("followingCount").GetInt32().Should().Be(1);

        _ = followerId;
    }

    [Fact]
    public async Task FollowSelf_Returns422()
    {
        var (selfId, client) = await NewUserAsync();

        var body = await ReadJsonAsync(await client.PostAsync($"/api/users/{selfId}/follow", null));

        body.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        body.GetProperty("statusCode").GetInt32().Should().Be((int)HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Follow_IsIdempotent()
    {
        var (_, follower)      = await NewUserAsync();
        var (targetId, target) = await NewUserAsync();

        await follower.PostAsync($"/api/users/{targetId}/follow", null);
        await follower.PostAsync($"/api/users/{targetId}/follow", null);   // second time

        var targetProfile = await GetJsonAsync(target, "/api/users/me");
        targetProfile.GetProperty("value").GetProperty("followersCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Unfollow_RemovesEdge_AndDecrementsCounter()
    {
        var (_, follower)      = await NewUserAsync();
        var (targetId, target) = await NewUserAsync();

        await follower.PostAsync($"/api/users/{targetId}/follow", null);
        await follower.DeleteAsync($"/api/users/{targetId}/follow");

        var targetProfile = await GetJsonAsync(target, "/api/users/me");
        targetProfile.GetProperty("value").GetProperty("followersCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task FollowPrivateUser_CreatesPendingRequest()
    {
        var (_, follower)      = await NewUserAsync();
        var (targetId, _)      = await NewUserAsync(isPrivate: true);

        var body = await ReadJsonAsync(
            await follower.PostAsync($"/api/users/{targetId}/follow", null));

        body.GetProperty("value").GetProperty("followState").GetString().Should().Be("requested");
    }

    // ---- Followers and following lists ---------------------------------

    [Fact]
    public async Task FollowersList_IncludesTheFollower()
    {
        var (followerId, follower) = await NewUserAsync();
        var (targetId, target)     = await NewUserAsync();

        await follower.PostAsync($"/api/users/{targetId}/follow", null);

        var body  = await GetJsonAsync(target, $"/api/users/{targetId}/followers");
        var items = body.GetProperty("value").GetProperty("items").EnumerateArray()
            .Select(e => e.GetProperty("id").GetInt64()).ToList();

        body.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
        items.Should().Contain(followerId);
    }

    [Fact]
    public async Task PrivateAccount_FollowersList_ForbiddenToNonFollower()
    {
        var (_, viewer)   = await NewUserAsync();
        var (targetId, _) = await NewUserAsync(isPrivate: true);

        var body = await GetJsonAsync(viewer, $"/api/users/{targetId}/followers");

        body.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        body.GetProperty("statusCode").GetInt32().Should().Be((int)HttpStatusCode.Forbidden);
    }

    // ---- Helpers -------------------------------------------------------

    /// <summary>Registers a fresh user, returns their id and an authenticated client.</summary>
    private async Task<(long id, HttpClient client)> NewUserAsync(bool isPrivate = false)
    {
        var client = fixture.CreateClient();
        var tag    = Guid.NewGuid().ToString("N")[..12];

        var reg = await client.PostAsJsonAsync("/api/auth/register",
            new { email = $"{tag}@example.com", username = $"u{tag}", password = "Password123!" });
        var token = (await ReadJsonAsync(reg))
            .GetProperty("value").GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var me = await GetJsonAsync(client, "/api/users/me");
        var id = me.GetProperty("value").GetProperty("id").GetInt64();

        if (isPrivate)
            await client.PutAsJsonAsync("/api/users/me", new { accountType = "private" });

        return (id, client);
    }

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string url)
        => await ReadJsonAsync(await client.GetAsync(url));

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage resp)
    {
        var stream = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        return doc.RootElement.Clone();
    }
}
