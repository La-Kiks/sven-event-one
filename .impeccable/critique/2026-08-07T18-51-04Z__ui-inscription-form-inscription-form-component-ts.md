---
target: inscription form
total_score: 16
max_score: 40
na_heuristics: 
p0_count: 2
p1_count: 2
timestamp: 2026-08-07T18-51-04Z
slug: ui-inscription-form-inscription-form-component-ts
---
Method: dual-agent (A: a0306767afcbedf07 · B: ab15f23ac615ea401)

## Design Health Score

Operate-mode surface (task completion) — all 10 heuristics scored for real, no automatic n/a.

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 1 | Plain-text "X/3" only, no stepper; no submit-in-flight state, double-click can double-submit |
| 2 | Match System / Real World | 3 | Domain-correct French terminology fits the audience |
| 3 | User Control and Freedom | 1 | No cancel/exit; browser Back leaves the route entirely instead of stepping back; no save/resume |
| 4 | Consistency and Standards | 2 | `phone_b` has `type="tel"`, `phone_a` doesn't; no `type="email"` anywhere |
| 5 | Error Prevention | 1 | Zero inline validation; consent checkbox can pass required validation while unchecked (see below) |
| 6 | Recognition Rather Than Recall | 2 | No review/summary step before Confirmer — user must recall ~14 fields entered with no way to re-check |
| 7 | Flexibility and Efficiency of Use | 1 | No `autocomplete` anywhere, no shortcuts, nothing speeds up entry |
| 8 | Aesthetic and Minimalist Design | 3 | Clean, boxed fieldsets, on-brand, no visual noise |
| 9 | Error Recovery | 1 | No field-level errors; server errors surface only as a generic modal with no field pointer |
| 10 | Help and Documentation | 1 | No help text, no contact link in the error modal, no expectation-setting about payment |
| **Total** | | **16/40** | **Poor** |

## Design Specificity Verdict

**LLM assessment**: this review is grounded in provable source facts, not impressions — two confirmed functional bugs (below) and a real Angular validator gotcha, verified against the actual service/model code, not guessed.

**Deterministic scan**: clean across all three scanned directories (inscription page, inscription-form component, modal component) — 0 findings, exit 0 on all three. The detector's ruleset doesn't cover form-logic defects like the ones found here, which is exactly why the LLM pass matters on this surface.

**Visual overlays**: not available — no browser automation tool is connected this session. `curl` confirms the SPA shell serves under `/inscription` (HTTP 200), which only proves client-side routing works, not that the form itself renders/functions correctly.

## ⚠️ Confirmed functional bug: registrations are never sent to Stripe checkout

This was flagged by the design-review pass and then independently verified against the real service code before writing this report:

- `TeamService.createTeam()` returns `{ teamId, message }` (confirmed in `create-team-response.ts`) — exactly what `StripeService.redirectToCheckout(teamId)` needs.
- `StripeService.redirectToCheckout()` is fully implemented and correctly calls the real backend Stripe endpoint (`POST /api/stripe/create-checkout-session/{teamId}`), matching the documented backend flow.
- But `inscription-form.component.ts`'s `submit()` ignores the `next` callback's response entirely (`next: () => {...}`, no parameter) and never calls `this.stripeService.redirectToCheckout(...)`. `StripeService` is injected into the constructor and never used anywhere else in the file.
- Instead, on success it waits 2 seconds and hard-redirects to a **hardcoded external URL**: `https://yp.events/9f201d18-648c-44ab-9933-c4494c0b4afe/HYROX-POLICE-NATIONALE-54`.

**This means, as the code stands, every team that registers through this form is never routed to Stripe to pay.** Either `yp.events` has become the real intended payment/ticketing destination and the Stripe wiring is stale dead code that should be removed (along with any copy implying Stripe), or this is a genuine regression and Stripe should be wired back in. This needs a real answer from you before anything else on this surface — it's not a design nitpick, it's whether registrations are currently collecting payment at all.

## Overall Impression

The wizard structure (3 steps, ~18 fields split sensibly) is sound scaffolding, and the visual identity carries over cleanly from the landing page. But the surface fails at its actual job in several concrete, provable ways: it may not be routing anyone to payment, it gives zero feedback when a field is wrong, and a required consent checkbox can be bypassed after one interaction. This is an Operate-mode task-completion surface — the score (16/40) reflects genuine functional and usability gaps, not a subjective taste read.

## What's Working

- `<fieldset>`/`<legend>` used correctly for every radio group — a real accessibility foundation, not just default markup.
- Angular's `FormGroup` retains all step data when navigating Suivant/Retour — no data loss moving between steps (only on refresh/browser-Back, see Priority Issues).
- Checked-state contrast on the custom radio/checkbox styling is genuinely good against the dark fieldset background.

## Priority Issues

**[P0] Payment step appears to be bypassed** (see verified bug above)
- **Fix**: Confirm with yourself/backend whether `yp.events` is now the intended payment destination. If yes, remove the unused `StripeService` injection and update the modal copy so it doesn't imply an activation-email-then-Stripe flow that no longer happens. If no, wire `stripeService.redirectToCheckout(response.teamId)` into the `next` callback instead of the hardcoded URL.
- **Suggested command**: `/impeccable harden`

**[P0] No inline validation feedback anywhere**
- **Why it matters**: `next()` calls `markAllAsTouched()` but no template binding anywhere reads `.invalid`/`.touched` to show an error — the only signal on a bad field is a disabled button with zero explanation of which of up to 7 fields is wrong. Directly undermines "fast, low-friction registration."
- **Fix**: Bind visible error text under each field on `control.invalid && control.touched`; switch email fields to `type="email"`.
- **Suggested command**: `/impeccable clarify`

**[P1] Consent checkbox can be submitted unchecked**
- **What**: `subscribe: new FormControl('', Validators.required)` — `Validators.required` does not treat boolean `false` as empty (only `null`/`''`/`[]` do), so once the checkbox has been interacted with at all, the control can hold `false` and still pass validation.
- **Why it matters**: The checkbox is a data-usage consent statement — a submission that bypasses it is a consent-integrity gap, not just a UX nit.
- **Fix**: Change the validator to `Validators.requiredTrue`.
- **Suggested command**: `/impeccable harden`

**[P1] No stepper; browser Back exits the flow; refresh wipes everything**
- **Why it matters**: `currentStep` is a plain component field with no URL/route tie — Back navigates off `/inscription` entirely instead of stepping back one wizard step, and any reload resets to Step 1 with total data loss, no warning. Realistic for this product's actual use context (gym environment, shared phone, interruptions).
- **Fix**: Add a visible step indicator; consider tying step to a route param or persisting form state to `sessionStorage`.
- **Suggested command**: `/impeccable layout`

**[P2] No review/summary step + no submit-in-flight state**
- **Why it matters**: No recap of ~14 fields before Confirmer, right before a payment handoff — and Participant 1's email becomes the team's account login per the backend, so a mistyped teammate email here has real downstream consequences with nothing in this UI to catch it. Submit button also isn't disabled/spinner-ed during the request, allowing double-submission.
- **Fix**: Add a lightweight read-only summary before Confirmer; disable + spinner the submit button while in flight.
- **Suggested command**: `/impeccable polish`

**[P2] Copy typos and input-attribute inconsistencies**
- **What**: "prette"→"prête", "paricipants"→"participants" (both steps); `phone_a` missing `type="tel"` (present on `phone_b`); no `autocomplete` attributes anywhere.
- **Fix**: Fix typos, add `type="tel"` to `phone_a`, add `autocomplete` (`given-name`/`family-name`/`email`/`tel`) to the four identity fields.
- **Suggested command**: `/impeccable polish`

## Persona Red Flags

- **Jordan (first-timer)**: Mistypes an email on Step 1 — "Suivant" just stays grey, no indication which field is wrong.
- **Sam (accessibility-dependent)**: Fieldset/legend structure is a real plus for screen readers, but a disabled button has no `aria-describedby` explaining why; custom radio/checkbox focus states beyond `.input:focus` (which only targets text inputs) need rendered verification against the dark background.
- **Casey (mobile)**: 7-field screens with no progress bar make remaining effort hard to gauge; inconsistent `type="tel"`/missing `type="email"` means wrong keyboards; a backgrounded/reloaded tab silently wipes all progress.
- **Police-duo-on-one-phone (project-specific)**: No way to split the task (e.g. a link for Participant 2); combined with zero inline validation and no review step, a misheard/mistyped teammate email over gym noise reaches the server unflagged — and since that email becomes the team's login, this can lock a teammate out of activation entirely.

## Minor Observations

- `inscription.component.scss` is empty — the form isn't actually unstyled (the form component supplies its own centering/background), but the page wrapper has no independent layout control of its own, a coupling smell.
- The `<h1>` carries an inline `style="align-items: center;"` rather than living in the stylesheet.
- `openModal()`/`transactionStatus` support a `'none'` status that renders no icon — unused dead surface area.

## Questions to Consider

- Is `yp.events` genuinely the current payment/ticketing destination, superseding the Stripe integration `CLAUDE.md` describes? This determines whether the P0 fix is "remove dead Stripe code" or "wire Stripe back in" — very different fixes.
- Should Participant 1 and 2 be able to complete their halves independently, given the realistic "two officers, one phone" completion pattern?
- Is there a server-side duplicate-submission guard, given the client currently allows a double-click during the in-flight request?
