# Mot de passe oublié — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a participant who forgot their password request a reset link by email, reusing the existing account-activation token mechanism, with anti-abuse rate limiting so the feature can't burn through the shared 100/day Mailgun quota.

**Architecture:** A new public `POST /api/auth/forgot-password` endpoint regenerates the same `VerificationToken`/`VerificationTokenExpiresAt` fields already used for activation and reuses the existing `/activer-compte` page and `POST /api/auth/activate` endpoint unchanged. A new in-memory singleton `PasswordResetRateLimiter` enforces a per-email cooldown, a per-IP hourly limit, and a global daily cap. The response is always the same generic success message regardless of whether the email exists, except for the two rate-limit cases which return an explicit 429.

**Tech Stack:** ASP.NET Core 8, EF Core, FluentValidation, xUnit (existing `docker compose run --rm tests` suite), Angular 19 standalone components.

## Global Constraints

- Reuse `/activer-compte` (`POST /api/auth/activate`) unchanged — no new "set password" page.
- Admin accounts (`Role == "Admin"`) are excluded from this flow entirely.
- Response on success (email found, email unknown, or email under cooldown) is always the identical body: `{ message: "Si un compte existe pour cet email, un lien a été envoyé." }`, HTTP 200.
- Per-email cooldown: 15 minutes.
- Per-IP limit: 5 requests/hour.
- Global daily cap: 20 emails/day.
- Rate-limit counters are in-memory only (no DB table, no external cache) — acceptable since one backend container runs in prod.
- `MailService`'s existing Mailgun HTTP mechanics must not be duplicated — factor into a shared private helper used by both the activation and reset email methods.

---

### Task 1: `RateLimitExceededException` + `PasswordResetRateLimiter`

**Files:**
- Create: `backend/SportsReservationAPI/Exceptions/RateLimitExceededException.cs`
- Create: `backend/SportsReservationAPI/Services/PasswordResetRateLimiter.cs`
- Test: `backend/SportsReservationAPI.Tests/PasswordResetRateLimiterTests.cs`

**Interfaces:**
- Produces (consumed by Task 3): `RateLimitExceededException(string message) : Exception`; `PasswordResetRateLimiter` with constructor `PasswordResetRateLimiter(int emailCooldownMinutes = 15, int maxRequestsPerIpPerHour = 5, int maxGlobalPerDay = 20, Func<DateTime>? now = null)` and methods `bool TryRegisterIpRequest(string ipKey)`, `bool TryRegisterGlobalRequest()`, `bool IsEmailInCooldown(string email)`, `void RecordEmailRequest(string email)`.

This class has no dependency on the DB or HTTP pipeline, so it's tested with plain xUnit unit tests (no `WebApplicationFactory`, no `[Collection("Api")]`) using an injectable clock to avoid real `Thread.Sleep` waits.

- [ ] **Step 1: Write the failing unit tests**

Create `backend/SportsReservationAPI.Tests/PasswordResetRateLimiterTests.cs`:

```csharp
using SportsReservationAPI.Services;
using Xunit;

namespace SportsReservationAPI.Tests;

public class PasswordResetRateLimiterTests
{
    [Fact]
    public void TryRegisterIpRequest_UnderLimit_ReturnsTrue()
    {
        var limiter = new PasswordResetRateLimiter(maxRequestsPerIpPerHour: 3);

        Assert.True(limiter.TryRegisterIpRequest("1.2.3.4"));
        Assert.True(limiter.TryRegisterIpRequest("1.2.3.4"));
        Assert.True(limiter.TryRegisterIpRequest("1.2.3.4"));
    }

    [Fact]
    public void TryRegisterIpRequest_OverLimit_ReturnsFalse()
    {
        var limiter = new PasswordResetRateLimiter(maxRequestsPerIpPerHour: 3);

        limiter.TryRegisterIpRequest("1.2.3.4");
        limiter.TryRegisterIpRequest("1.2.3.4");
        limiter.TryRegisterIpRequest("1.2.3.4");

        Assert.False(limiter.TryRegisterIpRequest("1.2.3.4"));
    }

    [Fact]
    public void TryRegisterIpRequest_DifferentIps_AreIndependent()
    {
        var limiter = new PasswordResetRateLimiter(maxRequestsPerIpPerHour: 1);

        Assert.True(limiter.TryRegisterIpRequest("1.2.3.4"));
        Assert.True(limiter.TryRegisterIpRequest("5.6.7.8"));
    }

    [Fact]
    public void TryRegisterIpRequest_AfterWindowExpires_AllowsAgain()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var limiter = new PasswordResetRateLimiter(maxRequestsPerIpPerHour: 1, now: () => now);

        Assert.True(limiter.TryRegisterIpRequest("1.2.3.4"));
        Assert.False(limiter.TryRegisterIpRequest("1.2.3.4"));

        now = now.AddHours(1).AddMinutes(1);

        Assert.True(limiter.TryRegisterIpRequest("1.2.3.4"));
    }

    [Fact]
    public void TryRegisterGlobalRequest_OverLimit_ReturnsFalse()
    {
        var limiter = new PasswordResetRateLimiter(maxGlobalPerDay: 2);

        Assert.True(limiter.TryRegisterGlobalRequest());
        Assert.True(limiter.TryRegisterGlobalRequest());
        Assert.False(limiter.TryRegisterGlobalRequest());
    }

    [Fact]
    public void TryRegisterGlobalRequest_AfterWindowExpires_AllowsAgain()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var limiter = new PasswordResetRateLimiter(maxGlobalPerDay: 1, now: () => now);

        Assert.True(limiter.TryRegisterGlobalRequest());
        Assert.False(limiter.TryRegisterGlobalRequest());

        now = now.AddHours(24).AddMinutes(1);

        Assert.True(limiter.TryRegisterGlobalRequest());
    }

    [Fact]
    public void IsEmailInCooldown_BeforeAnyRequest_ReturnsFalse()
    {
        var limiter = new PasswordResetRateLimiter();

        Assert.False(limiter.IsEmailInCooldown("alice@example.com"));
    }

    [Fact]
    public void IsEmailInCooldown_JustAfterRequest_ReturnsTrue()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var limiter = new PasswordResetRateLimiter(emailCooldownMinutes: 15, now: () => now);

        limiter.RecordEmailRequest("alice@example.com");

        Assert.True(limiter.IsEmailInCooldown("alice@example.com"));
    }

    [Fact]
    public void IsEmailInCooldown_AfterCooldownExpires_ReturnsFalse()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var limiter = new PasswordResetRateLimiter(emailCooldownMinutes: 15, now: () => now);

        limiter.RecordEmailRequest("alice@example.com");
        now = now.AddMinutes(16);

        Assert.False(limiter.IsEmailInCooldown("alice@example.com"));
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `docker compose run --rm tests`
Expected: build failure — `PasswordResetRateLimiter` does not exist yet.

- [ ] **Step 3: Implement `RateLimitExceededException`**

Create `backend/SportsReservationAPI/Exceptions/RateLimitExceededException.cs`:

```csharp
namespace SportsReservationAPI.Exceptions
{
    public class RateLimitExceededException : Exception
    {
        public RateLimitExceededException(string message) : base(message) { }
    }
}
```

- [ ] **Step 4: Implement `PasswordResetRateLimiter`**

Create `backend/SportsReservationAPI/Services/PasswordResetRateLimiter.cs`:

```csharp
using System.Collections.Concurrent;

namespace SportsReservationAPI.Services
{
    // Registered as a singleton (Program.cs) — state must persist across requests
    // within the process. In-memory only: counters reset on backend restart,
    // which is acceptable since a single backend container runs in prod.
    public class PasswordResetRateLimiter
    {
        private readonly TimeSpan _emailCooldown;
        private readonly int _maxRequestsPerIpPerHour;
        private readonly int _maxGlobalPerDay;
        private readonly Func<DateTime> _now;

        private readonly ConcurrentDictionary<string, DateTime> _lastRequestByEmail = new();
        private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _requestsByIp = new();
        private readonly ConcurrentQueue<DateTime> _globalRequests = new();

        public PasswordResetRateLimiter(
            int emailCooldownMinutes = 15,
            int maxRequestsPerIpPerHour = 5,
            int maxGlobalPerDay = 20,
            Func<DateTime>? now = null)
        {
            _emailCooldown = TimeSpan.FromMinutes(emailCooldownMinutes);
            _maxRequestsPerIpPerHour = maxRequestsPerIpPerHour;
            _maxGlobalPerDay = maxGlobalPerDay;
            _now = now ?? (() => DateTime.UtcNow);
        }

        public bool TryRegisterIpRequest(string ipKey)
        {
            var now = _now();
            var queue = _requestsByIp.GetOrAdd(ipKey, _ => new ConcurrentQueue<DateTime>());

            while (queue.TryPeek(out var oldest) && now - oldest > TimeSpan.FromHours(1))
                queue.TryDequeue(out _);

            if (queue.Count >= _maxRequestsPerIpPerHour)
                return false;

            queue.Enqueue(now);
            return true;
        }

        public bool TryRegisterGlobalRequest()
        {
            var now = _now();

            while (_globalRequests.TryPeek(out var oldest) && now - oldest > TimeSpan.FromHours(24))
                _globalRequests.TryDequeue(out _);

            if (_globalRequests.Count >= _maxGlobalPerDay)
                return false;

            _globalRequests.Enqueue(now);
            return true;
        }

        public bool IsEmailInCooldown(string email)
        {
            var now = _now();
            return _lastRequestByEmail.TryGetValue(email, out var last) && now - last < _emailCooldown;
        }

        public void RecordEmailRequest(string email)
        {
            _lastRequestByEmail[email] = _now();
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `docker compose run --rm tests`
Expected: all `PasswordResetRateLimiterTests` PASS, full suite still green.

- [ ] **Step 6: Commit**

```bash
git add backend/SportsReservationAPI/Exceptions/RateLimitExceededException.cs backend/SportsReservationAPI/Services/PasswordResetRateLimiter.cs backend/SportsReservationAPI.Tests/PasswordResetRateLimiterTests.cs
git commit -m "feat: add in-memory rate limiter for password reset requests"
```

---

### Task 2: `MailService` — factor shared sender, add password-reset email

**Files:**
- Modify: `backend/SportsReservationAPI/Services/MailService.cs` (entire file)

**Interfaces:**
- Consumes: nothing new.
- Produces (consumed by Task 3): `Task<bool> SendPasswordResetEmailAsync(string toEmail, string toName, string resetUrl)`, alongside the existing unchanged `Task<bool> SendActivationEmailAsync(string toEmail, string toName, string activationUrl)`.

No dedicated unit test exists for `MailService` today (its Mailgun call is exercised indirectly by integration tests that run with Mailgun unconfigured, hitting the no-op path) — this task follows that same established pattern; the deliverable is verified by the full suite staying green.

- [ ] **Step 1: Replace the file**

Replace the full contents of `backend/SportsReservationAPI/Services/MailService.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using SportsReservationAPI.Models;

namespace SportsReservationAPI.Services;

public class MailService
{
    private readonly HttpClient _httpClient;
    private readonly MailSettings _mailSettings;
    private readonly ILogger<MailService> _logger;

    public MailService(HttpClient httpClient, IOptions<ApiSettings> apiSettings, ILogger<MailService> logger)
    {
        _httpClient = httpClient;
        _mailSettings = apiSettings.Value.Mail;
        _logger = logger;
    }

    public Task<bool> SendActivationEmailAsync(string toEmail, string toName, string activationUrl)
    {
        var toNameHtml = WebUtility.HtmlEncode(toName);
        var text =
            $"Bonjour {toName},\n\n" +
            "Votre equipe a bien ete enregistree. Cliquez sur le lien ci-dessous pour verifier votre email et definir votre mot de passe :\n" +
            $"{activationUrl}\n\n" +
            "Ce lien est valable 7 jours.\n\n" +
            "A bientot,\nSport Challenge Police 54";
        var html =
            $"<p>Bonjour {toNameHtml},</p>" +
            "<p>Votre équipe a bien été enregistrée. Cliquez sur le lien ci-dessous pour vérifier votre email et définir votre mot de passe :</p>" +
            $"<p><a href=\"{activationUrl}\">{activationUrl}</a></p>" +
            "<p>Ce lien est valable 7 jours.</p>" +
            "<p>À bientôt,<br>Sport Challenge Police 54</p>";

        return SendEmailAsync(toEmail, toName, "Activez votre compte - Sport Challenge Police 54", text, html);
    }

    public Task<bool> SendPasswordResetEmailAsync(string toEmail, string toName, string resetUrl)
    {
        var toNameHtml = WebUtility.HtmlEncode(toName);
        var text =
            $"Bonjour {toName},\n\n" +
            "Vous avez demande la reinitialisation de votre mot de passe. Cliquez sur le lien ci-dessous pour en definir un nouveau :\n" +
            $"{resetUrl}\n\n" +
            "Ce lien est valable 7 jours. Si vous n'etes pas a l'origine de cette demande, ignorez cet email.\n\n" +
            "A bientot,\nSport Challenge Police 54";
        var html =
            $"<p>Bonjour {toNameHtml},</p>" +
            "<p>Vous avez demandé la réinitialisation de votre mot de passe. Cliquez sur le lien ci-dessous pour en définir un nouveau :</p>" +
            $"<p><a href=\"{resetUrl}\">{resetUrl}</a></p>" +
            "<p>Ce lien est valable 7 jours. Si vous n'êtes pas à l'origine de cette demande, ignorez cet email.</p>" +
            "<p>À bientôt,<br>Sport Challenge Police 54</p>";

        return SendEmailAsync(toEmail, toName, "Réinitialisation de votre mot de passe - Sport Challenge Police 54", text, html);
    }

    private async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string text, string html)
    {
        if (string.IsNullOrWhiteSpace(_mailSettings.ApiKey) || string.IsNullOrWhiteSpace(_mailSettings.Domain))
        {
            _logger.LogWarning("Mailgun is not configured (MAILGUN_API_KEY/MAILGUN_DOMAIN missing) - skipping email to {Email}", toEmail);
            return false;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_mailSettings.BaseUrl}/v3/{_mailSettings.Domain}/messages");
            var authBytes = Encoding.UTF8.GetBytes($"api:{_mailSettings.ApiKey}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var fromName = string.IsNullOrWhiteSpace(_mailSettings.FromName) ? "Sport Challenge Police 54" : _mailSettings.FromName;
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["from"] = $"{fromName} <{_mailSettings.FromAddress}>",
                ["to"] = $"{toName} <{toEmail}>",
                ["subject"] = subject,
                ["text"] = text,
                ["html"] = html
            });

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Mailgun request failed ({StatusCode}) for {Email}: {Body}", response.StatusCode, toEmail, responseBody);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email to {Email}", toEmail);
            return false;
        }
    }
}
```

- [ ] **Step 2: Run the full suite to confirm no regression**

Run: `docker compose run --rm tests`
Expected: same pass count as before this task (this refactor changes no observable behavior of `SendActivationEmailAsync`).

- [ ] **Step 3: Commit**

```bash
git add backend/SportsReservationAPI/Services/MailService.cs
git commit -m "refactor: factor MailService's Mailgun call into a shared helper, add password-reset email"
```

---

### Task 3: `POST /api/auth/forgot-password` endpoint

**Files:**
- Create: `backend/SportsReservationAPI/Models/User/ForgotPasswordDto.cs`
- Create: `backend/SportsReservationAPI/Models/User/ForgotPasswordDtoValidator.cs`
- Modify: `backend/SportsReservationAPI/Services/UserService.cs`
- Modify: `backend/SportsReservationAPI/Controllers/AuthController.cs`
- Modify: `backend/SportsReservationAPI/Program.cs`
- Test: `backend/SportsReservationAPI.Tests/PasswordResetTests.cs`

**Interfaces:**
- Consumes: `PasswordResetRateLimiter` (Task 1), `MailService.SendPasswordResetEmailAsync` (Task 2), existing private `UserService.GenerateToken()` and public `UserService.BuildActivationUrl(string token)`.
- Produces: `POST /api/auth/forgot-password` taking `{ email }` → 200 `{ message: "Si un compte existe pour cet email, un lien a été envoyé." }` (always, on the non-rate-limited path) or 429 `{ error: <message> }` when a rate limit is hit. Consumed by Task 4 (frontend).

**Test-budget note:** the endpoint's IP rate limit (5 requests/hour) is a real, non-overridable-per-test production limiter shared by every test in the `[Collection("Api")]` fixture (one simulated client IP for the whole test run). The 3 tests below make 4 total calls to this endpoint, leaving 1 call of headroom under the limit — do not add further calls to this endpoint elsewhere in the `Api` collection without recomputing this budget. The 429-on-exceeded-limit behavior itself is not re-verified here via HTTP (doing so would either consume this shared budget in a way that couples test-class ordering, or require a second `WebApplicationFactory` instance racing the shared fixture's database-drop/migrate step under xUnit's default cross-collection parallelism) — it's already covered deterministically by Task 1's unit tests against `PasswordResetRateLimiter` directly, and the controller's `catch`-and-map pattern is identical to the already-established `AccountAlreadyActivatedException` → 409 mapping in `TeamsController.CreateAccount`.

- [ ] **Step 1: Write the failing integration tests**

Create `backend/SportsReservationAPI.Tests/PasswordResetTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run to verify it fails**

Run: `docker compose run --rm tests`
Expected: FAIL — `/api/auth/forgot-password` returns 404 (route doesn't exist yet).

- [ ] **Step 3: Create the DTO and validator**

Create `backend/SportsReservationAPI/Models/User/ForgotPasswordDto.cs`:

```csharp
namespace SportsReservationAPI.Models.User
{
    public class ForgotPasswordDto
    {
        public string Email { get; set; } = null!;
    }
}
```

Create `backend/SportsReservationAPI/Models/User/ForgotPasswordDtoValidator.cs`:

```csharp
using FluentValidation;

namespace SportsReservationAPI.Models.User
{
    public class ForgotPasswordDtoValidator : AbstractValidator<ForgotPasswordDto>
    {
        public ForgotPasswordDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email is required.");
        }
    }
}
```

- [ ] **Step 4: Add `UserService.RequestPasswordResetAsync`**

In `backend/SportsReservationAPI/Services/UserService.cs`, add the `PasswordResetRateLimiter` dependency and the new method.

Replace the constructor:

```csharp
        private readonly ReservationContext _context;
        private readonly MailService _mailService;
        private readonly ApiSettings _apiSettings;
        private readonly PasswordResetRateLimiter _rateLimiter;

        public UserService(ReservationContext context, MailService mailService, IOptions<ApiSettings> apiSettings, PasswordResetRateLimiter rateLimiter)
        {
            _context = context;
            _mailService = mailService;
            _apiSettings = apiSettings.Value;
            _rateLimiter = rateLimiter;
        }
```

Add this method (e.g. after `BuildActivationUrl`, before the private `GenerateToken`):

```csharp
        // Public self-service password reset. Reuses the activation token fields
        // and the existing /activer-compte flow — VerifyAndSetPasswordAsync works
        // identically whether the account was previously verified or not.
        // Always completes without throwing except for the two rate-limit cases;
        // the caller (AuthController) must return the same generic response for
        // every other outcome (unknown email, email in cooldown) to avoid
        // revealing which emails have an account.
        public async Task RequestPasswordResetAsync(string email, string? ipAddress)
        {
            if (!_rateLimiter.TryRegisterIpRequest(ipAddress ?? "unknown"))
                throw new RateLimitExceededException("Trop de tentatives depuis cette adresse. Réessayez plus tard.");

            if (!_rateLimiter.TryRegisterGlobalRequest())
                throw new RateLimitExceededException("Trop de demandes de réinitialisation aujourd'hui. Réessayez demain.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == email && u.Role == "User");
            if (user == null || user.TeamId == null)
                return;

            if (_rateLimiter.IsEmailInCooldown(email))
                return;

            var team = await _context.Teams.Include(t => t.Players).FirstOrDefaultAsync(t => t.Id == user.TeamId);
            if (team == null || team.Players.Count == 0)
                return;

            var participant1 = team.Players.OrderBy(p => p.Id).First();

            user.VerificationToken = GenerateToken();
            user.VerificationTokenExpiresAt = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            _rateLimiter.RecordEmailRequest(email);

            await _mailService.SendPasswordResetEmailAsync(user.Username, participant1.FirstName, BuildActivationUrl(user.VerificationToken));
        }
```

- [ ] **Step 5: Add the controller endpoint**

In `backend/SportsReservationAPI/Controllers/AuthController.cs`, add this action (e.g. after `Activate`, before the closing brace of the class):

```csharp
        // Public self-service password reset request. Always returns the same
        // generic 200 message regardless of whether the email exists or is
        // under cooldown, to avoid revealing which emails have an account.
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _userService.RequestPasswordResetAsync(dto.Email, HttpContext.Connection.RemoteIpAddress?.ToString());
            }
            catch (RateLimitExceededException ex)
            {
                return StatusCode(429, new { Error = ex.Message });
            }

            return Ok(new { Message = "Si un compte existe pour cet email, un lien a été envoyé." });
        }
```

- [ ] **Step 6: Register the rate limiter in DI**

In `backend/SportsReservationAPI/Program.cs`, change:

```csharp
builder.Services.AddScoped<UserService>();
builder.Services.AddHttpClient<MailService>();
```

to:

```csharp
builder.Services.AddScoped<UserService>();
builder.Services.AddHttpClient<MailService>();
builder.Services.AddSingleton<PasswordResetRateLimiter>();
```

- [ ] **Step 7: Run tests to verify they pass**

Run: `docker compose run --rm tests`
Expected: all `PasswordResetTests` PASS, full suite still green.

- [ ] **Step 8: Update CLAUDE.md**

In `CLAUDE.md`, in the bullet describing `AuthController` / `AuthService`, add a mention of the new endpoint. Find the sentence starting with "`AuthController` / `AuthService`: `/login`..." and add after the existing description of `/activate`:

```
 A public `/forgot-password` endpoint (participants only, admin excluded) regenerates the same `VerificationToken`/`VerificationTokenExpiresAt` fields and reuses `/activate`'s flow unchanged — it always returns the same generic response regardless of whether the email exists, to avoid leaking which emails are registered. Anti-abuse (`Services/PasswordResetRateLimiter.cs`, in-memory singleton): 15-minute per-email cooldown, 5 requests/hour per IP, 20 emails/day globally — the IP/global limits return 429, the cooldown is silent.
```

- [ ] **Step 9: Commit**

```bash
git add backend/SportsReservationAPI/Models/User/ForgotPasswordDto.cs backend/SportsReservationAPI/Models/User/ForgotPasswordDtoValidator.cs backend/SportsReservationAPI/Services/UserService.cs backend/SportsReservationAPI/Controllers/AuthController.cs backend/SportsReservationAPI/Program.cs backend/SportsReservationAPI.Tests/PasswordResetTests.cs CLAUDE.md
git commit -m "feat: add forgot-password endpoint reusing the activation token flow"
```

---

### Task 4: Frontend — forgot-password page + login link

**Files:**
- Modify: `ui/src/app/services/auth/auth.service.ts`
- Create: `ui/src/app/pages/forgot-password/forgot-password.component.ts`
- Create: `ui/src/app/pages/forgot-password/forgot-password.component.html`
- Create: `ui/src/app/pages/forgot-password/forgot-password.component.scss`
- Modify: `ui/src/app/app.routes.ts`
- Modify: `ui/src/app/pages/login/login.component.html`
- Modify: `docs/manual-testing-guide.md`

**Interfaces:**
- Consumes: `POST /api/auth/forgot-password` (Task 3) taking `{ email }`, returning `{ message }` on 200 or `{ error }` on 429.
- Produces: nothing consumed by later tasks (this is the last task).

This task has no backend test suite to run; it's verified manually against the running dev stack (`docker compose up --build`), per this codebase's established pattern (frontend changes in this project have no automated test coverage — see `CLAUDE.md`: "There is no test project/suite" for the UI).

- [ ] **Step 1: Add `AuthService.forgotPassword`**

In `ui/src/app/services/auth/auth.service.ts`, add this method to the `AuthService` class (e.g. after `activate`):

```typescript
  forgotPassword(email: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/forgot-password`, { email });
  }
```

- [ ] **Step 2: Create `ForgotPasswordComponent`**

Create `ui/src/app/pages/forgot-password/forgot-password.component.ts`:

```typescript
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.scss']
})
export class ForgotPasswordComponent {
  email = '';
  errorMessage = '';
  successMessage = '';
  isLoading = false;

  constructor(private authService: AuthService) { }

  onSubmit(): void {
    if (!this.email) {
      this.errorMessage = 'Merci de renseigner votre email.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.authService.forgotPassword(this.email).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.successMessage = response.message;
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.status === 429
          ? (err.error?.error ?? 'Trop de tentatives, réessayez plus tard.')
          : 'Erreur serveur, veuillez réessayer.';
      }
    });
  }
}
```

- [ ] **Step 3: Create the template**

Create `ui/src/app/pages/forgot-password/forgot-password.component.html`:

```html
<div class="login-page">

    <div class="login-card">
        <div class="card-header">
            <div class="logo-mark">SR</div>
            <h1>Mot de passe oublié</h1>
            <p>Recevez un lien pour définir un nouveau mot de passe</p>
        </div>

        <div class="error-banner" *ngIf="errorMessage">
            <span class="error-icon">!</span>
            {{ errorMessage }}
        </div>

        <div *ngIf="!successMessage">
            <div class="form-group">
                <label for="email">Email</label>
                <input id="email" type="text" [(ngModel)]="email" placeholder="Entrez votre email"
                    [class.has-value]="email" (keyup.enter)="onSubmit()" autocomplete="email" />
            </div>

            <button class="submit-btn" (click)="onSubmit()" [disabled]="isLoading">
                <span *ngIf="!isLoading">Envoyer le lien</span>
                <span *ngIf="isLoading" class="spinner"></span>
            </button>
        </div>

        <p *ngIf="successMessage" class="success-text">{{ successMessage }}</p>

        <p class="back-link"><a routerLink="/login">Retour à la connexion</a></p>
    </div>

</div>
```

- [ ] **Step 4: Create the stylesheet**

Create `ui/src/app/pages/forgot-password/forgot-password.component.scss` — same base as `login.component.scss`, plus two small additions for the success text and back link:

```scss
@import url("https://fonts.googleapis.com/css2?family=Bebas+Neue&family=DM+Sans:wght@300;400;500&display=swap");

:host {
  display: block;
  height: 100vh;
}

.login-page {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background-color: #0d0d0d;
  background-image:
    radial-gradient(
      ellipse 80% 60% at 50% -10%,
      rgba(220, 38, 38, 0.15) 0%,
      transparent 70%
    ),
    repeating-linear-gradient(
      0deg,
      transparent,
      transparent 60px,
      rgba(255, 255, 255, 0.015) 60px,
      rgba(255, 255, 255, 0.015) 61px
    );
  font-family: "DM Sans", sans-serif;
}

.login-card {
  width: 100%;
  max-width: 400px;
  padding: 2.5rem;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 4px;
  backdrop-filter: blur(10px);
}

.card-header {
  text-align: center;
  margin-bottom: 2rem;

  .logo-mark {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 48px;
    height: 48px;
    background: #dc2626;
    color: white;
    font-family: "Bebas Neue", sans-serif;
    font-size: 1.2rem;
    letter-spacing: 0.05em;
    border-radius: 2px;
    margin-bottom: 1.2rem;
  }

  h1 {
    font-family: "Bebas Neue", sans-serif;
    font-size: 2rem;
    letter-spacing: 0.08em;
    color: #ffffff;
    margin: 0 0 0.3rem;
  }

  p {
    color: rgba(255, 255, 255, 0.35);
    font-size: 0.85rem;
    font-weight: 300;
    margin: 0;
  }
}

.error-banner {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  background: rgba(220, 38, 38, 0.1);
  border: 1px solid rgba(220, 38, 38, 0.3);
  color: #f87171;
  font-size: 0.85rem;
  padding: 0.7rem 1rem;
  border-radius: 3px;
  margin-bottom: 1.5rem;

  .error-icon {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 18px;
    height: 18px;
    background: #dc2626;
    color: white;
    border-radius: 50%;
    font-size: 0.7rem;
    font-weight: 700;
    flex-shrink: 0;
  }
}

.form-group {
  margin-bottom: 1.2rem;

  label {
    display: block;
    font-size: 0.75rem;
    font-weight: 500;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    color: rgba(255, 255, 255, 0.4);
    margin-bottom: 0.5rem;
  }

  input {
    width: 100%;
    padding: 0.75rem 1rem;
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid rgba(255, 255, 255, 0.1);
    border-radius: 3px;
    color: #ffffff;
    font-family: "DM Sans", sans-serif;
    font-size: 0.95rem;
    box-sizing: border-box;
    transition:
      border-color 0.2s,
      background 0.2s;

    &::placeholder {
      color: rgba(255, 255, 255, 0.2);
    }

    &:focus {
      outline: none;
      border-color: #dc2626;
      background: rgba(255, 255, 255, 0.07);
    }

    &.has-value {
      border-color: rgba(255, 255, 255, 0.2);
    }
  }
}

.submit-btn {
  width: 100%;
  padding: 0.85rem;
  margin-top: 0.5rem;
  background: #dc2626;
  color: white;
  border: none;
  border-radius: 3px;
  font-family: "Bebas Neue", sans-serif;
  font-size: 1.1rem;
  letter-spacing: 0.1em;
  cursor: pointer;
  transition:
    background 0.2s,
    transform 0.1s;
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 48px;

  &:hover:not(:disabled) {
    background: #b91c1c;
  }

  &:active:not(:disabled) {
    transform: scale(0.99);
  }

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
}

.spinner {
  width: 18px;
  height: 18px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.success-text {
  color: rgba(255, 255, 255, 0.7);
  font-size: 0.9rem;
  text-align: center;
  line-height: 1.5;
  margin: 0.5rem 0 0;
}

.back-link {
  text-align: center;
  margin: 1.5rem 0 0;

  a {
    color: rgba(255, 255, 255, 0.4);
    font-size: 0.8rem;
    text-decoration: none;

    &:hover {
      color: #f87171;
    }
  }
}
```

- [ ] **Step 5: Add the route**

In `ui/src/app/app.routes.ts`, add the import:

```typescript
import { ForgotPasswordComponent } from './pages/forgot-password/forgot-password.component';
```

and add this route entry (after `"login"`, before `"activer-compte"`):

```typescript
    { path: "mot-de-passe-oublie", component: ForgotPasswordComponent },
```

- [ ] **Step 6: Add the link on the login page**

In `ui/src/app/pages/login/login.component.html`, add a link after the submit button (before the closing `</div>` of `.login-card`):

```html
        <p class="forgot-password-link"><a routerLink="/mot-de-passe-oublie">Mot de passe oublié ?</a></p>
```

`login.component.ts` doesn't import `RouterLink` today — add it. In `ui/src/app/pages/login/login.component.ts`, change:

```typescript
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
```

to:

```typescript
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
```

In `ui/src/app/pages/login/login.component.scss`, add this rule at the end (matches the `.back-link` styling added to `forgot-password.component.scss` in Step 4, kept as a small separate rule here since this file isn't otherwise touched by this feature):

```scss
.forgot-password-link {
  text-align: center;
  margin: 1.2rem 0 0;

  a {
    color: rgba(255, 255, 255, 0.4);
    font-size: 0.8rem;
    text-decoration: none;

    &:hover {
      color: #f87171;
    }
  }
}
```

- [ ] **Step 7: Update the manual testing guide**

In `docs/manual-testing-guide.md`, add a new section after "## 3. Activate" (renumber the following sections +1, i.e. old "4" becomes "5", etc.):

```markdown
## 4. Reset a forgotten password

1. Log out, go to `/login`, click "Mot de passe oublié ?".
2. Submit the email from step 1. Expect the same generic confirmation message every time, whether or not the email exists.
3. Fetch the new `VerificationToken` from the DB (same query as step 2), open `/activer-compte?token=<token>`, set a new password.
4. Log in with the new password. Confirm the old password no longer works.
```

- [ ] **Step 8: Manual verification**

Run: `docker compose up --build`

1. Open `/login`, click "Mot de passe oublié ?" → arrive on `/mot-de-passe-oublie`.
2. Submit an activated participant's email → generic confirmation shown.
3. Fetch the new token from the DB (see updated manual-testing-guide.md), open `/activer-compte?token=...`, set a new password, confirm redirect to `/mon-equipe`.
4. Log out, log in with the new password → succeeds. Log in with the old password → fails.
5. Submit an email that was never registered → same generic confirmation shown, no error.

- [ ] **Step 9: Commit**

```bash
git add ui/src/app/services/auth/auth.service.ts ui/src/app/pages/forgot-password ui/src/app/app.routes.ts ui/src/app/pages/login/login.component.html ui/src/app/pages/login/login.component.ts ui/src/app/pages/login/login.component.scss docs/manual-testing-guide.md
git commit -m "feat: add forgot-password page and login link"
```
