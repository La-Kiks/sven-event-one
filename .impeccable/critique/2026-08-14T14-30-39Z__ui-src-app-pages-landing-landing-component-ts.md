---
target: landing page
total_score: 25
max_score: 32
na_heuristics: 7,10
p0_count: 0
p1_count: 2
timestamp: 2026-08-14T14-30-39Z
slug: ui-src-app-pages-landing-landing-component-ts
---
Method: dual-agent (A: design review · B: detector+browser evidence)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|---|---|---|
| 1 | Visibility of System Status | 3 | Sold-out/error states only swap button text, no live seats-remaining signal despite "Places limitées" copy |
| 2 | Match System / Real World | 4 | Real venue, organizer, partners, footage |
| 3 | User Control and Freedom | 3 | No back-to-top on long scroll; no escape from YouTube autoplay rabbit hole |
| 4 | Consistency and Standards | 3 | Google Maps embed is stock light-mode widget, breaks void-black discipline everywhere else |
| 5 | Error Prevention | 3 | Count-load failure fails closed (never oversells) but generic "Erreur" label has no retry path |
| 6 | Recognition Rather Than Recall | 4 | Hero stat strip restates everything needed to decide |
| 7 | Flexibility and Efficiency | n/a | Single-path marketing page, no power-user path to evaluate |
| 8 | Aesthetic and Minimalist Design | 3 | Partner logo tiles mix opaque-white and transparent assets, breaks filet-grid uniformity |
| 9 | Error Recovery | 2 | Error-state CTA routes to an anchor instead of surfacing phone/email directly |
| 10 | Help and Documentation | n/a | Persuade surface, appropriately n/a |
| **Total** | | **25/32** | **Good (78%)** |

## Design Specificity Verdict

**LLM assessment**: Grounded, not generic, with one seam. Real photos of officers in duty-adjacent gear at the actual venue, a marquee calling out the event's actual run-between-stations mechanic, a promo video shot on-site, a Maps pin on the real venue with the organizer's personal phone/email, and Police Nationale / Orphéopolis logos as first-class content — not swappable into a generic sports-signup template. The one generic seam: the final CTA banner copy ("Trouve ton binôme. Inscris ton équipe.") and Infos-pratiques block never reach for the charity/police angle PRODUCT.md names as differentiating positioning. "Orphéopolis" appears exactly once, in the smallest, grayest footer text on the page.

**Deterministic scan**: `detect.mjs` returned zero findings on `landing.component.html`/`.scss` (clean, exit 0) — confirmed independently by both agents across two runs.

**Visual overlays**: Browser-injected `detect.js` overlay evidence was blocked twice by the harness's own permission classifier on this target (a script-injection call it treated as sensitive) — no in-browser overlay findings were obtained. This is a genuine evidence gap, not a clean bill of health from that specific channel; the CLI scan and both agents' live visual walkthroughs stand in for it. Mobile check (390px, same-origin iframe): **pass**, zero horizontal overflow.

## Overall Impression

The landing page is the strongest-executed surface of the four — real photography, a distinctive visual system applied with discipline, and a genuinely useful hero stat strip. The single biggest opportunity: the page's two real trust assets (police organization, Orphéopolis charity beneficiary) are proven, specific, and completely absent from the three moments a visitor actually decides to click "S'inscrire."

## What's Working

1. **Hero stat strip** (8 ateliers / 8 km / 2 équipiers / 60 €) — answers what/how-hard/with-whom/how-much in one glance, in exactly the typographic voice DESIGN.md prescribes for key stats.
2. **The yellow marquee** — uses the system's one rationed-yellow exception correctly, communicating a real event mechanic rather than decorating.
3. **Real venue photography** in the format cards does double duty as format documentation and legitimacy signal.

## Priority Issues

**[P1] Charity/police trust signals are absent from every CTA moment**
- **Why it matters**: PRODUCT.md names trust/legitimacy as a core principle for exactly this reason — a stranger handing over money and personal data to what reads as a small independent operation needs the "police-organized, benefits a real charity" signal at the decision point, not three scrolls later.
- **Fix**: Add a compact trust line under the hero CTA row or in the eyebrow — e.g. "Au profit d'Orphéopolis" with the police-shield mark.
- **Suggested command**: `/impeccable clarify`

**[P1] "Places limitées" has no live evidence behind it**
- **Why it matters**: The scarcity claim is unsupported anywhere pre-click; a capacity-capped event (confirmed `MaxTeams` in PRODUCT.md) risks visitors completing the full two-player inscription form before discovering it's closed.
- **Fix**: Surface an actual count ("34/40 équipes inscrites") near the stat strip from the existing `TeamCountService`.
- **Suggested command**: `/impeccable clarify`

**[P2] Google Maps embed breaks the void-black visual system**
- **Fix**: Style via the Maps Embed API's style parameters for a dark theme, or use a static styled map matching the hero's grayscale treatment.
- **Suggested command**: `/impeccable colorize`

**[P2] Partner logo tiles have inconsistent backgrounds**
- **Fix**: Normalize all 12 sponsor assets to transparent PNG/SVG at a consistent max-height.
- **Suggested command**: `/impeccable polish`

**[P3] Error-state CTA is a dead end**
- **Fix**: Have the error-state label resolve to `tel:`/`mailto:` directly rather than routing through the `#infos` anchor.
- **Suggested command**: `/impeccable harden`

## Persona Red Flags

**Jordan (first-timer)**: "Réservé aux forces de l'ordre et personnels assimilés" — "personnels assimilés" is never defined; a civilian partner/spouse has no way to know if they're eligible before starting the form. The only legitimacy signal (police/charity framing) is in the smallest, grayest footer text — exactly the visitor most likely to need it up front.

**Riley (stress-tester)**: Header and hero CTAs both bind to the same `isRegistrationFull`/`countLoadError` flags from one `ngOnInit` subscribe with no loading state — on a slow connection both render as active "S'inscrire" for the request duration, then can silently flip to "Complet"/"Erreur" after the visitor started reading.

**Casey (mobile)**: `.hero__stats` uses `auto-fit, minmax(150px, 1fr)` — likely forces the 4-stat row to 2×2 at 375px; not a bug (confirmed zero horizontal overflow) but worth a deliberate visual check since it's a readability question, not an overflow one.

## Minor Observations

- Marquee text is static, not an actually scrolling marquee despite the class name.
- "Se connecter" sits at equal visual weight to "S'inscrire" in the header for an audience that's ~100% new-registration intent.
- Footer contact line duplicates the Infos-pratiques phone number verbatim — that space could carry the Orphéopolis/police reinforcement instead.

## Questions to Consider

1. If "Places limitées" is the scarcity hook the copy leans on, what changes if the actual seats-remaining count sat next to the price in the hero, live?
2. What would this page look like if the charity beneficiary were the headline and the workout format were the supporting detail, rather than the reverse?
3. Every trust signal on this page is real and specific — why is the actual moment of conversion the one place none of it appears?
