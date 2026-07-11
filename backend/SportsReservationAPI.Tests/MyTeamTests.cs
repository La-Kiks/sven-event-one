using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SportsReservationAPI.Tests;

[Collection("Api")]
public class MyTeamTests
{
    private readonly ApiTestFixture _fixture;

    public MyTeamTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    private HttpRequestMessage AuthedRequest(HttpMethod method, string url, string jwt)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Authorization", $"Bearer {jwt}");
        return request;
    }

    [Fact]
    public async Task GetMyTeam_ReturnsOwnTeamWithBothPlayers()
    {
        var (teamId, jwt) = await _fixture.RegisterAndActivateTeamAsync(
            "GetMyTeamTest", ApiTestFixture.UniqueEmail("getmt1"), ApiTestFixture.UniqueEmail("getmt2"));

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/teams/my-team", jwt));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(teamId, body.GetProperty("id").GetInt32());
        Assert.Equal(2, body.GetProperty("players").GetArrayLength());
    }

    [Fact]
    public async Task GetMyTeam_WithAdminToken_ReturnsForbidden()
    {
        var adminJwt = await _fixture.GetAdminJwtAsync();

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/teams/my-team", adminJwt));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMyTeam_WithNoToken_ReturnsUnauthorized()
    {
        var response = await _fixture.Client.GetAsync("/api/teams/my-team");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMyTeam_WithValidPayload_PersistsChangesAndLeavesIsPaidUntouched()
    {
        var (_, jwt) = await _fixture.RegisterAndActivateTeamAsync(
            "UpdateMyTeamTest", ApiTestFixture.UniqueEmail("updmt1"), ApiTestFixture.UniqueEmail("updmt2"));

        var current = await (await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/teams/my-team", jwt)))
            .Content.ReadFromJsonAsync<JsonElement>();
        var players = current.GetProperty("players").EnumerateArray().ToList();

        var updateRequest = AuthedRequest(HttpMethod.Put, "/api/teams/my-team", jwt);
        updateRequest.Content = JsonContent.Create(new
        {
            teamDto = new { teamName = "UpdateMyTeamTest - Renamed", version = "long", administration = "pompier" },
            playerDtos = new[]
            {
                new { id = players[0].GetProperty("id").GetInt32(), firstName = "AliceUpdated", lastName = "Test", email = players[0].GetProperty("email").GetString(), phoneNumber = "+33612345678", category = "woman", outfit = "yes", volunteer = true, acceptMails = true },
                new { id = players[1].GetProperty("id").GetInt32(), firstName = "BobUpdated", lastName = "Test", email = players[1].GetProperty("email").GetString(), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });

        var updateResponse = await _fixture.Client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var after = await (await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/teams/my-team", jwt)))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("UpdateMyTeamTest - Renamed", after.GetProperty("name").GetString());
        Assert.False(after.GetProperty("isPaid").GetBoolean());
        Assert.Equal("AliceUpdated", after.GetProperty("players")[0].GetProperty("firstName").GetString());
    }

    [Fact]
    public async Task UpdateMyTeam_WithDuplicatePlayerId_ReturnsBadRequest()
    {
        var (_, jwt) = await _fixture.RegisterAndActivateTeamAsync(
            "DuplicateIdTeam", ApiTestFixture.UniqueEmail("dupid1"), ApiTestFixture.UniqueEmail("dupid2"));

        var current = await (await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/teams/my-team", jwt)))
            .Content.ReadFromJsonAsync<JsonElement>();
        var firstPlayerId = current.GetProperty("players")[0].GetProperty("id").GetInt32();

        // Both entries reference the SAME player id — this is the exact regression
        // this test guards against (found during this project's own code review).
        var updateRequest = AuthedRequest(HttpMethod.Put, "/api/teams/my-team", jwt);
        updateRequest.Content = JsonContent.Create(new
        {
            teamDto = new { teamName = "DuplicateIdTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { id = firstPlayerId, firstName = "Alice", lastName = "Test", email = ApiTestFixture.UniqueEmail("dupid-a"), phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { id = firstPlayerId, firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("dupid-b"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });

        var response = await _fixture.Client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMyTeam_ChangingParticipant1Email_UpdatesLoginUsername()
    {
        var oldEmail = ApiTestFixture.UniqueEmail("emailsync-old");
        var newEmail = ApiTestFixture.UniqueEmail("emailsync-new");
        var (_, jwt) = await _fixture.RegisterAndActivateTeamAsync("EmailSyncTeam", oldEmail, ApiTestFixture.UniqueEmail("emailsync2"));

        var current = await (await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/teams/my-team", jwt)))
            .Content.ReadFromJsonAsync<JsonElement>();
        var players = current.GetProperty("players").EnumerateArray().ToList();

        var updateRequest = AuthedRequest(HttpMethod.Put, "/api/teams/my-team", jwt);
        updateRequest.Content = JsonContent.Create(new
        {
            teamDto = new { teamName = "EmailSyncTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { id = players[0].GetProperty("id").GetInt32(), firstName = "Alice", lastName = "Test", email = newEmail, phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { id = players[1].GetProperty("id").GetInt32(), firstName = "Bob", lastName = "Test", email = players[1].GetProperty("email").GetString(), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });
        (await _fixture.Client.SendAsync(updateRequest)).EnsureSuccessStatusCode();

        var oldLoginResponse = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new { username = oldEmail, password = "TestPassword123" });
        var newLoginResponse = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new { username = newEmail, password = "TestPassword123" });

        Assert.Equal(HttpStatusCode.Unauthorized, oldLoginResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newLoginResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateMyTeam_ChangingParticipant1EmailToAnotherAccountsEmail_ReturnsBadRequest()
    {
        var otherTeamEmail = ApiTestFixture.UniqueEmail("conflict-other");
        await _fixture.RegisterAndActivateTeamAsync("ConflictOtherTeam", otherTeamEmail, ApiTestFixture.UniqueEmail("conflict-other2"));

        var (_, jwt) = await _fixture.RegisterAndActivateTeamAsync(
            "ConflictMyTeam", ApiTestFixture.UniqueEmail("conflict-mine"), ApiTestFixture.UniqueEmail("conflict-mine2"));

        var current = await (await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/teams/my-team", jwt)))
            .Content.ReadFromJsonAsync<JsonElement>();
        var players = current.GetProperty("players").EnumerateArray().ToList();

        var updateRequest = AuthedRequest(HttpMethod.Put, "/api/teams/my-team", jwt);
        updateRequest.Content = JsonContent.Create(new
        {
            teamDto = new { teamName = "ConflictMyTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                // Attempts to steal the other team's participant-1 email as this team's own.
                new { id = players[0].GetProperty("id").GetInt32(), firstName = "Alice", lastName = "Test", email = otherTeamEmail, phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { id = players[1].GetProperty("id").GetInt32(), firstName = "Bob", lastName = "Test", email = players[1].GetProperty("email").GetString(), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });

        var response = await _fixture.Client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
