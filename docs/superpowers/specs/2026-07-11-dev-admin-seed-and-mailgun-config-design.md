# Dev-only admin seeding + Mailgun configuration

## Context

The participant-account feature (email verification, activation, `/mon-equipe`) shipped with a real Mailgun integration (`MailService`), but no Mailgun credentials were ever entered — the mailer has been running in no-op mode, logging a warning instead of sending. Separately, while verifying that feature end-to-end, we discovered the `Users` table in local/dev environments is empty: the only way to create an admin account was a `/api/auth/seed` endpoint in `AuthController.cs` that has been commented out since before this session, with a TODO to gate it to development. In production the admin account already exists (seeded once, out of band), so this only affects local/dev usage — but it currently blocks testing `/teams`, `/players`, and the new admin "create/resend account" action locally.

This change: (1) makes local dev self-sufficient by auto-creating the admin account on backend startup when running in development, and (2) wires up real Mailgun credentials so activation emails actually send.

## 1. Dev-only admin seeding

**Where:** `Program.cs`, immediately after the existing `dbContext.Database.Migrate()` block — same place migrations already apply automatically, so this follows an established pattern in this codebase rather than introducing a new one.

**Guard:** Runs only when `apiSettings.Environment == "Development"` (the `ApiKeys:Environment` config value populated from the `ENVIRONMENT` env var via `EnvLoader.cs` — **not** `app.Environment.IsDevelopment()` / `ASPNETCORE_ENVIRONMENT`, which isn't set in `docker-compose.yml` and would always evaluate to the ASP.NET Core default hosting environment regardless of the `.env` file).

**Behavior:**
1. Read `ADMIN_USERNAME` / `ADMIN_PASSWORD` directly from `IConfiguration` — these are already available as raw environment variables via ASP.NET Core's built-in environment-variable configuration provider (`WebApplication.CreateBuilder` includes it by default), so no new `EnvLoader.cs` mapping is needed. This matches how the old commented-out seed endpoint read them.
2. If either is null/empty, log a warning (`"ADMIN_USERNAME/ADMIN_PASSWORD not set — skipping dev admin seed"`) and skip. Never throw — this must not block backend startup, consistent with how missing Mailgun config is handled (best-effort, not a hard failure).
3. Query `Users` for an existing row with that `Username`. If found, skip silently (idempotent — safe to run on every container restart, won't reset an admin's password if they've since changed it... though nothing in this app currently lets an admin change their own password, so this is a future-proofing note, not a current behavior).
4. Otherwise, create `new User { Username = adminUsername, PasswordHash = BCrypt.HashPassword(adminPassword), Role = "Admin" }` and save via `ReservationContext`.

**Cleanup:** Delete the commented-out `/api/auth/seed` endpoint (and its now-unused `using` statements/imports if any become orphaned) from `AuthController.cs` — it's directly superseded by this mechanism, and leaving dead commented code around after replacing its purpose just adds noise.

**Out of scope:** No admin self-service password change, no re-seed-on-password-env-change behavior, no production seeding path (production already has its admin, seeded out of band as before — this whole block is inert there since `ENVIRONMENT` won't be `"Development"`).

## 2. Mailgun configuration

**New setting:** `MAILGUN_BASE_URL`, added alongside the three already-wired Mailgun vars (`MAILGUN_API_KEY`, `MAILGUN_DOMAIN`, `MAIL_FROM_ADDRESS`/`MAIL_FROM_NAME` — the latter two already existed as placeholders from the original feature).

**Plumbing (mirrors the existing Stripe/Mail pattern exactly):**
- `Configuration/EnvLoader.cs`: add `{ "MAILGUN_BASE_URL", "ApiKeys:Mail:BaseUrl" }` to the mapping dictionary.
- `Models/ApiSettings.cs`: add `public string BaseUrl { get; set; } = "";` to `MailSettings`.
- `Services/MailService.cs`: replace the hardcoded `"https://api.mailgun.net/v3/{domain}/messages"` request URL with `$"{_mailSettings.BaseUrl}/v3/{_mailSettings.Domain}/messages"`. The existing no-op-if-unconfigured guard (`ApiKey`/`Domain` blank check) stays as-is; `BaseUrl` being blank isn't separately guarded since it's always set alongside the other two in practice, and an empty base URL would just produce an obviously-broken URL that fails the HTTP call (caught by the existing try/catch + warning log, not a crash).

**Files to update with the new key:** `.env.sample` (placeholder), `.env` (real value — **never committed**, `.env` is gitignored), `docker-compose.yml` (backend service `environment:` block, alongside the other three Mailgun vars already added).

The user has a Mailgun sandbox API key, domain, and base URL ready to paste directly into their local `.env` during implementation — not reproduced here, since this spec file is committed to git and `.env` values must never appear in a tracked file.

**Known limitation (not a bug to fix):** Mailgun sandbox domains only deliver to recipient addresses pre-authorized in the Mailgun dashboard's "Authorized Recipients" list. A test registration using an unauthorized email will fail Mailgun's API call; `MailService`'s existing try/catch logs this as a warning without failing team registration, so the symptom is "no email arrives, warning in backend logs" rather than a visible error to the end user. This is expected sandbox behavior, not something to work around in code.

## Verification

1. `docker compose up --build` with the admin seed + new Mailgun vars in `.env`.
2. Check backend startup logs: either the admin-seed log line (created) or confirmation it already existed — no seeding attempt at all if `ENVIRONMENT` isn't `Development`.
3. `POST /api/auth/login` with `ADMIN_USERNAME`/`ADMIN_PASSWORD` from `.env` → expect a valid JWT with `Role: "Admin"`.
4. Log into `/teams` in the browser using those credentials, confirm the admin panel loads (previously impossible — no admin existed).
5. Register a test team with participant 1's email set to an address added to the Mailgun sandbox's authorized recipients list → confirm the activation email actually arrives (real send, not the no-op warning path).
6. Restart the stack (`docker compose up --build` again) and confirm the admin seed step is a no-op the second time (idempotency) — no duplicate-user error, login still works with the same credentials.
