---
target: not-found page
total_score: 17
max_score: 28
na_heuristics: 5,7,10
p0_count: 1
p1_count: 2
timestamp: 2026-08-08T09-44-19Z
slug: ui-src-app-pages-not-found-not-found-component-ts
---
#### Design Health Score

| # | Heuristic | Score | Justification |
|---|---|---|---|
| 1 | Visibility of system status | 3/4 | "404" + "Page introuvable" unambiguously signals the error; no explicit ARIA status region for assistive tech. |
| 2 | Match between system and real world | 3/4 | Plain, correct, appropriately low-key French copy. |
| 3 | User control & freedom | 2/4 | Only one exit path (home link). No browser-back affordance, no search, no link back into the registration funnel. |
| 4 | Consistency & standards | 1/4 | Internally consistent with itself, but diverges sharply from the rest of the app's brand system — different fonts, different color system, different button shape. |
| 5 | Error prevention | N/A | A 404 catch-all is itself the recovery surface, not an input-error-prone flow — nothing to score. |
| 6 | Recognition rather than recall | 3/4 | Button label is explicit ("Retour à l'accueil"); no memory burden on the visitor. |
| 7 | Flexibility and efficiency of use | N/A | No power-user shortcuts are meaningfully applicable to a static error page. |
| 8 | Aesthetic and minimalist design | 2/4 | Minimal, but the minimalism reads as *unbranded* rather than deliberately restrained — it's a different visual system, not a quieter version of this one. |
| 9 | Help recognize/diagnose/recover from errors | 3/4 | Clear statement plus one clear recovery action; would reach 4 with a second path (e.g., an inscription CTA). |
| 10 | Help and documentation | N/A | No help/documentation surface is expected on a 404; not applicable. |

**Total: 17/28 applicable points (≈61%)** — functionally recoverable, but poorly integrated with the brand and the registration funnel. Three heuristics (5, 7, 10) are N/A for a static catch-all error page and excluded from the denominator.

#### Design Specificity Verdict

Generic, off-the-shelf 404 template — not a bespoke extension of this app's identity. Both assessments converge on the same evidence:

- **Fonts**: the page imports Bebas Neue + DM Sans from the Google Fonts CDN (`@import url("https://fonts.googleapis.com/css2?family=Bebas+Neue&family=DM+Sans...")`). Every other page in the app uses self-hosted `Lemon` (Lemon Milk) and `Cabin` font files via `@font-face` in `ui/src/styles/_fonts.scss`. This is the only page in the codebase reaching out to an external font CDN, and it's the wrong typeface family entirely.
- **Color**: the page runs a near-black/red system (`#0d0d0d` background, `#dc2626`/`#f87171` accents). The app's actual brand tokens (`ui/src/styles/_variables.scss`) are `$main-color: #ffed00` (yellow) on `$background-color: #000000`. Live comparison against `/` confirmed saturated yellow blocking, bold black-on-yellow type, and pill-shaped yellow CTAs elsewhere in the app — none of which appears here.
- The red/dark palette reads as a generic "error/danger" template trope, not a deliberate variation on this police-charity event's yellow/black identity.

The mechanical scan (CLI `detect.mjs`) found zero pattern-matched anti-patterns in the `.ts`, `.html`, or `.scss` source — this page is not "broken" by any static rule, it is simply undesigned relative to the rest of the app.

#### Overall Impression

The page works: it renders, states the problem in clear French, and offers a real Angular `routerLink` back to the homepage (SPA navigation, no full reload). Cognitively it's light — one message, one button, no clutter. But it is visually foreign to the rest of the app. A visitor landing here via a stale QR code, an expired payment link, or a typo has no way to tell — from color, type, or shape alone — that they're still on the Hyrox Police 54 site. For a registration+payment flow where visitors may already be anxious about whether their signup went through, that moment of "did I get redirected somewhere else?" is an unnecessary tax, and the page does nothing to re-engage them with the event (no date, no venue, no CTA back into the funnel) — a missed opportunity given the app's stated Operate/Persuade hybrid mode.

#### What's Working

1. **Copy is clean and correctly scoped.** "Page introuvable" / "La page que vous cherchez n'existe pas." / "Retour à l'accueil" is terse, correct French with no jargon or dead ends.
2. **Single unambiguous recovery action**, implemented as a real `routerLink` (fast SPA navigation, not a full page reload) — mechanically correct.
3. **Low cognitive load by construction**: no competing CTAs, no distracting nav chrome, a clean centered composition with reasonable vertical rhythm.

#### Priority Issues

**[P0] Brand identity abandonment**
**Why it matters:** The page uses Google-Fonts-hosted Bebas Neue + DM Sans and a red/near-black palette, while the rest of the app self-hosts Lemon Milk + Cabin and runs on `$main-color: #ffed00` yellow / `#000000` black. This is the only page in the codebase with an external font dependency, and the only one with no trace of the brand's signature yellow. A confused visitor arriving here has no visual confirmation they're still on the event's site — the worst possible page for that doubt to appear on.
**Fix:** Rebuild the page on the app's existing design tokens — swap in the self-hosted Lemon/Cabin fonts, replace the red accent system with the yellow/black brand pairing used on `/`, and match the button treatment (shape/weight) used elsewhere in the app rather than the current thin ghost-outline.
**Suggested command:** `/impeccable adapt`

**[P1] Single, low-affordance recovery path with no funnel re-entry**
**Why it matters:** The only way forward is a link to the homepage. In Persuade mode, a lost visitor who may be mid-registration or mid-payment gets no path back into that flow (no event date/venue reminder, no inscription CTA, no "check my team" link) — a stress-tested visitor (e.g. following a broken payment-flow link) dead-ends into "start over from scratch" with zero acknowledgment of what they were actually trying to do. The button itself is also visually thin (`1px` outline at `rgba(255,255,255,0.15)`, text at `rgba(255,255,255,0.6)`) compared to the app's normal high-contrast pill CTAs, making the one exit easy to skim past.
**Fix:** Add a secondary path (e.g. a link to registration/`inscription`, or a short reminder of the event with a CTA), and raise the visual weight of the primary button to match the app's standard CTA styling.
**Suggested command:** `/impeccable clarify`

**[P1] Body copy fails WCAG AA contrast**
**Why it matters:** The `<p>` text is `rgba(255,255,255,0.4)` on `#0d0d0d`, roughly 3.8:1 contrast — below the 4.5:1 minimum for normal-size (15.2px) text under WCAG AA. The decorative "404" numeral (`rgba(220,38,38,0.3)`) is even lower, though its decorative role makes that less critical.
**Fix:** Raise the paragraph text opacity/color to clear 4.5:1 against the background (or against whatever new background results from the brand fix above).
**Suggested command:** `/impeccable harden`

**[P2] No deliberate interactive/focus states**
**Why it matters:** Keyboard-focus on the back button falls through to the browser's plain default outline — legible, but generic and not brand-matched (no yellow focus ring anywhere else this happens in the app). Only a `:hover` state is defined; there's no explicit `:focus-visible` or `:active` treatment, so keyboard-only and switch-device users get a lesser-considered experience than mouse users.
**Fix:** Add an explicit `:focus-visible` style using the brand's focus/accent token, and an `:active` state for the button, consistent with interactive elements elsewhere in the app.
**Suggested command:** `/impeccable polish`

#### Persona Red Flags

- **Jordan (first-timer):** Arriving via an old promotional or QR-code link, Jordan sees no yellow, no logo, no event name — nothing that visually ties this back to Hyrox Police 54. Real risk of assuming the page is broken or off-site and bouncing before ever clicking "Retour à l'accueil."
- **Sam (accessibility):** Body text at ~3.8:1 contrast fails WCAG AA; the decorative numeral is lower still. No custom `:focus-visible` styling means the experience is "acceptable by browser default," not deliberately accessible — a pattern this app doesn't otherwise seem to accept elsewhere.
- **Riley (stress-tester):** Someone bouncing off a broken/stale payment or registration link and unsure whether their signup went through gets exactly one option — go back to the homepage and start over — with no acknowledgment of the registration/payment context they were likely in.

#### Minor Observations

- Background is `#0d0d0d`, not the brand's pure `#000000` — a small inconsistency even within a "dark theme" reading.
- The automated live-DOM scan flagged the `.not-found` radial-gradient vignette (`rgba(220,38,38,0.06)` ellipse) as a "radial-spotlight-glow" pattern; given its very low opacity and purely decorative role on a small static page, this is a plausible false positive rather than a real usability defect — noted for completeness, not treated as a priority issue.
- No `document.title` update on this route — the browser tab keeps the app's generic title rather than reflecting the 404 state, which slightly weakens history/bookmark legibility.
- The external Google Fonts request adds an avoidable network dependency and latency that no other page in the app incurs.

#### Questions to Consider

1. If nothing about this page — color, type, or button shape — signals "Hyrox Police 54," is this really this app's 404 page, or a stock template that got wired up and never revisited?
2. This is a registration-and-payment funnel in Persuade mode — why does the page most likely to catch a confused, possibly-already-paid visitor offer no path back into that funnel at all?
3. Every other font and color in this codebase is deliberately owned (self-hosted font files, a named `$main-color` token) — what made this page the one exception that reaches out to Google Fonts and invents a brandless red/black palette from scratch?
