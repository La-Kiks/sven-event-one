---
target: activate-account page
total_score: 17
max_score: 40
na_heuristics: 
p0_count: 2
p1_count: 2
timestamp: 2026-08-08T09-45-01Z
slug: ges-activate-account-activate-account-component-ts
---
Method: dual-agent (A: general-purpose isolated worktree agent, live-browser + source read · B: general-purpose isolated worktree agent, detect.mjs CLI + browser console attempt)

#### Design Health Score

| # | Heuristic | Score | Key Issue |
|---|---|---|---|
| 1 | Visibility of system status | 2/4 | Error banner shown with no scroll/focus-into-view; no visual distinction between "your input is wrong" and "the server rejected your token" |
| 2 | Match between system and real world | 3/4 | Copy is plain, correct French; but the emergency-red/near-black visual register clashes with the police/charity fitness-event brand |
| 3 | User control and freedom | 1/4 | No cancel, back, or alternate path from any state; the one "contact an organizer" line only appears on the missing-token branch, not the far more common post-submit rejection |
| 4 | Consistency and standards | 1/4 | Component is hardcoded off-brand: `#dc2626`/`#0d0d0d`, CDN "Bebas Neue"+"DM Sans" — while `login.component.scss` has already been converged onto `$main-color`/`$font-title`/`$font-body` from `_variables.scss` |
| 5 | Error prevention | 2/4 | `autocomplete="new-password"` present (real plus); no blur-time validation, no show/hide password toggle for a type-twice field |
| 6 | Recognition rather than recall | 2/4 | The 8-character minimum lives only in a placeholder that disappears the moment the user starts typing — no persistent helper text |
| 7 | Flexibility and efficiency of use | 3/4 | `keyup.enter` wired on both fields is a genuine nicety; no autofocus on the first field |
| 8 | Aesthetic and minimalist design | 2/4 | Layout itself is clean and single-purpose; palette is a jarring mismatch for what should read as a reassuring "you're in" moment |
| 9 | Help recognize, diagnose, recover from errors | 0/4 | The core failure mode of this whole page — invalid/expired token — renders a bare "invalid or expired" string with zero next action: no resend, no link to `/mot-de-passe-oublie` (which fixes this exact case server-side), no organizer contact |
| 10 | Help and documentation | 1/4 | Support/organizer copy exists in the source but only on the missing-token branch — never reachable from the state a real participant with a stale or double-clicked link actually hits |

**Total: 17 / 40**

#### Design Specificity Verdict

Not merely generic — actively off-brand. `activate-account.component.scss` is a leftover, unconverged skin: hardcoded `#dc2626` red, `#0d0d0d` background, and Google-Fonts-CDN "Bebas Neue"/"DM Sans," while the rest of the app (confirmed in `login.component.scss`) already pulls `$main-color` (yellow `#ffed00`), self-hosted `Lemon`/`Cabin`, and black from `_variables.scss`. `forgot-password.component.scss` shares the same unconverged styling. This is the participant's first login, immediately after paying 60€ for a police-organized charity event — and the screen that greets them looks like a different, unrelated product's error page. A generic "SR" logo mark (leftover from an earlier, more generic app name) reinforces the mismatch. The static detector (Assessment B) found zero rule-based issues on either file — expected, since hardcoded off-brand tokens and dead-end UX copy are semantic/visual problems outside a pattern-matching linter's scope, not something the CLI scan is built to catch. Nothing in Assessment B's clean run contradicts Assessment A's findings; it simply operates at a different altitude (code-smell patterns vs. design/brand/UX semantics), and the two are complementary rather than in tension here.

#### Overall Impression

The layout is focused and single-purpose, and a couple of interaction details (autocomplete hints, Enter-to-submit, a disabled/spinner loading state) show real care. But the page fails at the one thing it exists to do reliably: carry a paying participant through their first login. The success path ends in silence (an unannounced redirect with no "you're in" moment), and the far more realistic failure path — a stale, double-clicked, or expired activation link — ends in a dead end: a bare error string, visually indistinguishable from a typo'd password, with no resend option, no link to the forgot-password flow that would fix it in two clicks, and no organizer contact. Layered on top of a color/typography scheme that doesn't match the rest of the funnel, this is the weakest link in an otherwise-branded signup journey, at exactly the moment trust and reassurance matter most.

#### What's Working

- Single-column, distraction-free layout with no unrelated chrome — the form is the only thing on screen in every state.
- Two concrete interaction wins: `autocomplete="new-password"` on both password fields (helps password managers) and `keyup.enter` wired to submit on both fields — small details that reduce friction for real users.
- Loading state disables the submit button and swaps in a spinner, preventing accidental double-submission of the activation request.

#### Priority Issues

**[P0] Invalid/expired-token error is a dead end**
**Why it matters:** This is the single most likely failure mode on this page — activation links go stale, get double-clicked, or get reused after the token is already burned. Live-testing confirms the rendered message ("Ce lien d'activation est invalide ou a expiré") offers no resend link, no link to `/mot-de-passe-oublie` (which regenerates the same token server-side and would resolve this in two clicks), and no organizer contact — and it's visually identical to a plain client-side validation error, so users can't tell "fix your password" from "no password will ever work here." A participant who paid 60€ for a single-entry event has no path forward and every reason to think something is broken.
**Fix:** On the server-rejected branch specifically (not the client-validation branch), show a distinct message with a `routerLink` to `/mot-de-passe-oublie` and organizer contact info, and give it a different visual treatment than the plain-validation error state.
**Suggested command:** /impeccable harden

**[P0] Entire component is hardcoded off-brand**
**Why it matters:** `activate-account.component.scss` uses `#dc2626`/`#0d0d0d` and CDN "Bebas Neue"/"DM Sans" instead of the app's actual `$main-color`/`$font-title`/`$font-body` tokens from `_variables.scss` — tokens `login.component.scss` has already adopted. At the highest-trust moment in the funnel (first login, right after payment, for a police + Orphéopolis-affiliated event), the participant lands on a screen that looks like a different, unrelated product.
**Fix:** Converge `activate-account.component.scss` (and `forgot-password.component.scss`, which shares the same issue) onto the shared `_variables.scss` tokens the way `login.component.scss` already was, per the pattern in the recent "converge login/mon-equipe brand" work.
**Suggested command:** /impeccable adapt

**[P1] Sequential "whack-a-mole" validation with no error-type distinction**
**Why it matters:** Empty-field, length, and mismatch checks fire one at a time (`onSubmit`), each overwriting `errorMessage`, forcing repeated submit-fix-resubmit cycles instead of surfacing problems together — and reusing the same `.error-banner` for both client validation and the fundamentally different "your token is dead" server case compounds the confusion documented in the P0 above.
**Fix:** Validate all fields at once and render per-field messages; give server-rejection errors a distinct component/state from client-side validation errors.
**Suggested command:** /impeccable clarify

**[P1] Successful activation has no acknowledgment**
**Why it matters:** On success the component silently calls `router.navigate(['/mon-equipe'])` with zero confirmation — no "welcome," no toast, nothing. This is the emotional payoff moment of the entire signup+payment funnel, and it's swallowed instantly.
**Fix:** Add a brief success state/toast ("Compte activé, bienvenue !") before or during the redirect to `/mon-equipe`.
**Suggested command:** /impeccable delight

**[P2] Accessibility gaps on the error banner and focus states**
**Why it matters:** The error banner has no `role="alert"`/`aria-live`, so screen-reader users may never hear it; the only focus affordance on inputs is a color change (border to `#dc2626`) with no `:focus-visible` outline, unlike the already-converged `outline: 3px solid $main-color` pattern in `login.component.scss`. There's also no show/hide toggle for the password fields.
**Fix:** Add `role="alert"` + `aria-live="polite"` to the error banner, add a visible focus outline consistent with `login.component.scss`, add a password show/hide toggle.
**Suggested command:** /impeccable harden

#### Persona Red Flags

- **Jordan (first-timer):** Registers, pays, comes back days later to click the activation email link — token's expired. Sees an alarm-red, brand-mismatched screen with no next step. Most likely reaction: assumes the event is cancelled or something broke, and either abandons or tries to re-register/re-pay.
- **Riley (stress-tester):** Double-clicks the activation link, which is extremely common. If the token is single-use and consumed on the first successful call, the second tab renders the identical generic "invalid or expired" message — with no "you may already be activated, try logging in" branch and no link to `/login` anywhere on the page.
- **Sam (accessibility):** The error banner is inserted via `*ngIf` with no `role="alert"`/`aria-live`, so it may never be announced to screen-reader users; the only focus-state cue is a color-only border change against a near-black card, which also fails a color-contrast/non-color-cue check.

#### Minor Observations

- The "SR" logo mark shown on this page (and shared with login/forgot-password) doesn't match the actual product name ("Hyrox Police 54" / "Sport Challenge Police 54" per the live browser tab title) — it reads as leftover initials from an earlier, more generic app name.
- `.has-value` toggles input border opacity from `0.1` to `0.2` — a functionally near-invisible affordance.
- `forgot-password.component.html` includes a "Retour à la connexion" back-link; `activate-account.component.html` has no links anywhere, in any state — no way back to `/login` even for a user who realizes they're already activated.
- Border-radius values here (`3px`/`4px`/`2px`) are inconsistent with the `0.25em`/`0.5em` convention already established in the converged `login.component.scss`.
- Static detector (`detect.mjs`) returned a clean, zero-finding result (exit 0) against both the `.ts` and `.html` files — no rule-based code-smell issues, no false positives to adjudicate. Its scope doesn't extend to hardcoded design tokens or dead-end error UX, which is why it's silent on the P0/P1 issues above; the two assessment methods are complementary, not contradictory.
- Assessment B's browser-console cross-check via the live-server/detect.js injection step was blocked by the Claude Code auto-mode permission classifier when injecting a remote `<script src>` into the live page (DOM mutation itself, via title-set and inline-script injection, was confirmed permitted in the preflight). This is a tooling/permissions limitation of this run, not a defect in the page — no console-based findings were available to cross-check against Assessment A's live observations as a result.

#### Questions to Consider

1. `forgot-password.component.ts` already regenerates the same activation token through this exact `/activate` flow — so why isn't there a one-line link to `/mot-de-passe-oublie` on the token-rejected error state, turning a dead end into a two-click recovery?
2. This component (and `forgot-password`) appear to have been skipped when `login.component.scss` was converged onto the real yellow/black/Lemon-Milk/Cabin brand tokens — was that an oversight, or is there a reason participants see a visibly different, unbranded product at the exact moment they're trusting the police + Orphéopolis affiliation?
3. Given the event's scale, double-clicked or reused activation links are near-guaranteed — has the "token burned on first success" behavior been verified server-side, and if so, why does a second click render the same "dead link" message as a genuinely expired token, instead of a "you're probably already in, try logging in" branch?
