# Dev Admin Seeding + Mailgun Base URL Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Local/dev backend startup auto-creates the admin account when missing, and Mailgun's API base URL becomes a configurable env var instead of a hardcoded string.

**Architecture:** Both changes extend existing, already-proven patterns in this codebase rather than introducing new ones: the admin seed reuses the "auto-run at startup, guarded, best-effort" shape already used for EF migrations and for `MailService`'s no-op-when-unconfigured behavior; the Mailgun base URL follows the exact `EnvLoader.cs` → `ApiSettings.cs` → consuming service` wiring already used for every other Stripe/Mail/JWT setting.

**Tech Stack:** ASP.NET Core 8 (C#), EF Core, BCrypt.Net, Docker Compose. No test project exists in this repo — verification is done by rebuilding via `docker compose up --build` and exercising the running API with `curl`, matching how every prior change in this codebase has been verified (there is no `dotnet test`/`npm test` step to run, and per `CLAUDE.md` the user does not run `dotnet`/`npm`/`ng` commands directly).

## Global Constraints

- This project is **only ever run via Docker Compose** (`docker compose up --build`) — never suggest or run `dotnet`, `npm`, or `ng` commands directly.
- `.env` is gitignored and must **never** contain values pasted into any tracked file (including this plan, specs, or code comments) — only variable *names* may appear in tracked files.
- The admin-seed logic must be inert in production: gate it on `ApiSettings.Environment == "Development"` (the `ENVIRONMENT` env var, mapped via `EnvLoader.cs`) — **not** `app.Environment.IsDevelopment()` / `ASPNETCORE_ENVIRONMENT`, which isn't set in `docker-compose.yml` and would not reflect the `.env` file's intent.
- Nothing in either task should throw and block backend startup on missing/blank config — match the existing best-effort pattern (`MailService` logs a warning and no-ops when unconfigured).

---

### Task 1: Dev-only admin account seeding

**Files:**
- Modify: `backend/SportsReservationAPI/Program.cs:137` (insert new block right after the existing migration `using` block, before the `// Middleware pipeline` comment)
- Modify: `backend/SportsReservationAPI/Controllers/AuthController.cs:78-97` (delete the commented-out `seed` endpoint — directly superseded by this task)

**Interfaces:**
- Consumes: `ApiSettings.Environment` (existing property, already populated from `ENVIRONMENT` env var — see `Program.cs:28`), `ReservationContext` (existing `DbContext`, already registered in DI — see `Program.cs:68`), `SportsReservationAPI.Models.User.User` (existing entity: `Username`, `PasswordHash`, `Role` properties), `BCrypt.Net.BCrypt.HashPassword(string)` (already used identically in `AuthService.cs`).
- Produces: nothing new consumed by later tasks — this is a self-contained startup step.

- [ ] **Step 1: Add the seeding block to `Program.cs`**

Open `backend/SportsReservationAPI/Program.cs`. Add a new `using` directive at the top, alongside the existing per-entity imports:

```csharp
using SportsReservationAPI.Models.User;
```

So the top of the file reads:

```csharp
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SportsReservationAPI.Configuration;
using SportsReservationAPI.Models;
using SportsReservationAPI.Models.Player;
using SportsReservationAPI.Models.Team;
using SportsReservationAPI.Models.User;
using SportsReservationAPI.Services;
using System.Text;
```

Then find this existing block (it ends the migration step):

```csharp
// Apply pending EF Core migrations automatically
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ReservationContext>();
    try
    {
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine("Database migration failed");
        Console.WriteLine(ex);
        throw;
    }
}
```

Immediately after that closing `}`, insert:

```csharp
// Dev-only: auto-create the admin account so a fresh local/dev database is
// immediately usable via `docker compose up --build`, with no manual step.
// Production already has its admin seeded out of band and is unaffected
// (ENVIRONMENT will not be "Development" there).
if (apiSettings?.Environment == "Development")
{
    using var seedScope = app.Services.CreateScope();
    var seedContext = seedScope.ServiceProvider.GetRequiredService<ReservationContext>();

    var adminUsername = app.Configuration["ADMIN_USERNAME"];
    var adminPassword = app.Configuration["ADMIN_PASSWORD"];

    if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminPassword))
    {
        Console.WriteLine("ADMIN_USERNAME/ADMIN_PASSWORD not set - skipping dev admin seed");
    }
    else if (!seedContext.Users.Any(u => u.Username == adminUsername))
    {
        seedContext.Users.Add(new User
        {
            Username = adminUsername,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            Role = "Admin"
        });
        seedContext.SaveChanges();
        Console.WriteLine($"Dev admin account seeded: {adminUsername}");
    }
}
```

- [ ] **Step 2: Delete the superseded commented-out seed endpoint**

Open `backend/SportsReservationAPI/Controllers/AuthController.cs`. Delete this entire block (currently lines 78-97, right before the final closing `}` of the class):

```csharp
        // TODO : Comment this out in production, or add a check to only allow in development environment 
        //[HttpPost("seed")]
        //public IActionResult SeedUser(
        //     [FromServices] ReservationContext context, 
        //     [FromServices] IConfiguration configuration)
        //{
        //    var username = configuration["ADMIN_USERNAME"];
        //    var password = configuration["ADMIN_PASSWORD"];

        //    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        //        return BadRequest("Admin credentials not configured.");

        //    if (context.Users.Any(u => u.Username == username))
        //        return Ok("User already exists");

        //    var hash = BCrypt.Net.BCrypt.HashPassword(password);
        //    context.Users.Add(new User { Username = username, PasswordHash = hash, Role = "Admin" });
        //    context.SaveChanges();
        //    return Ok("User created");
        //}
```

The class should end with the `Activate` method's closing `}` followed directly by the class's closing `}`.

- [ ] **Step 3: Rebuild and verify the admin account is created**

Run:
```bash
docker compose up --build -d
```

Then check the backend logs for the seed confirmation:
```bash
docker compose logs backend --tail=40
```
Expected: a line reading `Dev admin account seeded: admin` (or whatever `ADMIN_USERNAME` is set to in your local `.env`), with no exceptions or `ADMIN_USERNAME/ADMIN_PASSWORD not set` warning.

- [ ] **Step 4: Verify login works with the seeded admin**

Run (replace the password with your local `.env`'s `ADMIN_PASSWORD` — never paste real credentials into a committed file):
```bash
curl -s -X POST "http://localhost:7163/api/auth/login" -H "Content-Type: application/json" -d '{"username":"admin","password":"<your ADMIN_PASSWORD from .env>"}'
```
Expected: a JSON response containing `"role":"Admin"` and a non-empty `"token"` field. A `401 Unauthorized` means the seed didn't run or the password doesn't match — recheck Step 3's logs.

As a final sanity check, open `http://localhost:7193/login` in a browser and log in with the same admin credentials. Expected: redirect to `/teams`, showing the admin panel with the existing teams listed.

- [ ] **Step 5: Verify idempotency (safe to restart)**

Run:
```bash
docker compose up --build -d
docker compose logs backend --tail=15
```
Expected: **no** `Dev admin account seeded` line this time (the user already exists, so the seed step silently skips it) — and the Step 4 login command still succeeds with the same credentials, unchanged.

- [ ] **Step 6: Commit**

```bash
git add backend/SportsReservationAPI/Program.cs backend/SportsReservationAPI/Controllers/AuthController.cs
git commit -m "feat: auto-seed admin account on dev startup, drop superseded seed endpoint"
```

---

### Task 2: Configurable Mailgun base URL

**Files:**
- Modify: `backend/SportsReservationAPI/Configuration/EnvLoader.cs:17-20` (add one mapping entry)
- Modify: `backend/SportsReservationAPI/Models/ApiSettings.cs:29-35` (add `BaseUrl` to `MailSettings`)
- Modify: `backend/SportsReservationAPI/Services/MailService.cs:31` (use the configured base URL instead of the hardcoded string)
- Modify: `.env.sample` (document the new key with a placeholder — never a real value)
- Modify: `docker-compose.yml:28-31` (pass the new env var into the backend container)
- Modify (untracked, not committed): your local `.env` — add the real Mailgun values you already have on hand

**Interfaces:**
- Consumes: existing `MailSettings` class (`ApiKey`, `Domain`, `FromAddress`, `FromName` — see `ApiSettings.cs`), existing `EnvLoader.LoadToConfiguration()` mapping dictionary pattern.
- Produces: `MailSettings.BaseUrl` (string, defaults to `""`) — no other task depends on this.

- [ ] **Step 1: Add the env var mapping**

In `backend/SportsReservationAPI/Configuration/EnvLoader.cs`, find:

```csharp
                { "MAILGUN_API_KEY", "ApiKeys:Mail:ApiKey" },
                { "MAILGUN_DOMAIN", "ApiKeys:Mail:Domain" },
                { "MAIL_FROM_ADDRESS", "ApiKeys:Mail:FromAddress" },
                { "MAIL_FROM_NAME", "ApiKeys:Mail:FromName" },
```

Replace with:

```csharp
                { "MAILGUN_API_KEY", "ApiKeys:Mail:ApiKey" },
                { "MAILGUN_DOMAIN", "ApiKeys:Mail:Domain" },
                { "MAILGUN_BASE_URL", "ApiKeys:Mail:BaseUrl" },
                { "MAIL_FROM_ADDRESS", "ApiKeys:Mail:FromAddress" },
                { "MAIL_FROM_NAME", "ApiKeys:Mail:FromName" },
```

- [ ] **Step 2: Add `BaseUrl` to `MailSettings`**

In `backend/SportsReservationAPI/Models/ApiSettings.cs`, find:

```csharp
    public class MailSettings
    {
        public string ApiKey { get; set; } = "";
        public string Domain { get; set; } = "";
        public string FromAddress { get; set; } = "";
        public string FromName { get; set; } = "";
    }
```

Replace with:

```csharp
    public class MailSettings
    {
        public string ApiKey { get; set; } = "";
        public string Domain { get; set; } = "";
        public string BaseUrl { get; set; } = "";
        public string FromAddress { get; set; } = "";
        public string FromName { get; set; } = "";
    }
```

- [ ] **Step 3: Use the configured base URL in `MailService`**

In `backend/SportsReservationAPI/Services/MailService.cs`, find:

```csharp
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.mailgun.net/v3/{_mailSettings.Domain}/messages");
```

Replace with:

```csharp
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_mailSettings.BaseUrl}/v3/{_mailSettings.Domain}/messages");
```

- [ ] **Step 4: Document the new key in `.env.sample`**

In `.env.sample`, find:

```
# MAILGUN
MAILGUN_API_KEY=key-xxx
MAILGUN_DOMAIN=mg.example.com
MAIL_FROM_ADDRESS=noreply@example.com
MAIL_FROM_NAME=Sport Challenge Police 54
```

Replace with:

```
# MAILGUN
MAILGUN_API_KEY=key-xxx
MAILGUN_DOMAIN=mg.example.com
MAILGUN_BASE_URL=https://api.mailgun.net
MAIL_FROM_ADDRESS=noreply@example.com
MAIL_FROM_NAME=Sport Challenge Police 54
```

- [ ] **Step 5: Pass the new env var to the backend container**

In `docker-compose.yml`, find:

```yaml
      MAILGUN_API_KEY: ${MAILGUN_API_KEY}
      MAILGUN_DOMAIN: ${MAILGUN_DOMAIN}
      MAIL_FROM_ADDRESS: ${MAIL_FROM_ADDRESS}
      MAIL_FROM_NAME: ${MAIL_FROM_NAME}
```

Replace with:

```yaml
      MAILGUN_API_KEY: ${MAILGUN_API_KEY}
      MAILGUN_DOMAIN: ${MAILGUN_DOMAIN}
      MAILGUN_BASE_URL: ${MAILGUN_BASE_URL}
      MAIL_FROM_ADDRESS: ${MAIL_FROM_ADDRESS}
      MAIL_FROM_NAME: ${MAIL_FROM_NAME}
```

- [ ] **Step 6: Add real values to your local `.env` (do not commit)**

Open your local `.env` (already gitignored) and set:
```
MAILGUN_API_KEY=<your Mailgun sandbox API key>
MAILGUN_DOMAIN=<your Mailgun sandbox domain>
MAILGUN_BASE_URL=https://api.mailgun.net
```
You already have these three values from your Mailgun dashboard — paste them directly into `.env`, never into any other file.

- [ ] **Step 7: Rebuild and verify Mailgun is no longer treated as "unconfigured"**

Run:
```bash
docker compose up --build -d
```

In your Mailgun dashboard, add a real email address you control to the sandbox domain's **Authorized Recipients** list (sandbox domains refuse to send anywhere else). Then register a test team using that address as participant 1's email:

```bash
curl -s -X POST "http://localhost:7163/api/teams/create-team" -H "Content-Type: application/json" -d '{
  "teamDto": {"teamName": "MailgunVerifTeam", "version": "short", "administration": "none"},
  "playerDtos": [
    {"firstName": "Test", "lastName": "One", "email": "<your authorized recipient email>", "phoneNumber": "+33612345678", "category": "woman", "outfit": "no", "volunteer": false, "acceptMails": true},
    {"firstName": "Test", "lastName": "Two", "email": "test2@example.com", "phoneNumber": "+33612345679", "category": "man", "outfit": "no", "volunteer": false, "acceptMails": true}
  ]
}'
```

Check the logs:
```bash
docker compose logs backend --tail=20
```
Expected: **no** `Mailgun is not configured` warning, and no `Mailgun request failed` warning. You should receive the activation email at the authorized address within a minute or two.

Clean up the test data afterward (adjust the team name/id if it differs from what the API returned):
```bash
MSYS_NO_PATHCONV=1 docker exec sports-reservation-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '<your DB_PASSWORD from .env>' -d SportsReservationDB -C -Q "SET QUOTED_IDENTIFIER ON; DELETE FROM Users WHERE Username='<your authorized recipient email>'; DELETE FROM Players WHERE Email='<your authorized recipient email>' OR Email='test2@example.com'; DELETE FROM Teams WHERE Name='MailgunVerifTeam';"
```

- [ ] **Step 8: Commit the code and config changes**

Only the tracked files — never `.env`:
```bash
git add backend/SportsReservationAPI/Configuration/EnvLoader.cs backend/SportsReservationAPI/Models/ApiSettings.cs backend/SportsReservationAPI/Services/MailService.cs .env.sample docker-compose.yml
git commit -m "feat: make Mailgun API base URL configurable via MAILGUN_BASE_URL"
```
