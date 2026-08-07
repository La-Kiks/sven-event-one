---
target: admin teams page
total_score: 19
max_score: 40
na_heuristics: 
p0_count: 2
p1_count: 3
timestamp: 2026-08-07T20-21-46Z
slug: ui-src-app-pages-teams-teams-component-ts
---
Method: dual-agent (A: add9085840d79f582 · B: aaa2553eb85aba708)

## Design Health Score: 19/40 (Poor)

Operate-mode surface (dense data, repeated power-user task) — all 10 heuristics scored for real.

| # | Heuristic | Score | Key Issue |
|-----------|-------|-----------|
| 1 Visibility of System Status | 2 | Delete and account-creation have full in-flight/success/error states; `togglePayment()` has none |
| 2 Match System / Real World | 2 | Chrome is English ("Team", "Payment", "Sign out"), action copy is French — mixed for a French admin |
| 3 User Control and Freedom | 3 | Backdrop-click + close both dismiss the panel, cancel paths exist; no keyboard Escape |
| 4 Consistency and Standards | 2 | Delete gets a full confirm step; payment toggle (an equally real financial record) gets none |
| 5 Error Prevention | 1 | No confirmation before flipping `isPaid`; no debounce, rapid clicks can race |
| 6 Recognition Rather Than Recall | 2 | Payment status is scannable in the table; account status is not — must open every row |
| 7 Flexibility and Efficiency of Use | 1 | No keyboard operation, no search/filter, every action needs opening the panel (extra round trip) |
| 8 Aesthetic and Minimalist Design | 3 | Clean, unfussy, reasonable density — execution is fine even if the direction is generic |
| 9 Error Recovery | 1 | Payment-toggle failure is invisible (console.error only); bulk failures have no retry action |
| 10 Help and Documentation | 2 | One tooltip is the only in-context help; the highest-stakes control has the least explanation |
| **Total** | | **19/40** | **Poor** |

## Design Specificity Verdict

**LLM assessment**: A competently executed generic dark-admin-template aesthetic, not a purpose-built operator tool. The ingredients are right for dense tabular work (sortable table, skeleton shimmer rows, slide-in panel) but none of it is distinctive to this product or task — swap the red accent and Bebas Neue for anything else and this is indistinguishable from any SaaS admin template. What "purposeful" would look like — a payment column that visually screams which rows need action, keyboard-first row selection, a search bar for 52 teams — isn't here.

**Brand-consistency open question (flagging, not resolving)**: this page's red/Bebas-Neue/DM-Sans system is a hard fork from the yellow/black/Lemon+Cabin identity now established everywhere else (landing, inscription, login, mon-equipe). Arguments for keeping it distinct: admin tooling conventionally signals "back office" via a different palette, and red/near-black is objectively more legible for dense small-text tables than yellow-on-black would be at this density. Arguments for converging: it's the last holdout of an identity already moved away from twice, and a distinct admin skin doesn't require abandoning the brand's accent color entirely — black/`#262626`/yellow-accent is achievable without looking like the marketing site. This is a decision for you, not something to default either way — flagged as a Priority Issue below, not fixed.

**Deterministic scan**: 1 finding — `layout-transition` warning at `teams.component.scss:126` (`transition: margin-right` on `.content`, animating a layout property instead of transform/opacity). **Correction to the detector pass**: the mechanical scan initially reported no Google Fonts `@import` in this file, which is wrong — verified directly (and independently caught by the design-review pass): `teams.component.scss:1` does pull "Bebas Neue" and "DM Sans" from Google Fonts CDN, the same external-font pattern removed from login/mon-equipe in the last round.

**Visual overlays**: not available — no browser tool connected, and the route is Admin-auth-guarded besides.

## Overall Impression

The delete flow and the bulk-activation flow are genuinely well-built — confirmed, in-flight states, per-item outcome reporting. But the page's actual core recurring job — "confirm real money arrived, confirm the right people can log in" — is where it's weakest: the payment toggle can fail completely silently, has no confirmation despite being a real financial record, and account status isn't even visible in the table without opening every row. For a page one non-technical admin will check anxiously and repeatedly in the weeks before the event, that's a real mismatch between what the page needs to do and what it currently does well.

## What's Working

- **Bulk activation flow** is the one place the UI matches the stakes: confirms before firing, shows progress, reports sent/failed counts per team.
- **Skeleton pulse-row loading** for both table and panel avoids layout jump — a legitimate, well-executed pattern.
- **Delete flow** (named confirmation, disabled buttons mid-request, inline error) is a solid template the payment-toggle and account-creation flows should be measured against — and currently aren't.

## Priority Issues

**[P0] Payment toggle fails completely silently on error**
- **What**: `togglePayment()`'s error handler is `error: () => console.error('Failed to update payment status')` (`teams.component.ts:199`) — no UI state change, no message rendered anywhere. Verified directly in source.
- **Why it matters**: Directly violates the product principle that payment state must never be ambiguous. A failed PATCH leaves the badge unchanged with zero on-screen indication the click didn't work — the admin can't distinguish "it worked" from "the request silently died."
- **Fix**: Wire the error into a visible inline message (mirroring `createAccountError`/`deleteError`'s existing pattern in this same component) plus a pending state on the badge while in flight.
- **Suggested command**: `/impeccable harden`

**[P0] No confirmation before flipping a real financial record**
- **What**: `togglePayment()` fires on a single badge click with zero confirmation, while `deleteTeam()` (arguably lower financial stakes) requires an explicit confirm screen.
- **Why it matters**: A badge click is a far lower-friction gesture than a delete button — this is the exact wrong place to skip friction on a real financial record.
- **Fix**: Add a lightweight confirm step for the paid⇄unpaid transition, consistent with the delete pattern already built in this file.
- **Suggested command**: `/impeccable harden`

**[P1] Account status invisible at table level; auditing requires opening every row**
- **What**: Table columns are Team/Category/Version/Administration/Payment only — `hasAccount`/`accountVerified` never render outside the detail panel, even though the list endpoint already returns them.
- **Why it matters**: The admin's core recurring task ("which teams need an account created/resent") currently requires opening every row one at a time.
- **Fix**: Add a compact account-status indicator to the table so "needs attention" teams are scannable at a glance.
- **Suggested command**: `/impeccable clarify`

**[P1] No zero-state message when there are no teams**
- **What**: The table renders whenever `!isLoading && !error`, with no `*ngIf` branch for `teams.length === 0` — just an empty `<tbody>`.
- **Why it matters**: The organizer checks this page before registration opens too — an unexplained empty table reads as "is this broken?"
- **Fix**: Add an explicit empty-state message.
- **Suggested command**: `/impeccable clarify`

**[P1] Table isn't keyboard/screen-reader operable**
- **What**: Rows are `<tr (click)="openPanel(team)">` with no `tabindex`, `role`, or keyboard handler (verified directly). Sortable `<th>` headers have the same gap.
- **Why it matters**: This page's primary persona explicitly wants speed from repeated daily use — it currently cannot be operated without a mouse at all.
- **Fix**: Make rows and sortable headers real interactive elements (`tabindex`, `(keydown.enter)`, appropriate `role`), or use `<button>`s.
- **Suggested command**: `/impeccable harden`

**[P2] No debounce/lock on the payment toggle**
- **What**: Nothing disables the badge or blocks a second click while a PATCH is outstanding.
- **Why it matters**: Overlapping requests can resolve out of order, leaving displayed state inconsistent with the server's actual last-written value — with no error surfaced either way, compounding the P0 above.
- **Fix**: Disable the badge and/or ignore new clicks while a request for that team is pending.
- **Suggested command**: `/impeccable harden`

**[P2] English/French copy mix in the same component**
- **What**: Chrome copy is English ("Team", "Category", "Payment", "Sign out", "Delete", "Cancel", "Yes, delete"); action-specific copy is French ("Envoyer les emails d'activation", "Créer le compte", "Aucun compte") — same file, same French-speaking admin.
- **Fix**: Standardize on French for the whole page, matching the actual admin's language and the rest of the app.
- **Suggested command**: `/impeccable clarify`

**[P3] Bulk-send failures have no per-row retry**
- **What**: Failed results render as plain text with no action attached — retrying a specific failure means closing the results, finding that team in the table, opening its panel, and clicking resend.
- **Fix**: Add a retry/resend action inline on each failed row in the results list.
- **Suggested command**: `/impeccable clarify`

## Persona Red Flags

- **Alex (impatient power user)**: no keyboard operability at all on his own page; no search/filter for up to 52 rows; every single-team action requires opening the panel first, triggering a needless `GET /{id}` even though the list already has the data.
- **Riley (stress tester)**: zero-teams state renders a bare empty table with no message; rapid payment-toggle clicks have no lock, risking an out-of-order result with zero error surfaced.
- **The organizer (anxious, non-technical)**: the interaction she'll perform most anxiously — confirming a payment registered — is the one with zero confirmation and zero failure feedback. The single worst mismatch between persona need and current implementation on the page.

## Minor Observations

- Category badge color-coding (blue=man, pink=woman, purple=mixt) is functionally legible but leans on a literal gender-color association — worth a look, low priority.
- Several secondary-text treatments use very low white-alpha values on near-black (`.count` at 0.25, `.team-index` at 0.2) that likely fail WCAG AA for normal text — flagged for rendered verification, not asserted as certain.
- Re-running bulk-send after a partial failure will also re-email teams that already got a first activation email but haven't verified yet, since eligibility is keyed on `accountVerified` not `hasAccount` — not harmful, just redundant.
- `players.component.scss` reportedly shares this same red/Bebas-Neue system — worth deciding together with whatever direction is chosen here, since fixing only one would create a third visual language.
- One real detector finding: `transition: margin-right` on `.content` animates a layout property instead of transform/opacity (perf, not correctness).

## Questions to Consider

- Should the admin dashboard keep a deliberately distinct visual identity, or converge now that login and mon-equipe already have? If distinct, should the accent still be brand-adjacent (black/`#262626` base, yellow accent) rather than an unrelated red?
- Is a lightweight click-confirm enough for the payment toggle, or does "payment state must never be ambiguous" warrant a full modal matching delete's pattern?
- Should account status become a table column, or is opening each row acceptable given the team count is capped at 52?
