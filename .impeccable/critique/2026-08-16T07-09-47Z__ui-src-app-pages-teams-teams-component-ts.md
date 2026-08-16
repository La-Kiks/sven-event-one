---
target: teams
total_score: 26
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 2
timestamp: 2026-08-16T07-09-47Z
slug: ui-src-app-pages-teams-teams-component-ts
---
Method: dual-agent (A: ace05b7497aebc29f · B: a0ce9834a2b6035be)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3 | Shimmer skeletons and inline "Envoi.../Suppression..." states present; no distinct confirmation flash after a payment toggle completes beyond the badge flipping |
| 2 | Match Between System and Real World | 3 | Strong domain fit, but "Pénitancier" is very likely a misspelling of "Pénitentiaire" — a real French-language error on a police-facing tool |
| 3 | User Control and Freedom | 3 | Cancel/close available everywhere; no undo after a completed action, only pre-action confirms |
| 4 | Consistency and Standards | 2 | Team category uses the badge component; player category (same data type, same panel) renders as plain gray text |
| 5 | Error Prevention | 3 | Delete and bulk-send both gated by explicit confirm steps with irreversibility called out in copy |
| 6 | Recognition Rather Than Recall | 3 | Sort icons and text+symbol payment badges are good; icon-only close button has no accessible label |
| 7 | Flexibility and Efficiency of Use | 2 | One genuine efficiency feature (bulk activation-email), but no search/filter, no multi-select bulk payment/delete |
| 8 | Aesthetic and Minimalist Design | 3 | Clean and on-system overall; text-opacity steps (6 distinct values) exceed the documented 3-tier system |
| 9 | Help Recognize/Diagnose/Recover from Errors | 3 | Plain-language, near-source error copy with retry guidance |
| 10 | Help and Documentation | 1 | No help affordance anywhere — no legend for category colors, no explanation of "Compte" states for a new organizer |
| **Total** | | **26/40** | **Acceptable** |

Scored as a full Operate surface — heuristics 7 and 10 were not exempted (this is a repeatedly-used admin tool for a small organizing team).

## Design Specificity Verdict

**LLM assessment**: Authored for this product, with real drift in the details. The page is unambiguously built for Hyrox Police 54 — French police-organization admin labels (Gendarmerie, Militaire, Pénitancier, Police Municipale, Police Nationale, Pompier), duo-registration domain language, the hazard-yellow "54" mark, and the filet-pattern KPI band aren't generic dashboard chrome. The slide-in detail panel's top-offset is even accompanied by a code comment documenting a specific past bug (sign-out getting covered) — deliberate, product-aware engineering. Where it loses specificity is in small, uncredited departures from the system's own grammar: a 4th ad hoc button style, a `border-radius: 4px` where the system only recognizes 2px/0.25em, a `transform: scale()` hover where scale is explicitly forbidden, and player category silently dropping out of the badge component into plain text. This is drift from the product's own rules, not generic-template sameness.

**Deterministic scan**: `detect.mjs --json ui/src/app/pages/teams` returned exit code 0, zero static findings. The live browser-injected detector found **3 anti-patterns**: a `radial-spotlight-glow` on `div.teams-page.panel-open` (caveat: captured while the detail panel happened to be open, an artifact of the evidence-gathering session, not necessarily reproducible from a clean list view), `cramped-padding` on `div.table-wrapper` (children flush against the border with no inset — confirmed via source, panel-state-independent), and `undersized-ui-text` on `span.tag.outfit` ("Tenue : oui" rendering at 10.88px, below the 11px floor — confirmed via source: `.tag { font-size: 0.68rem }`).

**Visual overlays**: Console evidence: `[impeccable] 3 anti-patterns found`. The live-server instance used for injection was stopped after evidence collection.

**Process caveat**: As with the prior `my-team` critique run, both sub-agents again hit tab-isolation issues — Assessment A reported its tab's title changed unprompted and annotation overlays appeared mid-session (Assessment B's injection landing in A's tab), plus two `Page.captureScreenshot` timeouts and an inconsistent `window.innerWidth` reading while attempting a mobile-viewport check. A recovered and closed its tab without acting on the injected content. This is a recurring environment issue with concurrent browser sub-agents in this session, not a product defect — flagging again since it affects confidence in the (unconfirmed) mobile finding below.

## Overall Impression

This is the most functionally mature admin surface reviewed so far — real workflows (bulk activation email with per-team failure breakdown, a well-calibrated delete confirmation) that go beyond a generic CRUD table. But the page's own discipline slips in two specific, fixable ways: it violates its own design system's signature rule (One Signal Rule — five yellow elements competing at once) and it treats its highest-stakes action (marking real money as paid/unpaid for a police charity event) with the same two-click ceremony as any other toggle on the page.

## What's Working

1. **Slide-in panel's top-offset logic** — a deliberate, documented fix for a real prior bug (sign-out getting covered by the panel), not a generic drawer component.
2. **Bulk activation-email flow** — the "en attente" KPI feeds directly into a contextual CTA, a confirm step, and a per-team results breakdown with individual failure reasons rather than a generic "some failed."
3. **Badge component discipline (mostly)** — payment, account, and team-category badges consistently use the tinted-bg + matching-border + full-strength-text pattern exactly as DESIGN.md specifies.

## Priority Issues

**[P1] Payment-toggle confirmation weight doesn't match its real-world stakes**
- **Why it matters**: `togglePayment()` lets an admin flip a team's paid/unpaid state in two clicks (badge click → "Confirmer") with no note field and no visible audit trail (who/when). PRODUCT.md states payment state "must never be ambiguous... to the organizer" — but this makes an accidental reversal of a genuine payer trivially easy and undetectable after the fact, for a police charity event's actual money.
- **Fix**: Give this specific confirm a heavier, distinct treatment (not the generic "Confirmer" label), and surface a lightweight last-changed indicator in the panel.
- **Suggested command**: `/impeccable harden`

**[P1] One Signal Rule violated — five hazard-yellow elements compete simultaneously**
- **Why it matters**: `.kpi-value { color: $main-color }` applies to all 4 KPI tiles at once, plus `.bulk-send-trigger-btn` (yellow border + text). DESIGN.md is explicit that yellow's urgency signal depends on scarcity ("one dominant yellow element per screen... not several competing for attention"). The actionable stat ("En attente") gets no more visual weight than the inert one ("Équipes").
- **Fix**: Render only the stat that needs action (e.g. "En attente") in hazard-yellow; the other three KPI values in white/text-primary.
- **Suggested command**: `/impeccable quieter`

**[P2] Mobile header-wrap breaks the panel/backdrop's fixed top-offset assumption**
- **Why it matters**: `$top-bar-height: 4.5rem` (72px) is hardcoded as the `top` offset for both `.backdrop` and `.detail-panel`. DOM measurement during this review showed the actual rendered `.top-bar` height at 161.56px once its `flex-wrap: wrap` kicks in across 3 children — the exact overlap failure mode DESIGN.md calls out as "a bug, not a style choice." Here the header wraps correctly, but the panel positioning fixed to protect the sign-out control on desktop silently breaks the same guarantee on mobile. Not visually screenshot-confirmed this run (see process caveat above) — code/DOM-verified only.
- **Fix**: Measure the header's actual rendered height (ResizeObserver or a CSS custom property set from JS) instead of a static SCSS constant, or make the offset itself responsive.
- **Suggested command**: `/impeccable harden`

**[P2] Shipped radius/hover values drift from the documented system**
- **Why it matters**: `.info-section` and `.player-card` use `border-radius: 4px` — a third radius value the system doesn't recognize (DESIGN.md explicitly says "never 4px, 8px, or 0"). `.team-badge.clickable:hover` uses `transform: scale(1.05)` plus `opacity: .75`, exactly what the No-Lift Rule forbids.
- **Fix**: Change both radii to 2px; replace the scale/opacity hover with a border-color/brightness shift consistent with every other interactive element on the page.
- **Suggested command**: `/impeccable harden`

**[P2] Player category breaks the badge pattern the rest of the page follows**
- **Why it matters**: Team category renders through `.category-badge`; player category — the identical Homme/Femme/Mixte value set, one section below in the same panel — renders as unstyled gray text, violating DESIGN.md's own rule against styling a category value outside the badge pattern within a single screen.
- **Fix**: Wrap player category in the shared `.category-badge` component with `[ngClass]="player.category"`.
- **Suggested command**: `/impeccable harden`

## Persona Red Flags

**Alex (power user)**: No search/filter on the teams table. No multi-select bulk actions beyond the single global "send activation emails" button. No shortcut to move to the next row while the panel is open (must close, re-click). Also notices a redundant stat line — the page-title count and the KPI band both surface the same two numbers simultaneously.

**Sam (accessibility-dependent)**: The panel's close button (✕) has no `aria-label`. The payment badge-as-button's only explanation of consequence lives in a `title` attribute — not reliably exposed to assistive tech and unreachable without a mouse hover, so a keyboard/screen-reader user gets no warning before activating it. Credit where due: focus-visible outlines (2px hazard-yellow) are implemented consistently on every interactive element checked, and the sortable headers carry correct `role`/`aria-sort`/keyboard support (verified live by both agents).

**Riley (stress tester)**: Payment state can be flipped and flipped back with zero record of the change. `getCategoryLabel()` falls back silently on an unrecognized category value, which would render an unstyled badge with raw backend text — untested live since all 4 seed teams were "Mixte," but plausible with bad data (see the `my-team` critique's Administration finding for a confirmed instance of this exact pattern elsewhere in the app). Refreshing the browser while the detail panel is open loses the open-team state — no bookmarkable "viewing team X" URL.

## Minor Observations

- Six distinct text-opacity steps in use (0.75/0.55/0.4/0.35/0.25/0.2) versus DESIGN.md's documented 3-tier system (1/0.55/0.4).
- `.bulk-send-trigger-btn` is a 4th, undocumented button style matching neither the documented ghost nor primary variants.
- The bulk-activation-email confirm dialog reuses the destructive `.delete-confirm` red styling for a routine, reversible action — the opposite miscalibration from the payment-toggle issue above (something benign dressed up as scary, right next to something genuinely consequential treated too lightly).
- "Pénitancier" administration label is very likely a French spelling error (should probably be "Pénitentiaire") — same option list used on `my-team`, so a fix should be made once and apply everywhere it's referenced.

## Questions to Consider

- If yellow means "this needs your attention," what happens to an organizer's eye when four KPI tiles and a button are all yellow at once — is anything actually urgent?
- The payment toggle lets an admin silently overwrite the payment provider's source of truth in two clicks with no note or history — is that the level of ceremony you actually want around real money for a police charity event?
- Team category gets a badge; player category — the same data, one section down — gets plain text. Deliberate simplification, or did the badge pattern just not make it all the way down the panel?
