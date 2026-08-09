---
target: inscription page
total_score: 25
max_score: 40
na_heuristics: 
p0_count: 1
p1_count: 1
timestamp: 2026-08-08T17-39-33Z
slug: src-app-pages-inscription-inscription-component-ts
---
Method: dual-agent (A: a9f46acfe5a517e10 · B: a2e09898a10c2babd)

#### Design Health Score

Operate-mode surface (task completion, money-critical) — all 10 heuristics scored for real.

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3 | Stepper, query-param-synced step, and a spinner + "Envoi en cours…" on submit are all present and correct now. Loses a point because the disabled Confirm/Next button gives zero status on *why* it's disabled for radio/select fields. |
| 2 | Match System / Real World | 3 | Domain-correct French copy fits the police/gendarmerie audience; loses a point for a "tu" (step copy) vs "vous" (modal/review copy) register wobble right before payment. |
| 3 | User Control and Freedom | 3 | Browser Back and refresh now preserved via `sessionStorage` + `?step=` query param; step-edit links from the review step work. No way to explicitly abandon/clear a draft. |
| 4 | Consistency and Standards | 2 | Field-level styling is consistent, but there's an architecture-level inconsistency: the review step's own copy commits to an external "billetterie partenaire" flow the app's own Stripe infrastructure was built to replace. |
| 5 | Error Prevention | 2 | Inline validation now works for text/email fields, but Next/Confirm buttons are `[disabled]` while the step is invalid, which prevents Angular from ever firing `(click)` — so `markAllAsTouched()` can never run for a skipped radio group or select. |
| 6 | Recognition Rather Than Recall | 3 | Step 4 review is a genuine strength — full recap with per-section edit links, no memory burden. |
| 7 | Flexibility and Efficiency of Use | 2 | No save-and-resume-via-link for the second player's half of the form; no indication anywhere that team category is auto-derived rather than chosen. |
| 8 | Aesthetic and Minimalist Design | 3 | On-brand yellow/black palette, clean fieldset grouping; live scan flagged a thin type scale (1.8:1 largest:smallest) and a low-contrast disabled-button state, both real but low-severity. |
| 9 | Error Recovery | 3 | Specific messages ("Adresse mail invalide.", server error passthrough via `onError(err.error?.error)`); loses a point for the same unreachable-fieldset gap as #5. |
| 10 | Help and Documentation | 1 | Still no inline help text anywhere (event-specific jargon, why an administration/category is asked, what happens after payment) — unchanged from the prior critique. |
| **Total** | | **25/40** | **Improved, still incomplete** |

#### Design Specificity Verdict

Grounded and provable on both sides, not impressionistic. Assessment A verified the P0 finding by reading the live file *and* running `git log -p` on it — establishing that a working `StripeService.redirectToCheckout(response.teamId)` call existed in an earlier revision, was later commented out, and the most recent "fix" commit (55b3026) removed the `StripeService` injection entirely and rewrote the success/review copy to narrate the Yurplan redirect as intentional design rather than restoring the Stripe call. That is a materially stronger, more specific finding than "this bug is still present" — it shows the bug was actively re-entrenched by the very commit meant to harden this flow. Assessment B's CLI scan (`detect.mjs`) came back clean (0 findings, exit 0) across all four files, which is expected — the detector's static ruleset doesn't cover form-logic/architecture defects like the payment bypass — but its live browser injection caught two concrete, DOM-verified issues (gray-on-yellow disabled-button contrast, a 1.8:1 type-scale ratio) that a code read alone wouldn't surface. Independently, the orchestrating pass re-read `inscription-form.component.ts` and `stripe.service.ts` directly and confirms both agents' core claim byte-for-byte: `StripeService` is defined, fully correct, and referenced nowhere outside its own file.

#### Overall Impression

This is a substantially better form than the version critiqued on 2026-08-07 (16/40 → 25/40): inline validation, a real stepper, `sessionStorage` persistence across refresh/Back, a proper review step, and `Validators.requiredTrue` on consent are all implemented correctly and verified live. But the single most consequential finding from the prior pass — that this flow never reaches this app's own Stripe checkout — is not only still true, it has been made harder to notice: the "fix" commit polished the review-step copy to explicitly promise a redirect to "notre billetterie partenaire," normalizing the bypass in the UI rather than closing it in the code. Every team that completes this form today pays on an unrelated external ticketing site with no link back to this system's `MarkTeamAsPaidAsync`/webhook/payment-status infrastructure. A second, more subtle regression of the *same underlying category* (error prevention on non-text fields) was also found: disabling the Next/Confirm button while a step is invalid means `markAllAsTouched()` — the mechanism the prior fix added specifically to surface validation errors — can now never execute via click, so a user who skips a radio group or select still hits a wall with no diagnostic.

#### What's Working

- The stepper (synced to `?step=` query param), `sessionStorage`-backed draft persistence, and the step-4 review-with-edit-links are all real, correctly wired, and verified live — this is the right shape for a duo, pre-payment form.
- Participant 1 vs Participant 2 separation is unambiguous both structurally (nested `FormGroup`s) and visually (large headers + stepper position).
- Inline per-field validation now works as designed: blur-triggered errors, specific copy ("Adresse mail invalide.", "Merci de choisir une catégorie."), and server error passthrough on submit failure.

#### Priority Issues

**[P0] Team registration still bypasses this app's own Stripe checkout — and the latest fix commit entrenched it further**
- **Why it matters**: `inscription-form.component.ts` (submit's `next` callback, ~line 201-214, and `url`/`onSuccess()` at ~206-208, 221, 238) hardcodes `window.location.href` to `https://yp.events/9f201d18-648c-44ab-9933-c4494c0b4afe/HYROX-POLICE-NATIONALE-54` after team creation. `StripeService.redirectToCheckout(teamId)` (`ui/src/app/services/stripe.service.ts:16`) correctly POSTs to this app's own `/api/stripe/create-checkout-session/{teamId}` and is never imported or injected in the form component. Git history shows this call existed previously and was removed, and commit 55b3026 rewrote the review/success copy to present the Yurplan hand-off as intended design rather than restoring the Stripe call. This means the backend's `StripeController`, webhook, `MarkTeamAsPaidAsync`, and the `payment-success`/`payment-cancel` pages are all dead code on the only path meant to invoke them — an admin has no automated way to reconcile who actually paid.
- **Fix**: Confirm with the project owner whether Yurplan is now the genuine, intended payment destination (in which case remove the unused `StripeService`/webhook wiring and update `CLAUDE.md`) or whether this is unfinished migration debt (in which case wire `this.stripeService.redirectToCheckout(response.teamId)` into the `next` callback and drop the hardcoded URL and its copy).
- **Suggested command**: `/impeccable harden`

**[P1] Disabled Next/Confirm buttons make `markAllAsTouched()` unreachable for radio/select fields**
- **Why it matters**: Each step's advance button is bound `[disabled]="form.get('stepN')?.invalid"`. A disabled button never fires Angular's `(click)`, so `next()`'s `else { stepGroup?.markAllAsTouched() }` branch — added specifically to surface validation state — can never execute from a click. A user who fills every text input but never focuses into (and out of) a required radio fieldset or select (category, outfit, version, administration) is left with a permanently grayed-out button and zero explanation of what's missing. This is the same "error prevention" defect flagged in the prior critique, now present via a different mechanism.
- **Fix**: Either keep the button always enabled and call `markAllAsTouched()` + scroll-to-first-invalid-field on click, or keep it disabled but add a persistent hint ("Complète les champs en rouge ci-dessus") tied to `stepGroup.touched && stepGroup.invalid`.
- **Suggested command**: `/impeccable clarify`

**[P2] Disabled-button contrast and thin type scale (live-verified)**
- **Why it matters**: Browser injection found `#808080` text on `#ffed00` background on the disabled Suivant button (real DOM values, not simulated) — legible but low-contrast, making it harder for low-vision users to tell disabled-vs-enabled state at a glance. Separately, the live page's content font sizes (17.6px inputs, 20px form/stepper text, 24px button, 32px H1/H2) form only a 1.8:1 largest:smallest ratio, a thin scale for establishing clear hierarchy on a page whose H1 is meant to anchor trust before payment.
- **Fix**: Darken the disabled-state text or lighten/desaturate the disabled background to raise contrast; widen the type scale (e.g. push H1 larger or body text smaller) so heading vs. body reads unambiguously at a glance.
- **Suggested command**: `/impeccable polish`

**[P2] No scroll-to-top / focus management on step transition**
- **Why it matters**: Verified live — advancing from the bottom of a step (where the Next button lives) does not reset scroll position, so the next step can render with its "Participant 2" heading and stepper off-screen above the fold. For a form already asking users to track which of two people's data they're on, losing the heading on transition adds avoidable disorientation.
- **Fix**: Scroll the step container (or `window`) to top, or move focus to the new step's heading, on every `goToStep()` call.
- **Suggested command**: `/impeccable polish`

**[P3] Residual copy defects and register inconsistency**
- **What**: Missing accent ("Selectionne" → "Sélectionne"), "Pénitancier" → "Pénitentiaire", "dans la cadre" → "dans le cadre", "Paiment" persists in both `payment-success` and `payment-cancel` pages; step copy uses "tu" while modal/review copy switches to "vous". The review step also never surfaces the server-derived team category (Homme/Femme/Mixte) the two players' answers will produce, so a user pays without seeing what they'll actually compete as.
- **Fix**: Copy pass across both components and the payment-success/cancel pages; add the derived category to the step-4 review summary.
- **Suggested command**: `/impeccable polish`

#### Persona Red Flags

- **Jordan (first-timer)**: Hits the P1 issue directly — fills the three text inputs per step, skips a radio group because it doesn't read as visually mandatory, and is stuck on a grayed-out button with no diagnostic. No inline help explains event-specific jargon ("tenue d'intervention," administration categories) either.
- **Sam (accessibility)**: The success/error modal has no `role="dialog"`/`aria-modal`/Escape handler (unchanged from before); the disabled-button gray-on-yellow contrast (P2, live-verified) adds a second, concrete accessibility gap on top of that.
- **Riley (stress-tester)**: Would catch the P0 bypass fast — either by inspecting network requests during submit or simply by reading the review-step copy closely ("billetterie partenaire" on an app that clearly has its own Stripe branding elsewhere via payment-success/cancel pages). For a registration form explicitly restricted to police/law-enforcement personnel, an unbranded off-domain payment hand-off right after submitting names and phone numbers is exactly the kind of trust signal this persona is primed to distrust.

#### Minor Observations

- `.button-group` renders two `width:100%` buttons in a flex row without `flex:1` — works today via default shrink behavior but is fragile if label length changes.
- Field order in the entry form (Nom then Prénom) doesn't match the review step's display order (Prénom + Nom concatenated) — small, avoidable inconsistency.
- `inscription.component.scss` remains empty and the page wrapper (`inscription.component.html`) is a single-line pass-through to the form component — the page has no independent layout control, same coupling noted in the prior critique.
- CLI static scan (`detect.mjs`) is clean (0 findings) across all four files — expected, since none of the issues here are the kind of anti-pattern a static ruleset catches.

#### Questions to Consider

1. If Stripe's webhook, `MarkTeamAsPaidAsync`, and the payment-success/cancel pages all exist and work, why does the one flow meant to trigger them still not call `StripeService` — three commits in, with the most recent one rewriting copy to justify the bypass rather than close it? Is there an undocumented business reason (e.g. a live Yurplan contract) that should be reflected in `CLAUDE.md`, or is this unfinished work being re-justified instead of finished?
2. Given this form explicitly targets police/gendarmerie/military registrants, does an unbranded third-party payment redirect — immediately after collecting names and phone numbers — meet the trust bar this specific audience expects, versus staying on-domain with the already-built Stripe checkout?
3. Now that per-field inline validation exists, is disabling the primary CTA still earning its keep, or would an always-enabled button + `markAllAsTouched()` + scroll-to-first-error serve every field type (not just text inputs) better?
