using FluentAssertions;

namespace TrueCapture.Tests.Integration;

public sealed class HealthCheckTests(WebAppFixture fixture) : IClassFixture<WebAppFixture>
{
    [Fact]
    public async Task GET_health_Returns200()
    {
        var client = fixture.CreateClient();
        var resp   = await client.GetAsync("/api/health");
        resp.IsSuccessStatusCode.Should().BeTrue();
    }
}
