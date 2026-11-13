# Postponement Readiness Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prepare the site for the postponed event: login brute-force protection and localhost-only ports, an admin email composer that sends an announcement to every registered player through Mailgun, and an event date label configured from `.env`.

**Architecture:** Backend: a new in-memory `LoginRateLimiter` singleton (modeled on `PasswordResetRateLimiter`), a new `AnnouncementService` + admin-only `AnnouncementsController`, and a new `MailService.SendAnnouncementEmailAsync` that reuses the private Mailgun sender. Frontend: a new admin page `/annonce`, a 429 message on login, and an `eventDateLabel` runtime-config key added to the existing `env.js` path.

**Tech Stack:** ASP.NET Core 8, EF Core (SQL Server), FluentValidation, xUnit integration tests; Angular 19 standalone components, SCSS; Docker Compose, nginx + envsubst.

**Spec:** `docs/superpowers/specs/2026-11-13-postponement-readiness-design.md`

## Global Constraints

- Everything is run through Docker Compose. Never run `dotnet`, `ng` or `npm` directly. Backend tests: `docker compose run --rm --build tests` (keep `--build`). One class: append `--filter "FullyQualifiedName~ClassName"`.
- Test-first for backend changes: write the failing test, run it and see it fail for the expected reason, implement, run it green, then run the full suite.
- Tests share one database and one app instance (`[Collection("Api")]`). Never assume empty tables or exact absolute counts. Use `ApiTestFixture.UniqueEmail(...)`. Keep total calls to `/api/auth/forgot-password` across the suite at 5 or fewer (this plan adds none).
- New endpoints declare a role: `[Authorize(Roles = "Admin")]`. Never a bare `[Authorize]`.
- Mailgun is not configured in the test container, so every send returns `false`. Tests check selection, dedup, auth and validation, never delivery.
- User-facing copy is French. Admin page styling reuses the existing admin top-bar and `delete-confirm` look (black background, `$main-color` yellow `#ffed00`, `$font-body` Cabin, `$font-title` Lemon), imported with `@use "variables" as *;`.
- Each announcement email is sent to exactly one recipient (single `to`). Never put several participants in one message.
- Login lockout: 10 failed attempts per username (case-insensitive) in a sliding 15-minute window gives `429` with `{ message: "Trop de tentatives de connexion. Réessayez dans 15 minutes." }`, checked before the password. A successful login resets the counter.
- Announcement limits: `subject` required, max 200 chars. `message` required, max 10,000. `toEmail` required and a valid email.
- Event date fallback when `EVENT_DATE_LABEL` is empty or unset: `Date à confirmer`.
- Commit after each task. End every commit message with the line `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## File Map

**Backend (create)**
- `backend/SportsReservationAPI/Services/LoginRateLimiter.cs`: failed-login counter per username.
- `backend/SportsReservationAPI/Services/AnnouncementService.cs`: recipient selection + parallel batch send.
- `backend/SportsReservationAPI/Controllers/AnnouncementsController.cs`: `GET recipients`, `POST test`, `POST send`.
- `backend/SportsReservationAPI/Models/Announcement/AnnouncementDto.cs` + `AnnouncementDtoValidator.cs`
- `backend/SportsReservationAPI/Models/Announcement/TestAnnouncementDto.cs` + `TestAnnouncementDtoValidator.cs`
- `backend/SportsReservationAPI/Models/Announcement/AnnouncementSendResult.cs`
- Tests: `LoginRateLimiterTests.cs`, `AnnouncementEmailFormattingTests.cs`, `AnnouncementTests.cs` in `backend/SportsReservationAPI.Tests/`.

**Backend (modify)**
- `Controllers/AuthController.cs` (login lockout), `Services/MailService.cs` (announcement email), `Program.cs` (DI registrations), `backend/SportsReservationAPI.Tests/AuthTests.cs` (lockout integration tests).

**Frontend (create)**
- `ui/src/app/pages/announcement/announcement.component.{ts,html,scss}`

**Frontend (modify)**
- `ui/src/app/app.routes.ts`, the top bars in `pages/teams/teams.component.html` and `pages/players/players.component.html`, `pages/login/login.component.ts`
- Runtime config: `ui/src/environment.ts`, `ui/src/app/core/runtime-env.ts`, `ui/public/env.template.js`, `ui/docker-entrypoint.d/40-generate-runtime-env.sh`
- Date: `pages/landing/landing.component.{ts,html}`, `pages/my-team/my-team.component.{ts,html}`, `pages/not-found/not-found.component.{ts,html}`

**Infra / docs (modify)**
- `docker-compose.yml`, `.env.sample`, `CLAUDE.md`, `docs/manual-testing-guide.md`

---

### Task 1: Login brute-force protection (backend)

**Files:**
- Create: `backend/SportsReservationAPI/Services/LoginRateLimiter.cs`
- Create: `backend/SportsReservationAPI.Tests/LoginRateLimiterTests.cs`
- Modify: `backend/SportsReservationAPI/Controllers/AuthController.cs` (constructor + `Login`)
- Modify: `backend/SportsReservationAPI/Program.cs` (services block, next to `PasswordResetRateLimiter`)
- Modify: `backend/SportsReservationAPI.Tests/AuthTests.cs` (append 3 tests)

**Interfaces:**
- Produces: `LoginRateLimiter(int maxFailures = 10, int windowMinutes = 15, Func<DateTime>? now = null)` with `bool IsLockedOut(string username)`, `void RecordFailure(string username)`, `void Reset(string username)`. Registered as a singleton.

- [ ] **Step 1: Write the failing unit tests**

Create `backend/SportsReservationAPI.Tests/LoginRateLimiterTests.cs`:

```csharp
using SportsReservationAPI.Services;
using Xunit;

namespace SportsReservationAPI.Tests;

public class LoginRateLimiterTests
{
    [Fact]
    public void IsLockedOut_BelowMaxFailures_ReturnsFalse()
    {
        var limiter = new LoginRateLimiter(maxFailures: 3);

        limiter.RecordFailure("alice@example.com");
        limiter.RecordFailure("alice@example.com");

        Assert.False(limiter.IsLockedOut("alice@example.com"));
    }

    [Fact]
    public void IsLockedOut_AtMaxFailures_ReturnsTrue()
    {
        var limiter = new LoginRateLimiter(maxFailures: 3);

        limiter.RecordFailure("alice@example.com");
        limiter.RecordFailure("alice@example.com");
        limiter.RecordFailure("alice@example.com");

        Assert.True(limiter.IsLockedOut("alice@example.com"));
    }

    [Fact]
    public void IsLockedOut_IsCaseInsensitiveAndTrimmed()
    {
        var limiter = new LoginRateLimiter(maxFailures: 2);

        limiter.RecordFailure("Alice@Example.com");
        limiter.RecordFailure(" alice@example.com ");

        Assert.True(limiter.IsLockedOut("ALICE@EXAMPLE.COM"));
    }

    [Fact]
    public void IsLockedOut_DifferentUsernames_AreIndependent()
    {
        var limiter = new LoginRateLimiter(maxFailures: 1);

        limiter.RecordFailure("alice@example.com");

        Assert.False(limiter.IsLockedOut("bob@example.com"));
    }

    [Fact]
    public void Reset_ClearsFailures()
    {
        var limiter = new LoginRateLimiter(maxFailures: 2);

        limiter.RecordFailure("alice@example.com");
        limiter.RecordFailure("alice@example.com");
        limiter.Reset("alice@example.com");

        Assert.False(limiter.IsLockedOut("alice@example.com"));
    }

    [Fact]
    public void IsLockedOut_AfterWindowExpires_ReturnsFalse()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var limiter = new LoginRateLimiter(maxFailures: 2, windowMinutes: 15, now: () => now);

        limiter.RecordFailure("alice@example.com");
        limiter.RecordFailure("alice@example.com");
        Assert.True(limiter.IsLockedOut("alice@example.com"));

        now = now.AddMinutes(16);

        Assert.False(limiter.IsLockedOut("alice@example.com"));
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run: `docker compose run --rm --build tests --filter "FullyQualifiedName~LoginRateLimiterTests"`
Expected: build failure, `The type or namespace name 'LoginRateLimiter' could not be found`.

- [ ] **Step 3: Implement `LoginRateLimiter`**

Create `backend/SportsReservationAPI/Services/LoginRateLimiter.cs`:

```csharp
using System.Collections.Concurrent;

namespace SportsReservationAPI.Services
{
    // Registered as a singleton (Program.cs). Counts FAILED login attempts per
    // username in a sliding window; once maxFailures is reached the username is
    // locked out (even for a correct password) until old failures age out.
    // Per-username rather than per-IP: the test suite logs in many times from
    // one address, and it protects the admin account regardless of how many
    // IPs an attacker uses. In-memory only, like PasswordResetRateLimiter.
    public class LoginRateLimiter
    {
        private readonly int _maxFailures;
        private readonly TimeSpan _window;
        private readonly Func<DateTime> _now;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _failuresByUsername = new();

        public LoginRateLimiter(int maxFailures = 10, int windowMinutes = 15, Func<DateTime>? now = null)
        {
            _maxFailures = maxFailures;
            _window = TimeSpan.FromMinutes(windowMinutes);
            _now = now ?? (() => DateTime.UtcNow);
        }

        public bool IsLockedOut(string username)
        {
            if (!_failuresByUsername.TryGetValue(Key(username), out var queue))
                return false;

            lock (queue)
            {
                Prune(queue);
                return queue.Count >= _maxFailures;
            }
        }

        public void RecordFailure(string username)
        {
            var queue = _failuresByUsername.GetOrAdd(Key(username), _ => new ConcurrentQueue<DateTime>());
            lock (queue)
            {
                Prune(queue);
                queue.Enqueue(_now());
            }
        }

        public void Reset(string username)
        {
            _failuresByUsername.TryRemove(Key(username), out _);
        }

        private void Prune(ConcurrentQueue<DateTime> queue)
        {
            var now = _now();
            while (queue.TryPeek(out var oldest) && now - oldest > _window)
                queue.TryDequeue(out _);
        }

        private static string Key(string username) => (username ?? "").Trim().ToLowerInvariant();
    }
}
```

- [ ] **Step 4: Run the unit tests and confirm they pass**

Run: `docker compose run --rm --build tests --filter "FullyQualifiedName~LoginRateLimiterTests"`
Expected: 6 passed.

- [ ] **Step 5: Write the failing integration tests**

Append these three tests inside the `AuthTests` class in `backend/SportsReservationAPI.Tests/AuthTests.cs` (the file already has `using System.Net;` and `using System.Net.Http.Json;`):

```csharp
    [Fact]
    public async Task Login_AfterTenFailures_ReturnsTooManyRequestsEvenWithCorrectPassword()
    {
        var email = ApiTestFixture.UniqueEmail("lockout");
        await _fixture.RegisterAndActivateTeamAsync("LockoutTeam", email, ApiTestFixture.UniqueEmail("lockout2"), "CorrectPassword123");

        for (var i = 0; i < 10; i++)
        {
            var bad = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new { username = email, password = "wrong-password" });
            Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
        }

        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new { username = email, password = "CorrectPassword123" });

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("Trop de tentatives de connexion. Réessayez dans 15 minutes.", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Login_SuccessResetsFailureCounter()
    {
        var email = ApiTestFixture.UniqueEmail("lockreset");
        await _fixture.RegisterAndActivateTeamAsync("LockResetTeam", email, ApiTestFixture.UniqueEmail("lockreset2"), "CorrectPassword123");

        for (var i = 0; i < 9; i++)
            await _fixture.Client.PostAsJsonAsync("/api/auth/login", new { username = email, password = "wrong-password" });

        var ok = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new { username = email, password = "CorrectPassword123" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        for (var i = 0; i < 9; i++)
            await _fixture.Client.PostAsJsonAsync("/api/auth/login", new { username = email, password = "wrong-password" });

        var stillOk = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new { username = email, password = "CorrectPassword123" });
        Assert.Equal(HttpStatusCode.OK, stillOk.StatusCode);
    }

    [Fact]
    public async Task Login_LockoutIsCaseInsensitive()
    {
        var email = ApiTestFixture.UniqueEmail("lockcase");
        await _fixture.RegisterAndActivateTeamAsync("LockCaseTeam", email, ApiTestFixture.UniqueEmail("lockcase2"), "CorrectPassword123");

        for (var i = 0; i < 10; i++)
            await _fixture.Client.PostAsJsonAsync("/api/auth/login", new { username = email.ToUpperInvariant(), password = "wrong-password" });

        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new { username = email, password = "CorrectPassword123" });

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }
```

- [ ] **Step 6: Run them and confirm they fail**

Run: `docker compose run --rm --build tests --filter "FullyQualifiedName~AuthTests"`
Expected: `Login_AfterTenFailures_…` and `Login_LockoutIsCaseInsensitive` FAIL (`Expected: TooManyRequests, Actual: OK`). `Login_SuccessResetsFailureCounter` passes already (nothing locks out yet), which is expected.

- [ ] **Step 7: Wire the limiter into DI and the login endpoint**

In `backend/SportsReservationAPI/Program.cs`, after `builder.Services.AddSingleton<PasswordResetRateLimiter>();` add:

```csharp
builder.Services.AddSingleton<LoginRateLimiter>();
```

In `backend/SportsReservationAPI/Controllers/AuthController.cs`, replace the fields, the constructor and the `Login` action with:

```csharp
        private readonly AuthService _authService;
        private readonly UserService _userService;
        private readonly LoginRateLimiter _loginRateLimiter;

        public AuthController(AuthService authService, UserService userService, LoginRateLimiter loginRateLimiter)
        {
            _authService = authService;
            _userService = userService;
            _loginRateLimiter = loginRateLimiter;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Checked before the password so a locked-out username can't keep guessing.
            if (_loginRateLimiter.IsLockedOut(request.Username))
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new { message = "Trop de tentatives de connexion. Réessayez dans 15 minutes." });

            var user = _authService.Authenticate(request.Username, request.Password);

            if (user == null)
            {
                _loginRateLimiter.RecordFailure(request.Username);
                return Unauthorized(new { message = "Invalid credentials" });
            }

            _loginRateLimiter.Reset(request.Username);

            var token = _authService.GenerateToken(user);

            return Ok(new LoginResponse
            {
                Token = token,
                Username = user.Username,
                Role = user.Role
            });
        }
```

- [ ] **Step 8: Run the auth tests and confirm they pass**

Run: `docker compose run --rm --build tests --filter "FullyQualifiedName~AuthTests"`
Expected: all pass.

- [ ] **Step 9: Run the full suite**

Run: `docker compose run --rm --build tests`
Expected: all pass.

- [ ] **Step 10: Commit**

```bash
git add backend/SportsReservationAPI/Services/LoginRateLimiter.cs backend/SportsReservationAPI/Controllers/AuthController.cs backend/SportsReservationAPI/Program.cs backend/SportsReservationAPI.Tests/LoginRateLimiterTests.cs backend/SportsReservationAPI.Tests/AuthTests.cs
git commit -m "feat: lock out a username after 10 failed logins in 15 minutes

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: Login 429 message (frontend) + localhost-only ports

**Files:**
- Modify: `ui/src/app/pages/login/login.component.ts` (the `error:` handler, around line 38)
- Modify: `docker-compose.yml` (`frontend.ports`, `backend.ports`)

**Interfaces:**
- Consumes: the `429` + `{ message }` response from Task 1.

- [ ] **Step 1: Show the lockout message on the login page**

In `ui/src/app/pages/login/login.component.ts`, replace:

```ts
        this.errorMessage = err.status === 401
          ? 'Email ou mot de passe incorrect.'
          : 'Erreur serveur, veuillez réessayer.';
```

with:

```ts
        if (err.status === 401) {
          this.errorMessage = 'Email ou mot de passe incorrect.';
        } else if (err.status === 429) {
          this.errorMessage = err.error?.message ?? 'Trop de tentatives de connexion. Réessayez dans 15 minutes.';
        } else {
          this.errorMessage = 'Erreur serveur, veuillez réessayer.';
        }
```

- [ ] **Step 2: Bind published ports to localhost**

In `docker-compose.yml`, change the `frontend` service's ports to:

```yaml
    ports:
      - "127.0.0.1:${UI_PORT}:80"
```

and the `backend` service's ports to:

```yaml
    # Localhost only: the API must be reachable solely through the reverse proxy
    # (Caddy, on the host), otherwise X-Forwarded-For can be forged to bypass the
    # per-IP password-reset limit. See the ForwardedHeaders comment in Program.cs.
    ports:
      - "127.0.0.1:${API_PORT}:8080"
```

- [ ] **Step 3: Check it manually**

Run: `docker compose up --build -d`
Then: open `http://localhost:<UI_PORT>/login` and confirm the site still loads and the admin can log in (proves the localhost bindings work locally). Log in 10 times with a wrong password for a test participant account, then once with the right one: the banner shows "Trop de tentatives de connexion. Réessayez dans 15 minutes."
Run: `docker compose ps` and confirm both ports show as `127.0.0.1:<port>->…`.

- [ ] **Step 4: Commit**

```bash
git add ui/src/app/pages/login/login.component.ts docker-compose.yml
git commit -m "fix: show login lockout message and publish UI/API ports on localhost only

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Announcement email formatting in `MailService`

**Files:**
- Modify: `backend/SportsReservationAPI/Services/MailService.cs`
- Create: `backend/SportsReservationAPI.Tests/AnnouncementEmailFormattingTests.cs`

**Interfaces:**
- Produces:
  - `public Task<bool> SendAnnouncementEmailAsync(string toEmail, string? toName, string subject, string message)`: never throws, `false` on any failure or missing config.
  - `public static string BuildAnnouncementText(string? toName, string message)`
  - `public static string BuildAnnouncementHtml(string? toName, string message)`

- [ ] **Step 1: Write the failing tests**

Create `backend/SportsReservationAPI.Tests/AnnouncementEmailFormattingTests.cs`:

```csharp
using SportsReservationAPI.Services;
using Xunit;

namespace SportsReservationAPI.Tests;

public class AnnouncementEmailFormattingTests
{
    [Fact]
    public void BuildAnnouncementHtml_GreetsByEncodedFirstName()
    {
        // ASCII name on purpose: WebUtility.HtmlEncode turns Latin-1 letters like "é"
        // into numeric entities (&#233;), which is valid HTML but noisy to assert on.
        var html = MailService.BuildAnnouncementHtml("Zoe <b>", "Salut");

        Assert.Contains("<p>Bonjour Zoe &lt;b&gt;,</p>", html);
    }

    [Fact]
    public void BuildAnnouncementHtml_WithoutName_UsesPlainGreeting()
    {
        var html = MailService.BuildAnnouncementHtml(null, "Salut");

        Assert.Contains("<p>Bonjour,</p>", html);
    }

    [Fact]
    public void BuildAnnouncementHtml_EncodesMarkupInMessage()
    {
        var html = MailService.BuildAnnouncementHtml("Alice", "<script>alert(1)</script>");

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void BuildAnnouncementHtml_SplitsParagraphsAndLineBreaks()
    {
        var html = MailService.BuildAnnouncementHtml("Alice", "Ligne 1\r\nLigne 2\r\n\r\nParagraphe 2");

        Assert.Contains("<p>Ligne 1<br>Ligne 2</p>", html);
        Assert.Contains("<p>Paragraphe 2</p>", html);
    }

    [Fact]
    public void BuildAnnouncementHtml_LinksUrls()
    {
        var html = MailService.BuildAnnouncementHtml("Alice", "Voir https://example.com/mon-equipe?a=1&b=2 merci");

        Assert.Contains("<a href=\"https://example.com/mon-equipe?a=1&amp;b=2\">https://example.com/mon-equipe?a=1&amp;b=2</a>", html);
    }

    [Fact]
    public void BuildAnnouncementHtml_EndsWithFooter()
    {
        var html = MailService.BuildAnnouncementHtml("Alice", "Salut");

        Assert.Contains("Vous recevez cet email car vous êtes inscrit(e) à Hyrox Police 54.", html);
        Assert.Contains("Sport Challenge Police 54", html);
    }

    [Fact]
    public void BuildAnnouncementText_HasGreetingMessageAndFooter()
    {
        var text = MailService.BuildAnnouncementText("Alice", "Ligne 1\nLigne 2");

        Assert.StartsWith("Bonjour Alice,\n\nLigne 1\nLigne 2\n\n", text);
        Assert.Contains("Vous recevez cet email car vous êtes inscrit(e) à Hyrox Police 54.", text);
        Assert.EndsWith("Sport Challenge Police 54", text);
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run: `docker compose run --rm --build tests --filter "FullyQualifiedName~AnnouncementEmailFormattingTests"`
Expected: build failure, `'MailService' does not contain a definition for 'BuildAnnouncementHtml'`.

- [ ] **Step 3: Implement**

In `backend/SportsReservationAPI/Services/MailService.cs`, add `using System.Text.RegularExpressions;` at the top. Then add these members right after `SendPasswordResetEmailAsync` (before the private `SendEmailAsync`):

```csharp
    private const string AnnouncementFooter = "Vous recevez cet email car vous êtes inscrit(e) à Hyrox Police 54.";
    private const string Signature = "Sport Challenge Police 54";
    private static readonly Regex UrlPattern = new(@"https?://[^\s<]+", RegexOptions.Compiled);

    // Admin-written announcement (AnnouncementService). Same never-throw contract as
    // the other senders: false on any failure or missing Mailgun config.
    public Task<bool> SendAnnouncementEmailAsync(string toEmail, string? toName, string subject, string message)
    {
        return SendEmailAsync(
            toEmail,
            toName ?? "",
            subject,
            BuildAnnouncementText(toName, message),
            BuildAnnouncementHtml(toName, message));
    }

    public static string BuildAnnouncementText(string? toName, string message)
    {
        var greeting = string.IsNullOrWhiteSpace(toName) ? "Bonjour," : $"Bonjour {toName},";
        var body = message.Replace("\r\n", "\n").Trim();
        return $"{greeting}\n\n{body}\n\n{AnnouncementFooter}\n\n{Signature}";
    }

    // Everything the admin typed is HTML-encoded FIRST; only then are paragraphs,
    // line breaks and links added, so the message can never inject markup.
    public static string BuildAnnouncementHtml(string? toName, string message)
    {
        var greeting = string.IsNullOrWhiteSpace(toName)
            ? "Bonjour,"
            : $"Bonjour {WebUtility.HtmlEncode(toName)},";

        var paragraphs = Regex.Split(message.Replace("\r\n", "\n").Trim(), @"\n\s*\n")
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p =>
            {
                var encoded = WebUtility.HtmlEncode(p.Trim());
                var linked = UrlPattern.Replace(encoded, m => $"<a href=\"{m.Value}\">{m.Value}</a>");
                return $"<p>{linked.Replace("\n", "<br>")}</p>";
            });

        return $"<p>{greeting}</p>{string.Concat(paragraphs)}" +
               // Footer and signature are our own constants, not user input: inserted as-is
               // so "é"/"à" stay readable (WebUtility.HtmlEncode would turn them into &#233;).
               $"<p style=\"color:#666;font-size:12px\">{AnnouncementFooter}</p>" +
               $"<p>{Signature}</p>";
    }
```

Note: `WebUtility.HtmlEncode` encodes Latin-1 letters such as `é` as numeric entities (`&#233;`) in the admin's message and the first name. Email clients render those correctly, so this is fine. It's only why the tests assert on ASCII input.

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `docker compose run --rm --build tests --filter "FullyQualifiedName~AnnouncementEmailFormattingTests"`
Expected: 7 passed.

- [ ] **Step 5: Commit**

```bash
git add backend/SportsReservationAPI/Services/MailService.cs backend/SportsReservationAPI.Tests/AnnouncementEmailFormattingTests.cs
git commit -m "feat: add announcement email builder to MailService

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: Announcement API (recipients, test send, send to all)

**Files:**
- Create: `backend/SportsReservationAPI/Models/Announcement/AnnouncementDto.cs`
- Create: `backend/SportsReservationAPI/Models/Announcement/AnnouncementDtoValidator.cs`
- Create: `backend/SportsReservationAPI/Models/Announcement/TestAnnouncementDto.cs`
- Create: `backend/SportsReservationAPI/Models/Announcement/TestAnnouncementDtoValidator.cs`
- Create: `backend/SportsReservationAPI/Models/Announcement/AnnouncementSendResult.cs`
- Create: `backend/SportsReservationAPI/Services/AnnouncementService.cs`
- Create: `backend/SportsReservationAPI/Controllers/AnnouncementsController.cs`
- Modify: `backend/SportsReservationAPI/Program.cs` (register `AnnouncementService`)
- Create: `backend/SportsReservationAPI.Tests/AnnouncementTests.cs`

**Interfaces:**
- Consumes: `MailService.SendAnnouncementEmailAsync(string toEmail, string? toName, string subject, string message)` from Task 3.
- Produces (HTTP, consumed by Task 5; JSON is camelCase):
  - `GET /api/announcements/recipients` returns `{ "count": number }`
  - `POST /api/announcements/test` with body `{ subject, message, toEmail }` returns `200 { "sent": true }`, or `502 { "error": string }`
  - `POST /api/announcements/send` with body `{ subject, message }` returns `200 { "total", "sent", "failed", "failures": [{ "email", "error" }] }`
  - Any invalid body returns `400`. Participant token returns `403`, no token returns `401`.

- [ ] **Step 1: Write the failing integration tests**

Create `backend/SportsReservationAPI.Tests/AnnouncementTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run them and confirm they fail**

Run: `docker compose run --rm --build tests --filter "FullyQualifiedName~AnnouncementTests"`
Expected: all FAIL with `404 NotFound` where 200/400/401/403/502 was expected (the routes don't exist yet).

- [ ] **Step 3: Add the DTOs and validators**

`backend/SportsReservationAPI/Models/Announcement/AnnouncementDto.cs`:

```csharp
namespace SportsReservationAPI.Models.Announcement
{
    public class AnnouncementDto
    {
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
```

`backend/SportsReservationAPI/Models/Announcement/AnnouncementDtoValidator.cs`:

```csharp
using FluentValidation;

namespace SportsReservationAPI.Models.Announcement
{
    public class AnnouncementDtoValidator : AbstractValidator<AnnouncementDto>
    {
        public AnnouncementDtoValidator()
        {
            RuleFor(x => x.Subject)
                .NotEmpty().WithMessage("Subject is required.")
                .MaximumLength(200).WithMessage("Subject must be 200 characters or fewer.");
            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Message is required.")
                .MaximumLength(10000).WithMessage("Message must be 10000 characters or fewer.");
        }
    }
}
```

`backend/SportsReservationAPI/Models/Announcement/TestAnnouncementDto.cs`:

```csharp
namespace SportsReservationAPI.Models.Announcement
{
    public class TestAnnouncementDto : AnnouncementDto
    {
        public string ToEmail { get; set; } = string.Empty;
    }
}
```

`backend/SportsReservationAPI/Models/Announcement/TestAnnouncementDtoValidator.cs`:

```csharp
using FluentValidation;

namespace SportsReservationAPI.Models.Announcement
{
    public class TestAnnouncementDtoValidator : AbstractValidator<TestAnnouncementDto>
    {
        public TestAnnouncementDtoValidator()
        {
            Include(new AnnouncementDtoValidator());
            RuleFor(x => x.ToEmail)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email is required.");
        }
    }
}
```

`backend/SportsReservationAPI/Models/Announcement/AnnouncementSendResult.cs`:

```csharp
namespace SportsReservationAPI.Models.Announcement
{
    public class AnnouncementSendResult
    {
        public int Total { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
        public List<AnnouncementFailure> Failures { get; set; } = [];
    }

    public class AnnouncementFailure
    {
        public string Email { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
```

The validators are picked up automatically by `AddValidatorsFromAssemblyContaining<CreateTeamDtoValidator>()` in `Program.cs`, since they're in the same assembly.

- [ ] **Step 4: Add `AnnouncementService`**

`backend/SportsReservationAPI/Services/AnnouncementService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using SportsReservationAPI.Models;
using SportsReservationAPI.Models.Announcement;

namespace SportsReservationAPI.Services
{
    public class AnnouncementService
    {
        private const int BatchSize = 5;

        private readonly ReservationContext _context;
        private readonly MailService _mailService;
        private readonly ILogger<AnnouncementService> _logger;

        public AnnouncementService(ReservationContext context, MailService mailService, ILogger<AnnouncementService> logger)
        {
            _context = context;
            _mailService = mailService;
            _logger = logger;
        }

        // Every registered player, one entry per email address (trimmed, compared
        // case-insensitively). When an address is shared by several player rows,
        // the lowest-Id row supplies the email casing and first name.
        public async Task<List<(string Email, string FirstName)>> GetRecipientsAsync()
        {
            var players = await _context.Players
                .OrderBy(p => p.Id)
                .Select(p => new { p.Email, p.FirstName })
                .ToListAsync();

            return players
                .Where(p => !string.IsNullOrWhiteSpace(p.Email))
                .GroupBy(p => p.Email.Trim().ToLowerInvariant())
                .Select(g => (g.First().Email.Trim(), g.First().FirstName))
                .ToList();
        }

        public Task<bool> SendTestAsync(string toEmail, string subject, string message)
        {
            return _mailService.SendAnnouncementEmailAsync(toEmail.Trim(), null, subject, message);
        }

        // One Mailgun call per recipient (never several addresses in one message),
        // in parallel batches of 5, the same shape as the bulk activation send.
        public async Task<AnnouncementSendResult> SendToAllAsync(string subject, string message)
        {
            var recipients = await GetRecipientsAsync();
            var result = new AnnouncementSendResult { Total = recipients.Count };
            var failures = new System.Collections.Concurrent.ConcurrentBag<AnnouncementFailure>();
            var sentCount = 0;

            for (var i = 0; i < recipients.Count; i += BatchSize)
            {
                var batch = recipients.Skip(i).Take(BatchSize);
                await Task.WhenAll(batch.Select(async r =>
                {
                    var sent = await _mailService.SendAnnouncementEmailAsync(r.Email, r.FirstName, subject, message);
                    if (sent)
                        Interlocked.Increment(ref sentCount);
                    else
                        failures.Add(new AnnouncementFailure { Email = r.Email, Error = "Échec de l'envoi de l'email." });
                }));
            }

            result.Sent = sentCount;
            result.Failures = failures.OrderBy(f => f.Email).ToList();
            result.Failed = result.Failures.Count;

            _logger.LogInformation("Announcement \"{Subject}\" sent: {Total} total, {Sent} sent, {Failed} failed",
                subject, result.Total, result.Sent, result.Failed);

            return result;
        }
    }
}
```

- [ ] **Step 5: Add `AnnouncementsController` and register the service**

`backend/SportsReservationAPI/Controllers/AnnouncementsController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsReservationAPI.Models.Announcement;
using SportsReservationAPI.Services;

namespace SportsReservationAPI.Controllers
{
    [Route("api/announcements")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AnnouncementsController : ControllerBase
    {
        private readonly AnnouncementService _announcementService;

        public AnnouncementsController(AnnouncementService announcementService)
        {
            _announcementService = announcementService;
        }

        // Exact count "send" will email (dedup included), shown in the confirm dialog.
        [HttpGet("recipients")]
        public async Task<IActionResult> GetRecipientCount()
        {
            var recipients = await _announcementService.GetRecipientsAsync();
            return Ok(new { count = recipients.Count });
        }

        [HttpPost("test")]
        public async Task<IActionResult> SendTest([FromBody] TestAnnouncementDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var sent = await _announcementService.SendTestAsync(dto.ToEmail, dto.Subject, dto.Message);
            if (!sent)
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { error = "L'email de test n'a pas pu être envoyé. Vérifiez la configuration Mailgun." });

            return Ok(new { sent = true });
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendToAll([FromBody] AnnouncementDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _announcementService.SendToAllAsync(dto.Subject, dto.Message);
            return Ok(result);
        }
    }
}
```

In `backend/SportsReservationAPI/Program.cs`, in the `// Services` block after `builder.Services.AddScoped<UserService>();`, add:

```csharp
builder.Services.AddScoped<AnnouncementService>();
```

- [ ] **Step 6: Run the announcement tests and confirm they pass**

Run: `docker compose run --rm --build tests --filter "FullyQualifiedName~AnnouncementTests"`
Expected: all pass.

- [ ] **Step 7: Run the full suite**

Run: `docker compose run --rm --build tests`
Expected: all pass.

- [ ] **Step 8: Commit**

```bash
git add backend/SportsReservationAPI/Models/Announcement backend/SportsReservationAPI/Services/AnnouncementService.cs backend/SportsReservationAPI/Controllers/AnnouncementsController.cs backend/SportsReservationAPI/Program.cs backend/SportsReservationAPI.Tests/AnnouncementTests.cs
git commit -m "feat: add admin announcement API (recipients count, test send, send to all players)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: Admin "Annonce" page

**Files:**
- Create: `ui/src/app/pages/announcement/announcement.component.ts`
- Create: `ui/src/app/pages/announcement/announcement.component.html`
- Create: `ui/src/app/pages/announcement/announcement.component.scss`
- Modify: `ui/src/app/app.routes.ts`
- Modify: `ui/src/app/pages/teams/teams.component.html` (nav, around line 9-10)
- Modify: `ui/src/app/pages/players/players.component.html` (nav, around line 9-10)

**Interfaces:**
- Consumes: the three HTTP endpoints from Task 4. `environment.apiUrl` from `ui/src/app/core/runtime-env.ts`. `AuthService.getCurrentUser()` / `logout()`.

- [ ] **Step 1: Component class**

`ui/src/app/pages/announcement/announcement.component.ts`:

```ts
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';
import { environment } from '../../core/runtime-env';

interface AnnouncementSendResult {
  total: number;
  sent: number;
  failed: number;
  failures: { email: string; error: string }[];
}

@Component({
  selector: 'app-announcement',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './announcement.component.html',
  styleUrl: './announcement.component.scss'
})
export class AnnouncementComponent {
  readonly maxSubjectLength = 200;
  readonly maxMessageLength = 10000;

  currentUser: { username: string; role: string } | null = null;

  subject = '';
  message = '';

  testEmail = '';
  isSendingTest = false;
  testSuccess = '';
  testError = '';

  isLoadingCount = false;
  recipientCount: number | null = null;
  showConfirm = false;
  isSending = false;
  sendError = '';
  result: AnnouncementSendResult | null = null;

  private readonly baseUrl = `${environment.apiUrl}/api/announcements`;

  constructor(private http: HttpClient, public authService: AuthService) {
    this.currentUser = this.authService.getCurrentUser();
  }

  get canSend(): boolean {
    return this.subject.trim().length > 0 && this.message.trim().length > 0;
  }

  sendTest(): void {
    if (!this.canSend || !this.testEmail.trim()) return;
    this.isSendingTest = true;
    this.testSuccess = '';
    this.testError = '';

    const toEmail = this.testEmail.trim();
    this.http.post(`${this.baseUrl}/test`, { subject: this.subject, message: this.message, toEmail }).subscribe({
      next: () => {
        this.isSendingTest = false;
        this.testSuccess = `Test envoyé à ${toEmail}.`;
      },
      error: (err) => {
        this.isSendingTest = false;
        this.testError = err.status === 400
          ? 'Vérifiez l\'adresse email, l\'objet et le message.'
          : (err.error?.error ?? "L'email de test n'a pas pu être envoyé.");
      }
    });
  }

  openConfirm(): void {
    if (!this.canSend) return;
    this.isLoadingCount = true;
    this.sendError = '';
    this.result = null;

    this.http.get<{ count: number }>(`${this.baseUrl}/recipients`).subscribe({
      next: (res) => {
        this.isLoadingCount = false;
        this.recipientCount = res.count;
        this.showConfirm = true;
      },
      error: () => {
        this.isLoadingCount = false;
        this.sendError = 'Impossible de récupérer le nombre de destinataires. Réessayez.';
      }
    });
  }

  cancelConfirm(): void {
    this.showConfirm = false;
  }

  sendToAll(): void {
    this.isSending = true;
    this.sendError = '';

    this.http.post<AnnouncementSendResult>(`${this.baseUrl}/send`, { subject: this.subject, message: this.message }).subscribe({
      next: (res) => {
        this.isSending = false;
        this.showConfirm = false;
        this.result = res;
      },
      error: () => {
        this.isSending = false;
        this.sendError = "L'envoi a échoué. Vérifiez les logs avant de réessayer : une partie des emails a pu partir.";
      }
    });
  }
}
```

- [ ] **Step 2: Template**

`ui/src/app/pages/announcement/announcement.component.html`:

```html
<div class="announcement-page">

    <header class="top-bar">
        <div class="brand">
            <span class="logo-mark">54</span>
            <span class="brand-name">Administration</span>
        </div>
        <nav class="nav-links">
            <button class="nav-btn" routerLink="/teams">Équipes</button>
            <button class="nav-btn" routerLink="/players">Joueurs</button>
            <button class="nav-btn active">Annonce</button>
        </nav>
        <div class="user-area">
            <span class="username">{{ currentUser?.username }}</span>
            <button class="logout-btn" (click)="authService.logout()">Déconnexion</button>
        </div>
    </header>

    <main class="content">
        <div class="page-title">
            <h1>Annonce</h1>
            <span class="count">Email à tous les participants inscrits</span>
        </div>

        <section class="panel">
            <label class="field">
                <span class="field__label">Objet</span>
                <input class="field__input" type="text" [(ngModel)]="subject" [maxlength]="maxSubjectLength"
                    placeholder="Hyrox Police 54 : nouvelle date de l'événement" />
            </label>

            <label class="field">
                <span class="field__label">Message</span>
                <textarea class="field__input field__textarea" rows="12" [(ngModel)]="message"
                    [maxlength]="maxMessageLength"></textarea>
                <span class="field__hint">
                    Le message commence automatiquement par « Bonjour &#123;prénom&#125;, ».
                    Laissez une ligne vide entre deux paragraphes.
                    {{ message.length }} / {{ maxMessageLength }}
                </span>
            </label>
        </section>

        <section class="panel">
            <h2 class="panel__title">1. Envoyer un test</h2>
            <div class="test-row">
                <input class="field__input" type="email" [(ngModel)]="testEmail" placeholder="votre.email@exemple.fr" />
                <button class="secondary-btn" (click)="sendTest()"
                    [disabled]="!canSend || !testEmail.trim() || isSendingTest">
                    {{ isSendingTest ? 'Envoi...' : 'Envoyer un test' }}
                </button>
            </div>
            <p class="feedback feedback--ok" *ngIf="testSuccess">{{ testSuccess }}</p>
            <p class="feedback feedback--error" *ngIf="testError">{{ testError }}</p>
        </section>

        <section class="panel">
            <h2 class="panel__title">2. Envoyer à tous les participants</h2>
            <button class="primary-btn" (click)="openConfirm()"
                [disabled]="!canSend || isLoadingCount || isSending || showConfirm">
                {{ isLoadingCount ? 'Chargement...' : 'Envoyer à tous les participants' }}
            </button>
            <p class="feedback feedback--error" *ngIf="sendError && !showConfirm">{{ sendError }}</p>

            <div class="delete-confirm" *ngIf="showConfirm">
                <p class="confirm-text">
                    <strong>{{ recipientCount }}</strong> participant(s) vont recevoir cet email.
                    Cet envoi est définitif. Continuer ?
                </p>
                <div class="confirm-error" *ngIf="sendError">{{ sendError }}</div>
                <div class="confirm-actions">
                    <button class="confirm-cancel-btn" (click)="cancelConfirm()" [disabled]="isSending">Annuler</button>
                    <button class="confirm-delete-btn" (click)="sendToAll()" [disabled]="isSending">
                        {{ isSending ? "Envoi en cours (jusqu'à 1 minute)..." : "Confirmer l'envoi" }}
                    </button>
                </div>
            </div>

            <div class="result" *ngIf="result">
                <p class="confirm-text">
                    <strong>{{ result.sent }}</strong> envoyé(s), <strong>{{ result.failed }}</strong> échec(s)
                    sur {{ result.total }}.
                </p>
                <ul class="failures" *ngIf="result.failed > 0">
                    <li *ngFor="let f of result.failures">{{ f.email }} : {{ f.error }}</li>
                </ul>
            </div>
        </section>
    </main>
</div>
```

- [ ] **Step 3: Styles**

`ui/src/app/pages/announcement/announcement.component.scss` (top bar copied from `players.component.scss`, confirm block from `teams.component.scss`, following how those two pages each own their copy):

```scss
@use "variables" as *;

:host {
  display: block;
  min-height: 100vh;
}

.announcement-page {
  min-height: 100vh;
  background-color: $background-color;
  font-family: $font-body;
  color: #ffffff;
}

// ── Top bar (same as players/teams) ──────────────────────────────────────────
.top-bar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.8rem 2rem;
  padding: 1.2rem 2.5rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.07);
}

.brand {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-right: auto;

  .logo-mark {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 36px;
    height: 36px;
    background: $main-color;
    color: $text-color;
    font-family: $font-title;
    font-size: 1rem;
    border-radius: 0.25em;
  }

  .brand-name {
    font-family: $font-title;
    font-size: 0.78rem;
    letter-spacing: 0.14em;
    color: rgba(255, 255, 255, 0.6);
    text-transform: uppercase;
  }
}

.nav-links {
  display: flex;
  gap: 0.3rem;
}

.nav-btn {
  background: transparent;
  border: 1px solid transparent;
  color: rgba(255, 255, 255, 0.4);
  padding: 0.4rem 1rem;
  border-radius: 0.25em;
  font-family: $font-body;
  font-size: 0.85rem;
  cursor: pointer;
  transition: all 0.15s;

  &:hover {
    color: rgba(255, 255, 255, 0.7);
    border-color: rgba(255, 255, 255, 0.1);
  }

  &:focus-visible {
    outline: 2px solid $main-color;
    outline-offset: 2px;
  }

  &.active {
    color: #ffffff;
    border-color: rgba(255, 255, 255, 0.15);
    background: rgba(255, 255, 255, 0.05);
  }
}

.user-area {
  display: flex;
  align-items: center;
  gap: 1.2rem;

  .username {
    font-size: 0.85rem;
    color: rgba(255, 255, 255, 0.4);
  }
}

.logout-btn {
  background: transparent;
  border: 1px solid rgba(255, 255, 255, 0.15);
  color: rgba(255, 255, 255, 0.5);
  padding: 0.4rem 0.9rem;
  border-radius: 0.25em;
  font-family: $font-body;
  font-size: 0.8rem;
  cursor: pointer;

  &:hover {
    border-color: $main-color;
    color: $main-color;
  }

  &:focus-visible {
    outline: 2px solid $main-color;
    outline-offset: 2px;
  }
}

// ── Content ──────────────────────────────────────────────────────────────────
.content {
  padding: 3rem 2.5rem;
  max-width: 820px;
}

.page-title {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 1rem;
  margin-bottom: 2rem;

  h1 {
    font-family: $font-title;
    font-size: $text-2xl;
    margin: 0;
  }

  .count {
    font-size: 0.85rem;
    color: rgba(255, 255, 255, 0.4);
  }
}

.panel {
  border: 1px solid rgba(255, 255, 255, 0.07);
  border-radius: 0.25em;
  padding: 1.5rem;
  margin-bottom: 1.5rem;
}

.panel__title {
  font-family: $font-title;
  font-size: 0.85rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  margin: 0 0 1rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  margin-bottom: 1.2rem;

  &:last-child {
    margin-bottom: 0;
  }
}

.field__label {
  font-size: 0.8rem;
  color: rgba(255, 255, 255, 0.6);
}

.field__input {
  width: 100%;
  box-sizing: border-box;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 0.25em;
  color: #ffffff;
  font-family: $font-body;
  font-size: 0.95rem;
  padding: 0.6rem 0.8rem;

  &:focus-visible {
    outline: 2px solid $main-color;
    outline-offset: 1px;
  }
}

.field__textarea {
  resize: vertical;
  line-height: 1.5;
}

.field__hint {
  font-size: 0.78rem;
  color: rgba(255, 255, 255, 0.4);
}

.test-row {
  display: flex;
  flex-wrap: wrap;
  gap: 0.6rem;

  .field__input {
    flex: 1 1 240px;
    width: auto;
  }
}

.primary-btn,
.secondary-btn {
  padding: 0.65rem 1.2rem;
  border-radius: 0.25em;
  font-family: $font-body;
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;

  &:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }

  &:focus-visible {
    outline: 2px solid $main-color;
    outline-offset: 2px;
  }
}

.primary-btn {
  background: $main-color;
  color: $text-color;
  border: none;
}

.secondary-btn {
  background: transparent;
  color: #ffffff;
  border: 1px solid rgba(255, 255, 255, 0.25);

  &:hover:not(:disabled) {
    border-color: $main-color;
    color: $main-color;
  }
}

.feedback {
  font-size: 0.85rem;
  margin: 0.8rem 0 0;
}

.feedback--ok {
  color: #4ade80;
}

.feedback--error {
  color: #f87171;
}

// ── Confirm + result (same look as the bulk activation confirm on /teams) ─────
.delete-confirm,
.result {
  margin-top: 1.2rem;
  padding: 1.5rem;
  border-radius: 0.25em;
}

.delete-confirm {
  background: rgba(220, 38, 38, 0.05);
  border: 1px solid rgba(220, 38, 38, 0.3);
}

.result {
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.1);
}

.confirm-text {
  font-size: 0.9rem;
  color: rgba(255, 255, 255, 0.7);
  margin: 0 0 1.2rem;
  line-height: 1.5;

  strong {
    color: #ffffff;
  }
}

.confirm-error {
  font-size: 0.82rem;
  color: #f87171;
  margin-bottom: 1rem;
}

.confirm-actions {
  display: flex;
  gap: 0.6rem;
}

.confirm-cancel-btn,
.confirm-delete-btn {
  flex: 1;
  padding: 0.6rem;
  border-radius: 0.25em;
  font-family: $font-body;
  font-size: 0.85rem;
  cursor: pointer;

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}

.confirm-cancel-btn {
  background: transparent;
  border: 1px solid rgba(255, 255, 255, 0.15);
  color: rgba(255, 255, 255, 0.5);
}

.confirm-delete-btn {
  background: #dc2626;
  border: none;
  color: white;
  font-weight: 600;
}

.failures {
  margin: 0;
  padding-left: 1.2rem;
  font-size: 0.85rem;
  color: #f87171;
}

@media (max-width: 640px) {
  .top-bar,
  .content {
    padding-left: 1rem;
    padding-right: 1rem;
  }
}
```

- [ ] **Step 4: Route and nav links**

In `ui/src/app/app.routes.ts`, add the import:

```ts
import { AnnouncementComponent } from './pages/announcement/announcement.component';
```

and, after the `players` route, add:

```ts
    { path: "annonce", component: AnnouncementComponent, canActivate: [AuthGuard], data: { role: 'Admin' } },
```

In `ui/src/app/pages/teams/teams.component.html`, change the nav to:

```html
        <nav class="nav-links">
            <button class="nav-btn active">Équipes</button>
            <button class="nav-btn" routerLink="/players">Joueurs</button>
            <button class="nav-btn" routerLink="/annonce">Annonce</button>
        </nav>
```

In `ui/src/app/pages/players/players.component.html`, change the nav to:

```html
        <nav class="nav-links">
            <button class="nav-btn" (click)="goToTeams()">Équipes</button>
            <button class="nav-btn active">Joueurs</button>
            <button class="nav-btn" routerLink="/annonce">Annonce</button>
        </nav>
```

(Both components already import `RouterModule`.)

- [ ] **Step 5: Check it manually**

Run: `docker compose up --build -d` (this also compiles the Angular app, so a template or type error fails the build here).
Then, as admin:
1. Open `/teams`, click "Annonce", and confirm the page loads with the nav highlighted. "Équipes" and "Joueurs" navigate back.
2. With subject or message empty, both send buttons are disabled.
3. Fill both and "Envoyer un test" to a real address. If Mailgun is configured in dev, it shows "Test envoyé à …" and the email arrives with greeting "Bonjour,", paragraphs, clickable link and footer. If not, the red 502 message shows.
4. "Envoyer à tous les participants" opens the confirm with a count. Cancel closes it. **In dev only** (seeded `@example.com` players), confirm, and the result summary shows sent/failed.
5. Log in as a participant and open `/annonce`: redirected to `/mon-equipe`.
6. At 375px width, the top bar wraps and there's no horizontal scroll.

- [ ] **Step 6: Commit**

```bash
git add ui/src/app/pages/announcement ui/src/app/app.routes.ts ui/src/app/pages/teams/teams.component.html ui/src/app/pages/players/players.component.html
git commit -m "feat: add admin Annonce page to email all registered participants

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: Event date from `EVENT_DATE_LABEL`

**Files:**
- Modify: `.env.sample`, `docker-compose.yml` (`frontend.environment`)
- Modify: `ui/docker-entrypoint.d/40-generate-runtime-env.sh`, `ui/public/env.template.js`
- Modify: `ui/src/environment.ts`, `ui/src/app/core/runtime-env.ts`
- Modify: `ui/src/app/pages/landing/landing.component.{ts,html}`, `ui/src/app/pages/my-team/my-team.component.{ts,html}`, `ui/src/app/pages/not-found/not-found.component.{ts,html}`

**Interfaces:**
- Produces: `environment.eventDateLabel: string` exported from `ui/src/app/core/runtime-env.ts`.

- [ ] **Step 1: Config plumbing**

`.env.sample`, add at the end:

```
# EVENT (shown on the landing, participant and 404 pages; empty = "Date à confirmer")
EVENT_DATE_LABEL=
```

`docker-compose.yml`, in `frontend.environment` add:

```yaml
      EVENT_DATE_LABEL: ${EVENT_DATE_LABEL}
```

`ui/docker-entrypoint.d/40-generate-runtime-env.sh`, replace the `envsubst` line with:

```sh
envsubst '${API_BASE_URL} ${STRIPE_PUBLISHABLE_KEY} ${EVENT_DATE_LABEL}' \
```

`ui/public/env.template.js`, full new content:

```js
(function (window) {
  window.__env = window.__env || {};
  window.__env.apiUrl = "${API_BASE_URL}";
  window.__env.stripePublishableKey = "${STRIPE_PUBLISHABLE_KEY}";
  window.__env.eventDateLabel = "${EVENT_DATE_LABEL}";
})(this);
```

`ui/src/environment.ts`, replace the exported object with:

```ts
export const environment = {
    apiUrl: 'http://localhost:7163',
    stripePublishableKey: '',
    eventDateLabel: 'Date à confirmer'
};
```

`ui/src/app/core/runtime-env.ts`: add `eventDateLabel?: string;` to the `__env` type, and replace the exported object with:

```ts
export const environment = {
    apiUrl: pick(runtime?.apiUrl, buildEnvironment.apiUrl),
    stripePublishableKey: pick(runtime?.stripePublishableKey, buildEnvironment.stripePublishableKey),
    eventDateLabel: pick(runtime?.eventDateLabel, buildEnvironment.eventDateLabel)
};
```

Note: the label is placed inside a JS double-quoted string by `envsubst`. Don't put `"` in `EVENT_DATE_LABEL`.

- [ ] **Step 2: Use the label in the four places**

For each of `landing.component.ts`, `my-team.component.ts`, `not-found.component.ts`, add the import (path relative to `pages/<name>/`):

```ts
import { environment } from '../../core/runtime-env';
```

and, as the first property of the component class:

```ts
  readonly eventDateLabel = environment.eventDateLabel;
```

Templates:
- `landing.component.html`: `<span>Septembre 2026 · Laxou (54)</span>` → `<span>{{ eventDateLabel }} · Laxou (54)</span>`
- `landing.component.html`: `<p>Places limitées · 60 € par duo · Septembre 2026</p>` → `<p>Places limitées · 60 € par duo · {{ eventDateLabel }}</p>`
- `my-team.component.html`: `<div class="status-tile__value">Septembre 2026</div>` → `<div class="status-tile__value">{{ eventDateLabel }}</div>`
- `not-found.component.html`: `<p>La page que vous cherchez n'existe pas. Rendez-vous en Septembre pour Hyrox Police 54 !</p>` → `<p>La page que vous cherchez n'existe pas. Rendez-vous à Hyrox Police 54 : {{ eventDateLabel }} !</p>`

Then confirm nothing is left: search `ui/src/app` for `Septembre` and expect no matches.

- [ ] **Step 3: Check it manually**

1. With `EVENT_DATE_LABEL` absent from `.env`: `docker compose up --build -d`. The landing hero, the landing's final sign-up section, `/mon-equipe` (as a participant) and `/nimportequoi` (404) all show "Date à confirmer".
2. Add `EVENT_DATE_LABEL=Samedi 14 mars 2027` to `.env`, run `docker compose up -d frontend` (no `--build`), hard-reload: all four show "Samedi 14 mars 2027".
3. Remove the test value from `.env` again, or leave it if the real date is known.

- [ ] **Step 4: Commit**

```bash
git add .env.sample docker-compose.yml ui/docker-entrypoint.d/40-generate-runtime-env.sh ui/public/env.template.js ui/src/environment.ts ui/src/app/core/runtime-env.ts ui/src/app/pages/landing ui/src/app/pages/my-team ui/src/app/pages/not-found
git commit -m "feat: read the event date from EVENT_DATE_LABEL at runtime instead of hardcoding it

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 7: Docs + final verification

**Files:**
- Modify: `CLAUDE.md`
- Modify: `docs/manual-testing-guide.md`

- [ ] **Step 1: Update `CLAUDE.md`**

In the **Backend architecture → Controllers → Services** list, after the `MailService` bullet, add:

```markdown
  - `AnnouncementsController` / `AnnouncementService` (admin-only): free-text announcement email to **every registered player**, deduplicated by email (case-insensitive; lowest player `Id` supplies the first name). `GET recipients` (count for the confirm dialog), `POST test` (one address, 502 if Mailgun refused/unconfigured), `POST send` (one Mailgun call per recipient, parallel batches of 5, returns sent/failed + failures). Body formatting lives in `MailService.BuildAnnouncementText/Html`, which HTML-encodes the admin's text before adding paragraphs/links.
```

In the `AuthController / AuthService` bullet, after the sentence about `/login`, add:

```markdown
`/login` is protected by `Services/LoginRateLimiter.cs` (in-memory singleton): 10 failed attempts per username (case-insensitive) in a sliding 15 minutes return 429 even for a correct password; a successful login resets the counter. Per-username on purpose: per-IP would make the shared test suite lock itself out.
```

In **Frontend architecture**, in the routes sentence, add `annonce` to the pages that require `Admin` (`teams`/`players`/`annonce` require `Admin`). In the runtime-config bullet, change "using `API_BASE_URL` / `STRIPE_PUBLISHABLE_KEY`" to "using `API_BASE_URL` / `STRIPE_PUBLISHABLE_KEY` / `EVENT_DATE_LABEL`", and add at the end of that bullet:

```markdown
`EVENT_DATE_LABEL` is the event date text shown on the landing, `mon-equipe` and 404 pages (fallback "Date à confirmer"); change it in `.env` and run `docker compose up -d frontend`, no rebuild.
```

In the **Commands** section, after the paragraph about ports, add:

```markdown
The UI and API ports are published on `127.0.0.1` only: in prod they're reached through Caddy on the host. Don't publish the API on all interfaces, or `X-Forwarded-For` can be forged to bypass the per-IP password-reset limit.
```

- [ ] **Step 2: Update `docs/manual-testing-guide.md`**

Append two sections, numbered after the current last section (`## 8. Confirm role gating`):

```markdown
## 9. Send an announcement

1. Log in as admin, click **Annonce** in the top bar.
2. Fill **Objet** and **Message** (two paragraphs separated by a blank line, plus a `https://` link).
3. **Envoyer un test** to your own address. Check the email: "Bonjour," greeting, two paragraphs, a clickable link, the "Vous recevez cet email…" footer. With Mailgun unconfigured, a red error shows instead.
4. **Envoyer à tous les participants**: the confirm dialog shows a count. In dev only (seeded `@example.com` players), confirm and check the "envoyé(s) / échec(s)" summary.
5. As a participant, open `/annonce`: you're sent to `/mon-equipe`.

## 10. Login lockout and event date

1. Log in 10 times with a wrong password for one participant account, then with the right one: the banner says "Trop de tentatives de connexion. Réessayez dans 15 minutes."
2. With `EVENT_DATE_LABEL` unset, the landing hero, landing sign-up section, `/mon-equipe` and the 404 page show "Date à confirmer". Set it in `.env`, run `docker compose up -d frontend`, reload: the new text shows in all four places.
```

- [ ] **Step 3: Full suite**

Run: `docker compose run --rm --build tests`
Expected: all pass, including `LoginRateLimiterTests`, `AnnouncementEmailFormattingTests`, `AnnouncementTests` and the new `AuthTests`.

- [ ] **Step 4: Full app build**

Run: `docker compose up --build -d`
Expected: all three containers start. `docker compose ps` shows the UI and API on `127.0.0.1:<port>`.

- [ ] **Step 5: Commit**

```bash
git add CLAUDE.md docs/manual-testing-guide.md
git commit -m "docs: document announcements, login lockout, EVENT_DATE_LABEL and localhost ports

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

## Deployment notes (for the user, not an agent task)

- Before deploying Task 2's port change to prod, check that Caddy runs on the host (not in a container). After deploying, `curl http://<public-ip>:<API_PORT>` from outside must fail while the site works through its domain.
- Prod `.env`: add `EVENT_DATE_LABEL` once the date is known. Keep the placeholder `STRIPE_WEBHOOK_SECRET`.
- Check the Mailgun plan's daily quota against the count shown in the confirm dialog before sending.
- Update the event date on Yurplan.
- Draft email to paste: see the appendix of the spec.
