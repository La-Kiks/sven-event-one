# Impeccable full-site critique + fix pass — continuation plan

**STATUS: COMPLETE (2026-08-09).** All 5 remaining tasks below were finished
in the follow-up session, verified live in the browser (login error banner,
activate-account token-rejected state, admin teams panel/sign-out fix,
players CSV export label, landing sticky CTA), and the app was rebuilt with
`docker compose up --build` with no errors. Nothing was committed as of the
end of that session — check `git status`/`git log` before assuming this is
merged. Kept below as a historical record of what was done and why.

---

Paused mid-session on 2026-08-08 to stay under token budget. This file is the
handoff so a future session can pick up exactly where this one stopped.

## Context

Ran a full Impeccable dual-agent critique pass across all 11 pages of the
Angular app (6 never critiqued before, 5 re-critiqued to verify prior fixes
held). All 11 critique snapshots are persisted under `.impeccable/critique/`.
User chose "fix everything" (P0/P1 findings) across the 9 *live* pages.

Two pages — `/payment-success` and `/payment-cancel` — are confirmed **dead**:
registration pays via an external Yurplan link
(`ui/src/app/components/ui/inscription-form/inscription-form.component.ts`),
not this app's own Stripe integration. This is an intentional product decision
(client didn't want a Stripe account), not a bug — see memory file
`project_yurplan_payment.md`. User chose to document the Stripe code as
legacy rather than remove or ignore it (task 25 below, not yet done).

A disposable QA test team (**ID 35**, "Impeccable QA Test Team", unpaid,
login `impeccable-qa-1@example.com` / `ImpeccableQA!2026`) was created in the
dev DB during critique to exercise authenticated pages. It's still sitting in
the teams/players list — fine to keep using it for continued QA, or delete it
via the admin panel once done.

**Note:** the app runs via `docker compose up --build` (already running at the
time of writing, ports from `.env`). Since Angular is built into the image,
none of the source edits below are live in the running containers yet — a
rebuild is needed before visually verifying anything, and a design pass with
actual screenshots (per CLAUDE.md's UI-testing guidance) hasn't been done yet
for the changes in this session.

## Done (commits not yet made — nothing in this session has been committed)

- **Login** (`ui/src/app/pages/login/`): error banner now `role="alert"`/
  `aria-live`, `aria-invalid`/`aria-describedby` wired to inputs, logo links
  home, added "Pas encore inscrit(e) ?" link to `/inscription`.
- **Rebrand**: `activate-account`, `forgot-password`, `not-found` — replaced
  hardcoded red/`#0d0d0d`/Google-Fonts-CDN Bebas-Neue+DM-Sans with the shared
  `$main-color`/`$font-title`/`$font-body`/`$background-color` tokens from
  `_variables.scss` (same pattern `login.component.scss` already used).
- **activate-account**: expired/invalid-token rejection is now a distinct
  `tokenRejected` state with a link to `/mot-de-passe-oublie` + organizer
  phone/email, instead of a dead-end generic error banner.
- **forgot-password**: error handling now branches on HTTP status (400 →
  "vérifiez le format", 429 → existing rate-limit copy, else → generic
  server error) instead of collapsing everything into "erreur serveur";
  added a "Mauvaise adresse ? Réessayer" control after success so the form
  isn't fully removed from the DOM; email input is now `type="email"`.
- **not-found**: now uses the shared `app-button` component for two real CTAs
  (home + `/inscription`) instead of a thin ghost-outline link; paragraph
  contrast raised and capped to `32ch` width.
- **Admin teams/players rebrand** (`teams.component.scss`,
  `players.component.scss`): same red→brand-token conversion as above,
  applied file-wide. Kept destructive-action red (delete button/confirm,
  error text) deliberately unconverted — see the inline comment in
  `teams.component.scss` above `.delete-confirm`. Also opportunistically
  fixed some low-contrast text in `players.component.scss` (`th`, `.muted`,
  `.count` opacities raised) since the file was already being rewritten.
- **Admin teams panel/Sign-out misclick** (task 19): `.detail-panel` and
  `.backdrop` now start below the top bar (`$top-bar-height: 4.5rem` local
  var) instead of `top: 0`, so the slide-in panel can never visually cover
  the top bar's Sign-out button. Top bar also given `position: relative;
  z-index: 40` as a belt-and-braces safety margin above the panel's
  `z-index: 30`.
- **Admin teams keyboard/AT operability** (task 20): team table `<tr>` rows
  got `tabindex="0"`, `role="button"`, `(keydown.enter)`, and an
  `aria-label`; the two clickable payment-status badges
  (`team-badge.clickable`) were converted from `<span (click)>` to real
  `<button type="button">` elements (with a small CSS reset in
  `.team-badge` so they still render identically).

All edits above passed the impeccable post-edit design hook with no
deterministic issues flagged.

## Not started yet — pick up here

Tasks below are numbered to match the TaskList entries from this session
(IDs may not carry over to a fresh session's TaskList — use the descriptions).

1. **Task 21 — players CSV export scope indication.**
   `ui/src/app/pages/players/players.component.ts`, `exportCsv()` (~line 148)
   exports `this.sorted` (the filtered+sorted array) with a filename that's
   always `players-YYYY-MM-DD.csv` — no indication in the button label or
   filename of whether a filter/search is currently narrowing the export.
   Fix: check the players page's toolbar HTML for the `.export-btn` — make
   its label dynamic (e.g. "Exporter (N filtrés)" vs "Exporter tout (N)"
   depending on whether `filtered.length !== players.length`), and/or add
   the scope to the filename. Keep it simple — a label change is probably
   enough, this was a P1 not a P0.

2. **Task 22 — landing page: outage vs. sold-out + persistent CTA.**
   `ui/src/app/pages/landing/landing.component.ts` — `ngOnInit` sets
   `isRegistrationFull = true` on both a genuinely full roster AND any
   fetch error from the team-count service (fail-closed, but indistinguishable
   to the visitor). Need to check the actual service call (look for
   `TeamCountService` usage) and add a separate `loadError` flag so the
   template can show a distinct "something went wrong, try again" message
   instead of a false "complet" for outages. Second, smaller fix: the page's
   only CTA sits before the hero video with nothing persistent further down
   a long scroll page — consider a sticky/repeated CTA once scrolled past
   the hero.

3. **Task 23 — inscription stuck disabled-button bug.**
   `ui/src/app/components/ui/inscription-form/inscription-form.component.ts`
   / `.html` — the per-step Next/Confirm button is
   `[disabled]="form.get('stepN')?.invalid"`. A disabled button never fires
   `(click)`, so the `else { stepGroup?.markAllAsTouched() }` branch that's
   supposed to reveal validation errors can never run when a user skips a
   required radio group or select (only text-input steps happen to work,
   because typing triggers `markAsTouched` some other way). Fix: either (a)
   keep the button always enabled and call `markAllAsTouched()` +
   scroll-to-first-invalid on click instead of `[disabled]`, or (b) keep it
   disabled but add a persistent hint tied to
   `stepGroup.touched && stepGroup.invalid` telling the user what's missing.
   Option (a) is probably more robust — check how `next()` is currently
   structured before choosing.

4. **Task 24 — my-team backend enum validation hardening.**
   Backend: `CreatePlayerDtoValidator`/`CreateTeamDtoValidator`
   (`backend/SportsReservationAPI/Models/Player/` and `.../Team/`) only
   check `NotEmpty()` on `Category`/`Outfit`/`Administration`/`Version`, not
   that the value is one of the real allowed values (`man`/`woman`/`mixt`,
   `yes`/`lend`/`no`, `nationale`/`gendarmerie`/etc., `short`/`long` — see
   `ui/src/app/components/ui/inscription-form/inscription-form.component.html`
   for the authoritative list of valid values). This isn't hit by the real
   form today (confirmed: the "P0" the critique found was actually caused by
   my own QA test fixture using made-up values like `"Homme"`/`"M"` instead
   of the real enum strings — not a live bug). Still worth adding
   `.Must(v => allowedValues.Contains(v))` validators as defense-in-depth
   against a malformed direct API call. Low priority relative to the rest of
   this list.

5. **Task 25 — document Stripe integration as legacy.**
   User decided: keep the code, add a clear note. Needs:
   - A note in `CLAUDE.md` under the Stripe/`StripeController`/`StripeService`
     bullet in "Backend architecture", stating registration payment actually
     goes through an external Yurplan link
     (`inscription-form.component.ts`), and the Stripe checkout/webhook/
     `payment-success`/`payment-cancel` pages are unused in the live flow,
     kept as a fallback in case Yurplan is ever dropped.
   - Probably also a short comment at the top of
     `backend/SportsReservationAPI/Controllers/StripeController.cs`,
     `Services/StripeService.cs`, and the `payment-success`/`payment-cancel`
     page components, saying the same thing, so nobody stumbles on this code
     later and assumes it's live.

## After the fixes

- Nothing has been committed yet — review the diff and commit (the user's
  commit-message style is visible via `git log`, e.g. `fix: ...` /
  `chore: ...` prefixes, `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`).
- The app needs a `docker compose up --build` to actually see any of this
  session's changes live (nothing has been rebuilt yet).
- Per CLAUDE.md, UI changes should be verified in a running browser before
  calling this done — that verification pass (screenshots/manual click-through
  of at least login, activate-account, not-found, and the admin teams panel)
  hasn't happened yet for this session's edits.
- Consider whether to delete the QA test team (ID 35) once done, or keep it
  as a standing fixture — ask the user.
