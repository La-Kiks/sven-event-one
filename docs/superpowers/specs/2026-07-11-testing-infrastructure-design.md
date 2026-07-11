# Manual testing workflow + automated API test suite

## Context

This repo has no test project and no documented manual-testing workflow — every verification done so far (including catching 3 real SQL-Server-specific bugs and 2 functional bugs during this session's participant-account feature) happened ad hoc via `curl` and direct SQL queries run by whoever was driving the session. The user wants two related but distinct things: (1) a repeatable way to poke at the running app themselves — in the browser, with real sample data — to build confidence it works, and (2) an automated test suite they can run after every change to catch regressions without needing to remember or re-run all that manual `curl`/SQL verification by hand.

Both must work within the project's established constraint: **everything runs via Docker Compose, never bare `dotnet`/`npm`/`ng` commands** (see `CLAUDE.md`).

## 1. Manual testing guide

New doc: `docs/manual-testing-guide.md`, linked from `README.md`'s "Development notes" section.

Walks through the full participant lifecycle in the browser against the running `docker compose up --build` stack:
1. Register a team at `/inscription`.
2. Retrieve the activation token — either from the real inbox (if Mailgun is configured with an authorized sandbox recipient) or via a documented SQL query against the `Users` table (the same `sqlcmd` command used throughout this session) if not.
3. Activate at `/activer-compte?token=...`, confirm redirect to `/mon-equipe`.
4. Edit the team, confirm changes persist and `IsPaid` stays untouched.
5. Log in as admin (seeded automatically in dev — see `CLAUDE.md`), confirm the team appears in `/teams` with the correct payment/account-status badges, and exercise "create/resend account" and the payment toggle.
6. Log out, confirm role-gated redirects (participant hitting `/teams`, admin hitting `/mon-equipe`).

This is documentation only — no code changes — but it's written as a numbered checklist precise enough to run without this conversation's context.

## 2. Dev-only sample data seeder

New endpoint: `POST /api/dev/seed-sample-data`, gated identically to the existing admin dev-seed in `Program.cs` (`ApiSettings.Environment == "Development"` — inert in production).

Rather than raw SQL inserts, it calls `TeamService.CreateTeamWithPlayersAsync` in a loop (5 teams, varied categories/versions/administrations, distinct emails) — the exact same code path real registration uses, so it also exercises account creation and the best-effort activation email. New controller: `backend/SportsReservationAPI/Controllers/DevController.cs` (`[Route("api/dev")]`), one action, no auth requirement beyond the environment gate (mirrors the public `create-team` endpoint's exposure level — this only ever runs in dev). Returns the list of created team ids/names so the caller knows what to look at.

## 3. Automated integration test suite

**Approach:** integration tests against a real, ephemeral SQL Server container — not EF Core's InMemory provider and not SQLite. Both alternatives were considered and rejected: InMemory doesn't enforce real constraints and would have missed all three SQL-Server-specific bugs found manually this session (the `nvarchar(max)` index restriction, `QUOTED_IDENTIFIER` requirements, filtered unique index behavior); SQLite enforces more than InMemory but still diverges from SQL Server on exactly those points, trading one class of "works in test, breaks in prod" bug for another. Given this app's logic is thin and DB-coupled (`TeamService`/`UserService` are mostly EF Core orchestration), testing against the real engine is what actually validates "the API works."

**Project:** `backend/SportsReservationAPI.Tests`, xUnit (the de facto standard for ASP.NET Core, with first-class `WebApplicationFactory` support). Referenced from `SportsReservationAPI.sln` alongside the existing API project.

**Harness:** a `CustomWebApplicationFactory : WebApplicationFactory<Program>` that overrides the connection string to point at a dedicated test database (separate from the dev database used for manual testing/the sample-data seeder, so automated runs never pollute or depend on manually-created data). `Program.cs` currently uses top-level statements, which generate an `internal` `Program` class by default — `WebApplicationFactory<Program>` from a separate test assembly needs it `public`, so `Program.cs` gains one line at the end: `public partial class Program { }`.

**Isolation:** each test class implements `IClassFixture<CustomWebApplicationFactory>` and wraps its test methods in a database transaction that's rolled back on disposal (the standard EF Core integration-test pattern) — tests never depend on execution order and never leak state into each other, without needing a full DB reset between every single test.

**Infrastructure:** a new `test-database` service in `docker-compose.yml` — same `mcr.microsoft.com/mssql/server:2022-latest` image as the existing `database` service, but **no persisted volume** (ephemeral, wiped on every `docker compose down` or container recreation) and a distinct port/container name so it can run alongside the dev database without conflict. A new `tests` service (build context: `backend/SportsReservationAPI.Tests`, using the SDK image since tests need to compile) depends on `test-database`, runs `dotnet test` as its container command, and applies migrations against the test database on startup the same way `Program.cs` already does for the real app (so the test suite is also implicitly a regression check that the migration chain applies cleanly).

**Running the suite:** `docker compose run --rm tests` — builds the test project, waits for `test-database` to be healthy, runs all tests, exits with the test run's exit code (non-zero on failure, so this composes with CI later if wanted, though CI setup itself is out of scope here).

**Initial coverage** (the endpoints and rules exercised so far this session, prioritized by what's new/fragile):
- `POST /api/auth/login` — success, wrong password, pending account with empty `PasswordHash` (must 401, not 500).
- `POST /api/auth/activate` — valid token, expired token, already-used token, password under 8 characters rejected.
- `POST /api/teams/create-team` — success, exactly-2-players validation, duplicate participant-1 email rejected, `MaxTeams` cap enforced.
- `GET /api/teams/count` — reflects current count and `isFull`.
- `GET/PUT /api/teams/my-team` — own team returned, 404 when the account has no team, `IsPaid` never changes via `PUT`, mismatched/duplicate player ids rejected (direct regression test for the bug found in this session's code review), participant-1 email edit re-syncs `User.Username` including the "email already taken by another account" conflict case.
- Admin endpoints (`GET/DELETE /api/Teams/{id}`, `GET /api/Teams/teams`, `PATCH /api/Teams/{id}/payment`, `POST /api/Teams/{id}/create-account`, `GET /api/Players`) — success paths, plus a 403 check with a participant-role token and a 401 check with no token, for each.

**Explicitly out of scope for this pass:** Stripe endpoints (`create-checkout-session`, webhook — signature verification and external API calls need a different mocking strategy) and asserting real Mailgun delivery (already best-effort and covered by manual testing per section 1). Both can be added later as their own follow-up if needed.

## Verification

1. `docker compose run --rm tests` exits 0 with all listed cases passing, against a freshly-created `test-database` (confirms migrations apply cleanly from scratch, not just incrementally against a database that already has the old schema).
2. Deliberately reintroduce the player-id regression bug (revert the `UpdateMyTeamAsync` validation) locally and confirm the corresponding test fails — proves the test actually exercises the code path, not just that it compiles.
3. Run `docker compose run --rm tests` a second time immediately after the first, confirm it still passes (proves per-test transaction rollback actually isolates state and the suite is safe to re-run without manual cleanup).
4. Follow `docs/manual-testing-guide.md` end to end against `docker compose up --build` (the regular dev stack, not the test one) and confirm every step's expected outcome.
5. Call `POST /api/dev/seed-sample-data` against the dev stack, confirm 5 teams appear in `/teams` with distinct data, and confirm the same endpoint returns 404 (or is otherwise unreachable) when `ENVIRONMENT` isn't `Development`.
