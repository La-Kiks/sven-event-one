# Bulk Activation Email Sending Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let an admin send activation emails to every team still missing a verified account in one action, with per-team success/failure reporting, while keeping the existing per-team "create/resend account" button for stragglers.

**Architecture:** A new `UserService.CreateAccountsForPendingTeamsAsync()` prepares accounts for all pending teams in one sequential DB pass (fast, not parallelizable — `DbContext` isn't thread-safe), then sends the resulting activation emails in parallel batches of 5 (safe to parallelize — no shared DB state, just independent HTTP calls). This requires `MailService.SendActivationEmailAsync` to report success/failure instead of silently swallowing it, so the bulk response can tell the admin exactly which teams succeeded.

**Tech Stack:** ASP.NET Core 8, EF Core, Angular 19, xUnit integration tests.

## Global Constraints

- Everything runs via Docker Compose — `docker compose up --build` for the app, `docker compose run --rm tests` for the test suite. No bare `dotnet`/`npm`/`ng`.
- The Mailgun send itself must never throw past `MailService` — only its return value changes (from `Task` to `Task<bool>`), the internal catch-and-log behavior stays.
- The bulk endpoint is `[Authorize(Roles = "Admin")]`, matching every other admin-only endpoint on `TeamsController`.
- No background job infrastructure — this is a single request/response cycle, sized for the current volume (~50 teams, batches of 5 keep it well under a minute).

---

### Task 1: Backend — bulk endpoint

**Files:**
- Modify: `backend/SportsReservationAPI/Services/MailService.cs:22,27,58-62,64-67` (return `Task<bool>` instead of `Task`)
- Create: `backend/SportsReservationAPI/Models/User/BulkAccountResult.cs`
- Modify: `backend/SportsReservationAPI/Services/UserService.cs` (new `CreateAccountsForPendingTeamsAsync()` method)
- Modify: `backend/SportsReservationAPI/Controllers/TeamsController.cs` (new `POST create-account-bulk` action)
- Modify: `backend/SportsReservationAPI.Tests/AdminTeamsTests.cs` (add `using Microsoft.EntityFrameworkCore;`, two new tests)

**Interfaces:**
- Consumes: `ApiTestFixture.RegisterAndActivateTeamAsync(...)`, `ApiTestFixture.GetAdminJwtAsync()`, `ApiTestFixture.UniqueEmail(string)`, `ApiTestFixture.CreateDbContext()` (all existing, from `backend/SportsReservationAPI.Tests/ApiTestFixture.cs`).
- Produces: `POST /api/Teams/create-account-bulk` returning `List<BulkAccountResult>` — `{ teamId: number, teamName: string, status: "sent" | "failed", error?: string }` per team, JSON camelCased by the existing default serializer (matches every other endpoint's casing, e.g. `TeamDto`).

- [ ] **Step 1: Change `MailService.SendActivationEmailAsync` to report success**

Open `backend/SportsReservationAPI/Services/MailService.cs`. Replace the entire method:

```csharp
    public async Task<bool> SendActivationEmailAsync(string toEmail, string toName, string activationUrl)
    {
        if (string.IsNullOrWhiteSpace(_mailSettings.ApiKey) || string.IsNullOrWhiteSpace(_mailSettings.Domain))
        {
            _logger.LogWarning("Mailgun is not configured (MAILGUN_API_KEY/MAILGUN_DOMAIN missing) - skipping activation email to {Email}", toEmail);
            return false;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_mailSettings.BaseUrl}/v3/{_mailSettings.Domain}/messages");
            var authBytes = Encoding.UTF8.GetBytes($"api:{_mailSettings.ApiKey}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var fromName = string.IsNullOrWhiteSpace(_mailSettings.FromName) ? "Sport Challenge Police 54" : _mailSettings.FromName;
            var toNameHtml = WebUtility.HtmlEncode(toName);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["from"] = $"{fromName} <{_mailSettings.FromAddress}>",
                ["to"] = $"{toName} <{toEmail}>",
                ["subject"] = "Activez votre compte - Sport Challenge Police 54",
                ["text"] =
                    $"Bonjour {toName},\n\n" +
                    "Votre equipe a bien ete enregistree. Cliquez sur le lien ci-dessous pour verifier votre email et definir votre mot de passe :\n" +
                    $"{activationUrl}\n\n" +
                    "Ce lien est valable 7 jours.\n\n" +
                    "A bientot,\nSport Challenge Police 54",
                ["html"] =
                    $"<p>Bonjour {toNameHtml},</p>" +
                    "<p>Votre équipe a bien été enregistrée. Cliquez sur le lien ci-dessous pour vérifier votre email et définir votre mot de passe :</p>" +
                    $"<p><a href=\"{activationUrl}\">{activationUrl}</a></p>" +
                    "<p>Ce lien est valable 7 jours.</p>" +
                    "<p>À bientôt,<br>Sport Challenge Police 54</p>"
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
            _logger.LogWarning(ex, "Failed to send activation email to {Email}", toEmail);
            return false;
        }
    }
```

The two existing call sites (`TeamService.CreateTeamWithPlayersAsync` and `UserService.CreateOrRefreshAccountForTeamAsync`) call this as a bare `await ...;` statement without using the result — they compile and behave identically with the new `Task<bool>` return type, no changes needed there.

- [ ] **Step 2: Add the `BulkAccountResult` DTO**

Create `backend/SportsReservationAPI/Models/User/BulkAccountResult.cs`:

```csharp
namespace SportsReservationAPI.Models.User
{
    public class BulkAccountResult
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = "";
        public string Status { get; set; } = ""; // "sent" | "failed"
        public string? Error { get; set; }
    }
}
```

- [ ] **Step 3: Add `UserService.CreateAccountsForPendingTeamsAsync()`**

Open `backend/SportsReservationAPI/Services/UserService.cs`. Add this method right after `CreateOrRefreshAccountForTeamAsync` (before `BuildActivationUrl`):

```csharp
        // Admin-triggered bulk backfill: prepares an account for every team missing a
        // verified one, then sends all activation emails in parallel batches of 5 (DB
        // writes stay sequential — DbContext isn't safe for concurrent use — only the
        // independent Mailgun HTTP calls are parallelized). Returns one result per team
        // attempted; teams already fully verified are excluded from the query entirely.
        public async Task<List<BulkAccountResult>> CreateAccountsForPendingTeamsAsync()
        {
            var teams = await _context.Teams
                .Include(t => t.Players)
                .Include(t => t.Account)
                .Where(t => t.Account == null || !t.Account.EmailVerified)
                .ToListAsync();

            var results = new List<BulkAccountResult>();
            var toSend = new List<(BulkAccountResult Result, string Email, string FirstName, string ActivationUrl)>();

            foreach (var team in teams)
            {
                var result = new BulkAccountResult { TeamId = team.Id, TeamName = team.Name };

                if (team.Players.Count == 0)
                {
                    result.Status = "failed";
                    result.Error = "Équipe sans participant.";
                    results.Add(result);
                    continue;
                }

                var participant1 = team.Players.OrderBy(p => p.Id).First();
                var user = team.Account;

                try
                {
                    if (user == null)
                    {
                        user = await BuildPendingAccountAsync(participant1.Email);
                        user.TeamId = team.Id;
                        _context.Users.Add(user);
                    }
                    else
                    {
                        user.VerificationToken = GenerateToken();
                        user.VerificationTokenExpiresAt = DateTime.UtcNow.AddDays(7);
                    }
                }
                catch (ValidationException ex)
                {
                    result.Status = "failed";
                    result.Error = ex.Message;
                    results.Add(result);
                    continue;
                }

                results.Add(result);
                toSend.Add((result, user.Username, participant1.FirstName, BuildActivationUrl(user.VerificationToken!)));
            }

            await _context.SaveChangesAsync();

            const int batchSize = 5;
            for (var i = 0; i < toSend.Count; i += batchSize)
            {
                var batch = toSend.Skip(i).Take(batchSize);
                await Task.WhenAll(batch.Select(async item =>
                {
                    var sent = await _mailService.SendActivationEmailAsync(item.Email, item.FirstName, item.ActivationUrl);
                    item.Result.Status = sent ? "sent" : "failed";
                    if (!sent) item.Result.Error = "Échec de l'envoi de l'email.";
                }));
            }

            return results;
        }
```

- [ ] **Step 4: Add the controller endpoint**

Open `backend/SportsReservationAPI/Controllers/TeamsController.cs`. Find the `CreateAccount` action (the individual per-team one) and add this new action immediately after it:

```csharp
        // ── Protected: POST /api/Teams/create-account-bulk ─────────────────────────
        // Sends activation emails to every team still missing a verified account.
        [HttpPost("create-account-bulk")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAccountsBulk()
        {
            var results = await _userService.CreateAccountsForPendingTeamsAsync();
            return Ok(results);
        }
```

- [ ] **Step 5: Rebuild and smoke-test manually**

```bash
docker compose up --build -d
```

Register two teams without activating them (adjust emails to your own test addresses), then:

```bash
curl -s -X POST "http://localhost:7163/api/auth/login" -H "Content-Type: application/json" -d '{"username":"admin","password":"<your ADMIN_PASSWORD from .env>"}'
```

Copy the returned `token`, then:

```bash
curl -s -X POST "http://localhost:7163/api/Teams/create-account-bulk" -H "Authorization: Bearer <token>"
```

Expected: a JSON array with one entry per pending team, each `{"teamId":...,"teamName":"...","status":"sent","error":null}` (or `"failed"` with a message if Mailgun rejects it — check backend logs either way with `docker compose logs backend --tail=20`).

- [ ] **Step 6: Write the integration tests**

Open `backend/SportsReservationAPI.Tests/AdminTeamsTests.cs`. Add the missing import — find:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
```

Replace with:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
```

Then add these two tests at the end of the class, immediately before the final closing `}`:

```csharp
    [Fact]
    public async Task CreateAccountBulk_SendsToAllPendingTeamsAndSkipsVerified()
    {
        var adminJwt = await _fixture.GetAdminJwtAsync();

        var pending1Email = ApiTestFixture.UniqueEmail("bulk-pending1");
        await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName = "BulkPendingTeamOne", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Alice", lastName = "Test", email = pending1Email, phoneNumber = "+33612345678", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Bob", lastName = "Test", email = ApiTestFixture.UniqueEmail("bulk-pending1b"), phoneNumber = "+33612345679", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });

        var pending2Email = ApiTestFixture.UniqueEmail("bulk-pending2");
        await _fixture.Client.PostAsJsonAsync("/api/teams/create-team", new
        {
            teamDto = new { teamName = "BulkPendingTeamTwo", version = "short", administration = "none" },
            playerDtos = new[]
            {
                new { firstName = "Carol", lastName = "Test", email = pending2Email, phoneNumber = "+33612345680", category = "woman", outfit = "no", volunteer = false, acceptMails = true },
                new { firstName = "Dave", lastName = "Test", email = ApiTestFixture.UniqueEmail("bulk-pending2b"), phoneNumber = "+33612345681", category = "man", outfit = "no", volunteer = false, acceptMails = true }
            }
        });

        var (activatedTeamId, _) = await _fixture.RegisterAndActivateTeamAsync(
            "BulkAlreadyActiveTeam", ApiTestFixture.UniqueEmail("bulk-active1"), ApiTestFixture.UniqueEmail("bulk-active2"));

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Post, "/api/Teams/create-account-bulk", adminJwt));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<JsonElement>();
        var resultList = results.EnumerateArray().ToList();

        var teamOneResult = resultList.First(r => r.GetProperty("teamName").GetString() == "BulkPendingTeamOne");
        var teamTwoResult = resultList.First(r => r.GetProperty("teamName").GetString() == "BulkPendingTeamTwo");

        // Mailgun isn't configured in the test environment (see docker-compose.yml's
        // `tests` service — no MAILGUN_* vars), so every send reports "failed" here.
        // That's expected and still proves the endpoint processed each pending team
        // individually and reported per-team status instead of crashing the whole
        // batch. The DB assertions below prove phase 1 (account preparation) worked
        // regardless of phase 2 (the simulated mail outage).
        Assert.Equal("failed", teamOneResult.GetProperty("status").GetString());
        Assert.Equal("failed", teamTwoResult.GetProperty("status").GetString());

        Assert.DoesNotContain(resultList, r => r.GetProperty("teamId").GetInt32() == activatedTeamId);

        using var context = _fixture.CreateDbContext();
        var user1 = await context.Users.FirstAsync(u => u.Username == pending1Email);
        var user2 = await context.Users.FirstAsync(u => u.Username == pending2Email);
        Assert.False(string.IsNullOrEmpty(user1.VerificationToken));
        Assert.False(string.IsNullOrEmpty(user2.VerificationToken));
    }

    [Fact]
    public async Task CreateAccountBulk_WithParticipantToken_ReturnsForbidden()
    {
        var (_, participantJwt) = await _fixture.RegisterAndActivateTeamAsync(
            "BulkForbiddenTeam", ApiTestFixture.UniqueEmail("bulkforbid1"), ApiTestFixture.UniqueEmail("bulkforbid2"));

        var response = await _fixture.Client.SendAsync(AuthedRequest(HttpMethod.Post, "/api/Teams/create-account-bulk", participantJwt));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
```

- [ ] **Step 7: Run the suite**

```bash
docker compose run --rm tests
```

Expected: `Passed!  - Failed:     0, Passed:    32, Skipped:     0, Total:    32` (30 existing + these 2).

- [ ] **Step 8: Commit**

```bash
git add backend/SportsReservationAPI/Services/MailService.cs backend/SportsReservationAPI/Models/User/BulkAccountResult.cs backend/SportsReservationAPI/Services/UserService.cs backend/SportsReservationAPI/Controllers/TeamsController.cs backend/SportsReservationAPI.Tests/AdminTeamsTests.cs
git commit -m "feat: add bulk activation email endpoint for admin"
```

---

### Task 2: Frontend — bulk send button, confirmation, and results summary

**Files:**
- Modify: `ui/src/app/pages/teams/teams.component.ts`
- Modify: `ui/src/app/pages/teams/teams.component.html`
- Modify: `ui/src/app/pages/teams/teams.component.scss`

**Interfaces:**
- Consumes: `POST /api/Teams/create-account-bulk` (Task 1) returning `{ teamId: number; teamName: string; status: string; error?: string }[]`.
- Produces: nothing — last task in this plan.

- [ ] **Step 1: Add bulk-send state and methods to the component**

Open `ui/src/app/pages/teams/teams.component.ts`. Find:

```typescript
  // Account creation state
  isCreatingAccount = false;
  createAccountMessage = '';
  createAccountError = '';
```

Replace with:

```typescript
  // Account creation state
  isCreatingAccount = false;
  createAccountMessage = '';
  createAccountError = '';

  // Bulk account creation state
  isSendingBulk = false;
  showBulkConfirm = false;
  bulkResults: { teamId: number; teamName: string; status: string; error?: string }[] | null = null;
  bulkError = '';
```

Then find the closing `}` of the `createAccount` method (the last method in the class) and add these new methods right after it, before the class's final closing `}`:

```typescript

  get pendingAccountsCount(): number {
    return this.teams.filter(t => !t.accountVerified).length;
  }

  openBulkConfirm(): void {
    this.showBulkConfirm = true;
    this.bulkResults = null;
    this.bulkError = '';
  }

  cancelBulkConfirm(): void {
    this.showBulkConfirm = false;
  }

  sendBulkActivationEmails(): void {
    this.isSendingBulk = true;
    this.bulkError = '';

    this.http.post<{ teamId: number; teamName: string; status: string; error?: string }[]>(
      `${environment.apiUrl}/api/Teams/create-account-bulk`, {}
    ).subscribe({
      next: (results) => {
        this.isSendingBulk = false;
        this.showBulkConfirm = false;
        this.bulkResults = results;
        for (const result of results) {
          if (result.status !== 'sent') continue;
          const team = this.teams.find(t => t.id === result.teamId);
          if (team) team.hasAccount = true;
          if (this.selectedTeam?.id === result.teamId) this.selectedTeam.hasAccount = true;
        }
      },
      error: () => {
        this.isSendingBulk = false;
        this.bulkError = "Échec de l'envoi groupé. Réessayez.";
      }
    });
  }

  bulkResultsSentCount(): number {
    return this.bulkResults?.filter(r => r.status === 'sent').length ?? 0;
  }

  bulkResultsFailedCount(): number {
    return this.bulkResults?.filter(r => r.status === 'failed').length ?? 0;
  }
```

- [ ] **Step 2: Add the button and confirm/results UI**

Open `ui/src/app/pages/teams/teams.component.html`. Find:

```html
        <div class="page-title">
            <h1>Teams</h1>
            <span class="count" *ngIf="!isLoading && !error">{{ teams.length }} teams</span>
        </div>
```

Replace with:

```html
        <div class="page-title">
            <h1>Teams</h1>
            <span class="count" *ngIf="!isLoading && !error">{{ teams.length }} teams</span>
            <button class="bulk-send-trigger-btn" *ngIf="!isLoading && !error && pendingAccountsCount > 0"
                (click)="openBulkConfirm()">
                Envoyer les emails d'activation ({{ pendingAccountsCount }} en attente)
            </button>
        </div>

        <div class="delete-confirm" *ngIf="showBulkConfirm">
            <p class="confirm-text">
                <strong>{{ pendingAccountsCount }}</strong> équipe(s) vont recevoir un email d'activation. Continuer
                ?
            </p>
            <div class="confirm-error" *ngIf="bulkError">{{ bulkError }}</div>
            <div class="confirm-actions">
                <button class="confirm-cancel-btn" (click)="cancelBulkConfirm()"
                    [disabled]="isSendingBulk">Annuler</button>
                <button class="confirm-delete-btn" (click)="sendBulkActivationEmails()" [disabled]="isSendingBulk">
                    {{ isSendingBulk ? 'Envoi en cours...' : "Confirmer l'envoi" }}
                </button>
            </div>
        </div>

        <div class="delete-confirm" *ngIf="bulkResults">
            <p class="confirm-text">
                <strong>{{ bulkResults.length }}</strong> équipe(s) traitées :
                {{ bulkResultsSentCount() }} envoyé(s), {{ bulkResultsFailedCount() }} échec(s).
            </p>
            <ul *ngIf="bulkResultsFailedCount() > 0">
                <li *ngFor="let r of bulkResults">
                    <ng-container *ngIf="r.status === 'failed'">{{ r.teamName }} : {{ r.error }}</ng-container>
                </li>
            </ul>
            <div class="confirm-actions">
                <button class="confirm-cancel-btn" (click)="bulkResults = null">Fermer</button>
            </div>
        </div>
```

- [ ] **Step 3: Style the trigger button**

Open `ui/src/app/pages/teams/teams.component.scss`. Find:

```scss
.page-title {
  display: flex;
  align-items: baseline;
  gap: 1rem;
  margin-bottom: 2.5rem;

  h1 {
    font-family: "Bebas Neue", sans-serif;
    font-size: 3rem;
    letter-spacing: 0.08em;
    margin: 0;
  }

  .count {
    font-size: 0.8rem;
    color: rgba(255, 255, 255, 0.25);
    letter-spacing: 0.05em;
    text-transform: uppercase;
  }
}
```

Replace with:

```scss
.page-title {
  display: flex;
  align-items: baseline;
  gap: 1rem;
  margin-bottom: 2.5rem;

  h1 {
    font-family: "Bebas Neue", sans-serif;
    font-size: 3rem;
    letter-spacing: 0.08em;
    margin: 0;
  }

  .count {
    font-size: 0.8rem;
    color: rgba(255, 255, 255, 0.25);
    letter-spacing: 0.05em;
    text-transform: uppercase;
  }
}

.bulk-send-trigger-btn {
  margin-left: auto;
  padding: 0.5rem 1rem;
  background: transparent;
  border: 1px solid rgba(220, 38, 38, 0.4);
  color: #f87171;
  border-radius: 3px;
  font-family: "DM Sans", sans-serif;
  font-size: 0.8rem;
  cursor: pointer;
  transition: all 0.15s;

  &:hover {
    background: rgba(220, 38, 38, 0.1);
    border-color: #dc2626;
  }
}
```

- [ ] **Step 4: Rebuild and verify manually**

```bash
docker compose up --build -d
```

Register 2-3 test teams without activating them. Log into `/teams` as admin. Expected: the new button reads "Envoyer les emails d'activation (N en attente)" with the correct count. Click it — expect a confirmation block with the same count and an "Annuler"/"Confirmer l'envoi" pair. Click "Confirmer l'envoi" — expect a brief loading state, then a results summary ("N équipe(s) traitées : X envoyé(s), Y échec(s)"). If any failed, expect a bulleted list naming each failing team and its error. Confirm the button's count drops to reflect only the teams that failed (successfully-sent teams should no longer count as pending on a page refresh).

- [ ] **Step 5: Commit**

```bash
git add ui/src/app/pages/teams/teams.component.ts ui/src/app/pages/teams/teams.component.html ui/src/app/pages/teams/teams.component.scss
git commit -m "feat: add bulk activation email button to admin teams panel"
```
