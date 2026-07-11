using System.Net;
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
    public async Task GetTeamCount_ReturnsOkWithZeroTeamsOnFreshDatabase()
    {
        var response = await _fixture.Client.GetAsync("/api/teams/count");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"current\":0", body);
        Assert.Contains("\"isFull\":false", body);
    }
}
