using System.Net;
using FluentAssertions;

namespace NurseryCamera.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<NurseryCameraWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(NurseryCameraWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthLive_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthMe_WithoutToken_ReturnsUnauthorizedApiError()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("AUTHENTICATION_REQUIRED");
    }
}
