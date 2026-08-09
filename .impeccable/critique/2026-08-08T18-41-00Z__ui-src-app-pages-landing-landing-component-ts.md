---
target: landing page
total_score: 24
max_score: 36
na_heuristics: 7
p0_count: 1
p1_count: 2
timestamp: 2026-08-08T18-41-00Z
slug: ui-src-app-pages-landing-landing-component-ts
---
Method: dual-agent (A: a51bd3dc8b5283327 · B: af9b1a4bf2cb2ae62)

#### Fix Verification (from both prior critiques)

| Fix (source critique) | Status | Evidence |
|---|---|---|
| CTAs no longer force a new tab (run 1) | **PASS** | `button.component.html` still carries no `target` attribute |
| Hero `<h1>` sized correctly / single-h1 hierarchy (run 1) | **PASS** | `.title h1` is `$text-4xl`, `.section-heading` is `$text-2xl`; one `<h1>`, four consistent `<h2 class="section-heading">`s |
| Sold-out contrast + copy + next step (run 1) | **PASS** | `.btn--full { color: #c9c08a }` on the dark bright-background token, ~8:1 contrast; copy states a next step |
| Dead scroll-cue revived as wayfinding (run 1) | **PASS (partial scope)** | Real `<button aria-label>` calling `scrollTo('part-two')` → live target; still only bridges hero→format, not extended to later sections |
| Mobile: hero video was pushing CTA below the fold (run 2, P1) | **PASS** | `.video-wrapper iframe` now uses `aspect-ratio: 16/9` with `max-width: 900px` instead of the old `min-height: clamp(400px, 75vw, 600px)` — confirmed via `git show b7a04d1` |
| Sold-out anchor lands on Localisation, not Contact (run 2, P2) | **STILL OPEN** | `fullLink="#part-four"` unchanged; `#part-four` still opens on the Localisation heading/map before Contact |
| Down-arrow has no guaranteed contrast backdrop (run 2, P2) | **STILL OPEN** | `.down-arrow` is still bare `color: $main-color` over the hero photo, no scrim/shadow added |
| No loading state during registration-count check (run 2, P2) | **STILL OPEN** | `isRegistrationFull` still initializes `false` and flips post-fetch with no loading affordance |
| Event date still unspecified (run 2, P3) | **STILL OPEN** | Copy is still "Rendez-vous en Septembre" |
| Sponsor alt text still filename-derived (run 2, P3) | **STILL OPEN** | `alt="sponso-bfm"`, `alt="sponso-policenationale"`, etc., unchanged |
| No eligibility/organizer/charity statement (run 2, P3) | **STILL OPEN** | `.description` paragraph and sponsor grid unchanged; contact still a personal phone/`@orange.fr` address |

#### Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 2 | Sold-out check is async with no loading affordance, and (new this round) a genuine fetch failure renders **identically** to a genuine sellout — no signal distinguishes an outage from a real cap |
| 2 | Match System / Real World | 4 | Authentic imagery (real officers, real venue signage), real address/sponsors, domain-appropriate copy — no mismatch issues found this round |
| 3 | User Control and Freedom | 2 | Scroll-cue only bridges hero→format; nothing helps navigate or return across the remaining sections of a long single-page scroll |
| 4 | Consistency and Standards | 4 | Heading hierarchy and component reuse remain fully consistent, confirmed unchanged since last pass |
| 5 | Error Prevention | 3 | `TeamCountService` fails closed (safety-first default against overselling), but the sold-out CTA's promised "voir les contacts" still overshoots the actual Contact block |
| 6 | Recognition Rather Than Recall | 2 | New finding: the only CTA lives at the very top of a long page — a visitor persuaded later by the format cards, sponsor wall, or map must scroll all the way back up with no repeated/sticky CTA to act on formed intent |
| 7 | Flexibility and Efficiency of Use | n/a | Persuade-mode single-path page; no power-user shortcut meaningfully applies |
| 8 | Aesthetic and Minimalist Design | 3 | Disciplined black/yellow/Lemon+Cabin system holds; undercut by the undifferentiated 12-logo sponsor grid and one unconstrained paragraph running ~150 chars/line on desktop |
| 9 | Error Recovery | 2 | The count-fetch failure path and a genuine full roster produce byte-identical UI ("Inscriptions complètes") — neither a visitor nor the team can tell an outage from a real cap; compounds the still-open anchor-precision gap |
| 10 | Help and Documentation | 2 | No FAQ, no eligibility statement, no exact date, no glossary for the 8 exercise names — unchanged from last pass |
| **Total** | | **24/36** | **Good, with new depth found** (9 heuristics scored, heuristic 7 n/a for this Persuade surface) |

**Score trajectory: 15/36 → 25/36 → 24/36.** This is not a regression in the code — every fix verified above from both prior passes either holds or (for the mobile-video P1) has since been fixed in a later commit not yet re-scored. The 1-point dip is because this pass's assessors dug into an angle the first two didn't: the sold-out/error-state conflation (heuristic 9) and the lack of a persistent CTA on a long scroll (heuristic 6) are newly surfaced, real issues, not new defects introduced by the fixes.

#### Design Specificity Verdict

**Mostly bespoke, with one recurring generic seam.** The hero video, the 8 station photos (real officers, visible gym signage), the 12 real sponsor logos, and the real venue/address/phone are genuinely authored for this event — not templated. But the one sentence doing the page's actual persuasion work ("Cette compétition est l'enchainement d'épreuves combinant course à pied et ateliers fonctionnels pour tester l'endurance, la force et la polyvalence des participants") could describe any functional-fitness race anywhere — it still never mentions police, the charity, or the cause, exactly as flagged (differently worded) in both prior critiques. Nothing states an exact date (day/year), and Orphéopolis — the actual reason for the event's existence per product context — still gets identical visual weight to Intersport in an anonymous logo grid. Deterministic scan is clean on both the `.ts` and `.html` files (0 findings each); the one live-DOM finding (a `line-length` flag on that same generic-copy paragraph — `.description` in `landing.component.scss:112-117` has no `max-width` unlike sibling rules that cap at 900px/400px) is a genuine, confirmed defect, not a false positive — and it happens to sit on exactly the sentence most in need of a rewrite.

#### Overall Impression

The two prior critique/fix cycles did real, verifiable work: the CTA new-tab bug, the inverted heading hierarchy, the sold-out button's contrast, the dead scroll-cue, and (per a later commit, `b7a04d1`) the mobile video pushing the CTA below the fold are all confirmed fixed in the current source. What's left is no longer "broken UI," it's two classes of gap that have now survived three passes: (1) a handful of specific, still-open mechanical items from run 2 (sold-out anchor imprecision, no loading state, missing down-arrow contrast backdrop, filename alt text, no exact date) that keep getting deprioritized behind higher-severity work, and (2) a newly surfaced, more consequential pair of findings — the sold-out state can't be told apart from a genuine backend failure, and the page's only CTA has no persistent presence past the hero. The trust/charity story (police organization + Orphéopolis) also remains where run 1 first flagged it: present in content, absent from emphasis.

#### What's Working

- All four fixes verified in run 2 as landing correctly (CTA tabbing, heading hierarchy, sold-out contrast, scroll-cue) still hold under fresh, independent re-inspection — nothing regressed.
- The mobile video-height fix (`aspect-ratio` replacing a forced `min-height` clamp) is a real, structural fix to the P1 flagged in run 2, confirmed via the `b7a04d1` diff.
- `TeamCountService`'s fail-closed default (`isFull = true` on fetch error) is a defensible, safety-first design choice against overselling a capacity-capped, paid event — even though its downstream messaging needs work (see Priority Issues).

#### Priority Issues

**[P0] A genuine backend/network failure is indistinguishable from a genuine sellout**
- **Why it matters**: `LandingComponent.ngOnInit` sets `isRegistrationFull = true` on both a real full roster and any fetch error, and the template shows identical "Inscriptions complètes — voir les contacts" copy either way. During exactly the moment this matters most — a traffic spike from a shared link, or a backend hiccup — every simultaneous visitor would be silently told registration is closed with zero signal to differentiate it from a real cap, and the team would have no way to know from the page alone.
- **Fix**: Add a distinct error state (e.g., "Impossible de vérifier les places disponibles — réessayez ou contactez-nous") separate from the sold-out copy, and log/surface count-fetch failures so they don't masquerade as capacity events.
- **Suggested command**: `/impeccable harden`

**[P1] No persistent CTA on a long single-page scroll**
- **Why it matters**: The only "S'inscrire" button sits at the very top of the page, before the video. A visitor whose intent to register forms later — after the format cards, the sponsor/trust wall, or the map — has no CTA to act on without scrolling all the way back to the hero. For a Persuade-mode page whose entire job is conversion, this is a direct, structural leak.
- **Fix**: Add a repeated or sticky CTA (e.g., a condensed sticky footer bar with the same button) so conversion doesn't depend on backtracking.
- **Suggested command**: `/impeccable layout`

**[P1] Sold-out CTA still promises "voir les contacts" but lands on Localisation, not Contact**
- **Why it matters**: Carried over unresolved from the previous critique — `fullLink="#part-four"` anchors to the top of `part-four` (the Localisation heading + map), not the Contact block further down. The exact moment a blocked user needs a human contact fastest is where the promise falls short.
- **Fix**: Add `id="contact"` to the Contact heading/block and point `fullLink` at `#contact` instead of `#part-four`.
- **Suggested command**: `/impeccable harden`

**[P2] The one sentence meant to sell the event is both generic and typographically unconstrained**
- **Why it matters**: The `.description` copy in part-two never mentions police, Orphéopolis, or the cause — it reads as stock functional-fitness copy, a specificity gap flagged (in different words) by both prior critiques. Compounding it, live-DOM detection confirms `.description` (landing.component.scss:112-117) has no `max-width`, unlike sibling rules that cap at 900px/400px — on desktop this renders the paragraph at ~150 characters/line, well past readable line length, on exactly the sentence doing the page's persuasion work.
- **Fix**: Rewrite the paragraph to name the police/charity angle explicitly, and give `.description` a `max-width` (e.g., 65-75ch) consistent with its sibling containers.
- **Suggested command**: `/impeccable clarify`

**[P3] Trust-building details keep getting deprioritized across all three passes**
- **Why it matters**: Orphéopolis and Police Nationale — the event's actual credibility premise — still render at identical size to commercial sponsors in an unlabeled 12-logo grid with filename `alt` text (`alt="sponso-orpheopolis"`); the event date is still just "en Septembre" with no day/year; and contact is still a personal phone/`@orange.fr` address with no organizing body named. None of these are new — all three were flagged in run 1 or run 2 and remain open.
- **Fix**: Promote Police Nationale + Orphéopolis into a labeled "organisé par / au profit de" block, replace filename alt text with real sponsor names, and state an exact date.
- **Suggested command**: `/impeccable bolder`

#### Persona Red Flags

- **Jordan (confused first-timer)**: Still never told the event requires police affiliation or what exactly "Hyrox" is before committing; the eventual handoff from this branded page to an external Yurplan checkout (confirmed intentional per product decision) is never previewed, which could read as a late-funnel trust wobble even though the redirect itself isn't a bug.
- **Riley (deliberate stress-tester)**: Would immediately find that throttling or failing the count endpoint produces the exact same "sold out" UI as a genuinely full roster (the P0 above) — a reliable, low-effort way to make the site falsely claim registration is closed. Also still notices the sold-out anchor overshoots Contact.
- **Casey (distracted mobile user)**: The mobile-video-below-fold issue from run 2 is now fixed, which helps Casey reach the CTA faster — but once past it, Casey has no sticky CTA to fall back on if they keep scrolling instead of converting immediately, and the `.part-four` Tel/Mail two-column grid still has no mobile-collapsing rule, risking a squeezed email address on narrow phones.

#### Minor Observations

- `.landing { overflow: scroll }` still always renders scrollbars instead of `overflow-y: auto` (pre-existing, untouched across all three passes).
- `.down-arrow` remains bare `$main-color` directly over the photographic hero background with no scrim/shadow — contrast against the photo is still uncontrolled.
- Sponsor `<img>` tags still hardcode `width="200" height="200"` while CSS caps size differently (400px) at the ≥768px breakpoint.
- Sponsor image filenames still mix `.JPG`/`.JPEG`/`.PNG` case inconsistently.
- The live count data (`current`/`max`) that already powers the sold-out gate is never surfaced as a scarcity/urgency cue anywhere on the page — a missed lever specific to a hard-capped event.
- Deterministic CLI scan (`detect.mjs`) is clean (0 findings) on both `landing.component.ts` and `landing.component.html`; the only live-DOM finding was the `.description` line-length issue addressed above.

#### Questions to Consider

- Given the count-fetch failure path currently reads identically to "sold out," has this fallback ever been exercised against a real backend outage — or does every simultaneous visitor during a hiccup currently see a false "complet"?
- If Orphéopolis is the actual reason this event exists, what would conversion look like if the page led with the charity's story instead of burying it in a 12-logo grid equal to Intersport?
- Is a sticky/repeated CTA off the table for a deliberate design reason (e.g., wanting the page to read as one clean narrative), or is it simply not yet built?
