---
target: players
total_score: 25
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 1
timestamp: 2026-08-16T07-25-24Z
slug: ui-src-app-pages-players-players-component-ts
---
Method: dual-agent (A: a15414ef6a432429d · B: a99ab5c68cb510b97)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3 | Live count, export label, loading shimmer all verified working; no confirmation once a CSV download actually completes |
| 2 | Match Between System and Real World | 4 | French domain terms match the org's vocabulary exactly; column order matches how an organizer thinks about a roster |
| 3 | User Control and Freedom | 2 | Chips/select reset individually, but search input has no clear ("×") affordance and no single "reset all filters" action |
| 4 | Consistency and Standards | 1 | The category cell is plain uncolored text, while the same data type renders as a full tinted/bordered badge on the teams page — and even the volunteer column on this same page uses a proper badge |
| 5 | Error Prevention | 3 | Category filter is a constrained select, not free text; page is read-only so little to prevent |
| 6 | Recognition Rather Than Recall | 3 | All filter/sort state is visibly reflected; team-pill links resolve without needing to remember a team ID |
| 7 | Flexibility and Efficiency of Use | 3 | Fully keyboard-operable sortable headers and scope-aware CSV export are genuine accelerators; no shortcut to focus search, filter/sort state isn't persisted across navigation |
| 8 | Aesthetic and Minimalist Design | 3 | Clean, generous whitespace, correct hairline table pattern; the colorless category column reads as less important than it functionally is |
| 9 | Error Recovery | 2 | Load-failure message is plain language but offers no retry action |
| 10 | Help and Documentation | 1 | No contextual help anywhere except the export button's title tooltip |
| **Total** | | **25/40** | **Acceptable** |

Scored as a full Operate surface — heuristics 7 and 10 were not exempted.

## Design Specificity Verdict

**LLM assessment**: Visually authored for this system, but content/functionally interchangeable — and the one place it most needs to carry brand identity (category signaling) it drops it. The chrome is unmistakably "Tactical Briefing": void-black ground, Lemon Milk uppercase headline, hazard-yellow focus rings and active-chip fill, hairline table borders, the shimmer skeleton. But strip the visual skin and the content is a stock admin roster — Nom/Équipe/Catégorie/Email/Téléphone/Bénévole, search-filter-sort-export could belong to any roster with zero copy changes. That's a defensible trade-off given PRODUCT.md's principle that admin tooling favors clarity/speed over polish — the real issue isn't genericity of copy, it's that the page's own visual specificity is inconsistent within itself: the volunteer column gets the system's signature tinted badge, the category column (arguably more structurally important for a Hyrox duo event) gets plain muted text.

**Deterministic scan**: `detect.mjs --json ui/src/app/pages/players` returned exit code 0, zero static findings. The live browser-injected detector found **2 anti-patterns**: `radial-spotlight-glow` on `div.players-page` (matches the existing radial-gradient background, same pattern already present on the teams page) and `cramped-padding` on `div.table-wrapper` (the table sits flush against the wrapper's border on 3 sides — confirmed via source and visually in a mobile-width screenshot).

**Visual overlays**: Console evidence: 2 anti-patterns found via the injected detector. The live-server instance was stopped after evidence collection.

**Process note**: Same tab-interference pattern as prior runs — Assessment A reported an extra tab appearing unprompted (Assessment B's injection), didn't touch it, worked exclusively in its own tab. Mobile-viewport checks again hit the known `resize_window` limitation in this environment (reports success but doesn't change actual `window.innerWidth`) — no mobile findings are visually confirmed this run, only source-derived.

## Overall Impression

This is a well-built, genuinely accessible admin table — the keyboard-operable sortable headers and cross-page deep-link into the Teams detail panel are real workflow-level design, not just page polish. But it has one glaring, easy-to-fix inconsistency: it renders the same category data as a plain gray label here and a full tinted badge one page over, breaking the design system's own signature component within a single admin workflow.

## What's Working

1. **Fully keyboard-operable sortable headers with correct ARIA** — verified live: Tab reaches a header, gets a clean hazard-yellow focus outline, Enter toggles sort direction and the icon + `aria-sort` update instantly.
2. **Cross-page deep-link integration** — clicking a team-pill navigates to `/teams?teamId=N`, which auto-opens that team's detail panel. Verified live.
3. **Honest, scope-aware system status** — the live `X / Y JOUEURS` count, the `Export CSV (N filtrés)` vs `(N)` label swap, and the export button's disabled state at zero results all update live and were confirmed in the browser.

## Priority Issues

**[P1] Category column breaks the design system's own signature Status Badge pattern**
- **Why it matters**: DESIGN.md explicitly names the tinted/bordered badge "used identically for payment state, account state, and player category across admin and participant screens." The teams page renders category as a tinted/bordered badge (blue/pink/purple by category); this page renders the identical data as plain uppercase gray text with no color or border — inconsistent even within this one page, since the volunteer column right next to it does use a proper badge. An organizer scanning the roster for category mix loses the color-scan affordance they get one click away.
- **Fix**: Replace the category cell's markup/class with the same `.category-badge` pattern (+ `[ngClass]="player.category"`) already defined for teams.
- **Suggested command**: `/impeccable harden`

**[P2] Filter/search/sort state isn't persisted, so it's lost on every excursion into Teams**
- **Why it matters**: The page's own best feature — the team-pill deep link — actively punishes the exact workflow it enables: filter to volunteers, click a team-pill to check something, come back, and the filter/sort is gone. Inconsistent with the app's own pattern of round-tripping `teamId` through query params for the inbound direction.
- **Fix**: Mirror the existing query-param technique outbound — write search/filter/sort state to the URL so it survives navigation.
- **Suggested command**: `/impeccable optimize`

**[P2] Outfit ("tenue") data exists in the model and CSV but is invisible in the UI**
- **Why it matters**: `Player.outfit` is captured, exported in the CSV, and shown as a tag on the Teams page's player sub-list — but never rendered in this table. An organizer planning apparel/sizing has to export to Excel for data the page already knows how to display.
- **Fix**: Add an outfit column reusing the existing `.tag.outfit` styling from teams, or surface it in a detail affordance if intentionally cut for column-count reasons.
- **Suggested command**: `/impeccable clarify`

**[P2] No screen-reader announcement of dynamic result counts or empty state**
- **Why it matters**: Sighted users see the count and export label update live when filtering; a screen-reader user gets no equivalent — the count and empty/error states have no `aria-live` region, so state changes silently under non-visual users.
- **Fix**: Add `aria-live="polite"` to the count and to the empty/error state containers.
- **Suggested command**: `/impeccable harden`

**[P3] No one-click recovery from a dead-end filter/search state**
- **Why it matters**: The zero-results message is purely informational — no inline reset action, and the search input has no clear ("×") button.
- **Fix**: Add a "Réinitialiser les filtres" action inside the empty state, and a clear icon on the search field.
- **Suggested command**: `/impeccable polish`

## Persona Red Flags

**Alex (power user)**: Task speed is genuinely good — search-as-you-type, one-click sort, one-click export. But no shortcut to focus the search box, and filter/sort state resets on any navigation away and back — directly punishing the exact "check a team, come back" loop this tool is built for.

**Sam (accessibility-dependent)**: Sortable headers are real focusable, correctly-ARIA'd controls — verified working via keyboard alone. But dynamic count changes and the empty-state message aren't in an `aria-live` region, so filtering the table gives no auditory confirmation. Note: the plain-text category cell is ironically more screen-reader-friendly than a color-only badge would be — the fix should preserve text, not just add color.

**Riley (stress tester)**: The category-badge-vs-plain-text inconsistency is exactly the kind of same-data-two-treatments gap this persona is built to catch. Outfit is exportable via CSV but absent from the visible table. Boolean sort on "Bénévole" puts "Oui" before "Non" in ascending order — technically consistent code, but counter to the alphabetical-ascending intuition every other column trains.

## Minor Observations

- `.table-wrapper` (4px), `.pulse-row` (3px), and `.empty-state` (4px) all use radius values outside DESIGN.md's two sanctioned tokens (2px sharp / 0.25em control) — real token drift on a page that otherwise respects the system closely, and the same pattern already flagged on `teams` (`.info-section`/`.player-card`) and now fixed there.
- Sort-direction glyphs (↑↓↕) are visually subtle (0.7rem, 0.6 opacity) relative to the header label.
- CSV export has no post-download in-app confirmation — a cheap, appropriate reassurance beat is missing for an action that touches PII (email, phone).

## Questions to Consider

- The Teams page tells you a team's category at a glance via a colored badge — why does the same data go monochrome the moment you're one level down, looking at individual players?
- This is a repeatedly-used tool for a small organizing team — so why does every filter reset the moment someone clicks a team-pill and comes back?
- Outfit data is collected, exported, and shown on Teams — is its absence here a decision or an oversight?
