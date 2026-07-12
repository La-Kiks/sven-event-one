# Bulk activation email sending for admin

## Context

The admin can already trigger an activation email for one team at a time (`POST /api/Teams/{teamId}/create-account`, exposed as a button in each team's detail panel on `/teams`). In production there are ~50 pre-existing teams that need this done once, and clicking through 50 individual team panels is impractical. This adds a bulk "send to all pending teams" action while keeping the existing per-team button for catching stragglers afterward (e.g. a bounced email, or a team added after the bulk run).

This is real production traffic — the ~50 recipients are actual event participants, and an email send can't be recalled once it goes out. The design prioritizes: an explicit confirmation step showing exactly how many people will be emailed, accurate per-team success/failure reporting (not a silent best-effort swallow), and staying within a request/response cycle an admin can comfortably wait on (no background job infrastructure exists in this codebase, and 50 teams doesn't warrant introducing one).

## 1. Backend — `POST /api/teams/create-account-bulk`

New endpoint on `TeamsController`, `[Authorize(Roles = "Admin")]`, delegating to a new `UserService.CreateAccountsForPendingTeamsAsync()`.

**Query:** `_context.Teams.Include(t => t.Players).Include(t => t.Account).Where(t => t.Account == null || !t.Account.EmailVerified)` — the same "needs an account" condition the individual endpoint already uses, just applied to every team instead of one.

**Phase 1 — prepare accounts (sequential, DB-bound):** For each matching team, reuse the exact account create-or-refresh logic already in `CreateOrRefreshAccountForTeamAsync` (build a pending account if none exists, or regenerate the token if one exists but isn't verified). This phase does **not** call Mailgun yet — it only stages `(TeamId, TeamName, Email, ParticipantFirstName, ActivationUrl)` tuples in memory. A team with zero players (data integrity edge case, not reachable via the current registration flow but defensively handled the same way the individual endpoint does) is recorded as a `"failed"` result immediately and excluded from the send phase. One `SaveChangesAsync()` commits all prepared accounts together.

**Phase 2 — send emails (parallel, batches of 5):** `EF Core`'s `DbContext` isn't safe for concurrent use, which is why phase 1 (DB writes) and phase 2 (HTTP calls to Mailgun) are strictly separated — only phase 2 is parallelized, using `Task.WhenAll` over slices of 5 at a time (observed ~1.5s per Mailgun call means ~15-20s total for 50 teams, vs. ~80s sequential — comfortably within a normal HTTP request timeout).

**Response shape:** a list of `{ teamId, teamName, status: "sent" | "failed", error? }`, one entry per team that was in scope (teams already fully verified are never included — they were excluded by the query, not attempted-then-skipped).

### Required change: `MailService.SendActivationEmailAsync` must report success/failure

Today this method swallows all exceptions internally and logs a warning — correct for the best-effort registration-time call (a Mailgun hiccup must never fail a team's registration), but it means the caller currently has no way to know whether an email actually went out. For accurate bulk reporting, the method's signature changes from `Task` to `Task<bool>` (true = Mailgun accepted it, false = it failed for any reason — still never throws). The two existing call sites (`TeamService.CreateTeamWithPlayersAsync`'s best-effort try/catch, and the individual `CreateOrRefreshAccountForTeamAsync`) simply ignore the returned value, unchanged in behavior. Only the new bulk path uses it, to set each team's `status` in the response.

## 2. Frontend — bulk button + confirmation

New button on `/teams`, placed near the page title/count (not inside a team's detail panel, since the action spans the whole list): **"Envoyer les emails d'activation aux équipes en attente"**.

Clicking it opens a confirmation dialog showing the exact count of affected teams, computed client-side from the already-loaded team list (`teams.filter(t => !t.accountVerified).length`) — no extra network round-trip needed for the preview. Copy: *"47 équipes vont recevoir un email d'activation. Continuer ?"*. On confirm, call the new endpoint; show a loading state for the duration (up to ~20s); on completion, show a summary (*"45 envoyés, 2 échecs : Équipe X (raison), Équipe Y (raison)"*) and refresh the team list so `hasAccount`/`accountVerified` badges reflect the new state.

The existing per-team button is untouched — it remains the way to retry an individual failure after a bulk run, or handle a team added later.

## 3. Tests

New integration test(s) in `backend/SportsReservationAPI.Tests/AdminTeamsTests.cs`:
- `CreateAccountBulk_SendsToAllPendingTeamsAndSkipsVerified` — register several teams (mix of freshly-registered-pending and one activated via the existing `RegisterAndActivateTeamAsync` helper), call the bulk endpoint as admin, assert the response contains an entry with `status: "sent"` for each pending team and does **not** contain an entry for the already-activated one, then assert those teams' accounts are now marked as having a valid (non-null) verification token in the DB.
- `CreateAccountBulk_WithParticipantToken_ReturnsForbidden` — same role-check pattern already used for every other admin endpoint in this file.

## Out of scope

- Background/async job processing (not needed at this volume; revisit only if team counts grow far beyond ~50-100).
- Rate-limiting or cooldown protection against accidentally triggering the bulk send twice in a row — the confirmation dialog is the agreed-upon safeguard; a double-send would just re-email the same still-pending teams (identical risk profile to today's individual "resend" button, just at bulk scale).
- Changing the individual per-team endpoint's behavior or UI.

## Verification

1. Rebuild via `docker compose up --build`, register a handful of test teams without activating them, click the new bulk button in `/teams`, confirm the count shown matches, confirm all listed teams receive a real Mailgun-accepted send (watch backend logs for `200` responses per team, no `Mailgun request failed` warnings).
2. Activate one of the test teams first, then run the bulk send again — confirm that team is excluded from the count and from the results.
3. `docker compose run --rm tests` — new bulk tests pass, and the full suite (including the two existing individual-`create-account` tests) still passes after the `MailService.SendActivationEmailAsync` signature change.
