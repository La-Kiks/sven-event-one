---
target: ui/src/app/pages/activate-account/activate-account.component.ts
total_score: 22
max_score: 32
na_heuristics: 7,10
p0_count: 0
p1_count: 3
timestamp: 2026-08-15T14-11-54Z
slug: ges-activate-account-activate-account-component-ts
---
Method: dual-agent (A: ac344340cb49a9bcf · B: a5fb6cc418c93b530)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3 | Spinner + `aria-live="polite"` banner + `aria-invalid` present; no pre-flight signal before the user learns (post-submit) that a filled-in link was already dead |
| 2 | Match System / Real World | 4 | Plain, human French copy throughout ("Ce lien d'activation est invalide ou a expiré…") |
| 3 | User Control and Freedom | 3 | "Retour à la connexion" always present; docked because the no-token dead end's escape route is materially thinner than the rejected-token one (see Priority Issues) |
| 4 | Consistency and Standards | 2 | Visually foreign relative to the 4 redesigned screens — confirmed byte-near-identical to `login.component.scss`, itself already flagged as stale in the prior run of this sweep |
| 5 | Error Prevention | 2 | Client-side length/match checks exist, but nothing stops a user typing two full passwords into an already-dead link before the backend says so |
| 6 | Recognition Rather Than Recall | 2 | Password rule ("8 caractères minimum") lives only in a placeholder that disappears the moment typing starts |
| 7 | Flexibility and Efficiency | n/a | Single-purpose activation/recovery page — no power-user path applies |
| 8 | Aesthetic and Minimalist Design | 3 | Content itself is minimal; the `backdrop-filter: blur(10px)` card and radial yellow glow add visual noise not present anywhere else in the redesigned system |
| 9 | Error Recovery | 3 | `tokenRejected` copy is excellent (cause + 3 live-verified next steps); `missingToken` state one click earlier offers materially less, and a fixed 400px card with zero media queries risks clipping on narrow phones |
| 10 | Help and Documentation | n/a | No dedicated help surface needed for a single action; phone/email stand in as an implicit fallback |

**Total: 22/32 applicable (69%) — Acceptable, bordering on Good.** (na_heuristics: 7, 10)

## Design Specificity Verdict

**LLM assessment**: The *copy* is authored for this product — the rejected-token banner names Hyrox Police 54's actual organizer contact (06 48 73 50 15, svenbarberat@orange.fr) and speaks in plain, specific French rather than generic SaaS boilerplate. But the *visual shell* is category-interchangeable: a centered 400px glassmorphism card with `backdrop-filter: blur(10px)`, translucent `rgba(255,255,255,0.03)` background, and `border-radius: 8px` is the default template for any 2020s auth page, not something authored for a "void-black, hazard-yellow, tactical briefing, nothing floats/nothing softens" system. This page could be swapped into an unrelated SaaS product's activation flow with zero visual friction — which is exactly the failure mode DESIGN.md's Overview section says the redesign exists to reject ("a deliberate rejection of the product's previous look, which used soft ~0.5rem rounded corners... squares that off... Disciplined and angular over friendly and soft").

**Deterministic scan**: `node detect.mjs --json ui/src/app/pages/activate-account/` exited 0 with `[]` — zero findings. This is a **known limitation, not a clean bill of health**: the bundled detector checks generic markup/accessibility patterns, not this project's custom design-token compliance (2px `{rounded.sharp}`, no-blur, no-transform rules live only in DESIGN.md prose, not in any lint rule the detector can enforce). The literal violations below were caught by direct source diff and live `getComputedStyle()`, not by the detector — treat the `[]` as "nothing the generic ruleset flags," not "on-brand."

Confirmed literal values (Assessment B, live `getComputedStyle()` on `http://localhost:7193/activer-compte`):
- `.login-card`: `border-radius: 8px` (source: `0.5em`), `background-color: rgba(255,255,255,0.03)`, `backdrop-filter: blur(10px)`, `border: 0.67px solid rgba(255,255,255,0.08)`, `box-shadow: none`, fixed `width: 400px` / `max-width: 400px`
- `button.submit-btn`: `border-radius: 4.2px` (≈`0.25em`), `background-color: rgb(255,237,0)`, `text-transform: none` (i.e. sentence case, not the system's mandatory uppercase for CTAs)
- Source diff (Assessment A): `activate-account.component.scss` is a near-byte-for-byte copy of `login.component.scss`, including `.submit-btn { transform: scale(0.99) }` on `:active` and `.error-icon { border-radius: 50% }`

Against DESIGN.md: `{rounded.sharp}` = `2px` (card is 8px, 4x over), the No-Lift Rule bans blur as a card treatment and bans `transform` everywhere except one named admin-panel exception (this page has `scale(0.99)`), the Shapes section states "nothing in the system is fully round" (the circular error icon is 50%), and the Buttons section mandates uppercase CTAs (this button is sentence case) via the shared `<app-button>` component (this page hand-rolls its own `<button class="submit-btn">` instead of importing it).

**Visual overlays**: No `detect.js` script-injection overlay was run for this page (the adapted task scope called for direct `getComputedStyle()` capture instead); there is nothing to view in a `[Human]` tab. The literal computed-style values above are the evidentiary substitute.

## Overall Impression

The words are right; the walls are wrong. This is the second page in a row (after login) found running the pre-redesign "glassmorphism" shell wholesale — same blurred translucent card, same 8px-class rounding, same hand-rolled button with a banned hover/active transform — while the copy layer (especially the rejected-token recovery banner) is genuinely some of the best writing in the app. The biggest opportunity is mechanical, not creative: swap this page onto the same flat, hairline-bordered, `{rounded.sharp}` card and the shared `<app-button>` that the four redesigned screens already use, and most of the visual-identity issues disappear in one pass — the underlying UX (validation, error copy, aria wiring) is already close to solid.

## What's Working

1. **The rejected-token recovery copy** is the standout: it explains *why* (expired or already-used link), and gives three concrete, live-verified next steps (request a new link via `/mot-de-passe-oublie`, call, or email the organizer) rather than a dead end — exactly the reassurance a high-stakes "you're locked out of your paid registration" moment needs.
2. **Accessibility wiring is genuinely solid for a stale-shell page**: `aria-live="polite"` on the error banner, `aria-invalid`/`aria-describedby` on the password fields, visible `:focus-visible` outlines (confirmed live: solid ~2.67px hazard-yellow outline on the first tab-stop), and client-side validation that preserves entered values after a rejection instead of wiping the form.
3. **Client-side validation messaging** (empty/length/mismatch) is specific and prevents avoidable round-trips for the password-confirmation case.

## Priority Issues

**[P1] Stale pre-redesign visual shell, confirmed byte-near-identical to login's already-flagged bug**
Why it matters: `.login-card` on this page carries `border-radius: 0.5em` (8px), `backdrop-filter: blur(10px)`, and a translucent `rgba(255,255,255,0.03)` background — directly contradicting DESIGN.md's `{rounded.sharp}` (2px) token and the No-Lift Rule's explicit ban on blur as a card treatment ("Don't revert to rounded corners (0.5rem or larger) anywhere — that's the explicitly rejected previous identity"). Since this is the *second* page in the same sweep found running this exact stale shell, it suggests the redesign pass had a systematic gap around the auth-adjacent pages, not an isolated miss.
Fix: Replace `.login-card` with the system's flat, opaque, hairline-bordered card pattern (`{colors.surface-raised}` background, 1px `{colors.border-hairline}` border, `{rounded.sharp}` 2px radius, no `backdrop-filter`).
Suggested command: `/impeccable polish`

**[P1] No responsive handling — fixed 400px card, zero media queries**
Why it matters: `getComputedStyle()` confirms `.login-card` is a hard `width: 400px` / `max-width: 400px`, and both loaded stylesheets contain zero `@media` rules for this component. On any viewport narrower than ~440px (common Android phone widths: 360-390px), the card cannot shrink and will overflow horizontally — the exact class of bug DESIGN.md calls out as a shipped incident elsewhere ("this was shipped without `flex-wrap` initially and clipped the sign-out control off-screen entirely on narrow viewports; treat any un-wrapped flex header as a bug, not a style choice"). This page has the same failure mode via fixed width instead of missing `flex-wrap`.
Fix: Replace the fixed `400px`/`max-width: 400px` with a fluid width (`clamp()` or percentage + `max-width`) and verify at 360px/375px viewports.
Suggested command: `/impeccable harden`

**[P1] Recovery depth is inconsistent between the two functionally-identical dead ends**
Why it matters: The `missingToken` state (no `?token=` param at all — confirmed live, the message reads "Ce lien d'activation est incomplet") only offers a phone number and an organizer's personal email. The `tokenRejected` state (bad/expired/reused token) additionally offers a self-service `/mot-de-passe-oublie` link. Both are the same functional outcome for the user — "I cannot activate right now" — but a user who lands on the weaker path (arguably the more common one: a forwarded email client that stripped the query string) loses the self-service option and must fall back to a phone call or a personal inbox.
Fix: Give `missingToken` the same self-service link as `tokenRejected`, or merge the two states into one template.
Suggested command: `/impeccable clarify`

**[P2] Hand-rolled button bypasses the shared `<app-button>` component, including a banned hover/active transform**
Why it matters: `activate-account.component.html` uses a raw `<button class="submit-btn">` instead of `<app-button>`. Its CSS diverges from the shared component on radius (`0.25em`/4.2px vs. the button component's 2px), case (`text-transform: none`, i.e. sentence case, vs. the system's mandatory uppercase CTA), and — most concretely — carries `transform: scale(0.99)` on `:active`, which is exactly the pattern the No-Lift Rule names as banned everywhere except one unrelated admin slide-in animation.
Fix: Swap in `<app-button>`, or at minimum delete the `transform` and align radius/case/weight to the shared token.
Suggested command: `/impeccable polish`

**[P2] No acknowledgment at either end of the risk curve**
Why it matters: Two related gaps compound the emotional flatness of this page. First, nothing checks token validity before the user fills in and submits two passwords — they only learn the link was already dead *after* doing the work, right before the worst-news moment. Second, on success, `onSubmit()` (component.ts:56-58) does a silent, immediate `router.navigate(['/mon-equipe'])` with no acknowledgment inside this component at all — for what is, for this user, the single highest-stakes click in the app (the one that unlocks their paid registration), there is no positive "end" beat, only a neutral-to-negative arc throughout.
Fix: Add a lightweight pre-flight token check (or at least a loading/verifying state on mount) so a doomed link fails fast before data entry; add a brief success acknowledgment before/during the redirect.
Suggested command: `/impeccable delight`

## Persona Red Flags

**Jordan (Confused First-Timer)**: If Jordan's activation email link gets forwarded, copy-pasted incompletely, or opened from a client that strips query params, they land on `missingToken` with only a phone number and an organizer's personal email address — no button to self-serve a new link, unlike the adjacent `tokenRejected` state one click away. A first-timer unsure whether they even registered correctly is being asked to make a phone call to resolve what a link click could fix.

**Riley (Deliberate Stress Tester)**: Riley loads `?token=invalid123` and finds the full set-password form renders immediately — the token-presence check is purely "is the param non-empty," not "is it valid." Riley then fills in and submits two passwords, only to get the `tokenRejected` rejection *after* doing the work (confirmed live: this two-step reveal is real, not a source-only claim). Riley would also flag the exact CSS duplication with `login.component.scss` — same class names in spirit, same `transform: scale(0.99)`, same circular `.error-icon` — as evidence these files were hand-copied rather than sharing a base, meaning a future fix to one won't propagate to the other.

**Casey (Distracted Mobile User)**: Casey is very plausibly on a phone tapping an activation link from their email app. The card is a hard-coded `400px`/`max-width: 400px` with zero `@media` rules anywhere in either loaded stylesheet — on a 360-390px-wide Android viewport (extremely common), this card cannot shrink and will overflow, forcing Casey into horizontal scrolling on the one screen where they most need a frictionless tap-to-finish flow.

## Minor Observations

- `.error-icon` uses `border-radius: 50%` (a full circle) and the loading spinner does too — both contradict the Shapes section's "nothing in the system is fully round (pill/circle)." The spinner is a defensible pragmatic exception (spinners are conventionally circular); the status icon less so, since DESIGN.md's own Status Badge component is explicitly never circular.
- The `tel:` link (`06 48 73 50 15`) renders in unstyled browser-default blue with an underline (`color: rgb(0,0,238)`) — it's the only element on the page not touched by the app's type/color system at all.
- Hazard yellow appears in three places at once on the rejected-token screen (logo tile, banner's internal accent, submit button) — no single use is a large-fill violation, but it dilutes DESIGN.md's "one dominant yellow element per screen" intent.
- File-naming evidence that the two auth pages were hand-copied rather than sharing a base: `login.component.scss` names its footer links `.forgot-password-link`/`.register-link`; `activate-account.component.scss` names its analogous block `.recovery-link`.

## Questions to Consider

- These two auth-adjacent pages (login, activate-account) are now confirmed to share the same stale shell and the same specific bugs (blur card, scale-on-active, circular icon) — was this page genuinely out of scope for the redesign pass, or did it get missed because it's not a page anyone browses to directly?
- This is arguably the single highest-stakes page in the whole app — it's the only thing standing between a paid registration and permanent lockout from `mon-équipe` — does it make sense that it received *less* design attention than the four fully-redesigned screens?
- Should `missingToken` and `tokenRejected` simply collapse into one state, given a real user experiences them as functionally identical failures but currently gets asymmetric recovery options?
