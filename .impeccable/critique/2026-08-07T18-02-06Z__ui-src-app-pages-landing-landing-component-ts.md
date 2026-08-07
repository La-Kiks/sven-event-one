---
target: landing page
total_score: 15
max_score: 36
na_heuristics: 7
p0_count: 2
p1_count: 2
timestamp: 2026-08-07T18-02-06Z
slug: ui-src-app-pages-landing-landing-component-ts
---
Method: dual-agent (A: a567659137027eb8b · B: ad91a90e9454c84d8)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 2 | Sold-out state is fetched async and can flash from "open" to "closed" after load |
| 2 | Match System / Real World | 2 | English gym jargon ("SKI ERG", "SLED PUSH", "WALL BALLS") dropped into French copy unexplained; no eligibility statement |
| 3 | User Control and Freedom | 1 | No nav/anchors between the 4 sections; both CTAs force `target="_blank"` on an internal Angular `routerLink` |
| 4 | Consistency and Standards | 3 | Solid component reuse (button/card), undercut by inconsistent FR/EN station naming and inverted heading hierarchy |
| 5 | Error Prevention | 1 | Sold-out determined client-side after the CTA already renders as live; no eligibility pre-check |
| 6 | Recognition Rather Than Recall | 2 | Sponsor logos uncaptioned; no glossary for exercise names |
| 7 | Flexibility and Efficiency of Use | n/a | Persuade-mode single-path page; no power-user path applies |
| 8 | Aesthetic and Minimalist Design | 3 | Disciplined single-accent black/yellow system, undercut by the flattened 12-logo grid |
| 9 | Error Recovery | 1 | "INSCRIPTIONS FERMÉ" renders at ~2.5:1 contrast (fails AA 4.5:1) with no next step (waitlist, next edition) |
| 10 | Help and Documentation | 0 | No FAQ, no glossary for 8 unexplained exercise names, no schedule/what-to-bring info |
| **Total** | | **15/36** | **Poor** (9 heuristics scored, heuristic 7 marked n/a for this Persuade surface) |

## Design Specificity Verdict

**LLM assessment**: The page's *content* is specific — real Hyrox station names, the actual Laxou address, named sponsors including Police Nationale and Orphéopolis, a personal organizer contact. But the *design system's application* is generic: a stock-feeling hero photo, a plain description-paragraph → numbered-card-grid → logo-wall → map-and-contact stack that could belong to any local 5K or CrossFit throwdown. Nothing on the page visually dramatizes "this is police-organized" or "this funds a police-orphans charity" — those facts exist only as one unlabeled logo apiece inside a 12-up grid. Verdict: specific content poured into a generic template, not a page authored around the event's two real differentiators (institutional legitimacy, charitable cause).

**Deterministic scan**: Clean on the landing page itself and the button component (0 findings each). One finding on the shared card component: `broken-image` rule at `card.component.html:3` (`<img [src]="image" alt="card image">`), flagging that a dynamically-bound `<img>` with no fallback ships as a broken-image box if the binding is ever empty.

**False positive check**: Verified against the landing page's actual usage — every `<app-card>` instance passes a real static string (`image="images/ski-erg.webp"`, etc.), so this specific finding is a false positive *for this page*. It's a legitimate defensive-coding gap in the shared component itself (no fallback if a future caller omits `image`), just not a live defect here.

**Visual overlays**: Not available this run — no local server was reachable (Docker Desktop offline, and this project only ever serves the app via `docker compose up --build`, never a bare dev server), so no browser injection/overlay was attempted.

## Overall Impression

The visual system (black/yellow/grayscale-photo, Lemon Milk + Cabin) is disciplined and consistent where it's applied — the problem isn't taste, it's that the page's structure and hierarchy work against its own conversion goal and its own credibility story. The event's own name renders smaller than its section headers, both CTAs force an unwanted new tab, the sold-out state is nearly invisible, and the one thing that makes this event trustworthy (police organization + a named charity) is buried in an anonymous 12-logo grid. Biggest opportunity: fix the mechanical hierarchy/navigation bugs first (cheap, high-impact), then make the police + charity story load-bearing instead of decorative.

## What's Working

- **Grayscale filter on the 8 station photos** is a genuinely deliberate, specific choice — it keeps every photo inside the black/yellow accent system instead of letting stock-photo color noise compete with the palette.
- **Component reuse discipline** — `app-button` and `app-card` are consistently reused for both CTAs and all 8 stations, keeping the visual language predictable even where content varies. The detector's clean scan on both components confirms this structurally.
- **`loading="lazy"` on the Google Maps iframe** shows some performance awareness, even though it wasn't extended to the YouTube iframe or the sponsor images.

## Priority Issues

**[P0] Both CTAs open an internal route in a new tab**
- **Why it matters**: `button.component.html` sets `target="_blank"` on an `<a>` that also carries Angular's `[routerLink]`, so "S'INSCRIRE EN DUO - 60€" and "Déjà inscrit ? Se connecter" — the page's only two calls to action — fragment the session into a second tab, break the back button, and can misbehave in mobile in-app browsers (e.g. an Instagram-bio tap). Directly works against the "fast, low-friction registration" product principle.
- **Fix**: Remove `target="_blank"`/`rel` from internal `routerLink` anchors; reserve `target="_blank"` for genuinely external links.
- **Suggested command**: `/impeccable harden`

**[P0] The event's own name renders smaller than its section headers**
- **What**: `.title { font-size: $text-xs; }` sets the hero `<h1>` wrapper to the smallest token in the type scale, while `.cards h1`, `.part-three h1`, `.part-four h1` inherit the browser default (~2em), which renders larger.
- **Why it matters**: "Hyrox Police 54" — the single most important string on the page — visually loses to "Partenaires" and "Localisation," inverting the intended hierarchy on the primary conversion surface.
- **Fix**: Give the hero `<h1>` an explicit large token (`$text-3xl`/`$text-4xl`) instead of inheriting ambient sizing.
- **Suggested command**: `/impeccable typeset`

**[P1] Sold-out state is nearly invisible and offers no next step**
- **Why it matters**: `.btn--full` is `#666666` on `#2a2a2a` — roughly 2.5:1 contrast, failing WCAG AA's 4.5:1 minimum — and the label ("INSCRIPTIONS FERMÉ") offers no waitlist, next-edition, or contact option. This directly contradicts the product requirement that the capacity-capped, time-boxed nature of the event be clearly communicated, not fail silently.
- **Fix**: Raise the disabled-state contrast to pass AA; add a one-line next step ("Complet — contactez-nous pour la liste d'attente").
- **Suggested command**: `/impeccable clarify`

**[P1] No wayfinding across the page's 4 stacked sections**
- **Why it matters**: Hero → format cards → sponsors → location run one after another with no in-page nav, progress cue, or jump links. `landing.component.ts` has an unused `scrollTo(id)` method and an orphaned `.down-arrow` class in the SCSS — neither is referenced in the template, indicating a wayfinding affordance was built and then disconnected. A first-time visitor can't gauge how much page remains or skip to what they need.
- **Fix**: Wire the existing scroll-indicator code back into the template, or add lightweight section anchors/progress dots.
- **Suggested command**: `/impeccable layout`

**[P2] Sponsor grid flattens the credibility hierarchy**
- **Why it matters**: 12 sponsor images — including Police Nationale and Orphéopolis, the event's actual credibility premise per the product context — render at identical size in an unlabeled grid, with filename-as-alt-text (`alt="sponso-bfm"`) and `object-fit: cover` risking crops of wordmark logos. Burying the two logos that establish legitimacy at equal weight to a local gym undersells the one thing that makes this event trustworthy, and screen-reader users get no meaningful label for any sponsor.
- **Fix**: Promote Police Nationale + Orphéopolis into a labeled, larger "organized by / supporting" block near the CTA; give remaining sponsors real alt text; switch to `object-fit: contain`.
- **Suggested command**: `/impeccable bolder`

## Persona Red Flags

**Jordan (Confused First-Timer)**: "1000m SKI ERG," "50m SLED PUSH," "80m BURPEES BROAD JUMP," "200m FARMERS CARRY," "100x WALL BALLS" — untranslated gym jargon with no tooltip or definition, sitting beside French copy. No line anywhere explains what "Hyrox" itself is before the CTA — a first-timer can register without ever getting a plain-language explanation of what they're signing up for.

**Riley (Deliberate Stress Tester)**: Sold-out renders as a low-contrast dead end with zero alternative action. The YouTube iframe has no `loading` attribute, poster, or fallback markup — a failed load leaves a blank 400-600px box directly above the CTA. Hero background image + eager YouTube iframe + Maps iframe all compete for bandwidth; only Maps is lazy-loaded. The `target="_blank"` + `routerLink` combination on both CTAs is exactly the kind of edge case this persona would catch first.

**Casey (Distracted Mobile User)**: `.video-wrapper iframe` reserves `min-height: clamp(400px, 75vw, 600px)` — on a typical phone the CTA sits below at least 400px of video, well past a single thumb-scroll. The two CTAs are pulled close together (`margin-top: -1.5rem` on the secondary), raising mis-tap risk. None of the 12 sponsor images carry `loading="lazy"`, so a mobile visitor on a slow connection eagerly downloads all 12 logos plus the hero image and video iframe before reaching anything actionable.

**Police-officer persona (trust/legitimacy)**: Police Nationale's logo is one of 12 equally-sized, uncaptioned tiles — nothing states "organized by" or "in partnership with" the police. Orphéopolis, the named charity, is likewise just a logo tile with no sentence explaining what it is or that entry fees support it. The sole contact channel is a personal mobile number and a personal `@orange.fr` address — reads as one individual's side project rather than a sanctioned institutional event. "Rendez-vous en Septembre" gives no specific date, so an officer requesting time off can't plan around it. No copy confirms the event is restricted to police/law-enforcement duos, so this persona can't self-confirm eligibility before starting registration.

## Minor Observations

- `scrollTo()` in `landing.component.ts` and `.down-arrow` in the SCSS are dead code, never referenced in the template.
- Sponsor `<img>` tags hardcode `width="200" height="200"` while CSS caps size differently at the 768px breakpoint (400px) — HTML attributes no longer match rendered size.
- Sponsor image filenames mix `.JPG`/`.JPEG`/`.PNG` case inconsistently.
- `<h1>Partenaires : </h1>` has a stray space before the colon.
- Google Maps iframe has no `title` attribute (screen-reader gap).
- Station naming mixes French ("FENTES," "1 km RUN") and English ("SLED PUSH," "WALL BALLS") registers inconsistently.
- The shared `card.component.html` `<img [src]="image">` has no fallback for a missing/empty binding (detector-flagged; not currently triggered on the landing page, but worth hardening since the component is reused elsewhere).

## Questions to Consider

- Is "sometime in September" a placeholder for a date not yet locked, or an oversight — given the event is capacity-capped, shouldn't a specific date with a countdown be the strongest urgency lever on the page rather than absent?
- Should the police affiliation and Orphéopolis partnership be pulled out of the anonymous 12-logo grid into their own trust-building block near the CTA, given they're the entire credibility premise?
- Was `target="_blank"` on the CTA buttons intentional, or a leftover from a different link pattern — because applied to internal routes it works directly against the low-friction-registration goal?
