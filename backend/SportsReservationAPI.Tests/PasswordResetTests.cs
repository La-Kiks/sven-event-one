using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SportsReservationAPI.Tests;

// Total calls to /api/auth/forgot-password across this file: 4 (1 + 1 + 2).
// The endpoint's IP rate limit (5/hour) is a real, shared, non-overridable
// singleton for the whole "Api" collection — keep this file's total at or
// below 5 when adding tests, since every test in the collection shares one
// simulated client IP.
[Collection("Api")]
public class PasswordResetTests
{
    private readonly ApiTestFixture _fixture;

    public PasswordResetTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ForgotPassword_ForActivatedAccount_RegeneratesTokenAndAllowsFullResetFlow()
    {
        var participant1 = ApiTestFixture.UniqueEmail("reset-ok");
        await _fixture.RegisterAndActivateTeamAsync(
            "ResetOkTeam", participant1, ApiTestFixture.UniqueEmail("reset-ok2"), password: "OldPassword123");

        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/forgot-password", new { email = participant1 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string newToken;
        using (var context = _fixture.CreateDbContext())
        {
            var user = await context.Users.FirstAsync(u => u.Username == participant1);
            Assert.NotNull(user.VerificationToken);
            Assert.NotNull(user.VerificationTokenExpiresAt);
            newToken = user.VerificationToken!;
        }

        var activateResponse = await _fixture.Client.PostAsJsonAsync("/api/auth/activate", new { token = newToken, password = "NewPassword456" });
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);
        var activated = await activateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("User", activated.GetProperty("role").GetString());

        var loginResponse = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new { username = participant1, password = "NewPassword456" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_ForUnknownEmail_ReturnsSameGenericSuccess()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/forgot-password", new { email = ApiTestFixture.UniqueEmail("unknown") });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Si un compte existe pour cet email, un lien a été envoyé.", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ForgotPassword_CalledTwiceQuicklyForSameEmail_OnlyRegeneratesTokenOnce()
    {
        var participant1 = ApiTestFixture.UniqueEmail("reset-cooldown");
        await _fixture.RegisterAndActivateTeamAsync(
            "ResetCooldownTeam", participant1, ApiTestFixture.UniqueEmail("reset-cooldown2"), password: "OldPassword123");

        var firstResponse = await _fixture.Client.PostAsJsonAsync("/api/auth/forgot-password", new { email = participant1 });
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        string firstToken;
        using (var context = _fixture.CreateDbContext())
        {
            firstToken = (await context.Users.FirstAsync(u => u.Username == participant1)).VerificationToken!;
        }

        var secondResponse = await _fixture.Client.PostAsJsonAsync("/api/auth/forgot-password", new { email = participant1 });
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        using (var context = _fixture.CreateDbContext())
        {
            var secondToken = (await context.Users.FirstAsync(u => u.Username == participant1)).VerificationToken!;
            Assert.Equal(firstToken, secondToken);
        }
    }
}
