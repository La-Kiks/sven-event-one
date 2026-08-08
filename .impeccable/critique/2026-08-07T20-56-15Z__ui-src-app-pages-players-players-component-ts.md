---
target: admin players page
total_score: 17
max_score: 36
na_heuristics: 10
p0_count: 2
p1_count: 2
timestamp: 2026-08-07T20-56-15Z
slug: ui-src-app-pages-players-players-component-ts
---
Method: dual-agent (A: a302290eda49f6990 · B: a7306812e62455466)

## Design Health Score: 17/36 (Poor)

Operate-mode surface (dense data, repeated use). Heuristic 10 (Help and Documentation) marked n/a — appropriate for a single-purpose internal admin table for one seeded user; a help system would be over-engineering for the stated "working tool for a small team" principle.

| # | Heuristic | Score | Key Issue |
|-----------|-------|-----------|
| 1 Visibility of System Status | 2 | Shimmer loading + a live count exist, but no per-column "interesting" indicator and no explicit zero-vs-loading distinction |
| 2 Match System / Real World | 2 | English-only labels on a French organizer's tool for a French event — continuation of the Teams-page finding, at least internally consistent here unlike Teams' mix |
| 3 User Control and Freedom | 1 | No search, no filter, no way to isolate a subset — with up to 104 rows and only column-sort, almost no way to narrow to what's needed |
| 4 Consistency and Standards | 2 | Consistent with Teams' visual system, but same accessibility gap (no `<th scope>`, no keyboard sort) repeated unaddressed |
| 5 Error Prevention | 3 | Read-only page, nothing to break — low risk by design |
| 6 Recognition Rather Than Recall | 2 | All data visible, but no link from a player back to their team — organizer must leave and re-search manually |
| 7 Flexibility and Efficiency of Use | 1 | No multi-column sort, no saved filter, no export, no keyboard trigger, no persisted sort — single-column sort is the only tool |
| 8 Aesthetic and Minimalist Design | 3 | Table itself is clean and dense; loses a point to decorative chrome (gradient glow, oversized display title) that doesn't serve a "clarity and speed" utility page |
| 9 Error Recovery | 1 | One generic string, no retry, no cause differentiation — repeats the Teams-page gap |
| 10 Help and Documentation | n/a | Appropriate for single-user internal tooling at this scope |
| **Total** | | **17/36** | **Poor** |

## Design Specificity Verdict

**LLM assessment**: Same verdict as the Teams page, because it's the same design system file-for-file — Google Fonts "Bebas Neue"+"DM Sans", `#dc2626` red, `#0d0d0d` background, none of it drawn from the app's real tokens. **This is the same open cross-page fork already flagged on Teams, not a new finding** — Players is simply the second and last instance of it. The app now has exactly two pages left on the red/Bebas-Neue identity; nothing here moves that decision forward, still not resolved either way.

**Deterministic scan**: clean (0 findings). External Google Fonts import confirmed and double-verified at `players.component.scss:1` (a prior pass on a sibling file had incorrectly reported "none found" for the same pattern — this time independently re-verified via direct file read, matches). No `routerLink`, `tabindex`, `role`, `keydown`, or any `<input>` anywhere in the template — confirmed by direct grep, every clickable element listed: 2 nav buttons + 7 sortable `<th>`s, nothing else.

**Visual overlays**: not available — no browser tool connected, route is Admin-auth-guarded besides.

## Overall Impression

This page reads as "the same table pattern as Teams, copy-pasted for a different entity" rather than a page shaped by what an organizer actually does with a player roster days before race day. Her real jobs here — building a day-of check-in sheet, counting volunteers, tallying outfit sizes, occasionally contacting one specific player directly — are all "extract a filtered subset or count" tasks, and the page currently offers exactly one tool (single-column sort) for all of them.

## What's Working

- Sortable columns with a clear active-state icon and a sensible default (last name, matching the backend's own order) — well executed for what it does.
- Volunteer Yes/No is a distinct color-coded badge, not plain text — makes the one column tied to a named real task ("who volunteered") scannable at a glance.
- Honestly minimal — no fake row actions or dead affordances implying interactivity that isn't there, consistent with the admin-tooling product principle.

## Priority Issues

**[P0] No cross-navigation from a player row to their team**
- **What**: `.team-pill` is a plain `<span>` — no `routerLink`, no click handler anywhere in the row (confirmed: zero `routerLink` occurrences in the file).
- **Why it matters**: The organizer's most natural next question after spotting a player ("is this team paid? who's their teammate?") requires leaving this page, going to Teams, and manually re-searching by team name — every single time.
- **Fix**: Make `.team-pill` a link to the Teams page, ideally deep-linked to open that team's detail panel directly.
- **Suggested command**: `/impeccable clarify`

**[P0] No search or filter on a table that can hold up to 104 rows**
- **What**: Zero `<input>` elements anywhere in the template; the only interaction is per-column sort. With `MaxTeams = 52`, the roster tops out at 104 players.
- **Why it matters**: Finding one specific player, or isolating "just volunteers" / "just size L outfits," is currently a manual scroll-and-eyeball task — directly undermining the named real tasks this page should serve.
- **Fix**: Add a text search (name/team/email) plus quick filter chips for volunteer yes/no and category.
- **Suggested command**: `/impeccable clarify`

**[P1] No CSV/print export despite a plausible, named day-of use case**
- **Why it matters**: "Building a day-of check-in list" is one of the explicit real jobs this page should serve; the only current path is manual transcription or screenshotting a dark table for print.
- **Fix**: Add a CSV export and/or print-friendly action in the page header.
- **Suggested command**: `/impeccable clarify`

**[P1] Keyboard-inaccessible sortable headers**
- **What**: `<th class="sortable" (click)="sort(...)">` has no `tabindex`, `role`, `keydown` handler, or `aria-sort` — confirmed via direct grep, same gap as the Teams page.
- **Why it matters**: A keyboard-only or screen-reader admin cannot sort this table at all.
- **Fix**: Add `tabindex="0"`, `role="columnheader" aria-sort="..."`, and an Enter/Space keydown handler mirroring the click.
- **Suggested command**: `/impeccable harden`

**[P2] Generic, non-actionable error state**
- **What**: `"Failed to load players."` is the only message, no retry, no cause differentiation — repeats the same gap already flagged on Teams.
- **Fix**: Add a retry button; distinguish session-expired from generic network failure if feasible.
- **Suggested command**: `/impeccable harden`

**[P2] No count breakdown, only a total**
- **What**: `{{ players.length }} players` is the only summary statistic.
- **Why it matters**: The named tasks are about subsets (volunteer count, per-category, per-outfit) — the organizer currently counts by hand from a sorted column.
- **Fix**: Add summary chips (volunteer count, per-category/outfit tallies) near the total, ideally dynamic against an active filter.
- **Suggested command**: `/impeccable clarify`

**[P3] Email/phone aren't actionable**
- **What**: Plain text, not `mailto:`/`tel:` links.
- **Why it matters**: "Contact a specific player directly" is a named real task; currently requires copy-pasting text out.
- **Fix**: Wrap in `mailto:`/`tel:` anchors.
- **Suggested command**: `/impeccable clarify`

## Persona Red Flags

- **Alex (impatient power user)**: blocked hardest of the three personas — no search, filter, persisted sort, keyboard sort, or export. Single-column sort doesn't even compose (can't sort by team, then volunteer, within team).
- **Riley (stress tester)**: zero-players state renders a bare empty `<tbody>` with no explicit "no players yet" message, reading as a possible bug rather than confirmed zero data. 100+ rows works but is slow with only sort to manage it. The `teamName: "—"` fallback in `PlayerService.cs` is very likely unreachable dead code — **verified**: `Player.cs` declares `TeamId` as non-nullable `int` and `Team` as required (`= null!`), consistent with EF Core's default cascade-delete convention for a required FK (the project's only custom `OnModelCreating` override, per CLAUDE.md, is for User↔Team, not Player↔Team, so Player↔Team gets EF's default behavior).
- **The organizer (day-of check-in, volunteer counting, outfit tallying)**: worst-served of all three personas relative to her actual job. She needs a volunteer count, an outfit-size breakdown, a way to isolate a subset, and something printable — the page gives her a single global count and an unfilterable sortable table.

## Minor Observations

- `.category-tag` and `.team-pill` share the same dim-white/bordered treatment as several other elements — hard to distinguish category from team from outfit at a glance since only the volunteer badge gets color.
- The radial-gradient glow and 3rem Bebas Neue title are decorative flourishes on a page whose own product principle is "clarity and speed over polish" — a mild self-contradiction, minor next to the P0/P1s.
- `applySort`'s boolean-sort branch looks inverted at first read but is intentional (puts Yes first on ascending) — not a bug, just worth a maintainer comment.

## Questions to Consider

- Both admin pages are now on the same red/Bebas-Neue fork — is this the moment to resolve the brand-consistency question, or stay deliberately deferred?
- Given the 104-player ceiling, is search/filter/export actually out of scope for "a working tool for a small team," or does the day-of check-in need push past what a plain sortable table can serve?
- Should player-to-team navigation exist bidirectionally, given both pages already share a nav toggle and reference the same entities?
