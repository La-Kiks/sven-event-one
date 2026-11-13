using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SportsReservationAPI.Tests;

[Collection("Api")]
public class AnnouncementTests
{
    private readonly ApiTestFixture _fixture;

    public AnnouncementTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    private static HttpRequestMessage AuthedRequest(HttpMethod method, string url, string jwt, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Authorization", $"Bearer {jwt}");
        if (body != null) request.Content = JsonContent.Create(body);
        return request;
    }

    private async Task RegisterTeamAsync(string teamName, string p1Email, string p2Email)
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName, version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = p1Email, phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = p2Email, phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });
        response.EnsureSuccessStatusCode();
    }

    private async Task<int> GetRecipientCountAsync(string adminJwt)
    {
        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/announcements/recipients", adminJwt));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("count").GetInt32();
    }

    [Fact]
    public async Task Recipients_CountsUniqueEmailsCaseInsensitively()
    {
        var adminJwt = await _fixture.GetAdminJwtAsync();
        var before = await GetRecipientCountAsync(adminJwt);

        var shared = ApiTestFixture.UniqueEmail("ann-shared");
        await RegisterTeamAsync("AnnDedupTeamA", ApiTestFixture.UniqueEmail("ann-a1"), shared);
        await RegisterTeamAsync("AnnDedupTeamB", ApiTestFixture.UniqueEmail("ann-b1"), shared.ToUpperInvariant());

        var after = await GetRecipientCountAsync(adminJwt);

        // 4 player rows, 3 unique addresses.
        Assert.Equal(before + 3, after);
    }

    [Fact]
    public async Task Send_ReturnsOneResultPerUniqueEmail()
    {
        var adminJwt = await _fixture.GetAdminJwtAsync();
        var p1 = ApiTestFixture.UniqueEmail("ann-send1");
        var shared = ApiTestFixture.UniqueEmail("ann-send-shared");
        await RegisterTeamAsync("AnnSendTeamA", p1, shared);
        await RegisterTeamAsync("AnnSendTeamB", ApiTestFixture.UniqueEmail("ann-send2"), shared.ToUpperInvariant());

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Post, "/api/announcements/send", adminJwt,
            new { subject = "Report", message = "Nouvelle date bientôt." }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var total = body.GetProperty("total").GetInt32();
        Assert.Equal(total, body.GetProperty("sent").GetInt32() + body.GetProperty("failed").GetInt32());
        Assert.Equal(await GetRecipientCountAsync(adminJwt), total);

        // Mailgun is unconfigured in tests, so every send fails and is listed exactly once.
        var failedEmails = body.GetProperty("failures").EnumerateArray()
            .Select(f => f.GetProperty("email").GetString()!.ToLowerInvariant())
            .ToList();
        Assert.Single(failedEmails, e => e == p1.ToLowerInvariant());
        Assert.Single(failedEmails, e => e == shared.ToLowerInvariant());
    }

    [Fact]
    public async Task Test_WithMailgunUnconfigured_Returns502()
    {
        var adminJwt = await _fixture.GetAdminJwtAsync();

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Post, "/api/announcements/test", adminJwt,
            new { subject = "Test", message = "Bonjour", toEmail = "organizer@example.com" }));

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Theory]
    [InlineData("", "Message")]
    [InlineData("Objet", "")]
    public async Task Send_WithEmptySubjectOrMessage_Returns400(string subject, string message)
    {
        var adminJwt = await _fixture.GetAdminJwtAsync();

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Post, "/api/announcements/send", adminJwt,
            new { subject, message }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Send_WithTooLongSubject_Returns400()
    {
        var adminJwt = await _fixture.GetAdminJwtAsync();

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Post, "/api/announcements/send", adminJwt,
            new { subject = new string('a', 201), message = "Message" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Test_WithInvalidEmail_Returns400()
    {
        var adminJwt = await _fixture.GetAdminJwtAsync();

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Post, "/api/announcements/test", adminJwt,
            new { subject = "Test", message = "Bonjour", toEmail = "not-an-email" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Endpoints_WithParticipantToken_ReturnForbidden()
    {
        var (_, participantJwt) = await _fixture.RegisterAndActivateTeamAsync(
            "AnnForbiddenTeam", ApiTestFixture.UniqueEmail("annforbid1"), ApiTestFixture.UniqueEmail("annforbid2"));

        var recipients = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/announcements/recipients", participantJwt));
        var send = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Post, "/api/announcements/send", participantJwt,
            new { subject = "x", message = "y" }));
        var test = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Post, "/api/announcements/test", participantJwt,
            new { subject = "x", message = "y", toEmail = "a@example.com" }));

        Assert.Equal(HttpStatusCode.Forbidden, recipients.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, send.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, test.StatusCode);
    }

    [Fact]
    public async Task Endpoints_WithoutToken_ReturnUnauthorized()
    {
        var recipients = await _fixture.Client.GetAsync("/api/announcements/recipients");
        var send = await _fixture.Client.PostAsJsonAsync("/api/announcements/send", new { subject = "x", message = "y" });
        var test = await _fixture.Client.PostAsJsonAsync("/api/announcements/test", new { subject = "x", message = "y", toEmail = "a@example.com" });

        Assert.Equal(HttpStatusCode.Unauthorized, recipients.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, send.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, test.StatusCode);
    }
}
