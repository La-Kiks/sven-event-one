---
target: ui/src/app/pages/forgot-password/forgot-password.component.ts
total_score: 27
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 2
timestamp: 2026-08-15T14-21-04Z
slug: pages-forgot-password-forgot-password-component-ts
---
Method: dual-agent (A: a03b82b7a0430517e · B: a37199467520d262a)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3 | Spinner replaces the submit button's text with no accompanying label ("Envoi en cours…"), so the button's accessible name goes empty mid-submit. |
| 2 | Match System / Real World | 3 | Plain, clear French copy throughout; no complaints on language itself. |
| 3 | User Control and Freedom | 3 | "Mauvaise adresse ? Réessayer" lets the user back out of the success state and retry without a reload — a genuinely good escape hatch. |
| 4 | Consistency and Standards | 1 | Confirmed live via `getComputedStyle`: `.login-card` renders `border-radius: 8px` + `backdrop-filter: blur(10px)` + `background: rgba(255,255,255,0.03)` — the exact "explicitly rejected previous identity" DESIGN.md calls out, while every redesigned page (landing, inscription, teams, my-team) runs flat 2px-radius opaque surfaces. |
| 5 | Error Prevention | 2 | Only checks `email.trim()` non-empty client-side; no inline email-format validation before the network round trip. |
| 6 | Recognition Rather Than Recall | 4 | Single-field form, nothing to remember. |
| 7 | Flexibility and Efficiency | 3 | `(keyup.enter)="onSubmit()"` supports keyboard-only submission. |
| 8 | Aesthetic and Minimalist Design | 3 | Genuinely minimal in isolation (one field, one action, one secondary link) — its problem is being a different clean than the rest of the product, not clutter. |
| 9 | Error Recovery | 3 | Error banner differentiates 429 / 400 / generic-500 with distinct copy, `role="alert"`/`aria-live="polite"`/`aria-describedby` wired correctly; docked for not giving concrete next steps (e.g. no retry-after guidance on 429). |
| 10 | Help and Documentation | 2 | Only escape hatch is "Retour à la connexion" — no fallback contact path for a user whose reset email genuinely never arrives (Mailgun sending is best-effort per CLAUDE.md; failures are silent by design). |
| **Total** | | **27/40** | **Acceptable — significant improvements needed** |

## Design Specificity Verdict

**LLM assessment (Assessment A)**: Confirmed as the **third page in the copy-pasted pre-redesign family**, not an authored Tactical Briefing surface — verified three independent ways, not inferred: (1) live `getComputedStyle` on the rendered page returns `border-radius: 8px` on `.login-card`, the literal radius value DESIGN.md names as the rejected old identity; (2) `forgot-password.component.scss` is near-byte-identical to `login.component.scss`/`activate-account.component.scss` — same `.login-page` background gradient, same `.login-card` (`0.5em` radius, `backdrop-filter: blur(10px)`, translucent `rgba(255,255,255,0.03)` fill), same `.logo-mark`/`.error-banner`/`.form-group input`/`.spinner`, and the same banned `transform: scale(0.99)` on `.submit-btn:active`; (3) the page's own root selectors are literally `.login-page`/`.login-card` — nobody even renamed the wrapper classes when this was cloned from login. Only the logo mark and heading read "on-brand" because those were touched in an earlier logo-only fix pass; the container, inputs, and button are unmistakably the old glass-morphism system. The one genuinely custom addition, `.error-banner--recovery`, is dead CSS — nothing in the current HTML uses it.

**Deterministic scan**: `detect.mjs --json ui/src/app/pages/forgot-password/` exited 0 with an empty findings array (`[]`) — a clean scan. This is a **notable gap, not a clean bill of health**: the detector's markup-pattern rules did not catch the border-radius/backdrop-filter/translucent-surface deviations from DESIGN.md's tokens that both live measurement and source comparison confirmed. Treat the zero-findings result as evidence the detector isn't scoped to this project's specific token values (2px radius, no-blur, opaque surfaces) rather than evidence the page is on-system. No false positives to report, since there were no findings at all to evaluate.

**Visual overlays**: Script-injection overlay (the `live-server.mjs` + `detect.js`-in-page flow) was not run by either assessment — both instead used direct `getComputedStyle` queries and screenshots against the live page at `http://localhost:7193/mot-de-passe-oublie`, which is a valid but different evidence path. No user-visible overlay is available in the browser tab from this run. In its place: Assessment B captured raw computed values from the live DOM — `.login-card` at desktop (1280px): `border-radius: 8px`, `background-color: rgba(255,255,255,0.03)`, `box-shadow: none`, `backdrop-filter: blur(10px)`, `width/max-width: 400px/400px`; `.submit-btn` (measured at 375px, since the button leaves the DOM in the post-submit state at desktop): `border-radius: 4.2px` (`0.25em`), `background-color: rgb(255,237,0)`, `box-shadow: none`, `backdrop-filter: none`. Console was clean (no errors/warnings) through the full submit flow.

## Overall Impression

The interaction design here is quietly above average — the "wrong email? retry without a reload" affordance and the differentiated 429/400/500 error copy are better thought-through than most forgot-password flows. But the visual shell is not this product's: it's a direct, unrenamed clone of the pre-redesign `login`/`activate-account` glass-card, confirmed live (not just in source) via `border-radius: 8px` and `backdrop-filter: blur(10px)` on the rendered page — both explicitly banned by DESIGN.md. The single biggest opportunity is that this is now the third page in an identical family, so fixing it as a one-off risks a fourth drift; the login/activate-account fix (already done per this session's context) and this page's fix should share one fix so the pattern can't reappear.

## What's Working

1. **The "Mauvaise adresse ? Réessayer" retry affordance.** Most forgot-password flows strand the user on a static "check your email" screen with no recourse for a mistyped address. This one collapses `successMessage` and returns to the form state instantly, no reload, no re-navigation — genuinely better flow design than the visual polish level suggests.
2. **Error-state differentiation.** `onSubmit()`'s error handler distinguishes rate-limit (429, matching the backend's documented 15-min/hour/day anti-abuse limiter), bad format (400), and a generic fallback, each with distinct copy — wired with `role="alert"`, `aria-live="polite"`, and `aria-describedby` on the input. More accessible and more specific than the average form in this app's class.
3. **Correct, un-clever anti-enumeration behavior.** The component never branches UI on whether the email existed; it just renders `response.message` verbatim on one success path. No timing or copy leaks. This is the right frontend behavior for a security-sensitive endpoint, even though the visual treatment of that success state is weak (see Priority Issues).

## Priority Issues

**[P1] Third page running the rejected pre-redesign visual system, confirmed live**
Why it matters: `getComputedStyle` on the rendered page returns `border-radius: 8px` and `backdrop-filter: blur(10px)` on the main card — not a source-only concern, this is what ships. DESIGN.md names this exact radius value as "the explicitly rejected previous identity," and PRODUCT.md frames the primary audience as police/law-enforcement personnel for whom "trust and legitimacy" is a named product principle. A user bouncing between `/inscription` (2px, flat, opaque `#0a0a0a`) and `/mot-de-passe-oublie` (8px, blurred glass, translucent) experiences a visually different product mid-task, at the exact moment (account lockout) trust matters most.
Fix: restyle `forgot-password.component.scss` onto the shared system — flat Raised Surface (`#0a0a0a`), 2px `{rounded.sharp}` radius, no `backdrop-filter`, replace the hand-rolled `<button class="submit-btn">` with the shared `<app-button>`, remove `transform: scale(0.99)` on `:active`. Since this is confirmed the third near-identical copy of the same stylesheet (login → activate-account → forgot-password), do this as one shared-pattern pass across all three rather than three separate one-offs, to stop a fourth drift.
Suggested command: `/impeccable polish`

**[P1] Success confirmation carries zero visual weight at the highest-anxiety moment**
Why it matters: `.success-text` renders in `rgba(255,255,255,0.7)` — identical visual weight to any secondary paragraph, with no color, icon, or badge — despite DESIGN.md defining a reusable Success Badge pattern (`#4ade80` on `rgba(34,197,94,0.15)` with matching border) already used elsewhere in the app for exactly this kind of "did it work" state (payment/activation). Password recovery is a peak-end-rule moment: the resolution disproportionately shapes how the whole flow is remembered, and this one visually shrugs. The generic wording is correct and must stay (anti-enumeration); its flat presentation is not a security requirement, it's a missed pairing of honest ambiguity with confident visual reassurance.
Fix: wrap the success message in the system's existing success-badge treatment (tinted green background + border + icon) while keeping the copy itself unchanged.
Suggested command: `/impeccable polish`

**[P2] Loading state has no accessible or visible text label**
Why it matters: `<span *ngIf="isLoading" class="spinner"></span>` replaces the button's only text content during submit, so a screen reader announces an effectively empty/unlabeled button mid-action, and sighted users get a bare spinning ring with no "sending…" language at the one deliberate action on this page.
Fix: add visually-hidden text ("Envoi en cours…") alongside the spinner, or set `aria-label` on the button while `isLoading` is true.
Suggested command: `/impeccable harden`

**[P2] Retry clears the typed email instead of preserving it for correction**
Why it matters: `tryAnotherEmail()` sets `this.email = ''`, so a user who mistyped one character has to retype the full address from memory rather than fix the typo — punitive for exactly the error case this affordance exists to solve.
Fix: stop clearing `email` in `tryAnotherEmail()`; leave it populated so the user edits in place (the existing `.has-value` styling and `(input)="clearError()"` already support a re-focused, editable field).
Suggested command: `/impeccable polish`

**[P3] No fallback contact path for genuine non-delivery**
Why it matters: Mailgun sending is explicitly best-effort in this app (mail failures are silently swallowed so they never block registration per the backend design), meaning a real, registered user can legitimately never receive the reset email with zero error surfaced anywhere. The only path forward on this page is "Retour à la connexion" — a dead end for that user.
Fix: add a low-key line under the success message ("Toujours rien après quelques minutes ? Contactez [organizer contact]") using the already-documented organizer contact info (Sven Barberat) from PRODUCT.md.
Suggested command: `/impeccable clarify`

## Persona Red Flags

**Jordan (first-timer / officer who forgot their password before the event)**: Arrives via `/login`'s "Mot de passe oublié ?" link; because `/login` is itself still on the old glass-card system, the login→forgot-password hop is visually smooth, so no red flag at entry. Types email, hits submit, sees an unlabeled spinner (brief "did that register?" pause), then a plain-gray success sentence with no visual confirmation strength. Jordan's real risk moment: no email arrives within a few minutes (junk-folder delay, or a genuine best-effort Mailgun no-op) and the page offers nothing beyond "go back to login" — no "check spam," no "still nothing? contact us." Jordan has no diagnosis path and no next step.

**Sam (accessibility-dependent, screen-reader user)**: Tabs to the email field (correctly labeled via `for`/`id`), submits via Enter — both fine. During loading, the button's accessible content becomes an empty `<span class="spinner">`, so Sam's screen reader announces nothing informative at the exact moment state changes. On success, `role="status"` on `.success-text` is present and correctly wired (a genuine win) — but that's undercut by sighted users getting no equivalent visual confirmation strength, an inconsistency between what sighted and non-sighted users perceive as "this worked."

**Casey (mobile user)**: Confirmed via iframe-based CSS-viewport emulation at 375px and 390px (a true top-level browser resize wasn't available in this session, so this is iframe-verified, not a full-viewport screenshot): no horizontal overflow at either width (`scrollWidth > innerWidth` is false), and no text/element clipping — the earlier fixed-width-card risk flagged for this page family does **not** reproduce here. However, the card renders **edge-to-edge with zero outer margin** at these widths (`.login-page` has no horizontal gutter, `.login-card` is `width:100%; max-width:400px`), so below 400px the panel's background/border touches the screen edge directly — inheriting mobile safety by accident (fixed max-width + centering) rather than by the system's actual mobile strategy (fluid `clamp()`-based gutters used elsewhere per DESIGN.md).

## Minor Observations

- `.error-banner--recovery` (column layout, yellow bold links) is defined in the SCSS but nothing in the current HTML uses that modifier — dead CSS, or a hook for a variant that was never wired up.
- `clearError()` fires on every keystroke but there's no positive signal (e.g. border color) confirming the field is now considered valid again — the error just silently vanishes.
- Only client-side validation is a non-empty check; email format is left entirely to the browser's native `type="email"` behavior and the backend's 400 response, with no inline format hint before submit.
- Detector returned a clean scan (0 findings) on this directory, which should not be read as "on-system" given the confirmed live token deviations above — a scope gap in the automated check for this project's specific radius/blur/surface tokens, not a contradiction of the manual findings.

## Questions to Consider

1. If login and activate-account are being restyled to match the Tactical Briefing system, is there a reason forgot-password wasn't included in the same pass — is a "rare path" being treated as lower priority even though it's the single highest-anxiety screen in the app for the person going through it?
2. The system already solved "communicate a positive-but-uncertain state clearly" with the Success Badge pattern used for payment/activation — was leaving it out of this success state an oversight, or a conscious call that this state didn't "count"?
3. The retry-without-reload flow is some of the better UX thinking in this whole page and lives in the same file as `border-radius: 0.5em`, `backdrop-filter: blur(10px)`, and a banned `transform: scale(0.99)` — should the restyle pass also carry the two small logic fixes (preserve email on retry, label the loading state) along, since they touch the exact same lines?
