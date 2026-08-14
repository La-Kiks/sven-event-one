---
target: inscription form (4-step)
total_score: 32
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 3
timestamp: 2026-08-14T14-31-25Z
slug: ui-inscription-form-inscription-form-component-ts
---
Method: dual-agent (A: design review · B: detector+browser evidence)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|---|---|---|
| 1 | Visibility of System Status | 3 | Step-rail jump doesn't reset scroll; sessionStorage autosave is fully silent |
| 2 | Match System / Real World | 4 | Police-specific administration taxonomy and terminology throughout |
| 3 | User Control and Freedom | 3 | Strong step-rail/Modifier navigation; no full-form reset, no resumable link |
| 4 | Consistency and Standards | 4 | Chips, fields, radius, error color, focus rings match DESIGN.md tokens exactly across all 4 steps |
| 5 | Error Prevention | 2 | Phone field has zero format validation; no check that participant 1/2 emails differ despite participant 1 becoming the team login |
| 6 | Recognition Rather Than Recall | 4 | Step-4 recap restates every field before the irreversible action |
| 7 | Flexibility and Efficiency | 2 | No "same as participant 1" shortcut despite duos frequently sharing an administration |
| 8 | Aesthetic and Minimalist Design | 4 | Clean, one-step-at-a-time, yellow rationed to CTA/active states only |
| 9 | Error Recovery | 3 | Inline errors are clear and specific; error summary renders below the button that was just clicked, no auto-scroll to first invalid field |
| 10 | Help and Documentation | 3 | Persistent named-human contact info on every step; zero anticipatory help for the off-site Yurplan handoff |
| **Total** | | **32/40** | **Good (80%)** |

## Design Specificity Verdict

**LLM assessment**: Grounded, not generic. Load-bearing, audience-specific vocabulary throughout ("tenue d'intervention", a 7-value administration picker naming Gendarmerie/Militaire/Pénitentiaire/Police Municipale/Police Nationale/Pompier), a duo structure matching the real event format, and a named human organizer standing in for generic "support." The Tactical Briefing visual language is applied consistently to every control, not just marketing chrome.

**Deterministic scan**: `detect.mjs` returned zero findings on both source files (clean, exit 0).

**Visual overlays**: Browser-injected `detect.js` succeeded on this target. One finding: **all-caps-body** — `p.chip-caption` renders 38 characters of body text ("Participer en tenue d'intervention ? *") in `text-transform: uppercase`, tripping a length-based readability heuristic. Notably this did **not** show up in the static CLI scan of the same files — the live/rendered DOM check caught a text-length-dependent issue the static template scanner missed. Confirmed visually via the injected overlay. Mobile check (390px, step 1): pass, zero overflow. Step 4 (récapitulatif, reached by scripting steps 1-3): **fail**, 9px overflow — five `<dd>` elements in `.review-card__body`'s `1fr` grid column get pushed wide by an unbroken long token (a participant's email address), forcing the whole page into horizontal scroll at 390px. Root cause: no `min-width: 0`/`overflow-wrap` on the grid track.

## Overall Impression

This is the most disciplined implementation of the four surfaces — genuinely hard to hold consistent styling together across a stateful 4-step wizard with two duplicated participant sections, and it's held. The single biggest opportunity is the off-site payment handoff: this form creates the team record and then sends the visitor to an unfamiliar domain with almost no preparation, at the exact moment — the actual conversion — where trust is most fragile.

## What's Working

1. **Step-4 review with per-section "Modifier" deep-links** — textbook error-recovery/user-control, verified live to be pixel-accurate to entered data.
2. **Design-system adherence under real complexity** — chips, error states, focus rings match DESIGN.md tokens with zero drift across three data-heavy steps.
3. **Persistent human contact channel** — a named organizer's phone/email in the aside on every step, not a generic "Contact Support" link.

## Priority Issues

**[P1] No reassurance or state-clarity at the off-site payment handoff**
- **Why it matters**: This is the single highest-stakes moment in the product — the actual conversion event — and also where trust is most fragile (leaving a tightly-branded page for an unfamiliar `yp.events` domain with no warning). The team record is created in the backend *before* the redirect fires; if payment doesn't complete, nothing on-screen tells the visitor what state they're in.
- **Fix**: Name "Yurplan" in the step-4 warning copy (not just the post-submit modal), and add one line clarifying registration status if payment isn't finished.
- **Suggested command**: `/impeccable clarify`

**[P1] Mobile overflow at step 4 from unbroken long text (confirmed, 9px)**
- **Why it matters**: The récapitulatif's `<dl>`/`<dd>` grid has no wrap guard, so a real participant email address pushes the whole page into horizontal scroll on a 390px phone — on the one step where the visitor is reviewing before an irreversible action.
- **Fix**: Add `min-width: 0` to the grid track and `overflow-wrap: break-word` on `.review-card__body dd`.
- **Suggested command**: `/impeccable harden`

**[P1] Error summary and scroll position don't guide the user to the problem**
- **Why it matters**: The "Complète les champs manquants" message renders below the Continuer button the user just clicked, and step-rail jumps don't reset scroll — on step 1's 7-field/2-chip-group layout, a low missing field can go completely unseen.
- **Fix**: Move the summary above the action bar and/or auto-scroll to the first invalid control on failed `next()`.
- **Suggested command**: `/impeccable harden`

**[P2] Phone field has no format validation**
- **Fix**: Add a French phone pattern validator with an inline error message, matching the existing email validation mechanism.
- **Suggested command**: `/impeccable harden`

**[P2] No shortcut for shared answers between participants**
- **Fix**: Add an optional "Même administration que le participant 1" quick-fill on step 2.
- **Suggested command**: `/impeccable optimize`

## Persona Red Flags

**Jordan (first-timer)**: Lands on an unbranded `yp.events` domain with no prior mention of "Yurplan" until the post-submit modal — a plausible bailout point for a security-conscious audience. The "Version" chip (Courte/Longue) has zero explanatory copy, unlike Outfit/Category.

**Sam (accessibility)**: Chip/field/progressbar markup is genuinely above-average (`role="radiogroup"`, `aria-invalid`, `aria-describedby`, live `aria-valuenow` all confirmed). Real gap: nothing is `aria-live` — when a step change swaps the entire content, a screen-reader user gets zero announcement that the page changed or which step they're now on.

**Casey (mobile)**: Confirmed overflow at step 4 (see Priority Issues). Additionally, `.aside` (hero title, lead, step list, contact block) sits above the form card in DOM order and only becomes sticky at ≥860px — every "Retour" or step-rail jump on mobile drops the user above a full screen of hero content before the next step's fields, four times over one registration.

## Minor Observations

- One chip click required two taps to register the active state during live testing — possibly a tooling artifact, worth a human recheck rather than a confirmed bug.
- The step-3 consent checkbox's user-facing copy is a one-time data-use notice, but its payload maps to `acceptMails: true` for both players in the backend request — a copy/scope mismatch worth a second look.
- `autocomplete` attributes (`given-name`, `family-name`, `email`, `tel`) are correctly wired throughout.

## Questions to Consider

1. Registration draft lives only in `sessionStorage`, gone the instant the tab closes — for a form estimated at "4 minutes" that in practice requires gathering a second person's details, why isn't there a resumable draft?
2. A team record exists in the backend before Yurplan is ever reached — should the review screen say what happens if payment doesn't complete, at the exact moment trust is most fragile?
3. The chip pattern was clearly built accessibility-first at the control level — was that investment matched at the step-transition level, or does the polish stop at the control?
