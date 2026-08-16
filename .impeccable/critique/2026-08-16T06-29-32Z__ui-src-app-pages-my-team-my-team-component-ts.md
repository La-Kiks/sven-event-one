---
target: my-team
total_score: 25
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 2
timestamp: 2026-08-16T06-29-32Z
slug: ui-src-app-pages-my-team-my-team-component-ts
---
Method: dual-agent (A: a939aa6707661d958 · B: a95c22c8e94bd2e46)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3 | Save flow gives clear feedback, but the `Administration` select silently shows no selection for a field that has a real, saved value |
| 2 | Match Between System and Real World | 4 | Police/gendarmerie-specific terminology throughout; domain-fluent copy |
| 3 | User Control and Freedom | 2 | No cancel/revert-to-saved control; no unsaved-changes guard before navigating away |
| 4 | Consistency and Standards | 3 | Follows DESIGN.md's card/badge/filet patterns well, but chips fall short of the system's own 44px touch-target rule |
| 5 | Error Prevention | 2 | The unmatched-administration-value case isn't prevented or flagged — silently absorbed |
| 6 | Recognition Rather Than Recall | 2 | Locked email field shows its value + why; Administration field hides a real value behind a blank select |
| 7 | Flexibility and Efficiency of Use | 1 | No shortcuts, no autosave, no bulk action — zero accelerators exist |
| 8 | Aesthetic and Minimalist Design | 3 | Clean, on-system, well-chunked; not cluttered |
| 9 | Help Recognize/Diagnose/Recover from Errors | 3 | Field-level errors are plain-language and adjacent to fields; generic save-error fallback is vague |
| 10 | Help and Documentation | 2 | Payment/lock hints double as contextual help; no general help path |
| **Total** | | **25/40** | **Acceptable** |

Scored as a full Operate surface — heuristics 7 and 10 were not exempted.

## Design Specificity Verdict

**LLM assessment**: Genuinely authored, not a reskin — with one glaring inconsistency against its own system. The strongest specificity signals: the payment-status hint copy references the product's real operational quirk (external Yurplan payment sync lag, "peut mettre 48h à s'actualiser," with the actual organizer's phone/email as an escape hatch), and the `Administration` option list (Gendarmerie, Police Municipale, Pénitancier, Pompier...) and "Tenue d'intervention" outfit-loan options are written for this exact police/law-enforcement audience — no generic placeholder copy anywhere. Where it slips into generic-form territory: the edit form is structurally identical to a typical "profile settings" page, and the chip components measurably fall short of this project's own documented touch-target spec — the code drifting away from a system that was written specifically for this product.

**Deterministic scan**: `detect.mjs --json ui/src/app/pages/my-team` returned exit code 0 with zero static findings. The live browser-injected detector (via `live-server.mjs` + `detect.js` against the running `/mon-equipe` page) found **1 anti-pattern**: a "hero-eyebrow-chip" — the tracked-caps eyebrow "Mon équipe" sitting above the h1 team name (`div.page-head__eyebrow`) — a generic, category-interchangeable pattern the LLM review didn't flag on its own. This is a genuine case of the detector catching something the unanchored design review missed.

**Visual overlays**: The live-injected overlay is not currently visible (the live-server instance used for injection was stopped after evidence collection, per the critique protocol). The console evidence: `[impeccable] 1 anti-pattern found` → `hero-eyebrow-chip` on `div.page-head__eyebrow`.

**Process caveat**: Both sub-agents independently reported a tab-isolation glitch — Assessment B's freshly created tabs kept disappearing and it fell back to reusing the one surviving tab, and Assessment A observed an unprompted title change ("IMPECCABLE-PREFLIGHT-TEST") and an orange annotation box appear mid-session, consistent with B's injection landing in A's tab. Assessment A confirmed it did not act on anything in the injected content and re-navigated before continuing. Findings from both are still reported as-is since each agent flagged the issue transparently rather than silently producing contaminated output, but full tab isolation between assessments could not be guaranteed this run.

## Overall Impression

The page's copy is the strongest evidence of real design authorship on this whole site — the payment-status reassurance and police-specific terminology are exactly right. But the actual data-binding has a real bug hiding behind good visual design: a required field can render blank while holding a real, saved value, with zero visual signal that anything is wrong. That's the single biggest opportunity here — not a visual polish pass, but closing the gap between "looks handled" and "is handled" on the Administration field.

## What's Working

1. **Payment-status hint copy** — turns a scary "NON PAYÉ" flag into a reassuring, specific explanation (48h Yurplan sync delay + organizer's actual phone/email). Empathetic, product-aware copywriting, not boilerplate.
2. **Locked email field pattern** — readonly styling paired with inline "why is this locked and who do I contact" copy correctly implements DESIGN.md's Locked Field Surface spec and prevents a confused-user dead end.
3. **Domain-specific option sets** — Administration and "Tenue d'intervention" outfit-loan chips are written for this exact audience, not generic placeholders.

## Priority Issues

**[P1] Administration select silently discards visibility of a real stored value**
- **Why it matters**: Live-verified: the test team's `administration` value ("police test") doesn't match any of the 7 fixed `<option>` values. `document.querySelector('#administration').value` returns `""` with no option marked `selected`, even though the header pill correctly shows "POLICE TEST" from the same underlying data. `Validators.required` still passes and Save succeeds with no warning — no data is lost, but the user has zero visual confirmation of their real setting on a required field at the top of the first card. This hits exactly the audience (cross-service duos, values not on the 7-item list) most likely to encounter it.
- **Fix**: When `team.administration` doesn't match a listed option, either inject a hidden `<option [value]="team.administration" selected>` mirroring the raw value, or fall back visibly to "Autre" with the stored text shown alongside it — never let a populated required field render as blank.
- **Suggested command**: `/impeccable harden`

**[P1] Chips are ~37px tall, under the design system's own 44px minimum touch target**
- **Why it matters**: Measured live via `getBoundingClientRect()`: `.chip` height is 37.19px (padding `9.6px 16px`, font 14.08px). DESIGN.md explicitly states chips need a "Minimum 44px touch target." This affects all 14 chip targets on the page (version ×2, category ×2 players ×3, outfit ×2 players ×3) — exactly the quick, one-handed taps a participant makes when editing on a phone.
- **Fix**: Increase `.chip` vertical padding or set `min-height: 44px` with centered content.
- **Suggested command**: `/impeccable harden`

**[P2] Field-hint text fails WCAG AA contrast on the page's most-needed explanatory copy**
- **Why it matters**: `.field-hint` (the locked-email explainer) is `rgba(255,255,255,0.4)` at 12.8px — computed contrast ≈3.96:1, below the 4.5:1 AA minimum. This is the exact copy a confused user needs to read to understand why a required-looking field won't accept input.
- **Fix**: Promote `.field-hint` to the higher-contrast secondary-text tier (0.55 opacity, ~6.2:1), matching the payment-tile hint.
- **Suggested command**: `/impeccable harden`

**[P2] No unsaved-changes protection**
- **Why it matters**: A participant can edit several fields and then click the header brand link or "Se déconnecter" — both sit directly beside the form — with zero warning that changes are unsaved, and no "discard/revert" control short of a full reload.
- **Fix**: Add a `CanDeactivate` guard / `beforeunload` confirm when the form is dirty and unsaved, and/or a visible "Annuler" ghost button that resets to the loaded team.
- **Suggested command**: `/impeccable harden`

**[P3] Generic save-error fallback undersells the page's otherwise-specific copy**
- **Why it matters**: "Une erreur est survenue lors de l'enregistrement. Réessayez." doesn't distinguish network failure from server rejection from session expiry, in contrast to the specificity everywhere else on this page.
- **Fix**: Branch the message (network vs. server) and word each explicitly.
- **Suggested command**: `/impeccable clarify`

## Persona Red Flags

**Jordan (occasional/returning editor)**: Hits the blank-looking `Administration *` select on first load and reasonably assumes the page lost their data — nothing distinguishes "field is empty" from "field has a value the UI can't display." The reassurance copy on payment status is good but doesn't close a similar mental gap for a first-time confused reader.

**Sam (accessibility-dependent)**: `.field-hint` contrast (≈3.96:1, verified) fails AA exactly where explanatory text matters most. Chips measure 37px tall (verified), under the 44×44 target this system itself specifies, making selection harder to hit for anyone with reduced precision. Positive baseline: `aria-invalid`/`aria-describedby` are correctly wired on every validated field, and focus-visible (hazard-yellow outline) was confirmed live via keyboard Tab.

**Casey (mobile, quick status check)**: The far more common visit to this page — "did my payment clear" — requires loading the entire editable form before reaching the answer; the status band isn't the first thing rendered. True mobile-viewport rendering could not be confirmed live this session (see Minor Observations) — conclusions rest on static SCSS review only.

## Minor Observations

- The live-injected detector flagged one anti-pattern the LLM review didn't independently catch: a generic "hero eyebrow chip" (tracked-caps label above the h1) on `div.page-head__eyebrow` — worth a look during a specificity/polish pass even though it's minor.
- The "Version courte/longue" pill uses a raw ternary — any third value would silently default to "Version longue" with no fallback indicator, the same latent-bug class as the Administration field, just less visible since it's a read-only badge.
- `.retry-btn` duplicates `.btn-primary`'s styling via a separate rule instead of reusing the class.
- The sticky save bar's fade hardcodes pure black; fine today, fragile if this component is ever placed on a non-void-black surface.
- **Mobile-viewport verification gap**: neither sub-agent could get a true browser resize in this environment (`resize_window` reported success but `window.innerWidth` stayed pinned at 1280px in both runs). Mobile conclusions above rest on static SCSS review (no component-local `@media` rules; relies on `auto-fit`/`flex-wrap` per DESIGN.md's documented strategy), not live visual confirmation.

## Questions to Consider

- The biggest, boldest element on the page is the participant's own (possibly joke) team name — is that the right visual peak for an "Operate" screen whose real job is confirming payment/account state?
- Given the audience already includes cross-service duos, should "Administration" stay a closed 7-option list at all, or does it need an actual "Autre: ___" free-text fallback?
- If the most common visit to this page is "just check if I'm paid," does it need to expose all ~20 editable fields by default, or would a read-only summary with an explicit "Modifier" toggle serve better?
