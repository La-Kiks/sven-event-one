using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SportsReservationAPI.Tests;

[Collection("Api")]
public class SmokeTests
{
    private readonly ApiTestFixture _fixture;

    public SmokeTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetTeamCount_ReturnsOkWithExpectedShape()
    {
        // Doesn't assume current == 0 — other test classes share this collection
        // and xUnit doesn't guarantee cross-class execution order, only that they
        // don't run concurrently. Asserts structure/consistency instead.
        var response = await _fixture.Client.GetAsync("/api/teams/count");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var current = body.GetProperty("current").GetInt32();
        var max = body.GetProperty("max").GetInt32();
        Assert.True(current >= 0);
        Assert.Equal(current >= max, body.GetProperty("isFull").GetBoolean());
    }
}
