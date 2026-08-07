---
target: landing page
total_score: 25
max_score: 36
na_heuristics: 7
p0_count: 0
p1_count: 1
timestamp: 2026-08-07T18-35-35Z
slug: ui-src-app-pages-landing-landing-component-ts
---
Method: dual-agent (A: ab461069214431e20 · B: a54d4d3b556a36162)

## Fix Verification (from the prior critique's action items)

| Fix | Status | Evidence |
|---|---|---|
| CTAs no longer force a new tab | **PASS** | `button.component.html` has no `target` attribute anywhere |
| Hero `<h1>` sized correctly, single-h1 hierarchy | **PASS** | `.title h1` is `$text-4xl`, strictly larger than `.section-heading`'s `$text-2xl` at every viewport; exactly one `<h1>` on the page, four consistent `<h2 class="section-heading">`s |
| Sold-out state: contrast + copy + next step | **PARTIAL** | Contrast fixed (`#c9c08a` on `#262626` ≈ 8.2:1, well past AA). Copy fixed. But `fullLink="#part-four"` lands on the Localisation heading, not the Contact block the copy promises ("voir les contacts") — reachable, but imprecise |
| Wayfinding: dead scroll-cue revived | **PASS** | Real `<button>` with `aria-label`, calls `scrollTo('part-two')` → a live `#part-two` target |

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 2 | Registration-full check is async with no loading state; CTA can flip active→sold-out after paint |
| 2 | Match System / Real World | 3 | Clear French copy, domain terms fit the audience |
| 3 | User Control and Freedom | 2 | No in-page nav beyond the single down-arrow; one-directional |
| 4 | Consistency and Standards | 4 | Heading hierarchy and `.section-heading` role now fully consistent |
| 5 | Error Prevention | 3 | Sold-out fails safe (defaults to full on fetch error); contact anchor imprecision is minor |
| 6 | Recognition Rather Than Recall | 3 | Labels/icons self-explanatory |
| 7 | Flexibility and Efficiency of Use | n/a | Persuade-mode single-path page |
| 8 | Aesthetic and Minimalist Design | 3 | Consistent black/yellow/Lemon+Cabin identity; Tel/Mail grid-span needs a render check |
| 9 | Error Recovery | 3 | Safe fallback, but silent (no visible message when the count fetch fails) |
| 10 | Help and Documentation | 2 | No eligibility statement, no exact date, no FAQ for a restricted, capped event |
| **Total** | | **25/36** | **Good** (9 heuristics scored, heuristic 7 marked n/a for this Persuade surface) |

**Previous run: 15/36 → This run: 25/36** — the 4 fixes materially moved the score.

## Design Specificity Verdict

**LLM assessment**: The fixes were executed correctly at the code level — contrast math checks out, the heading hierarchy is now genuinely valid HTML, and both dead interactions (new-tab CTA, dead scroll cue) are gone. But none of the fixes touched the surface's actual conversion risk: the primary CTA can sit below the fold on mobile behind a forced-tall embedded video, and trust-building copy (eligibility, exact date, organizer identity) is still absent. This is now a technically-correct page that hasn't yet been re-examined for whether it converts.

**Deterministic scan**: Clean on the landing page and button component (0 findings each). Same single finding as before on the shared `card` component (`broken-image`, dynamic `<img>` binding with no fallback) — still a false positive for this page's actual usage (static image paths), unchanged since it wasn't in scope of this pass.

**Visual overlays**: Not available this run either — the app is live and reachable (`HTTP 200` confirmed via curl at `localhost:7193`), but no browser automation tool is connected this session, so no live overlay/screenshot was possible. This is a tooling gap, not a Docker/server issue this time.

## Overall Impression

Real progress: the score moved from 15/36 (Poor) to 25/36 (Good), and every one of the 4 targeted fixes landed as intended in the source, with one imprecision (the sold-out anchor lands near, not on, the Contact block). What's left is no longer "broken," it's "not yet optimized for conversion" — a mobile-viewport CTA visibility risk, a missing loading state, and thin trust-building copy for a restricted, paid, capacity-capped event.

## What's Working

- The heading-hierarchy fix is structurally real, not cosmetic: `$text-4xl` vs `$text-2xl` is a verifiable, consistent gap, and the h1/h2 nesting is now valid across the whole page.
- The sold-out button's three-state pattern (active / full-with-link / full-disabled) is sensible and extensible, and the contrast fix is mathematically solid (~8.2:1).
- The black/yellow/Lemon Milk identity reads distinctive and consistent, not template-generic.

## Priority Issues

**[P1] Hero video can push the primary CTA below the fold on mobile**
- **Why it matters**: `.video-wrapper iframe` reserves `min-height: clamp(400px, 75vw, 600px)`, stacked between the title and the CTA. On a ~375-414px phone, title + video can consume the entire first viewport — directly undermining the "fast, low-friction registration" principle for the audience most likely to arrive on mobile.
- **Fix**: Reduce the video's forced minimum height at narrow breakpoints, or reposition the CTA so it's visible without scrolling on common phone sizes.
- **Suggested command**: `/impeccable layout`

**[P2] Sold-out CTA promises "voir les contacts" but lands on Localisation, not Contact**
- **Why it matters**: `fullLink="#part-four"` anchors to the top of the section (Localisation heading + map), not the Contact block further down — the copy sets an expectation the anchor doesn't precisely deliver, right at the moment a blocked user needs a human contact fastest.
- **Fix**: Add `id="contact"` to the Contact heading (or its wrapping block) and point `fullLink` at `#contact`.
- **Suggested command**: `/impeccable harden`

**[P2] Down-arrow icon has no guaranteed contrast against the hero photo**
- **Why it matters**: `.down-arrow` is solid `$main-color` yellow directly over a photographic background with no scrim or backdrop — contrast against a photo is uncontrolled, risking the icon washing out exactly where the image is light.
- **Fix**: Add a subtle drop-shadow or translucent circular backdrop behind the SVG so it stays legible regardless of the underlying image.
- **Suggested command**: `/impeccable harden`

**[P2] No loading state while the registration-count check resolves**
- **Why it matters**: `isRegistrationFull` starts `false` and can flip to `true` after an async fetch (or on fetch error) — a user can see an active CTA, click during that window, and get a sold-out result that reads as a bait-and-switch.
- **Fix**: Gate the CTA render (or show a neutral/loading variant) until the count check resolves.
- **Suggested command**: `/impeccable harden`

**[P3] Event date is still unspecified ("Rendez-vous en Septembre")**
- **Why it matters**: For a capacity-capped, time-boxed event, no exact date undercuts urgency and makes the page harder to verify against a calendar or search result.
- **Fix**: State the exact date near the title or CTA — no date was supplied to PRODUCT.md, so this needs a real answer from the organizer before it can ship.
- **Suggested command**: `/impeccable clarify`

**[P3] Sponsor alt text is filename-derived, not descriptive**
- **Why it matters**: `alt="sponso-bfm"`, `alt="sponso-policenationale"`, etc. — screen-reader users can't identify the actual partners in exactly the section meant to build credibility.
- **Fix**: Replace with real sponsor names.
- **Suggested command**: `/impeccable polish`

**[P3] No eligibility, organizer-identity, or charity statement in copy**
- **Why it matters**: Police-only eligibility and the Orphéopolis charity tie-in are only implied via unlabeled logos; contact is a personal address/number with no organizer name stated — a soft trust deficit for an audience deciding whether to pay 60€ upfront.
- **Fix**: Add a short organizer/eligibility/cause line near the hero or before Contact.
- **Suggested command**: `/impeccable clarify`

## Persona Red Flags

- **Jordan (first-timer)**: Still never told the event requires being an active police officer; still no exact date to check against their schedule.
- **Riley (stress tester)**: Clicks the sold-out CTA expecting contact info, lands on the map/Localisation heading instead. Also notices the YouTube iframe still has no `loading="lazy"` while the Maps iframe does.
- **Casey (mobile)**: May never scroll past the ~400-600px video to discover the register button at all if arriving via a shared link expecting it above the fold.
- **Police-officer trust persona**: Contact is still a personal Orange.fr address/cell number with no organizing body named in visible copy; the Orphéopolis charity partnership is still only an unlabeled logo.

## Minor Observations

- `.landing { overflow: scroll }` always renders scrollbars instead of `overflow-y: auto` — pre-existing, untouched by this pass.
- `.part-one { height: 100% }` is likely dead CSS (no ancestor defines an explicit height).
- In `.part-four`, the Tel/Mail `<p>`s don't get `grid-column: span 2` like their sibling headings — may render side-by-side in the 2-column grid rather than stacked; needs a render check.
- All 12 sponsor images are still eager-loaded (no `loading="lazy"`), unlike the lazy-loaded Maps iframe.
- Sponsor `img width="200" height="200"` vs. CSS `max-width: 400px` at ≥768px — minor layout-shift risk, unchanged from before.

## Questions to Consider

- Is the exact competition date genuinely still TBD, or just missing from copy — worth locking down since it's now the single biggest remaining trust/urgency gap?
- Should the sold-out anchor go to a dedicated `#contact` id, or is landing on `#part-four` (Localisation + Contact together) intentional?
- Is the personal `orange.fr` contact address intentional for a small, personally-run event, or should it move to an organizational address as registration scales?
