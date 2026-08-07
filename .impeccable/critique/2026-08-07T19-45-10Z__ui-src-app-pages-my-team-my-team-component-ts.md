---
target: mon-equipe page
total_score: 13
max_score: 40
na_heuristics: 
p0_count: 2
p1_count: 2
timestamp: 2026-08-07T19-45-10Z
slug: ui-src-app-pages-my-team-my-team-component-ts
---
Method: dual-agent (A: a235d7b6132d87c03 · B: a5afe5167e19f6956)

## Design Health Score: 13/40 (Poor)

Operate-mode surface — all 10 heuristics scored for real.

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 2 | An invalid-form save attempt produces zero visible feedback anywhere in the DOM |
| 2 | Match System / Real World | 3 | Domain-correct French copy, sensible ✓/✗ badge iconography |
| 3 | User Control and Freedom | 1 | No cancel/discard, no confirmation before overwriting teammate's data or the login email, no undo |
| 4 | Consistency and Standards | 1 | Two colliding visual systems on one page (see below); email/phone fields silently dropped `type`/`autocomplete` present on the sibling inscription form |
| 5 | Error Prevention | 0 | Zero inline validation; no confirmation on the highest-stakes action on the page (changing the login email) |
| 6 | Recognition Rather Than Recall | 3 | Form is pre-populated from `getMyTeam()` — correct fit for a review task |
| 7 | Flexibility and Efficiency of Use | 1 | No jump-to-status shortcut; must scroll past ~20 fields to confirm payment |
| 8 | Aesthetic and Minimalist Design | 1 | Clashing brand systems plus a hard visual seam mid-page |
| 9 | Error Recovery | 1 | `loadError` has no retry button; `saveError` always shows the same generic string, discarding the backend's actual (often actionable) message |
| 10 | Help and Documentation | 0 | No contact link, no payment-sync explanation, no receipt reference near the unpaid badge |
| **Total** | | **13/40** | **Poor** |

## Design Specificity Verdict

**LLM assessment**: Not a deliberate sub-brand, unreviewed drift. `.top-bar` (dark red `#dc2626`, `#0d0d0d` bg, Bebas Neue + DM Sans via Google Fonts CDN) is byte-for-byte the same treatment as `login.component.scss` — internally consistent with login, but the `.form` block 50 lines later in the *same file* switches wholesale to the yellow/black/Lemon+Cabin system cloned from the inscription form, with a hard seam between them and zero shared tokens (the top-bar doesn't `@use "variables"` at all). Two separate Google Fonts CDN calls exist for login and my-team's identical header treatment, with no shared partial.

**Deterministic scan**: clean (0 findings). Confirmed external font import at `my-team.component.scss:2` — `@import url("https://fonts.googleapis.com/css2?family=Bebas+Neue&family=DM+Sans...")`.

**Visual overlays**: not available — no browser tool connected, and the route is auth-guarded besides.

## ⚠️ Confirmed: editing your own email silently changes your login credential

Verified directly against `backend/SportsReservationAPI/Services/TeamService.cs` (`UpdateMyTeamAsync`, lines 178-187): when Participant 1's submitted email differs from the account's current username, the backend checks for a duplicate and then does `team.Account.Username = playerDto.Email;` — no re-verification email, no separate confirmation step. The frontend gives zero indication this field is dual-purpose (contact info *and* login credential). A participant fixing a typo in their own email can lock themselves out with nothing in the UI warning them first.

## Overall Impression

This page has the right instincts in a few places (pre-population, preserved input on save failure, modeled loading/error/success states) but is a rougher build than either surface critiqued so far: zero inline validation (worse than the inscription form's original gap, since here the button stays clickable and does nothing rather than visibly disabling), a real account-lockout risk with no guardrail, and two unrelated visual identities stacked in one file. The page whose entire job is "reassure a participant their payment went through" gives that participant a bare, unsourced 48h claim with nothing to click.

## What's Working

- Pre-population from `getMyTeam()` — correct instinct for a review-not-reenter task.
- `save()`'s error handler never clears or re-patches the form on failure — typed values survive a failed request, no data loss.
- Loading/error/save-success/save-error states are all modeled as component flags and bound in the template — the skeleton for good status visibility exists, just incompletely wired.

## Priority Issues

**[P0] Editing your own email silently changes your login credential** (verified above)
- **Fix**: surface an explicit warning when the Participant-1 email field is dirty ("Cet email est aussi ton identifiant de connexion — tu devras te reconnecter avec la nouvelle adresse"), or gate the change behind an explicit confirm step.
- **Suggested command**: `/impeccable harden`

**[P0] No inline validation; save button never reflects form validity**
- **Why it matters**: unlike the inscription form (now fixed to bind `errorMessage()`/`aria-invalid`/disable on `.invalid`), this page's save button only checks `isSaving`, never `form.invalid` — it stays clickable and clicking it with bad data does nothing visible in the DOM. Worse than a disabled button: reads as a hung/broken app.
- **Fix**: port the same `errorMessage()` + per-field error binding + validity-gated button pattern already built for the inscription form.
- **Suggested command**: `/impeccable harden`

**[P1] Unpaid payment state offers no recourse**
- **Why it matters**: "peut mettre 48h à s'actualiser" is the entire message — no sync timestamp, no receipt link, no organizer contact. This is exactly the moment an anxious, already-paying participant lands on this page days before the event, and gets nothing to act on.
- **Fix**: add a last-checked indicator if available, a link to the Yurplan confirmation, and organizer contact info directly under the badge.
- **Suggested command**: `/impeccable clarify`

**[P1] `loadError` is a dead end**
- **Why it matters**: any transient network failure strands the user with static text and no way forward except a manual browser refresh.
- **Fix**: add a "Réessayer" button that re-invokes `getMyTeam()`.
- **Suggested command**: `/impeccable harden`

**[P2] Specific backend error messages are discarded**
- **What**: `save()`'s error handler always shows the same hard-coded string, ignoring `err.error?.error` — even though the backend can return a precise message (e.g. "Cet email est déjà associé à un autre compte.") that the inscription form's equivalent handler already knows how to surface.
- **Fix**: pass `err.error?.error` through to `saveError` with the generic string as fallback only.
- **Suggested command**: `/impeccable harden`

**[P2] One page, two design systems**
- **Why it matters**: undermines the established brand identity right when a converted, returning user is on the page, and doubles webfont payload for no functional reason.
- **Fix**: either fold the top-bar into the shared yellow/black/Lemon system (dropping the Bebas Neue/DM Sans imports here and in login), or, if a distinct "account chrome" is genuinely wanted, extend it consistently into the form body and document it as intentional — right now it's neither.
- **Suggested command**: `/impeccable adapt`

**[P3] Missing `type`/`autocomplete` attributes present on the sibling form**
- **Fix**: add the same `type="email"`/`type="tel"`/`autocomplete` attributes already used in `inscription-form.component.html`.
- **Suggested command**: `/impeccable polish`

## Persona Red Flags

- **Sam (accessibility-dependent)**: no `aria-invalid`/`aria-describedby` anywhere (present on the sibling inscription form, absent here) — a screen reader user gets zero signal when validation silently fails.
- **Alex (impatient returning user)**: must scroll past ~20 fields across both players to confirm the one thing he came for, with no jump link or summary view.
- **Anxious Yurplan payer (days before event)**: the worst-served persona on this page — a badge and a bare 48h claim with no timestamp, no receipt link, no contact.

## Minor Observations

- `getAdminLabel()` is dead code — nothing in the template calls it.
- `.required { color: red; }` uses a literal keyword instead of a design token, inconsistent with the rest of the file.
- The "SR" logo mark in both login and my-team headers never reflects "Hyrox Police 54," the actual event brand used on the landing page.

## Questions to Consider

- Is Participant 1 freely editing Participant 2's PII an accepted product tradeoff, or does it need a read-only/notify-teammate treatment?
- Should the login-email-change consequence be prevented entirely (read-only here, changed via a separate verified flow) rather than just warned about?
- Is the login/my-team red-Bebas-Neue chrome meant to diverge from landing/inscription's identity as an intentional "account area" brand, or should everything converge on one token set? This determines the right fix for the P2 design-system clash.
