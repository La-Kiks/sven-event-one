using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SportsReservationAPI.Tests;

[Collection("Api")]
public class AuthTests
{
    private readonly ApiTestFixture _fixture;

    public AuthTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_WithCorrectAdminCredentials_ReturnsAdminRoleToken()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new
        {
            username = Environment.GetEnvironmentVariable("ADMIN_USERNAME"),
            password = Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("Admin", body.GetProperty("role").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new
        {
            username = Environment.GetEnvironmentVariable("ADMIN_USERNAME"),
            password = "definitely-not-the-right-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ForPendingUnactivatedAccount_ReturnsUnauthorizedNotServerError()
    {
        var email = ApiTestFixture.UniqueEmail("pending");
        await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName = "PendingLoginTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email, phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("pending2"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });

        // Account exists but has never been activated (empty PasswordHash) — must 401 cleanly, not 500.
        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new { username = email, password = "anything" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Activate_WithValidToken_SetsPasswordAndReturnsUserRoleToken()
    {
        var participant1 = ApiTestFixture.UniqueEmail("activate-ok");
        var teamResponse = await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName = "ActivateOkTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = participant1, phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("activate-ok2"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });
        teamResponse.EnsureSuccessStatusCode();

        using var context = _fixture.CreateDbContext();
        var user = await context.Users.FirstAsync(u => u.Username == participant1);
        var token = user.VerificationToken!;

        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/activate", new { token, password = "BrandNewPassword123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("User", body.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Activate_WithInvalidToken_ReturnsBadRequest()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/activate", new { token = "this-token-does-not-exist", password = "SomePassword123" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Activate_WithExpiredToken_ReturnsBadRequest()
    {
        var participant1 = ApiTestFixture.UniqueEmail("expired");
        await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName = "ExpiredTokenTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = participant1, phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("expired2"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });

        // No API path produces an already-expired token — back-date it directly
        // to exercise UserService.VerifyAndSetPasswordAsync's expiry check.
        string token;
        using (var context = _fixture.CreateDbContext())
        {
            var user = await context.Users.FirstAsync(u => u.Username == participant1);
            token = user.VerificationToken!;
            user.VerificationTokenExpiresAt = DateTime.UtcNow.AddDays(-1);
            await context.SaveChangesAsync();
        }

        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/activate", new { token, password = "SomePassword123" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Activate_WithPasswordUnder8Characters_ReturnsBadRequest()
    {
        var participant1 = ApiTestFixture.UniqueEmail("short-pw");
        await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName = "ShortPwTeam", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = participant1, phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("short-pw2"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });

        using var context = _fixture.CreateDbContext();
        var user = await context.Users.FirstAsync(u => u.Username == participant1);

        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/activate", new { token = user.VerificationToken, password = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
