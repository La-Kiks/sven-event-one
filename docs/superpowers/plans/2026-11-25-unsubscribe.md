# Désabonnement « prochaines éditions » Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let any player (participant 2 included, no account needed) withdraw their `AcceptMails` consent via a signed link in announcement emails, and let participant 1 toggle it in `mon-equipe`.

**Architecture:** A singleton `UnsubscribeLinkService` signs the normalized email with HMAC-SHA256 (key derived from `Jwt:Key`) and builds `/desabonnement?email=…&sig=…` links. A public `POST /api/mail-preferences/unsubscribe` verifies the signature and sets `AcceptMails = false` on every player with that email. Announcement emails get the link in their footer; a new Angular page confirms via button (GET never writes). No migration.

**Tech Stack:** ASP.NET Core 8, EF Core, xUnit integration tests (Docker), Angular 19 standalone.

**Spec:** `docs/superpowers/specs/2026-11-25-unsubscribe-design.md`

## Global Constraints

- No DB migration; no new env var. Existing sends (activation, reset, `/annonce` recipients) unchanged.
- Normalized email = `email.Trim().ToLowerInvariant()` everywhere (signing, URL, DB match).
- HMAC key = `SHA256(UTF8("unsubscribe:" + config["Jwt:Key"]))`; signature = `WebEncoders.Base64UrlEncode(HMACSHA256(key, UTF8(normalizedEmail)))`; compare with `CryptographicOperations.FixedTimeEquals`.
- Link = `{FrontendBaseUrl.TrimEnd('/')}/desabonnement?email={Uri.EscapeDataString(normalizedEmail)}&sig={sig}`.
- Footer copy (exact): `Vous ne souhaitez plus recevoir d'emails sur nos prochaines éditions ?` then `Se désabonner : {url}` (text) / `<a href="{url html-encoded}">Se désabonner</a>` (HTML).
- Success copy (exact): « Vous ne recevrez plus d'emails sur nos prochaines éditions. Vous continuerez de recevoir les informations liées à votre inscription actuelle. »
- Run commands only via Docker: `docker compose run --rm --build tests [--filter "FullyQualifiedName~X"]`. No `ng`/`npm`/`dotnet`.

## Review Focus

1. Emails containing `+` or other reserved chars (`a+b@example.com`): must round-trip through the URL and Angular's query parsing → Task 1 asserts `%2B` encoding; Task 5 manual check.
2. Malformed / non-base64 / empty `sig` or missing `email`: must be 400, never 500 → Task 2 tests.
3. Stored email differing in case/whitespace from the link's email: must still unsubscribe → Task 2 test.
4. Link for an address with no player (test email, or email edited since): 200, no change, no info leak → Task 2 test.
5. Participant 1 saving `mon-equipe` after participant 2 unsubscribed: p2 must stay unsubscribed → Task 4 test.

---

### Task 1: `UnsubscribeLinkService` (sign, verify, build URL)

**Files:**
- Create: `backend/SportsReservationAPI/Services/UnsubscribeLinkService.cs`
- Modify: `backend/SportsReservationAPI/Program.cs:92` (register `AddSingleton<UnsubscribeLinkService>()`)
- Test: `backend/SportsReservationAPI.Tests/UnsubscribeLinkServiceTests.cs`

**Interfaces:**
- Produces:
  - `UnsubscribeLinkService(IConfiguration config, IOptions<ApiSettings> apiSettings)`
  - `static string Normalize(string email)`
  - `string Sign(string email)` (normalizes internally)
  - `bool Verify(string? email, string? sig)` (false on null/empty/mismatch, never throws)
  - `string BuildUrl(string email)`

- [ ] **Step 1: Write the failing tests** (plain unit tests, no `[Collection]`; build the service with `new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Jwt:Key","unit-test-key"}}).Build()` and `Options.Create(new ApiSettings { FrontendBaseUrl = "https://example.com/" })`):
  - `Verify_AcceptsOwnSignature`: `Verify("alice@example.com", Sign("alice@example.com"))` is true.
  - `Verify_IgnoresCaseAndWhitespace`: `Verify("  Alice@Example.com ", Sign("alice@example.com"))` is true.
  - `Verify_RejectsOtherEmail`: `Verify("bob@example.com", Sign("alice@example.com"))` is false.
  - `Verify_RejectsTamperedOrMalformedSig`: false for `Sign(...) + "x"`, `"not base64 !!"`, `""`, `null`; false when email is `null`.
  - `Sign_DependsOnJwtKey`: a service built with `Jwt:Key = "other-key"` produces a different signature for the same email.
  - `BuildUrl_EncodesEmailAndTrimsBaseSlash`: `BuildUrl("A+b@Example.com")` starts with `"https://example.com/desabonnement?email=a%2Bb%40example.com&sig="` and its `sig` value equals `Sign("a+b@example.com")`.

- [ ] **Step 2: Run to verify failure**

Run: `docker compose run --rm --build tests --filter "FullyQualifiedName~UnsubscribeLinkServiceTests"`
Expected: build FAIL, `UnsubscribeLinkService` not found.

- [ ] **Step 3: Implement** `UnsubscribeLinkService` per Global Constraints (derive the key once in the constructor; `Verify` compares UTF-8 bytes of the expected vs given signature strings with `FixedTimeEquals`, so malformed input simply mismatches). Register as singleton in `Program.cs`.

- [ ] **Step 4: Run to verify pass** (same command). Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/SportsReservationAPI/Services/UnsubscribeLinkService.cs backend/SportsReservationAPI/Program.cs backend/SportsReservationAPI.Tests/UnsubscribeLinkServiceTests.cs
git commit -m "feat: add signed unsubscribe link service"
```

### Task 2: Public unsubscribe endpoint

**Files:**
- Create: `backend/SportsReservationAPI/Controllers/MailPreferencesController.cs` (route `api/mail-preferences`, no `[Authorize]`)
- Create: `backend/SportsReservationAPI/Models/MailPreferences/UnsubscribeDto.cs` (`string? Email`, `string? Sig`)
- Modify: `backend/SportsReservationAPI/Services/PlayerService.cs` (add method)
- Modify: `CLAUDE.md` (Backend architecture: one bullet for `MailPreferencesController` / `UnsubscribeLinkService`: public, signed link, GET-never-writes rationale, announcements still go to all)
- Test: `backend/SportsReservationAPI.Tests/UnsubscribeTests.cs` (`[Collection("Api")]`)

**Interfaces:**
- Consumes: `UnsubscribeLinkService.Verify`, `.Sign`, `.Normalize` (Task 1).
- Produces: `POST /api/mail-preferences/unsubscribe` body `{ email, sig }` → 200 `{ unsubscribed: true }` | 400 `{ error: "Ce lien de désabonnement est invalide ou incomplet." }`; `PlayerService.UnsubscribeByEmailAsync(string normalizedEmail) : Task<int>` (rows set to false).

- [ ] **Step 1: Write the failing tests.** Get signatures via `_fixture.Factory.Services.GetRequiredService<UnsubscribeLinkService>().Sign(email)`; register teams via `create-team` with `acceptMails = true` (copy the helper from `AnnouncementTests`); read `AcceptMails` back through `_fixture.CreateDbContext()`.
  - `Unsubscribe_WithValidSig_ClearsAcceptMailsForEveryRowWithThatEmail`: same address used as p2 in two teams (second time upper-cased, as in `AnnouncementTests`) → 200; both rows `AcceptMails == false`; the other players of those teams still `true`.
  - `Unsubscribe_MatchesStoredEmailCaseInsensitively`: stored `Mixed.Case-…@example.com`, posted lowercase with matching sig → row false.
  - `Unsubscribe_IsIdempotent`: second identical POST → 200.
  - `Unsubscribe_UnknownEmail_ReturnsOk`: valid sig for `UniqueEmail("nobody")` → 200.
  - `Unsubscribe_WithBadSig_ReturnsBadRequestAndChangesNothing`: theory over sig `Sign(other email)`, `"garbage!!"`, `""`, `null`, and email `null` → 400; row still `true`.

- [ ] **Step 2: Run to verify failure**

Run: `docker compose run --rm --build tests --filter "FullyQualifiedName~UnsubscribeTests"`
Expected: FAIL with 404 responses.

- [ ] **Step 3: Implement** controller (verify → 400, else `UnsubscribeByEmailAsync(Normalize(email))` → 200) and `UnsubscribeByEmailAsync` (`Where(p => p.Email.Trim().ToLower() == normalizedEmail)`, set false, `SaveChangesAsync`, return count). Log the count at Information level without the email. Update `CLAUDE.md`.

- [ ] **Step 4: Run to verify pass** (same command), then the full suite: `docker compose run --rm --build tests`. Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/SportsReservationAPI/Controllers/MailPreferencesController.cs backend/SportsReservationAPI/Models/MailPreferences backend/SportsReservationAPI/Services/PlayerService.cs backend/SportsReservationAPI.Tests/UnsubscribeTests.cs CLAUDE.md
git commit -m "feat: add public signed unsubscribe endpoint"
```

### Task 3: Unsubscribe link in announcement emails

**Files:**
- Modify: `backend/SportsReservationAPI/Services/MailService.cs:67-106`
- Modify: `backend/SportsReservationAPI/Services/AnnouncementService.cs` (inject `UnsubscribeLinkService`)
- Test: `backend/SportsReservationAPI.Tests/AnnouncementEmailFormattingTests.cs` (update existing calls, add tests)

**Interfaces:**
- Consumes: `UnsubscribeLinkService.BuildUrl(string email)` (Task 1).
- Produces (signature changes):
  - `SendAnnouncementEmailAsync(string toEmail, string? toName, string subject, string message, string unsubscribeUrl)`
  - `static BuildAnnouncementText(string? toName, string message, string unsubscribeUrl)`
  - `static BuildAnnouncementHtml(string? toName, string message, string unsubscribeUrl)`

- [ ] **Step 1: Update the failing tests.** Pass `const string Url = "https://example.com/desabonnement?email=a%40example.com&sig=abc"` to every existing call. Add:
  - `BuildAnnouncementHtml_HasUnsubscribeLinkBeforeSignature`: contains `"Vous ne souhaitez plus recevoir d'emails sur nos prochaines éditions ?"` and `"<a href=\"https://example.com/desabonnement?email=a%40example.com&amp;sig=abc\">Se désabonner</a>"`; its index is after the `AnnouncementFooter` text and before `"Sport Challenge Police 54"`.
  - `BuildAnnouncementText_HasUnsubscribeLine`: contains `"Se désabonner : " + Url`; still `EndsWith("Sport Challenge Police 54")`.
  - `BuildAnnouncementHtml_EncodesUnsubscribeUrl`: url `"https://example.com/x?a=\"><script>"` never produces `<script>` in output.

- [ ] **Step 2: Run to verify failure**

Run: `docker compose run --rm --build tests --filter "FullyQualifiedName~AnnouncementEmailFormattingTests"`
Expected: build FAIL (arity mismatch).

- [ ] **Step 3: Implement.** Add the unsubscribe paragraph (same grey 12px style as the footer) between footer and signature in HTML; in text, `"\n\n{line1} Se désabonner : {url}"` between footer and signature. The French constant is inserted as-is (like the footer), only the URL is `WebUtility.HtmlEncode`d. In `AnnouncementService`, pass `_unsubscribeLinks.BuildUrl(email)` for both `SendTestAsync` (test address) and each recipient in `SendToAllAsync`.

- [ ] **Step 4: Run to verify pass**, then full suite `docker compose run --rm --build tests`. Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/SportsReservationAPI/Services/MailService.cs backend/SportsReservationAPI/Services/AnnouncementService.cs backend/SportsReservationAPI.Tests/AnnouncementEmailFormattingTests.cs
git commit -m "feat: add unsubscribe link to announcement emails"
```

### Task 4: `mon-equipe` — participant 1 only controls their own choice

**Files:**
- Modify: `backend/SportsReservationAPI/Services/TeamService.cs:195` (`UpdateMyTeamAsync`)
- Modify: `ui/src/app/pages/my-team/my-team.component.html:233-237` (add checkbox after p1's volunteer row)
- Test: `backend/SportsReservationAPI.Tests/MyTeamTests.cs`

**Interfaces:** none new; `PUT /api/teams/my-team` contract unchanged, p2's `acceptMails` is now ignored.

- [ ] **Step 1: Write the failing test** `UpdateMyTeam_AppliesAcceptMailsForParticipant1Only`: register+activate (both `acceptMails = true`); PUT with p1 `false`, p2 `false` → GET shows p1 `false`, p2 `true`. Then set p2 `AcceptMails = false` directly via `CreateDbContext()`, PUT with p2 `true` → p2 still `false` in DB.

- [ ] **Step 2: Run to verify failure**

Run: `docker compose run --rm --build tests --filter "FullyQualifiedName~MyTeamTests"`
Expected: FAIL on p2 assertion (`true` expected, `false` actual).

- [ ] **Step 3: Implement.** Only assign `player.AcceptMails` when `player.Id == participant1Id` (comment: p1 must not re-subscribe their teammate, who withdraws via the email link). In the HTML, add to the player1 section:

```html
<label class="checkbox-row" [class.checkbox-row--checked]="!!form.get('player1.acceptMails')?.value">
    <input type="checkbox" formControlName="acceptMails">
    <span>Je souhaite recevoir des emails pour les prochaines éditions.</span>
</label>
```

No checkbox for player2 (the form keeps round-tripping its value; the server ignores it).

- [ ] **Step 4: Run to verify pass**, then full suite. Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/SportsReservationAPI/Services/TeamService.cs backend/SportsReservationAPI.Tests/MyTeamTests.cs ui/src/app/pages/my-team/my-team.component.html
git commit -m "feat: let participant 1 manage only their own email opt-in"
```

### Task 5: `/desabonnement` page, privacy policy, docs

**Files:**
- Create: `ui/src/app/services/mail-preferences.service.ts`
- Create: `ui/src/app/pages/unsubscribe/unsubscribe.component.ts` + `.html` (reuse `../activate-account/activate-account.component.scss` via `styleUrls`, no new scss)
- Modify: `ui/src/app/app.routes.ts` (public `{ path: "desabonnement", component: UnsubscribeComponent }`, before `**`)
- Modify: `ui/src/app/pages/legal/legal.component.html:34-36`
- Modify: `docs/manual-testing-guide.md` (new `## 11. Unsubscribe link`)
- Modify: `CLAUDE.md` (Frontend: add `desabonnement` to the public pages list)

**Interfaces:**
- Consumes: `POST /api/mail-preferences/unsubscribe` (Task 2).
- Produces: `MailPreferencesService.unsubscribe(email: string, sig: string): Observable<unknown>` posting to `${environment.apiUrl}/api/mail-preferences/unsubscribe`.

- [ ] **Step 1: Implement the service and component.** Read `email`/`sig` from `route.snapshot.queryParamMap` in `ngOnInit`. States, mirroring `activate-account`'s markup (`login-page` / `login-card` / `card-header` / `error-banner` / `submit-btn`):
  - missing `email` or `sig` → card « Lien invalide », text « Ce lien de désabonnement est incomplet. Utilisez celui reçu par email, ou contactez un organisateur par email. » with `mailto:svenbarberat@orange.fr`.
  - idle → h1 « Se désabonner », text « Vous ne recevrez plus d'emails sur nos prochaines éditions à l'adresse {{ email }}. », button « Confirmer le désabonnement ». Nothing is sent before the click.
  - loading → button disabled with spinner.
  - success → h1 « C'est noté », the exact success copy from Global Constraints, link « Retour à l'accueil ».
  - 400 → error banner with the server's `error` (fallback « Ce lien de désabonnement est invalide. ») + organizer mailto; button hidden.
  - other error → banner « Une erreur est survenue. Réessayez dans quelques instants. »; button stays for retry.

- [ ] **Step 2: Add the route and update the privacy policy** sentence to: « …repose sur votre consentement explicite et facultatif, que vous pouvez retirer à tout moment via le lien présent en bas de nos emails, ou en nous contactant. »

- [ ] **Step 3: Write `## 11. Unsubscribe link` in the manual testing guide:** send a test announcement from `/annonce` to yourself (needs Mailgun configured) → link present below the footer; open it → page shows the address, DB unchanged (re-run the section 2 `sqlcmd` query); click Confirmer → success copy, `AcceptMails = 0` for every row with that address; reopen → still 200/success; change one character of `sig` → invalid-link banner; remove `sig` → « Lien invalide » card; register a player as `a+b@example.com`, build/receive their link → works. `mon-equipe`: checkbox shown for participant 1 only; unchecking + saving sets p1 `AcceptMails = 0`, p2 untouched.

- [ ] **Step 4: Verify the build** with `docker compose up --build -d frontend backend` (the Angular build runs in the image; a template/type error fails it), then walk section 11 of the guide.

- [ ] **Step 5: Commit**

```bash
git add ui/src/app/services/mail-preferences.service.ts ui/src/app/pages/unsubscribe ui/src/app/app.routes.ts ui/src/app/pages/legal/legal.component.html docs/manual-testing-guide.md CLAUDE.md
git commit -m "feat: add /desabonnement page and update privacy policy"
```
