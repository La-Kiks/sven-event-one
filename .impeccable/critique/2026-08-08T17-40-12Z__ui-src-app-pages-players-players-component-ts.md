---
target: players page
total_score: 25
max_score: 36
na_heuristics: 10
p0_count: 0
p1_count: 1
timestamp: 2026-08-08T17-40-12Z
slug: ui-src-app-pages-players-players-component-ts
---
Method: dual-agent (A: acb7d16261e836344 · B: a45d0ac13d7d48a6d)

#### Design Health Score

Operate-mode surface (dense data, repeated use), re-critiqued after commit b3bbc71 added search/filter, keyboard-accessible sort, CSV export, and a team cross-link. Heuristic 10 (Help and Documentation) remains n/a for the same reason as the prior pass — a help system would be over-engineering for a single-purpose internal tool at this scale.

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 Visibility of System Status | 2/4 | Loading/empty/error states all exist and are distinct, but there's no post-export confirmation and no `aria-live` on filtered-count changes |
| 2 Match System / Real World | 4/4 | Search fields (name/team/email), filter chips (volunteer yes/no), and CSV headers all map directly to how organizers actually work the roster |
| 3 User Control and Freedom | 3/4 | Search, filters, and sort are all easily reversible; no single "reset all filters" action, but the toolbar is small enough that this is a minor gap |
| 4 Consistency and Standards | 3/4 | Internally consistent typography/spacing; focus-ring style is inconsistent within the same toolbar (custom red on search/select/th/team-pill, default browser blue on chips/export button) |
| 5 Error Prevention | 2/4 | Export-when-empty is correctly prevented (disabled button, verified live); export-when-filtered-without-warning is not — a real silent-mistake risk the heuristic exists to catch |
| 6 Recognition Rather Than Recall | 3/4 | Filters are visible chips, not hidden state, and the new team cross-link removes the old back-and-forth; but the export button gives no cue about what's currently in scope |
| 7 Flexibility and Efficiency of Use | 3/4 | Verified live: keyboard sort (Tab → Enter/Space) works, search filters as you type — real power-user support now exists where none did before |
| 8 Aesthetic and Minimalist Design | 3/4 | Table and toolbar are appropriately restrained, but the live mutation scan independently re-confirmed the radial-gradient glow flagged last round, plus a new cramped-padding finding on `.table-wrapper` |
| 9 Error Recovery | 2/4 | "Failed to load players." remains a single generic string with no retry action — unchanged from the prior pass |
| 10 Help and Documentation | n/a | Appropriate for single-user internal tooling at this scope |
| **Total** | | **25/36** | **Fair** (up from 17/36 / Poor) |

#### Design Specificity Verdict

**LLM assessment**: Templated shell, purpose-built table. The top-bar/nav chrome is still a near-verbatim copy of the Teams page's SCSS (same red/Bebas-Neue/DM-Sans system, same spacing values) — a reasonable DRY choice for an internal shell, not a new flaw. But the toolbar added in b3bbc71 shows genuine product thinking rather than a generic bolt-on: search spans exactly the fields a check-in volunteer would search by (name, team, email), the volunteer/category filters map to real operational questions, and the CSV column order (Last name, First name, Team, Category, Outfit, Email, Phone, Volunteer) mirrors a printable check-in sheet, complete with a UTF-8 BOM for Excel compatibility and proper field-escaping. The cross-link to Teams is a real deep-link (`routerLink` + `queryParams` consumed by `teams.component.ts` to auto-open the matching team panel, then `replaceUrl: true` to clean the URL) — verified working live, not a superficial gesture.

**Deterministic scan**: clean for both files — `detect.mjs --json` returned exit 0 / empty findings array for both `players.component.ts` and `players.component.html`. The live mutation-based scan (browser-injected `detect.js`, separate from the static CLI pass) found 2 items: a `radial-spotlight-glow` on `div.players-page` (`#dc2626` at 0.07 alpha) and `cramped-padding` on `div.table-wrapper` (children flush to top/right/bottom with no inset). Both are carried-over/structural rather than newly introduced by the fix commit — the glow is the same decorative flourish flagged as a "self-contradiction" against the Operate-mode principle in the prior snapshot; the cramped-padding flag is plausibly an intentional full-bleed table pattern rather than a genuine defect, noted as a probable false positive by Assessment B.

**Visual overlays**: both assessments had live browser access this round (unlike the prior pass) and independently verified the page in a real session — search, filters, sort, export, and the cross-link were all exercised interactively, not just read from source.

#### Overall Impression

This is a substantially different page than the one critiqued on 2026-08-07. Both P0s from that pass — no way to narrow a 100+ row table, and no path from a player back to their team — are resolved and verified working end-to-end in a live browser, not just present in the diff. The fix also went further than the minimum: CSV export handles the empty-state guard correctly, escapes fields properly, and includes a real deep-link rather than a static team-name label. What's left is a second-order gap the original fix didn't anticipate: the export doesn't tell the admin what's in scope. An admin who searches for one name to confirm a bib number and then reflexively hits "Export CSV" gets a 1-row file with no warning — a plausible, costly mistake for the exact day-of check-in moment this feature exists to serve. The remaining issues are otherwise accessibility contrast/state gaps rather than missing functionality.

#### What's Working

- Keyboard-operable sortable headers are a real, verified implementation, not a checkbox exercise: `tabindex="0"`, `role="columnheader"`, `aria-sort` wired to actual state, and both Enter and Space trigger the sort — confirmed live by tabbing to a header and re-sorting.
- The team cross-link is a genuine deep-link consumed by the Teams page to auto-open the correct team's detail panel (with `replaceUrl: true` to clean the URL afterward) — verified live end-to-end, including against the disposable QA test team.
- CSV export shows real defensive thought: proper comma/quote/newline escaping, a UTF-8 BOM for Excel, and a disabled state when the filtered set is empty — all verified live by driving the search/filter to zero rows and confirming the button disables correctly.

#### Priority Issues

**[P1] CSV export silently scopes to the current filter/search with no indication**
- **What**: `exportCsv()` always exports `this.sorted` — the currently filtered and sorted array — never the full roster, and the button label stays "Export CSV" regardless of whether 2, 40, or 0 rows are in scope.
- **Why it matters**: This is exactly the failure mode of a distracted or fast-moving admin on event morning: search for one name to confirm something, then reflexively click Export, and get a 1-row "check-in list" with no warning that it isn't the full roster.
- **Fix**: Make scope visible at the point of action — dynamic button label ("Export 2 of 40" / "Export all 40"), a short always-visible line near the button, or split into explicit "Export filtered" / "Export all" actions.
- **Suggested command**: `/impeccable clarify`

**[P2] Header, muted-cell, and count text fail WCAG AA contrast**
- **What**: Table header labels (`rgba(255,255,255,0.35)` on `#0d0d0d`) compute to roughly 3.19:1; muted cells for Outfit/Email/Phone (`rgba(255,255,255,0.4)`) to roughly 3.8:1; the results count (`rgba(255,255,255,0.25)`) to roughly 2.2:1 — all below the 4.5:1 AA floor for normal text, and the count fails even the relaxed 3:1 large-text floor.
- **Why it matters**: This is the outfit/email/phone data and the live results-count indicator an admin needs to read quickly, plausibly in bright outdoor light on event day.
- **Fix**: Raise these to roughly `rgba(255,255,255,0.55–0.6)` or equivalent to clear 4.5:1, verified against real contrast tooling rather than eyeballing.
- **Suggested command**: `/impeccable harden`

**[P2] Filter chips have no `aria-pressed`, and the CSV filename has no time component**
- **What**: The volunteer filter chips (`All` / `Volunteers` / `Non-volunteers`) toggle purely via a CSS `.active` class with no `aria-pressed` attribute, so a screen-reader user gets no state feedback on which is selected. Separately, the export filename (`players-${date}.csv`) has no time component, so a same-day re-export silently becomes a browser-auto-renamed `(1)`, `(2)` file with nothing distinguishing which is current — reproduced live.
- **Why it matters**: Both are small, concrete gaps in an otherwise carefully-built feature, and the filename collision compounds the P1 scope-ambiguity issue on exactly the kind of day where an admin might export more than once.
- **Fix**: Add `[attr.aria-pressed]` bound to each chip's active state; add `HH-mm` to the export filename.
- **Suggested command**: `/impeccable harden`

**[P3] Inconsistent focus-ring styling within one toolbar, and no `aria-live` on filtered results**
- **What**: Search input, category select, sortable `th`, and the team-pill link all get a custom red `focus-visible` outline, but the filter chips and export button fall back to the browser's default blue outline — a visible mismatch inside a single small toolbar. Separately, neither the results count nor the empty-search-results message is wrapped in `aria-live`, so a screen-reader user who searches to zero results gets no spoken confirmation.
- **Why it matters**: Minor on their own, but both are exactly the kind of small, stackable gap that compounds for a keyboard/AT user working this page.
- **Fix**: Add matching `:focus-visible` red-outline rules to `.chip` and `.export-btn`; add `aria-live="polite"` to the count span and empty-state container.
- **Suggested command**: `/impeccable harden`

**[P3] Sort-icon glyphs aren't hidden from assistive tech**
- **What**: The raw Unicode sort glyphs (`↕ ↑ ↓`) are exposed to screen readers alongside the already-correct `aria-sort` attribute doing the same job.
- **Why it matters**: Redundant/confusing double-announcement for screen-reader users on every sortable header.
- **Fix**: Add `aria-hidden="true"` to the sort-icon spans — a one-line fix.
- **Suggested command**: `/impeccable harden`

#### Persona Red Flags

- **Alex (impatient power user)**: Far better served than the prior pass — instant search-as-you-type, one-click/one-keypress sort, and a visible toolbar all suit Alex's style. The one place Alex gets burned is exactly the P1: Alex moves fast, doesn't double-check scope before clicking Export, and would only discover a wrong export by opening the file later.
- **Sam (accessibility/dashboard-data persona)**: Three small gaps stack for Sam specifically: sub-AA contrast on the exact cells (muted data, results count) Sam is most likely to be zooming or overriding contrast for; unlabeled/un-stated filter-chip state meaning a screen reader can't confirm which filter is live; and inconsistent focus rings making visual scan-for-focus harder for a low-vision keyboard user in the same toolbar. None are fatal — the page remains fully operable by keyboard and AT — but together they're a "death by a thousand cuts" experience that would surface for Sam well before it would for Alex.

#### Minor Observations

- The live mutation scan re-confirmed the radial-gradient glow and (newly) a cramped-padding pattern on `.table-wrapper` (children flush to the container edge with no inset) — the glow is a carry-over from the prior snapshot's "decorative flourish vs. Operate-mode principle" note; the padding flag is plausibly an intentional full-bleed table treatment rather than a defect.
- The volunteer "No" badge is rendered so faintly (same low-opacity treatment as the failing count/header text) that it's nearly invisible — likely an intentional choice to make "Yes" pop for fast scanning, but it happens to fail contrast rules if read literally as informational text rather than de-emphasis.
- Two of the three P2/P3 issues carried over unresolved from the prior pass without being the focus of this fix: the generic, retry-less "Failed to load players." error message, and email/phone still being plain text rather than `mailto:`/`tel:` links — neither was addressed by b3bbc71, which focused on discoverability/export/navigation rather than error-recovery or contact-actionability.

#### Questions to Consider

- Given the roster is hard-capped at 104 players, is CSV really the right export format for a day-of check-in sheet, or would a print-optimized view (larger row height, a checkbox column, no email/phone noise) better serve a clipboard-at-the-door use case than a spreadsheet file?
- Should the export button make its scope an explicit, unavoidable part of the interaction (e.g. a confirm step showing row count) given how costly a silent wrong-scope export could be on event morning specifically?
- The admin shell still runs on the "Sports Reservation / SR" red/Bebas-Neue identity rather than the public-facing "Hyrox Police 54" branding — is this an intentional internal/external split, or should the cross-page brand-consistency question raised in the prior snapshot finally get resolved?
