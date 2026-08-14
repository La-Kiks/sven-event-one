---
target: admin panel (teams+players)
total_score: 27
max_score: 36
na_heuristics: 10
p0_count: 0
p1_count: 1
timestamp: 2026-08-14T14-32-53Z
slug: ui-src-app-pages-teams-teams-component-ts
---
Method: dual-agent (A: design review · B: detector+browser evidence)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|---|---|---|
| 1 | Visibility of System Status | 3 | Toggles/badges/KPIs update live and accurately; the "En attente" KPI tile measures account-activation, not payment, but sits next to "Payées" |
| 2 | Match System / Real World | 3 | Correct French admin terminology; same KPI-label ambiguity bleeds in |
| 3 | User Control and Freedom | 3 | Backdrop-click and X both close the panel, cancel exists on every confirm; no Escape key anywhere |
| 4 | Consistency and Standards | 2 | Teams' sortable headers lack the keyboard/ARIA support Players' identical pattern has; border-radius drifts to 4px/3px against the documented 2px token in 6 places, inconsistent even within one file |
| 5 | Error Prevention | 3 | Delete and payment-toggle are properly gated behind explicit confirms; bulk-send confirm reuses the destructive-red pattern for a routine action, diluting that signal |
| 6 | Recognition Rather Than Recall | 3 | Color-coded badges are scannable; "En attente" tile forces recall of what it actually tracks |
| 7 | Flexibility and Efficiency | 3 | Deep-link from Players→Teams, filter-aware CSV export, bulk activation email, live search; Teams table cannot be sorted by payment status, arguably the most-used daily sort |
| 8 | Aesthetic and Minimalist Design | 3 | Tight, restrained yellow, good density; radius drift and a stray hover-lift break the flat/no-lift language |
| 9 | Error Recovery | 4 | Every mutating action has its own scoped error state with a specific retry message; bulk-send reports per-team failure reasons |
| 10 | Help and Documentation | n/a | Reasonably n/a for a 1-2-person internal tool per PRODUCT.md's own stated principle |
| **Total** | | **27/36** | **Good (75%)** |

## Design Specificity Verdict

**LLM assessment**: A well-executed instance of the Tactical Briefing system on the surface most likely to slip under time pressure — and it mostly doesn't. The filet KPI band, hairline-bordered tables, status-badge language, and flat depth model all match DESIGN.md's tokens closely. But there are concrete, source-verified deviations from the system's own explicit rules: a rogue 4px/3px radius across six components against a documented "never 4px, 8px, or 0" Don't, a `transform: scale` hover against the No-Lift Rule, and a functional inconsistency between the two pages of the *same* surface — Players' sortable headers are keyboard-accessible with `aria-sort`; Teams' visually-identical headers have neither.

**Deterministic scan**: `detect.mjs` returned zero findings on all four source files. Browser-injected `detect.js` was blocked on `/teams` (permission classifier) but succeeded on `/players`, surfacing two additional findings not caught by the static scan: **radial-spotlight-glow** on `.players-page` (a `rgba(255,237,0,0.07)` radial gradient — plausibly an intentional brand accent matching the hazard-yellow system, not confirmed either way against `.teams-page` which has no equivalent), and **cramped-padding** on `.table-wrapper` (children sit flush against the border with no inset — `/teams` shares the identical structure but wasn't confirmed via overlay since injection was blocked there).

**Visual overlays**: Mobile check (390px): **pass** on both `/teams` and `/players`, zero horizontal overflow — the header flex-wrap fix from earlier this session was independently re-verified working.

## Overall Impression

For the core organizer loop — scan teams, open one, check payment, resend activation — this feels fast and confident, with the best-scoring Error Recovery of any surface reviewed. The single biggest opportunity: the admin surface has quietly drifted from its own design system's explicit rules in enough small places (radius, hover motion, an unstyled danger-red confirm reused for a safe action) that it reads as "the system's second-class citizen" next to the more polished landing/inscription surfaces, despite functioning well.

## What's Working

1. **Payment-toggle-in-panel pattern** — click the badge → inline confirm → optimistic update to panel and KPI band without a reload. Exactly the right friction for a frequent, reversible, high-stakes-adjacent edit.
2. **Deep-linking discipline** — clicking a team pill on `/players` navigates to `/teams?teamId=X`, opens the correct panel, and cleans the URL. A real cross-page continuity win most admin tools skip.
3. **Error recovery granularity** — every mutating action has its own specific, actionable error state; bulk-send reports per-team pass/fail with the actual error string.

## Priority Issues

**[P1] Teams' sortable headers are not keyboard-operable and lack `aria-sort`, unlike Players' identical pattern**
- **Why it matters**: A keyboard-only or screen-reader user can sort Players but cannot reach or trigger sorting on Teams at all — a full workflow gap on the same admin tool, not a cosmetic inconsistency.
- **Fix**: Port `players.component.ts`'s `onSortKeydown`/`getAriaSort` and the matching template attributes onto `teams.component.html`'s five sortable headers.
- **Suggested command**: `/impeccable harden`

**[P2] Border-radius drifts to 4px/3px in six components against DESIGN.md's own "never 4px, 8px, or 0" rule**
- **Why it matters**: DESIGN.md names this exact mistake as a Don't by number — this isn't a judgment call, it's drift from a rule the system explicitly flags, and it's inconsistent even within one file (`teams.component.scss`'s own `.table-wrapper` correctly uses 2px while `.info-section`/`.player-card`/`.pulse-row` in the same file don't).
- **Fix**: Sweep both SCSS files, replace 4px/3px radii with `{rounded.sharp}` (2px) per the Do's/Don'ts section.
- **Suggested command**: `/impeccable audit`

**[P2] Bulk-activation-email confirm reuses the destructive/danger-red pattern for a routine, reversible action**
- **Why it matters**: Sending an activation email is safe and resendable; using the same visual severity as an irreversible delete either causes needless hesitation on a harmless daily action, or dulls the red = irreversible signal by the time the organizer hits an actual delete.
- **Fix**: Give the bulk-send confirm the payment-toggle confirm's non-danger treatment (surface-raised background, hazard-yellow confirm button) already used a few lines away in the same file.
- **Suggested command**: `/impeccable clarify`

**[P3] KPI band's "En attente" tile is ambiguous next to "Payées"**
- **Why it matters**: It counts unverified accounts, not pending payments, but visually paired next to "Payées" it reads as payment-pending to a fast-scanning daily organizer. In the current 2-team test data the unpaid and unverified teams coincidentally match, which would make the mislabeling invisible until real data breaks the coincidence.
- **Fix**: Relabel to "Comptes en attente" or move it away from adjacency with "Payées."
- **Suggested command**: `/impeccable clarify`

**[P3] Payment-toggle badge hover uses `transform: scale(1.05)`, violating the documented No-Lift Rule**
- **Why it matters**: DESIGN.md states the only sanctioned transform in the whole system is the panel's slide-in; everything else signals state via color/brightness, exactly as the primary button does two rules above this one in the same file.
- **Fix**: Drop the `transform: scale`, keep the opacity/border shift.
- **Suggested command**: `/impeccable polish`

## Persona Red Flags

**Sam (accessibility)**: Teams' sortable headers have no `tabindex`, `role="columnheader"`, `aria-sort`, or keydown handler (see P1). The payment badge's "click to toggle" instruction lives only in a hover-only `title` tooltip; its accessible name is just "✓ Payé", giving no hint that activating it changes state.

**Alex (power-user/organizer)**: Cannot sort Teams by payment status (`sortField` is limited to name/version/administration/category) — no way to group unpaid teams for a payment-chasing pass near the deadline; must eyeball every row. No Escape key closes the panel or any of the three confirms, breaking keyboard flow when moving fast through many teams.

## Minor Observations

- Players' `.table-wrapper` border color is `rgba(255,255,255,0.07)` vs. Teams' `0.1` — a second small, unexplained divergence between the two pages' otherwise-matching table shell.
- Contact info in both the Teams panel's player cards and the Players table is plain text, not `mailto:`/`tel:` links.
- The panel's `close-btn` is a bare `✕` glyph with no explicit `aria-label`.

## Questions to Consider

1. If "En attente" has read as payment-pending to every organizer who's glanced at that KPI band so far, has that false signal already caused someone to chase a team for money that was in fact already paid?
2. DESIGN.md names "never 4px, 8px, or 0" as a Don't by number, yet it's present in six places across both files — was there ever a check tying component radii back to the tokens, or is compliance purely a matter of the next person remembering?
3. Every workflow claim here — sort, bulk-send, filtered export — was verified against 2 test teams and 4 players. Has this surface been exercised anywhere near `MaxTeams` capacity, where "eyeball the unpaid rows" and the unsortable payment column stop being minor and start being the organizer's actual bottleneck?
