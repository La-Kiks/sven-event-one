using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportsReservationAPI.Services;
using Xunit;

namespace SportsReservationAPI.Tests;

[Collection("Api")]
public class UnsubscribeTests
{
    private readonly ApiTestFixture _fixture;

    public UnsubscribeTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    private string Sign(string email) =>
        _fixture.Factory.Services.GetRequiredService<UnsubscribeLinkService>().Sign(email);

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

    private Task<HttpResponseMessage> PostUnsubscribeAsync(string? email, string? sig) =>
        _fixture.Client.PostAsJsonAsync("/api/mail-preferences/unsubscribe", new { email, sig });

    private async Task<List<(string Email, bool AcceptMails)>> GetPlayersAsync(params string[] emails)
    {
        var lowered = emails.Select(e => e.ToLowerInvariant()).ToList();
        using var db = _fixture.CreateDbContext();
        var rows = await db.Players.Where(p => lowered.Contains(p.Email.ToLower())).ToListAsync();
        return rows.Select(p => (p.Email, p.AcceptMails)).ToList();
    }

    [Fact]
    public async Task Unsubscribe_WithValidSig_ClearsAcceptMailsForEveryRowWithThatEmail()
    {
        var p1a = ApiTestFixture.UniqueEmail("unsub-a1");
        var p1b = ApiTestFixture.UniqueEmail("unsub-b1");
        var shared = ApiTestFixture.UniqueEmail("unsub-shared");
        await RegisterTeamAsync("UnsubTeamA", p1a, shared);
        await RegisterTeamAsync("UnsubTeamB", p1b, shared.ToUpperInvariant());

        var response = await PostUnsubscribeAsync(shared, Sign(shared));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sharedRows = await GetPlayersAsync(shared);
        Assert.Equal(2, sharedRows.Count);
        Assert.All(sharedRows, r => Assert.False(r.AcceptMails));
        var others = await GetPlayersAsync(p1a, p1b);
        Assert.Equal(2, others.Count);
        Assert.All(others, r => Assert.True(r.AcceptMails));
    }

    [Fact]
    public async Task Unsubscribe_MatchesStoredEmailCaseInsensitively()
    {
        var mixed = $"Mixed.Case-{Guid.NewGuid():N}@example.com";
        await RegisterTeamAsync("UnsubCaseTeam", ApiTestFixture.UniqueEmail("unsub-case1"), mixed);
        var lower = mixed.ToLowerInvariant();

        var response = await PostUnsubscribeAsync(lower, Sign(lower));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rows = await GetPlayersAsync(mixed);
        Assert.Single(rows);
        Assert.False(rows[0].AcceptMails);
    }

    [Fact]
    public async Task Unsubscribe_IsIdempotent()
    {
        var email = ApiTestFixture.UniqueEmail("unsub-idem");
        await RegisterTeamAsync("UnsubIdemTeam", ApiTestFixture.UniqueEmail("unsub-idem1"), email);

        var first = await PostUnsubscribeAsync(email, Sign(email));
        var second = await PostUnsubscribeAsync(email, Sign(email));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task Unsubscribe_UnknownEmail_ReturnsOk()
    {
        var email = ApiTestFixture.UniqueEmail("nobody");

        var response = await PostUnsubscribeAsync(email, Sign(email));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("other")]
    [InlineData("garbage!!")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("null-email")]
    public async Task Unsubscribe_WithBadSig_ReturnsBadRequestAndChangesNothing(string? sigCase)
    {
        var email = ApiTestFixture.UniqueEmail("unsub-bad");
        await RegisterTeamAsync("UnsubBadTeam" + Guid.NewGuid().ToString("N")[..6], ApiTestFixture.UniqueEmail("unsub-bad1"), email);

        string? postedEmail = email;
        string? sig = sigCase;
        if (sigCase == "other") sig = Sign(ApiTestFixture.UniqueEmail("unsub-other"));
        if (sigCase == "null-email") { postedEmail = null; sig = Sign(email); }

        var response = await PostUnsubscribeAsync(postedEmail, sig);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var rows = await GetPlayersAsync(email);
        Assert.Single(rows);
        Assert.True(rows[0].AcceptMails);
    }
}
