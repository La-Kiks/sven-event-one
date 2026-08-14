---
target: mon-équipe
total_score: 30
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 3
timestamp: 2026-08-14T14-32-08Z
slug: ui-src-app-pages-my-team-my-team-component-ts
---
Method: dual-agent (A: design review · B: detector+browser evidence)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|---|---|---|
| 1 | Visibility of System Status | 3 | Status tiles are excellent; save confirmation renders at the top of the page while Save is a bottom sticky button — easy to miss |
| 2 | Match System / Real World | 3 | Domain vocabulary fits; team's derived category (man/woman/mixt) is computed but never shown back to the user |
| 3 | User Control and Freedom | 2 | No "discard changes"/reset; only escape from a bad edit is a full reload |
| 4 | Consistency and Standards | 3 | Token-perfect on colors/type/radius; header is missing `flex-wrap`, the exact regression DESIGN.md documents as previously fixed elsewhere |
| 5 | Error Prevention | 3 | Real-time required-field validation disables Save proactively; phone fields have zero format validation |
| 6 | Recognition Rather Than Recall | 4 | Labels always visible, active chips clearly highlighted |
| 7 | Flexibility and Efficiency | 2 | All ~14 fields across 3 cards always open, no per-section save or read-only summary view |
| 8 | Aesthetic and Minimalist Design | 3 | Clean overall; "Compte" tile's hint text doesn't describe account status, breaks the pattern "Paiement" sets |
| 9 | Error Recovery | 2 | Clearing a field silently disables Save with no message identifying which field, no scroll-to-error |
| 10 | Help and Documentation | 4 | Excellent in-place contextual help exactly where needed (unpaid-state hint, locked-email hint) |
| **Total** | | **30/40** | **Good (75%)** |

## Design Specificity Verdict

**LLM assessment**: Highly specific, not generic. Faithfully implements the Tactical Briefing system — void-black ground, rationed hazard-yellow, the filet-grid status band, Lemon Milk/Cabin pairing, and the exact locked-field/status-badge tokens documented in DESIGN.md. Nothing reads as a default form template.

**Deterministic scan**: `detect.mjs` returned zero findings on both source files (clean, exit 0).

**Visual overlays**: Browser-injected `detect.js` was blocked by the harness's permission classifier — no overlay evidence obtained for this target, a genuine gap (as on landing). Mobile check (390px, same-origin iframe): **pass** — but the underlying cause is fragile, not structural. Both agents independently confirmed from source that `.header` has no `flex-wrap` declared (defaults to `nowrap`), the identical pattern DESIGN.md names as a shipped-and-fixed bug elsewhere ("treat any un-wrapped flex header as a bug, not a style choice"). At 390px with the test account's short email it currently fits with 16px to spare and doesn't clip — but there's no wrap fallback and no truncation on the email span, so a longer real username has nowhere to go.

## Overall Impression

The page's hardest job — telling an anxious "did my payment go through" participant the truth clearly — is handled well. The unpaid-state copy is the single best-executed piece of reassurance across all four surfaces reviewed. The biggest opportunity is that the page defaults to full-edit mode for all 14 fields on every visit, when its own stated job (per PRODUCT.md, an Operate surface) is usually "check my status," not "re-fill my registration."

## What's Working

1. **Unpaid-status copy** — turns "I paid but it says unpaid" into a handled moment: explicit 48h timeframe plus two direct contact links, inside the alarming red tile itself.
2. **Locked participant-1 email field** — visually distinct with copy explaining why it's locked and how to actually change it.
3. **Real-time reactive validation** — clearing a required field instantly disables Save via `form.invalid`, before any failed round-trip.

## Priority Issues

**[P1] Header has no `flex-wrap` — the same regression DESIGN.md documents as previously shipped and fixed on the admin pages**
- **Why it matters**: Both agents independently confirmed from source that `.header { display: flex; align-items: center; gap: 1rem; }` has no wrap fallback. It currently passes at 390px only because the test account's email happens to be short enough — a longer real username has no truncation or wrap to fall back to and could push "Se déconnecter" off-screen, unreachable, exactly like the admin bug fixed earlier this session.
- **Fix**: Add `flex-wrap: wrap` to `.header` in `my-team.component.scss`, matching the fix already applied to `teams.component.scss`/`players.component.scss`.
- **Suggested command**: `/impeccable harden`

**[P1] Save feedback isn't proximate to the action**
- **Why it matters**: The success/error banner renders at the top of the document while Save is a `position: sticky; bottom: 0` button — on this page, whose entire job is unambiguous status communication, the one message tied to the user's own action is the one most likely to be missed.
- **Fix**: Surface confirmation/error near the sticky bar itself, or a transient "Enregistré ✓" state on the button.
- **Suggested command**: `/impeccable polish`

**[P1] Disabled Save gives no explanation when the invalid field is off-screen**
- **Why it matters**: Clearing a required field disables Save immediately with no message identifying which field is the problem — directly undermines Error Recovery on the page's main interaction.
- **Fix**: Surface which field is blocking, or scroll/focus the first invalid control on a blocked Save attempt.
- **Suggested command**: `/impeccable harden`

**[P2] "Compte" status tile's hint doesn't describe the Compte state**
- **Fix**: Give Compte its own conditional hint mirroring Paiement's structure; move the edit-window note to page-level copy.
- **Suggested command**: `/impeccable clarify`

**[P2] No chunking for a page built for repeat, low-friction visits**
- **Fix**: Default to a read/summary view per section that expands to edit on demand.
- **Suggested command**: `/impeccable layout`

## Persona Red Flags

**Sam (accessibility)**: The three text-input error paragraphs get proper `id`/`aria-describedby` wiring, but chip-group errors (`team.version`, `player1/2.category`, `player1/2.outfit`) render a bare `<p class="field-error">` with no `id` and nothing on the `role="radiogroup"` wrapper pointing at it — a screen-reader user tabbing through the visually-hidden radios never hears the validation message sighted users see.

**Casey (mobile)**: See the header flex-wrap finding above (P1) — plus the sticky save bar permanently consumes vertical space on an already-short viewport.

**Riley (stress-tester)**: Rapid double-submit is properly guarded. But phone fields have zero format validation (`Validators.required` only) — "asdf" saves as a valid phone number, a real problem for a field organizers use to reach participants.

## Minor Observations

- The header brand mark links to `href="#top"`, but no element carries `id="top"` — the link is dead.
- Team category (man/woman/mixt) is computed server-side but never displayed back on this page, only version + administration pills.
- `getAdminLabel('none')` maps to "Autre" — a small naming mismatch in code, invisible to users.

## Questions to Consider

1. If "I paid but it says unpaid" is the scariest moment on this page, why does a routine field-edit confirmation get more durable visual presence than the reassurance a worried unpaid user needs?
2. This page always renders in full-edit mode — is "edit" really the default a returning participant wants on a page framed as "check status," or would a read-first summary reduce accidental half-finished edits?
3. DESIGN.md already documents the unwrapped-flex-header bug as a known, previously-fixed regression on another screen — why did the same gap ship here too, and is there a shared header component that would prevent a third recurrence?
