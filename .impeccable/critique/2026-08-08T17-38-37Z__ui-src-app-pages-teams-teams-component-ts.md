---
target: admin teams page
total_score: 27
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 3
timestamp: 2026-08-08T17-38-37Z
slug: ui-src-app-pages-teams-teams-component-ts
---
Method: dual-agent (A: a435dcbc3448624ed · B: a79035c90387544a8)

#### Design Health Score

Operate-mode surface (dense data, repeated power-user task) — all 10 heuristics scored for real.

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3 | Loading shimmer + disabled/relabeled buttons mid-request + immediate badge flip after payment-toggle confirm (all verified live); docked one point for no toast/highlight after a successful action, easy to miss on a fast click |
| 2 | Match System / Real World | 3 | Real administrative-corps/category vocabulary matches the domain; docked for the still-present English/French copy mix in the same panel |
| 3 | User Control and Freedom | 3 | Cancel paths exist and were verified live on both delete and payment-toggle confirm; still no keyboard Escape |
| 4 | Consistency and Standards | 2 | Internally consistent confirm-pattern reuse, but the page is still forked visually (red/Bebas Neue) from the rest of the app's yellow/Lemon-Milk brand, and language is inconsistent within the same panel |
| 5 | Error Prevention | 3 | **Resolved from 1→3**: confirm-before-write now correctly implemented and tested live for payment toggle, delete, and bulk-send |
| 6 | Recognition Rather Than Recall | 3 | Payment and account status are both shown as badges where visible; only gap is account/activation status isn't in the main table, so cross-row scanning still requires memory of the aggregate count |
| 7 | Flexibility and Efficiency of Use | 2 | Guard against double-submit exists (`isTogglingPayment` early-return, confirmed in code); still no keyboard operability and no search/filter on this page (Players page has one, Teams doesn't) |
| 8 | Aesthetic and Minimalist Design | 3 | Clean, unfussy, reasonable density — unchanged from prior pass |
| 9 | Error Recovery | 3 | **Resolved from 1→3**: independent error slots per action (`paymentError`, `deleteError`, `createAccountError`, `bulkError`) verified in code and live; messages are generic but present and scoped |
| 10 | Help and Documentation | 2 | Unchanged — one tooltip is still the only in-context help; acceptable for a small trained internal team per the product's own Operate-mode principle |
| **Total** | | **27/40** | **Fair — up from 19/40** |

#### Design Specificity Verdict

Still a competently-built, domain-specific tool rather than a swapped-in template — both assessments independently confirmed real business logic embedded in the UI: administrative-corps labels (Gendarmerie, Police Municipale, Police Nationale, Pénitentiaire, Militaire, Pompier, Autre), category derivation display, a working `?teamId=` deep link from the Players page that opens the right panel and cleans up its own URL, and a genuinely operational bulk-activation-email flow with per-team sent/failed reporting.

The unresolved issue flagged in the prior pass remains exactly as it was: this page runs red/black/Bebas-Neue/DM-Sans while login (and per CLAUDE.md, the rest of the app) runs yellow/black/Lemon-Milk/Cabin — confirmed live by direct comparison (login's logo mark is yellow, teams' is red, headings use a visibly different display face). This is a still-open decision, not a defect that was supposed to be fixed in the last round — it was flagged as a question, not a P0, and it's still a question.

Deterministic scan (CLI detector, `detect.mjs`) is now clean on both `teams.component.ts` and `teams.component.html` — **the `layout-transition` warning on `.content`'s `margin-right` transition from the prior snapshot is resolved**, consistent with commit cd5ed21's "remove dead CSS transition." Live browser instrumentation (injected `detect.js` against the rendered page) found one finding: a `radial-spotlight-glow` (red radial-gradient at 8% alpha) on `.teams-page`. Investigated in context: this exact gradient pattern exists in essentially every page's SCSS across the app (login, activate-account, forgot-password, my-team, not-found, players, teams) — it's a shared, deliberate design token, not a teams-specific defect. Flagging it as isolated to this page would be misleading; if it's ever addressed it needs to move as an app-wide token change, not a one-file fix.

#### Overall Impression

The two P0s from the 19/40 pass are genuinely fixed, and fixed well — not just patched. Payment toggle now requires an inline confirm (tested live: confirm, cancel, and re-confirm all behaved correctly), and a failed toggle resets state and surfaces a visible message rather than failing silently. The debounce/race-condition gap (prior P2) is also closed via an early-return guard. This is real, verified progress on the exact job this page exists to do — protecting the integrity of a real financial record — and it shows in the heuristic score jump from 19 to 27.

What's left is now weighted toward structural and accessibility gaps rather than safety gaps: the table and payment toggle are still entirely unusable without a mouse (confirmed via a live accessibility-tree read showing only 5 real interactive elements on a page with a full data table and a clickable financial control), the visual brand fork is still unresolved, and copy still code-switches between English chrome and French actions in the same view. One new and more concrete safety-adjacent finding surfaced this round: the slide-in detail panel visually overlaps the top bar's Sign-out control, and a fast click aimed at Sign-out can land on the panel's Delete button instead — worth fixing before it costs someone a real team's data.

#### What's Working

- **The confirm-before-mutate pattern is now uniformly and correctly applied** across every state-changing action (delete, payment toggle, bulk email), each with its own disabled-state handling and independently-scoped error message — verified live for both the cancel and confirm paths on the payment toggle, not just read from source.
- **The cross-team deep link (added since the last pass) works cleanly**: clicking a team pill on the Players page navigates to `/teams`, auto-opens that team's panel via a `teamId` query param, and clears the param afterward (`replaceUrl`) — confirmed live with no flash of the wrong panel or dangling URL state.
- **The mechanical CSS regression from the last pass is gone** — the `.content` `margin-right` layout-transition finding no longer appears in either static or live scans.

#### Priority Issues

**[P1] Table rows and the payment-toggle control are still not keyboard/screen-reader operable**
- **What**: Live accessibility-tree inspection found only 5 real interactive elements on the whole page (nav, sign-out, panel close, delete) — the entire teams table (`<tr (click)>`) and the payment badge (`<span (click)>`) have no `role`, `tabindex`, or keydown handler.
- **Why it matters**: This is the same P1 flagged last round and it is unresolved — a keyboard-only or screen-reader admin cannot open a team's panel or change payment status at all, on the one page whose job is repeated daily operation.
- **Fix**: Make rows and the payment badge real interactive elements (`tabindex="0"`, `(keydown.enter)`, `role="button"` or actual `<button>`s).
- **Suggested command**: `/impeccable harden`

**[P1] Detail panel visually overlaps the top-bar Sign-out control, risking an accidental Delete click**
- **What**: New finding this round. The panel is `position: fixed`, full-height, right-anchored, `z-index: 20` — it sits directly over where the top bar's Sign-out button normally is. During live testing, a click aimed at Sign-out while the panel was open instead landed on the panel's own Delete button in the same screen position.
- **Why it matters**: A fast or distracted admin (exactly the persona this tool is built for) could delete a live, paid team by muscle-memory misclick rather than intent — this is a real-money, real-registrant safety gap, not a cosmetic one.
- **Fix**: Either have the panel push/shrink the top bar instead of overlapping it, hide/disable the covered top-bar controls while the panel is open, or move Delete to a position a stray click can't reach.
- **Suggested command**: `/impeccable harden`

**[P1] Visual identity is still forked from the rest of the app**
- **What**: Confirmed unresolved by direct live comparison — login uses yellow/black/Lemon-Milk/Cabin; teams uses red/black/Bebas-Neue/DM-Sans. This was raised as an open question last round, not a defect to auto-fix, and it remains open.
- **Why it matters**: An admin bouncing between login and this page constantly experiences two different visual languages for the same product, undermining the sense that this is one coherent tool rather than a bolted-on admin skin.
- **Fix**: Make an explicit decision — converge on brand (black/`#262626` base with yellow accent, achievable without looking like the marketing site) or deliberately keep back-office tooling visually distinct — and apply it consistently, including to the matching fork on `players.component.scss`.
- **Suggested command**: `/impeccable adapt`

**[P2] English/French copy mix persists in the same view**
- **What**: Verified live — the delete-confirmation copy ("Delete Impeccable QA Test Team and all its players? This cannot be undone.") is English, while the payment-confirm copy two rows above it in the same panel ("Marquer comme payé ?", "Confirmer", "Annuler") is French. Same admin, same screen, same session.
- **Why it matters**: Forces a French-speaking admin to context-switch language mid-task for no functional reason.
- **Fix**: Standardize on French throughout, matching the rest of the panel and the actual admin's language.
- **Suggested command**: `/impeccable clarify`

**[P2] Account/activation status still not scannable at the table level**
- **What**: The table shows payment status per row but not account/activation status — the only signals are the aggregate "pending accounts" count on the bulk-send button or opening each team's panel individually.
- **Why it matters**: The admin's other recurring job — "which teams still need an account created or resent" — still requires opening rows one at a time, or trusting a single aggregate number, exactly as flagged last round.
- **Fix**: Add a compact account-status indicator to the table so teams needing attention are scannable without opening a panel.
- **Suggested command**: `/impeccable clarify`

#### Persona Red Flags

- **Sam (accessibility)**: Still completely blocked — cannot open a team's panel or toggle payment status by keyboard at all; a screen reader would perceive the table as static text with no evident way to drill in. This is the most serious unresolved finding from either round.
- **Riley (stress-tester)**: The double-click race on payment toggle is now guarded (`isTogglingPayment` early-return, confirmed in code). The new risk is spatial, not temporal: a fast click aimed at Sign-out while the panel is open can land on Delete instead — a real near-miss confirmed live during this assessment.
- **The organizer (anxious, non-technical)**: Materially better served than last round — the interaction she performs most anxiously (confirming payment) now has both a confirm step and visible failure feedback, closing the single worst mismatch flagged previously. The remaining gap is that a successful toggle produces no acknowledgment beyond the badge itself flipping, so a hesitant admin on a slow connection may not be fully sure a click "took."

#### Minor Observations

- The live-detected `radial-spotlight-glow` on `.teams-page` is a shared token used across nearly every page in the app, not a teams-specific defect — noted so it isn't mistaken for an isolated issue if this snapshot is read in isolation later.
- No dedicated zero-state message when `teams.length === 0` — the table would render with header and zero rows plus a "0 teams" count. Still unaddressed from last round, still genuinely low priority given this is a single-admin, controlled-roster tool.
- Bulk-send failures still have no per-row retry action inline in the results list (carried from last round; not re-verified this round since no failing send was reproduced).
- Category/payment/account badges reuse the same green/red classes for different semantics, but all remain disambiguated by icon and text rather than color alone — a genuine accessibility plus, not a gap.
- `players.component.scss` reportedly still shares the same red/Bebas-Neue system as this page — any brand-convergence decision (Priority Issue above) should cover both files together to avoid creating a third visual language.

#### Questions to Consider

- Given the panel now demonstrably overlaps and can be mis-clicked into Delete instead of the top bar's Sign-out, should the fix be layout (push the top bar aside), state (disable covered controls while the panel is open), or repositioning (move Delete out of that danger zone)?
- Is the red/Bebas-Neue admin skin an intentional "back-office tools look different" decision that should be documented as such, or is it drift from before the brand converged elsewhere that should now be resolved one way or the other?
- Should account/activation status become a table column now that payment status already lives there, or is opening each row still acceptable given the team count is capped?
