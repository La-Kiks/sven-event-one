# Postponement readiness: configurable event date, announcement email, prod hardening

## Context

The event (Hyrox Police 54) was planned for September 2026 and is being **postponed** to a date not yet known. Existing registrations are kept as-is: same teams, same 52-team cap, same Yurplan payment. Before the new date is announced, three things need to be in place:

1. The site shows the date in four hardcoded places ("Septembre 2026"). It must become easy to change without a code edit, because the date isn't known yet.
2. Every registered player must be told about the postponement by email. The admin needs a way to send that from the app, through the existing Mailgun integration.
3. A production review found two hardening gaps: the API and UI ports are published on every network interface, and `/api/auth/login` has no brute-force protection.

The email goes to real participants and can't be recalled, so the email part prioritizes a test send before the real one, an explicit confirmation showing the exact recipient count, and accurate per-recipient success/failure reporting. It uses the same patterns as the bulk activation email (see `2026-07-12-bulk-activation-email-design.md`).

## 1. Configurable event date (`EVENT_DATE_LABEL`)

A single free-text label, e.g. `Samedi 14 mars 2027`, set in `.env` and injected at **container runtime** through the existing `env.js` mechanism (same path as `API_BASE_URL`). Frontend only: the backend doesn't need it, since the announcement text is typed by the admin.

- `.env.sample`: add `EVENT_DATE_LABEL=` under a new `# EVENT` heading.
- `docker-compose.yml`: pass `EVENT_DATE_LABEL: ${EVENT_DATE_LABEL}` to the `frontend` service.
- `ui/docker-entrypoint.d/40-generate-runtime-env.sh`: add `${EVENT_DATE_LABEL}` to the `envsubst` variable list.
- `ui/public/env.template.js`: `window.__env.eventDateLabel = "${EVENT_DATE_LABEL}";`
- `ui/src/environment.ts`: fallback `eventDateLabel: 'Date à confirmer'`.
- `ui/src/app/core/runtime-env.ts`: add `eventDateLabel` to the `Window.__env` type and the exported `environment`, using the existing `pick()` (an empty or unset value falls back to `Date à confirmer`).

The four places that use it (each component exposes `eventDateLabel = environment.eventDateLabel`):

| File | Today | After |
|---|---|---|
| `landing.component.html:22` | `Septembre 2026 · Laxou (54)` | `{{ eventDateLabel }} · Laxou (54)` |
| `landing.component.html:149` | `Places limitées · 60 € par duo · Septembre 2026` | `… · {{ eventDateLabel }}` |
| `my-team.component.html:59` | `Septembre 2026` | `{{ eventDateLabel }}` |
| `not-found.component.html:6` | `… Rendez-vous en Septembre pour Hyrox Police 54 !` | `… Rendez-vous à Hyrox Police 54 : {{ eventDateLabel }} !` |

The 404 sentence is reworded so it reads correctly whatever the label's format is ("le samedi…" vs "en mars…").

To change the date later: edit `EVENT_DATE_LABEL` in the prod `.env`, then `docker compose up -d frontend`. No rebuild needed.

## 2. Announcement email to all registered players

### 2a. Backend: `AnnouncementsController` + `AnnouncementService`

New controller `api/announcements`, `[Authorize(Roles = "Admin")]` on the class. It's thin, and the logic lives in a new scoped `AnnouncementService`.

**Recipients.** All rows in `Players`, deduplicated by email **case-insensitively** (the same person could be participant 2 in two teams). When an email appears more than once, the first name comes from the player with the lowest `Id`. Emails are trimmed before comparison. Each recipient gets **their own message** (one Mailgun call per address, single `to`), so no participant ever sees another participant's address.

**Endpoints**

- `GET /api/announcements/recipients` returns `{ count }`, the number of unique addresses that "send to all" would email. The confirmation dialog uses it, so the count shown is exactly the count the backend will use, dedup included.
- `POST /api/announcements/test` takes `{ subject, message, toEmail }`. It sends one email to `toEmail` with the greeting `Bonjour,` (no first name). Returns `{ sent: true }`, or `502` with `{ error }` if Mailgun refused or isn't configured, so a broken Mailgun setup is caught before the real send.
- `POST /api/announcements/send` takes `{ subject, message }` and sends to every recipient. It returns `{ total, sent, failed, failures: [{ email, error }] }`.

**Validation** (FluentValidation, `AnnouncementDto` / `TestAnnouncementDto` in a new `Models/Announcement/` folder, following the per-entity folder convention): `subject` is required, max 200 chars. `message` is required, max 10,000 chars. `toEmail` is required and must be a valid email. Invalid input returns `400` with `ModelState`, the same as other endpoints.

**Sending.** Same two-phase shape as `CreateAccountsForPendingTeamsAsync`: one DB query builds the recipient list in memory, then emails go out in parallel batches of 5 with `Task.WhenAll`. At ~100 recipients and ~1.5 s per Mailgun call, that takes about 30 s, which fits within one request.

**Email body.** A new public method on `MailService`:

```csharp
public Task<bool> SendAnnouncementEmailAsync(string toEmail, string? toName, string subject, string message)
```

It reuses the existing private `SendEmailAsync` (never throws, returns `false` on any failure or missing config).
- Text part: `Bonjour {toName},` (or `Bonjour,` when `toName` is null), a blank line, `message` as typed, then the footer.
- HTML part: the greeting and the message are **HTML-encoded** first. Then blank lines split paragraphs (`<p>`), single newlines become `<br>`, and `http(s)://` URLs are turned into links (after encoding, so this can't inject markup). Same footer.
- Footer (both parts): `Vous recevez cet email car vous êtes inscrit(e) à Hyrox Police 54.`, then the signature `Sport Challenge Police 54`, as in the existing emails.
- `subject` is used as-is.

**Logging.** Each failure logs a warning (the existing `SendEmailAsync` already does this). The send endpoint also logs one info line with total / sent / failed.

### 2b. Frontend: admin "Annonce" page

New route `annonce` → `pages/announcement/announcement.component.ts`, `canActivate: [AuthGuard]`, `data: { role: 'Admin' }`. A new "Annonce" `nav-btn` is added to the admin top bar on `/teams` and `/players` (and on the new page itself, where it's `active`), next to "Équipes" / "Joueurs". It uses the existing admin brand tokens and top-bar styles.

The page, top to bottom:
1. **Objet** (text input, max 200) and **Message** (textarea, about 12 rows, max 10,000). A short hint under the message: "Le message commence automatiquement par « Bonjour {prénom}, ». Laissez une ligne vide entre deux paragraphes."
2. **Envoyer un test**: an email input plus a button. It calls `POST /test` and shows inline success ("Test envoyé à x@y.fr") or the error.
3. **Envoyer à tous les participants**: a primary button. Before opening the dialog, it calls `GET /recipients`. The confirmation dialog (same `delete-confirm` pattern as the bulk activation one) says: *"{count} participants vont recevoir cet email. Cet envoi est définitif. Continuer ?"*. The send button is disabled while the request is running, and its label says it's sending and can take up to a minute.
4. **Result**: a summary ("98 envoyés, 2 échecs") plus the list of failed addresses with their error. After a send the result stays on screen and the form fields stay filled. The "send to all" button stays usable, because the confirmation dialog is the safeguard against double sends, the same as for the bulk activation email.

Subject and message are required on the client side too (buttons disabled while empty).

## 3. Production hardening

### 3a. Bind published ports to localhost

`docker-compose.yml`:

```yaml
frontend:  ports: ["127.0.0.1:${UI_PORT}:80"]
backend:   ports: ["127.0.0.1:${API_PORT}:8080"]
```

This makes the comment in `Program.cs` true: the API is reachable only through the reverse proxy, so `X-Forwarded-For` can't be forged to bypass the per-IP password-reset limit. **Assumption:** Caddy runs directly on the host, since it isn't part of this compose file. If it actually runs in a container, this change would cut it off, so check before deploying (see Verification). Local dev is unaffected, because the browser reaches `localhost:7193` / `localhost:7163` either way.

### 3b. Login brute-force protection

New in-memory singleton `Services/LoginRateLimiter.cs`, modeled on `PasswordResetRateLimiter`. It counts **failed** login attempts **per username** (case-insensitive) over a sliding 15-minute window. Once a username has 10 failures in the window, `/api/auth/login` returns `429` with `{ message: "Trop de tentatives de connexion. Réessayez dans 15 minutes." }` **without checking the password**, even if the password is correct. A successful login clears that username's counter.

Why per username and not per IP: the test suite logs in many times from the same address, so a per-IP limiter would make tests interfere with each other. Per username also protects the one account worth attacking (admin) no matter how many IPs the attacker uses. The trade-off is that someone who knows a username can lock it out for 15 minutes, which is acceptable at this scale.

Frontend: `login.component.ts` shows the `429` message in the existing error banner instead of the generic "identifiants incorrects".

## 4. Tests (written first, per CLAUDE.md)

New `AnnouncementTests.cs` in the shared `[Collection("Api")]`. The test container has no Mailgun config, so every send results in `failed` / `502`. The tests check selection, dedup, auth and validation, not delivery:

- `Recipients_CountsUniqueEmailsCaseInsensitively`: register a team whose participant 2 has the same email as another team's participant 2, in different case. Check that `count` went up by the number of *unique* addresses added (compared with a count taken before; the shared DB isn't empty).
- `Send_ReturnsOneResultPerUniqueEmail`: after registering teams with known emails, `POST /send`. `total == sent + failed`, and (since Mailgun is unconfigured, every send fails) each known email appears **exactly once** in `failures`, including the duplicated one.
- `Test_WithMailgunUnconfigured_Returns502`
- `Send_WithEmptySubjectOrMessage_Returns400`, and `Test_WithInvalidEmail_Returns400`
- `Recipients_Send_Test_WithParticipantToken_ReturnForbidden` and `…_WithoutToken_ReturnUnauthorized`

New `LoginRateLimiterTests.cs` (unit) plus integration tests in `AuthTests.cs`:
- After 10 wrong passwords on a unique username, the 11th attempt **with the correct password** returns `429`.
- A successful login after fewer than 10 failures resets the counter (9 failures, success, then 9 more failures, and the next correct login still succeeds).
- Usernames are compared case-insensitively.

`MailService` HTML building is extracted to a `public static string BuildAnnouncementHtml(string? toName, string message)` so it can be unit-tested directly: `<script>` in the message comes out encoded, and a URL becomes an `<a>`.

Then run the full suite: `docker compose run --rm --build tests`.

## Out of scope

- Storing a history of sent announcements, scheduling, or targeting subsets (one team, unpaid teams only). YAGNI for a one-off postponement notice.
- Rich-text editing or attachments.
- Unsubscribe links: this is a service message about the participant's own registration, not marketing.
- Background job processing for sends (~100 recipients fit in one request).
- Changes to Stripe, the team cap, or registration flow.

## Things to check outside the code

- **Mailgun plan quota:** the free/trial tier has a daily cap of about 100 emails, and a sandbox domain only delivers to authorized recipients. Check the prod domain and plan against the recipient count shown by `GET /recipients` before sending.
- **Yurplan:** update the event date on Yurplan too.
- **Prod `.env`:** keep a placeholder `STRIPE_WEBHOOK_SECRET` (startup validation requires it), and add `EVENT_DATE_LABEL`.

## Verification

1. `docker compose run --rm --build tests`: the new tests fail first for the expected reason, then pass. The full suite stays green.
2. `docker compose up --build` locally with `EVENT_DATE_LABEL` unset: the four places show "Date à confirmer". Set it, run `docker compose up -d frontend`, reload: the new label shows everywhere, with no rebuild.
3. As admin, open `/annonce`, send a test to your own address with a real Mailgun key, and check greeting, paragraphs, link and footer in Gmail (HTML) and in a text-only view. Check the confirmation count matches `/recipients`. In dev, run the full send against seeded `@example.com` players only.
4. Login: 10 wrong passwords on a test account, then the correct one returns the "Trop de tentatives" banner.
5. On the prod server, before deploying 3a, run `docker ps` / check how Caddy runs. If it's on the host, after deploy `curl http://<public-ip>:<API_PORT>` from outside must fail, while the site keeps working through the domain.

## Appendix: draft postponement email (French, to paste into the composer)

**Objet :** Hyrox Police 54 : nouvelle date de l'événement

**Message :**

```
L'édition Hyrox Police 54 initialement prévue en septembre 2026 est reportée. Elle aura lieu le [NOUVELLE DATE], toujours au Well and Fit, 113 Bd Émile Zola à Laxou.

Bonne nouvelle : votre inscription est conservée automatiquement. Vous n'avez rien à faire, votre équipe et votre paiement restent valables pour la nouvelle date.

Vous pouvez retrouver et modifier les informations de votre équipe à tout moment sur votre espace : [LIEN DU SITE]/mon-equipe

Si vous ne pouvez pas être présent(e) à cette nouvelle date, écrivez-nous à svenbarberat@orange.fr et nous verrons ensemble la meilleure solution.

Merci pour votre compréhension, et à très vite sur la ligne de départ !
```

Before sending, replace `[NOUVELLE DATE]` and `[LIEN DU SITE]`, and confirm the cancellation wording with the organizer (refund policy is the organizer's call).
