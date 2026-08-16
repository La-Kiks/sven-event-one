---
target: landing page (ui/src/app/pages/landing/landing.component.ts)
total_score: 32
max_score: 36
na_heuristics: 7
p0_count: 0
p1_count: 1
timestamp: 2026-08-15T13-19-51Z
slug: ui-src-app-pages-landing-landing-component-ts
---
Method: dual-agent (A: ac0c212c6ae14b21a · B: ab1c46f217f7e9b9e)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3/4 | The live "X/52 équipes inscrites" hero tile pops into the 4-tile stat row after the async fetch resolves, visibly growing the row to 5 tiles in front of the user — a real layout shift on the highest-visibility element on the page |
| 2 | Match Between System and Real World | 4/4 | Terminology, price, venue, and contact all match PRODUCT.md exactly; French copy is domain-accurate for a police-organized event |
| 3 | User Control and Freedom | 4/4 | No dead ends; anchor scrolling, native back nav, working `tel:`/`mailto:` links, no modal traps |
| 4 | Consistency and Standards | 3/4 | Buttons/cards/inputs strictly follow the token system, but the full-bleed yellow marquee isn't accounted for by DESIGN.md's "two bandeaux" exception — an internal inconsistency between the stated system and the shipped page |
| 5 | Error Prevention | 4/4 | `isRegistrationFull` fails closed on API error (never oversells) while still surfacing a distinct, specific error message rather than collapsing into the generic sold-out state |
| 6 | Recognition Rather Than Recall | 4/4 | Price/date/venue repeated across hero, infos tiles, cta-banner, and footer; sticky header keeps the CTA always reachable |
| 7 | Flexibility and Efficiency of Use | n/a | Landing/persuade page — no power-user shortcuts or repeat-use affordances apply |
| 8 | Aesthetic and Minimalist Design | 3/4 | Strong angular/flat execution overall, but the yellow over-use below works against the system's own "one dominant element" discipline |
| 9 | Error Recovery | 4/4 | `countLoadError` produces distinct, specific copy ("Erreur de chargement — contactez-nous") linked to a working contact fallback, not a generic failure message |
| 10 | Help and Documentation | 3/4 | No help center, but "Infos pratiques" surfaces direct phone/email contact as a de facto help channel — adequate for this context |
| **Total** | | **32/36** | **Good (89%)** |

Heuristic 7 scored n/a (landing/persuade page, no applicable power-user affordances); the 36-point maximum and 89% place this just under the Excellent band.

## Design Specificity Verdict

**LLM assessment**: This is not a generic sports-signup template — it is specifically authored for "The Tactical Briefing." The void-black ground, near-universal 2px radius (verified across buttons, video/map iframes, and the brand mark), all-caps Lemon Milk headings paired with Cabin body copy, and the filet grid pattern (`gap:1px; background:$border` with opaque children) are all correctly and repeatedly applied — the filet pattern alone shows up three separate times (`hero__stats`, `partners__grid`, `infos__tiles`), which is reuse of a system idiom, not a one-off accent. The French copy ("forces de l'ordre et personnels assimilés," "binôme") and charity framing ("Au profit d'Orphéopolis") are specific to this brief.

Where specificity breaks down is exactly where DESIGN.md stakes its strongest claim: the One Signal Rule. The doc is explicit that yellow is rationed to "one dominant element per screen," with a named, narrow exception ("the two full-bleed CTA bandeaux"). This single page ships a full-bleed yellow marquee band with no CTA or link, five simultaneously-yellow hero stat numbers, eight yellow `card__number` badges in the format grid, a yellow phone link, and the yellow cta-banner — considerably more yellow surface than the system's own governing rule licenses. The page is typographically and structurally on-brief but has drifted from the system's central color discipline, which is the one rule DESIGN.md calls out most forcefully ("its scarcity is what makes it read as urgent... alarm fatigue the moment it stops being rare").

**Deterministic scan**: `detect.mjs --json ui/src/app/pages/landing/` returned `[]` with exit code 0 — zero automated findings. No false positives to note since there was nothing to flag; for reference, the near-universal 2px radius and absence of box-shadow are confirmed in DESIGN.md as deliberate system rules, so a hypothetical detector complaint about either would itself be a false positive against this project.

**Visual overlays**: Not applicable in the form described in the workflow — `detect.mjs` is a Node CLI scanner, not a browser-injectable script, so there is no live overlay to point to in a `[Human]` tab. In its place, Assessment B did real DOM mutation (confirmed via `document.title` change + appended script element) and a full top-to-bottom desktop screenshot walkthrough at ~1512-1568px: no broken images, no misalignment or overlap, zero console errors/warnings, zero failed network or image loads. The mobile-viewport pass could not be completed — `resize_window(390, 844)` reported success four times across two tabs but `window.innerWidth` never actually changed from 1280×800, most likely because the Chrome window/tab-group was shared with several other concurrently-running agent sessions (visible as ~10 unrelated tabs, one of which closed one of Assessment B's tabs mid-task). Mobile-layout conclusions in this report are therefore inferred from source (`flex-wrap`, `auto-fit`/`minmax`, a single confirmed `@media (min-width: 900px)` breakpoint at line 129 of `landing.component.scss`) rather than visually verified — flagged explicitly given this project's known history of mobile flex-wrap/overflow bugs (the admin header clipping fixed in commit 09ba6b3).

## Overall Impression

The landing page is a confident, well-executed application of the Tactical Briefing system — typography, shape language, and the filet grid pattern are all handled with real fidelity to DESIGN.md, and the copy is specific to a police-charity event rather than generic race-signup boilerplate. The single biggest opportunity is tightening yellow back down to the system's own "one dominant element" rule: right now the page runs a yellow hero title, a yellow full-bleed marquee, eight yellow station-number badges, and a yellow CTA banner in quick succession, which dilutes exactly the urgency effect the color is supposed to create. Fix that one thing and this page moves from "good execution, drifted rule" to a clean example of the system's own stated discipline.

## What's Working

1. **The filet grid pattern is reused faithfully three times** (`hero__stats`, `partners__grid`, `infos__tiles`), each correctly implemented as `gap: 1px; background: $border` with opaque children per DESIGN.md's Filet Rule — this is a system idiom being applied as a system, not copy-pasted once and forgotten.
2. **`.video__frame-outline`** (landing.component.scss:279-284) — an asymmetrically-offset 2px yellow border around the embedded video is a genuine, product-specific "target/crosshair" flourish tied to the Tactical Briefing concept, not a generic accent border.
3. **`countLoadError` handling** in landing.component.ts fails closed (never oversells registration during an API outage) while still surfacing distinct, specific recovery copy ("Erreur de chargement — contactez-nous") wired to a working contact channel — confirmed clean in the browser pass with zero console errors and no broken requests.

## Priority Issues

**[P1] Yellow rationing is violated on this exact page, undermining the redesign's central rule.**
Why it matters: DESIGN.md's whole premise is that yellow's scarcity is what makes it read as a directive ("the rarest color on any given screen by design"), with a narrow, explicit exception for two full-bleed CTA bandeaux. This page ships a full-bleed yellow `.marquee` band that is not itself a CTA (no link, no button — it's decorative), five simultaneously-yellow hero stat numbers, eight yellow `card__number` badges in the format grid, and the yellow cta-banner. That's meaningfully more yellow than the system licenses, and it's the exact failure mode the doc warns against by name ("alarm fatigue... the moment it stops being rare").
Fix: convert the marquee to a black band with yellow text only (no fill), reduce the hero stat values to white with only one number (live capacity, or price) kept yellow as the single highlighted stat, and switch the format grid's `card__number` badges to a bordered/outline treatment instead of a solid yellow fill.
Suggested command: /impeccable quieter

**[P2] Hero stat grid visibly reflows when the team-count tile loads late.**
Why it matters: the 5th tile (`*ngIf="teamCount && !countLoadError"`) is absent on first paint and pops in once the async fetch resolves, growing the stat row from 4 to 5 tiles directly in the user's eyeline — a real layout shift on the page's most prominent element, and the opposite of the "stamped/precise" feel the system is going for.
Fix: reserve the 5th grid slot from initial render (skeleton or `visibility: hidden` placeholder sized to match) so the tile fills in place instead of resizing the row.
Suggested command: /impeccable harden

**[P2] The police/law-enforcement eligibility restriction is buried in body prose.**
Why it matters: PRODUCT.md states registration "is restricted to police/law-enforcement teams, not the general public" — a hard business rule — but on the page it appears only as a clause inside the `hero__lead` paragraph ("Réservé aux forces de l'ordre et personnels assimilés"), styled identically to the rest of the sentence. A skimming visitor can easily miss it and invest time in the registration/payment flow before discovering they're ineligible.
Fix: pull it out as a small Label-styled tag near the eyebrow or CTA instead of folding it into descriptive prose.
Suggested command: /impeccable clarify

**[P2] Decorative marquee text is not hidden from assistive tech.**
Why it matters: `<div class="marquee">1 KM RUN · AVANT CHAQUE ÉPREUVE · ...</div>` (landing.component.html:73) has no `aria-hidden`, so a screen reader announces the same phrase three times in a row with zero new information beyond the format section that follows it.
Fix: add `aria-hidden="true"` to the marquee element.
Suggested command: /impeccable harden

**[P3] Dead code: `hasScrolledPastHero` / `IntersectionObserver` is tracked but never consumed.**
Why it matters: landing.component.ts (lines 16, 40-49) implements scroll-based hero-visibility tracking with a comment describing an intended "persistent CTA" once the hero scrolls out of view, but landing.component.html never references `hasScrolledPastHero`. No user-facing defect (the sticky header's own CTA already covers this), but it's orphaned intent that should either ship (e.g. a bottom-anchored mobile CTA bar) or be deleted.
Fix: implement the originally-intended persistent CTA, or remove the unused observer/property.
Suggested command: /impeccable audit

## Persona Red Flags

**Jordan (First-Timer)**: Looking for the exact competition date in "Infos pratiques" — the natural place to look — finds only "Lieu / Contact organisateur / Inscription" tiles, no date tile at all. The only date reference anywhere on the page is the vague "Septembre 2026" in the hero eyebrow, easy to miss on a first skim.

**Sam (Accessibility-Dependent)**: The `.marquee` div forces a screen reader to announce a fully redundant repeating phrase with no `aria-hidden` (see P2 above), adding pure noise right after the video section with zero new information.

**Casey (Distracted Mobile User)**: The eligibility gate ("Réservé aux forces de l'ordre et personnels assimilés") is one clause in a longer sentence with no visual distinction — exactly the qualifying detail a fast-scrolling mobile visitor skips past before landing on the registration form. Note: this run could not visually confirm mobile layout behavior (the browser pass's viewport resize did not take effect due to a shared Chrome session across concurrently-running agents), so any additional mobile-specific overflow/wrap issues remain unverified this round — worth a targeted mobile-only recheck given this project's prior history of un-wrapped flex headers clipping content (fixed in commit 09ba6b3).

## Minor Observations

- Header anchor nav (Format/Partenaires/Infos) is fully `display:none` below 900px per an explicit DESIGN.md rule, so mobile visitors lose in-page quick-nav and must rely on scrolling — intentional per spec, but worth naming as a discoverability trade-off.
- The hero's "Voir le format" button uses a bespoke `.btn-outline` class local to landing.component.scss rather than `app-button[variant=ghost]` (documented in a code comment as deliberate — same-page anchor vs. routerLink) — two parallel ghost-button implementations now exist in the codebase, a minor divergence risk for future edits.
- The Google Maps iframe has no app-provided "get directions" fallback link outside Google's own embedded chrome.
- The Format section's "8 ateliers · 8 km" subhead duplicates the hero stats almost verbatim — reasonable reinforcement, not a real problem.
- The embedded video's yellow frame outline sits very close to the card's right edge at desktop width — not a confirmed clipping bug, but tight enough to warrant a visual once-over.
- Mobile viewport verification could not be completed this run (see Casey red flag above) — recommend a follow-up pass in an isolated browser session focused specifically on `hero__stats`, `.header`, and `.cta-banner__inner` at ≤390px width.

## Questions to Consider

1. DESIGN.md licenses exactly "two full-bleed CTA bandeaux" — the marquee has neither a CTA nor a button. Is it meant to be one of the sanctioned two, or is it an unaccounted-for third yellow fill?
2. Should the live capacity counter ("X/52 équipes inscrites") really share visual weight with static format facts (8 ateliers, 8 km) in one undifferentiated stat row, given it's the one number on the page that actually changes and creates urgency?
3. If eligibility is genuinely restricted to police/law-enforcement personnel, what actually happens today if an ineligible visitor completes the full registration and payment flow — is that restriction enforced anywhere beyond this one sentence of hero copy?
