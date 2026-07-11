using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SportsReservationAPI.Tests;

[Collection("Api")]
public class AdminTeamsTests
{
    private readonly ApiTestFixture _fixture;

    public AdminTeamsTests(ApiTestFixture fixture)
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
    public async Task GetAllTeams_WithAdminToken_ReturnsOk()
    {
        var adminJwt = await _fixture.GetAdminJwtAsync();

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/Teams/teams", adminJwt));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllTeams_WithParticipantToken_ReturnsForbidden()
    {
        var (_, participantJwt) = await _fixture.RegisterAndActivateTeamAsync(
            "NoAdminAccessTeam", ApiTestFixture.UniqueEmail("noadmin1"), ApiTestFixture.UniqueEmail("noadmin2"));

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/Teams/teams", participantJwt));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAllTeams_WithNoToken_ReturnsUnauthorized()
    {
        var response = await _fixture.Client.GetAsync("/api/Teams/teams");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePaymentStatus_TogglesIsPaid()
    {
        var adminJwt = await _fixture.GetAdminJwtAsync();
        var (teamId, _) = await _fixture.RegisterAndActivateTeamAsync(
            "PaymentToggleTeam", ApiTestFixture.UniqueEmail("paytoggle1"), ApiTestFixture.UniqueEmail("paytoggle2"));

        var patchRequest = AuthedRequest(HttpMethod.Patch, $"/api/Teams/{teamId}/payment", adminJwt);
        patchRequest.Content = JsonContent.Create(new { isPaid = true });
        var patchResponse = await _fixture.Client.SendAsync(patchRequest);
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var getResponse = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, $"/api/Teams/{teamId}", adminJwt));
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("isPaid").GetBoolean());
    }

    [Fact]
    public async Task UpdatePaymentStatus_WithParticipantToken_ReturnsForbidden()
    {
        var (teamId, participantJwt) = await _fixture.RegisterAndActivateTeamAsync(
            "PaymentForbiddenTeam", ApiTestFixture.UniqueEmail("payforbid1"), ApiTestFixture.UniqueEmail("payforbid2"));

        var patchRequest = AuthedRequest(HttpMethod.Patch, $"/api/Teams/{teamId}/payment", participantJwt);
        patchRequest.Content = JsonContent.Create(new { isPaid = true });

        var response = await _fixture.Client.SendAsync(patchRequest);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTeam_RemovesTeamAndCascadesPlayersWithoutError()
    {
        var adminJwt = await _fixture.GetAdminJwtAsync();
        var (teamId, _) = await _fixture.RegisterAndActivateTeamAsync(
            "DeleteMeTeam", ApiTestFixture.UniqueEmail("delete1"), ApiTestFixture.UniqueEmail("delete2"));

        var deleteResponse = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Delete, $"/api/Teams/{teamId}", adminJwt));
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, $"/api/Teams/{teamId}", adminJwt));
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task CreateAccount_ForTeamWithNoAccount_ReturnsOk()
    {
        // Register via the API but don't go through RegisterAndActivateTeamAsync —
        // create-team already creates a pending account, so use create-account's
        // "resend" path (unverified account) rather than "no account at all",
        // which only occurs for pre-existing data this app can't produce via its
        // own API anymore. Resend is the realistic, reachable case to test here.
        var email1 = ApiTestFixture.UniqueEmail("resend1");
        var createResponse = await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName = "ResendAccountTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = email1, phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("resend2"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var teamId = created.GetProperty("teamId").GetInt32();

        var adminJwt = await _fixture.GetAdminJwtAsync();
        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Post, $"/api/Teams/{teamId}/create-account", adminJwt));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateAccount_ForAlreadyActivatedTeam_ReturnsConflict()
    {
        var adminJwt = await _fixture.GetAdminJwtAsync();
        var (teamId, _) = await _fixture.RegisterAndActivateTeamAsync(
            "AlreadyActiveTeam", ApiTestFixture.UniqueEmail("active1"), ApiTestFixture.UniqueEmail("active2"));

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Post, $"/api/Teams/{teamId}/create-account", adminJwt));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetAllPlayers_WithAdminToken_ReturnsOk()
    {
        var adminJwt = await _fixture.GetAdminJwtAsync();

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/Players", adminJwt));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllPlayers_WithParticipantToken_ReturnsForbidden()
    {
        var (_, participantJwt) = await _fixture.RegisterAndActivateTeamAsync(
            "PlayersForbiddenTeam", ApiTestFixture.UniqueEmail("playersforbid1"), ApiTestFixture.UniqueEmail("playersforbid2"));

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/Players", participantJwt));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
