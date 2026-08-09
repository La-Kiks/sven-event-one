---
target: payment-success page
total_score: 16
max_score: 36
na_heuristics: Flexibility and efficiency of use
p0_count: 1
p1_count: 2
timestamp: 2026-08-08T09-45-18Z
slug: pages-payment-success-payment-success-component-ts
---
Method: dual-agent (A: a269a868f97d43678 · B: a89c8e0e8abfba076)

#### Design Health Score

| # | Heuristic | Score | Justification |
|---|---|---|---|
| 1 | Visibility of system status | 1/4 | `transactionStatus` is a hardcoded literal, never derived from the `session_id`/`team_id` Stripe appends to the redirect URL (backend supplies them in `StripeController.cs`'s `SuccessUrl`) or any live API check — the page asserts a fact it never verified. |
| 2 | Match between system and real world | 2/4 | Checkmark icon reads fine, but its color (`#2ECC71`, generic traffic-light green) and the modal's black backdrop ignore the yellow/black brand; no transaction context (team, amount, event date) grounds it in this real registration. |
| 3 | User control and freedom | 2/4 | One consistent exit (button or backdrop → `/`), but no path to view team status or a receipt. |
| 4 | Consistency and standards | 2/4 | Internally consistent with `payment-cancel` (same modal chrome), but visually inconsistent with the app's own brand palette used elsewhere. |
| 5 | Error prevention | 2/4 | Backdrop click instantly closes *and* navigates home with no confirmation — low stakes, but still a one-click point of no return. |
| 6 | Recognition rather than recall | 3/4 | Self-contained message; nothing from prior steps needs to be remembered. |
| 7 | Flexibility and efficiency of use | n/a | A one-shot, single-action confirmation screen has no expert/shortcut path to speak of. |
| 8 | Aesthetic and minimalist design | 2/4 | Reads as unfinished rather than intentionally restrained — `payment-success.component.scss` is 0 bytes, black void behind the box, no brand touches. |
| 9 | Help recognize/diagnose/recover from errors | 1/4 | No branching logic exists; if the webhook hasn't landed or failed, this screen has no way to reflect that — it's success or nothing. |
| 10 | Help and documentation | 1/4 | No support/contact affordance despite this being a real 60€ payment confirmation. |

**Total: 16/36** (9 applicable heuristics; #7 marked n/a)

#### Design Specificity Verdict

Generic and templated, not designed for this moment. It's the same reusable `ModalComponent` used for arbitrary success/error states elsewhere, rendered with an off-brand green checkmark, no event name/venue/date, no logo, and an empty page-level SCSS file. Mechanical scanning corroborates this from a different angle: the component's own `.ts` and `.html` came back completely clean on the CLI detector (no anti-patterns in the markup/logic itself) — the flatness is a content and design-intent gap, not a code-smell problem. The one live-DOM finding (`flat-type-hierarchy`, 16/20/24px type scale at a 1.5:1 ratio) was reported against `body`, and given the component's own template is 129 bytes with zero local styles, it's more likely inherited from the surrounding app shell/layout than authored here — flagged as a probable false positive against this specific component.

#### Overall Impression

This screen is meant to be the emotional peak of the whole registration+payment funnel (per the peak-end rule) but is visually and structurally identical to the *cancellation* screen — same dark box, same layout, only a small icon and one line of typo'd text ("Paiment réussi") differ. More seriously, the "success" it declares is never actually verified: the component ignores the `session_id`/`team_id` query params the backend supplies on redirect and unconditionally renders success regardless of whether the Stripe webhook has landed, failed, or never fired. Both assessors agree the component's code itself is small and clean (no anti-patterns detected mechanically), which is exactly the problem — there's almost nothing here to create the desired confidence-building, on-brand moment, and the one thing it does assert (definite payment success) is a claim the client-side code has no way to actually know.

#### What's Working

- Both the "Fermer" button and the backdrop dismiss the modal, giving impatient users a fast, low-friction exit.
- Layout is genuinely responsive: `clamp()`-based type scale and percentage/max-width sizing on `.modal-box` should hold up on small screens.
- The close action is a real `<button>` (not a styled `<div>`), so it's natively keyboard-focusable and operable via Enter/Space — and static analysis of the component's own `.ts`/`.html` came back clean with zero anti-pattern findings.

#### Priority Issues

**[P0] Copy asserts an unconditional fact the client cannot know**
**Why it matters**: The page never reads `session_id`/`team_id` from the URL despite the backend supplying them, and never checks with the API whether the webhook has actually landed. Live-tested with and without a fake `?session_id=cs_test_fake123&team_id=999` param — identical "Paiement réussi" render either way. This is a public, unauthenticated, bookmarkable URL that claims success regardless of whether any payment happened, which is both a trust problem and a support-ticket generator if the webhook lags or fails.
**Fix**: Use the `session_id`/`team_id` already present in the Stripe `SuccessUrl` redirect to poll or check team payment status server-side, and show an honest interim "confirming your payment…" state when status is still pending, rather than asserting success unconditionally.
**Suggested command**: `/impeccable harden`

**[P1] No brand or event identity on the funnel's emotional peak**
**Why it matters**: Off-brand green success icon on a plain black backdrop, no "Hyrox Police 54" name, no date/venue, no logo — a stark contrast to what should be a warm, confidence-building payoff after a duo has just registered and paid for a real event.
**Fix**: Give this screen (or the shared modal's success state) on-brand yellow/black treatment and surface the event name/date so the confirmation feels tied to what was actually just purchased.
**Suggested command**: `/impeccable adapt`

**[P1] No next-step content**
**Why it matters**: The only affordance is "Fermer" → home. No order summary (team name, 60€ paid), no "check your inbox," no link to team status (`/mon-equipe`). This is especially costly if the webhook really is still pending — the user has no way to later confirm anything landed.
**Fix**: Add a brief summary line and a link to the participant's team page, plus a note that a confirmation email is on its way.
**Suggested command**: `/impeccable delight`

**[P2] Dead hover state on the only interactive element**
**Why it matters**: `.modal-close-btn:hover { background-color: filter(brightness(1.25)); }` in `modal.component.scss` is invalid CSS — `filter()` isn't a valid `background-color` value. Confirmed empirically: hovering the close button produces zero visual change.
**Fix**: Replace with a valid hover treatment (e.g. `filter: brightness(1.25);` as its own declaration, or a real background-color swap).
**Suggested command**: `/impeccable polish`

**[P2] Missing accessibility semantics for a status-change screen**
**Why it matters**: The accessibility tree exposes only a bare text node and a button — no `role="dialog"`/`aria-modal`, no `aria-live`/`role="alert"` on the outcome message, and nothing calls `.focus()` when the modal renders. A screen-reader user gets no proactive signal that a payment outcome was just reported.
**Fix**: Add `role="dialog"` + `aria-modal="true"` to the modal container, `role="status"`/`aria-live="polite"` on the message, and focus the dialog (or its close button) on open.
**Suggested command**: `/impeccable harden`

#### Persona Red Flags

- **Jordan (first-timer, needs reassurance)**: Lands on a near-empty black screen with a terse, typo'd sentence ("Paiment" instead of "Paiement") and no mention of the event, confirmation email, or what happens next — easy to wonder "did that actually work?"
- **Riley (stress-tester)**: `/payment-success` is public and ignores every query param — navigating straight to the bare URL, with no auth and no session id, renders the identical "success" modal every time. Refreshing, revisiting later, or sharing the link all produce the same unconditional claim regardless of actual payment state.
- **Sam (accessibility)**: No dialog role, no live-region announcement, and no programmatic focus on open means a screen-reader user may not even realize a status message appeared, let alone what it said.

#### Minor Observations

- `payment-cancel.component.ts` has its own typo ("ultériement" → "ultérieurement"), reinforcing that both transactional screens were copy-pasted without a copy pass.
- No route sets a `title`, so the browser tab stays on the generic app name rather than reflecting the outcome.
- Success and error modal variants share identical container chrome (same dark box/layout); only the small icon differs, reducing at-a-glance status differentiation.
- The one live-DOM detector finding (`flat-type-hierarchy`, 16/20/24px sizes) is attributed to `body` broadly and likely originates from the app shell/global styles rather than this component, whose own template and stylesheet are both essentially empty — treat as a probable false positive against this specific file rather than a defect to fix here.

#### Questions to Consider

1. Given the webhook is the real source of truth and can lag behind this redirect, should the page actually call the API (using the `team_id`/`session_id` already in the URL) and show an interim "confirming your payment…" state instead of asserting success unconditionally?
2. Is a generic, reusable confirmation modal the right vehicle for what's explicitly the emotional peak of the funnel — or does this moment warrant its own on-brand page with event details and a clear next step?
3. Since this route is public and renders "success" for anyone regardless of payment state, is there a trust/support-burden risk worth addressing before launch?
