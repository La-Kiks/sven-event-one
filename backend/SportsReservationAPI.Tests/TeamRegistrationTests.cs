using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SportsReservationAPI.Tests;

[Collection("Api")]
public class TeamRegistrationTests
{
    private readonly ApiTestFixture _fixture;

    public TeamRegistrationTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateTeam_WithValidPayload_ReturnsTeamId()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName = "ValidRegTeam", version = "long", administration = "pompier" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = ApiTestFixture.UniqueEmail("reg1"), phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("reg2"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("teamId").GetInt32() > 0);
    }

    [Fact]
    public async Task CreateTeam_WithOnePlayer_ReturnsBadRequest()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName = "OnePlayerTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = ApiTestFixture.UniqueEmail("onlyone"), phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTeam_WithDuplicateParticipant1Email_ReturnsBadRequest()
    {
        var sharedEmail = ApiTestFixture.UniqueEmail("dup");
        var payload = new
        {
            teamDto = new { teamName = "FirstDupTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = sharedEmail, phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("dup2"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        };
        var firstResponse = await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", payload);
        firstResponse.EnsureSuccessStatusCode();

        var secondPayload = new
        {
            teamDto = new { teamName = "SecondDupTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Someone", lastName = "Else", email = sharedEmail, phoneNumber = "+33612345680", category = "man", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Other", lastName = "Person", email = ApiTestFixture.UniqueEmail("dup3"), phoneNumber = "+33612345681", category = "woman", outfit = "no", volunteer = false, acceptMails = true }
            }
        };

        var secondResponse = await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", secondPayload);

        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
        var body = await secondResponse.Content.ReadAsStringAsync();
        Assert.Contains("associé", body);
    }

    [Fact]
    public async Task GetTeamCount_IncreasesAfterRegistration()
    {
        var before = await _fixture.Client.GetFromJsonAsync<JsonElement>("/api/teams/count");
        var beforeCount = before.GetProperty("current").GetInt32();

        await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName = "CountTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = ApiTestFixture.UniqueEmail("count1"), phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("count2"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });

        var after = await _fixture.Client.GetFromJsonAsync<JsonElement>("/api/teams/count");
        Assert.Equal(beforeCount + 1, after.GetProperty("current").GetInt32());
    }

    [Fact]
    public async Task CreateTeam_DerivesMixtCategoryWhenPlayersDiffer()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName = "MixtCategoryTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = ApiTestFixture.UniqueEmail("mixt1"), phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("mixt2"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        var teamId = created.GetProperty("teamId").GetInt32();

        var adminJwt = await _fixture.GetAdminJwtAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Teams/{teamId}");
        request.Headers.Add("Authorization", $"Bearer {adminJwt}");
        var teamResponse = await _fixture.Client.SendAsync(request);
        var team = await teamResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("mixt", team.GetProperty("category").GetString());
    }
}
