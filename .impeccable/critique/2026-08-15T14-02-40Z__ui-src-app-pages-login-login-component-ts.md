---
target: login page (ui/src/app/pages/login/login.component.ts)
total_score: 22
max_score: 32
na_heuristics: 7,10
p0_count: 0
p1_count: 2
timestamp: 2026-08-15T14-02-40Z
slug: ui-src-app-pages-login-login-component-ts
---
Method: dual-agent (A: a6af836a3bd6fa5a8 · B: af19e4c3e0d9673c2)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3/4 | Loading spinner + disabled button on submit is clear; no visible rate-limit/lockout state |
| 2 | Match System / Real World | 3/4 | French copy reads naturally ("Bon retour", "Mot de passe oublié ?"); "Bon retour" is generic-safe rather than event-specific |
| 3 | User Control and Freedom | 3/4 | Enter-to-submit both fields, clear escape routes (forgot-password, register, logo→home), no traps |
| 4 | Consistency and Standards | **1/4** | Card/button/input radius, card material (glass+blur), and button typography all diverge from the shared design system and the shared `<app-button>` component |
| 5 | Error Prevention | 3/4 | Client-side empty-field check before network call; `type="email"` gives basic format hinting |
| 6 | Recognition Rather Than Recall | 4/4 | Both fields always visible, autocomplete wired (`email`, `current-password`), no memory burden |
| 7 | Flexibility and Efficiency of Use | n/a | No meaningful expert-mode surface for a 2-field login form beyond what's already credited under #6 |
| 8 | Aesthetic and Minimalist Design | 2/4 | Minimal, but the aesthetic itself is generic-SaaS-glass-card, not the product's own "tactical briefing" voice |
| 9 | Error Recovery | 3/4 | Specific error copy (401 vs. server error), `aria-live` announces it; doesn't proactively surface "Mot de passe oublié ?" from inside the error banner itself |
| 10 | Help and Documentation | n/a | Consistent with the rest of the product's minimal-help philosophy; not a login-specific gap |
| **Total** | | **22/32** | **Acceptable (68.75%)** |

Two heuristics (7, 10) scored n/a — a 2-field login form has no meaningful expert/shortcut surface beyond autocomplete, and no help/documentation gap distinct from the rest of the product. Applicable max is 32 (8 heuristics × 4).

## Design Specificity Verdict

**LLM assessment**: Weak. This is a generic centered-glass-card SaaS login pattern — dark translucent card, backdrop-blur, soft rounded corners, two fields, a full-width accent button. Strip the "54" mark and the yellow hue and it's interchangeable with any B2B SaaS login screen. Nothing structurally echoes the "mission briefing board" framing DESIGN.md establishes for the rest of the product (no hairline-bordered chrome, no filet pattern, nothing "stamped" — this is the one surface in the app that visually floats via blur). Copy ("Bon retour" / "Connectez-vous à votre compte") is safe and forgettable; a missed opportunity given this gates access to either admin tooling or a participant's own paid, capacity-capped registration.

**Deterministic scan**: Static CLI scan (`detect.mjs --json ui/src/app/pages/login/`) returned **0 findings**, confirmed across four variants (default, `--no-design-system`, `--no-config`, individual-file scans) and re-confirmed scanning the whole `pages/` tree. This is a genuine clean static result, not a tool failure — but its scope is limited: the static-html engine only covers `element`/`page` rule categories, not `layout` (browser-only) or `visual-contrast` (screenshot-only), and a URL-based scan failed because puppeteer isn't installed (`Error: puppeteer is required for URL scanning`). So the 0-findings result should be read as "clean on the axes it can check," not "clean overall."

**Visual overlays**: Browser injection succeeded (mutation preflight confirmed via `document.title` + script-tag write) and the live overlay found **2 anti-patterns** the static scan structurally couldn't reach:
- `radial-spotlight-glow` — a radial-gradient spotlight glow (`#ffed00` alpha 0.07 → transparent) behind `div.login-page`
- `repeating-stripes-gradient` — a repeating-gradient decorative stripe pattern on `body`

These are visible as a toast overlay injected into the live tab used by Assessment B. Note the second finding (`repeating-stripes-gradient` on `body`) applies to a global/site-wide element rather than anything login-specific — worth treating as a lower-confidence/possibly-shared-pattern flag rather than a login-page-specific issue, since it wasn't scoped to the login component itself. The radial glow finding directly corroborates the LLM assessment's read of this page as leaning on generic decorative flourishes (a soft yellow glow) that don't appear anywhere else in the documented system.

No contradictions between the two assessments; the detector's evidence (raw computed styles below) is what let Assessment A cite exact pixel deltas from DESIGN.md's tokens.

**Raw computed values captured by Assessment B** (for the record):
- `.login-card`: `border-radius: 8px`, `background-color: rgba(255,255,255,0.03)`, `border: 0.67px solid rgba(255,255,255,0.08)`, `backdrop-filter: blur(10px)`, `box-shadow: none`
- `.submit-btn`: `border-radius: 4.2px`, `background-color: rgb(255,237,0)`, `font-weight: 600`, no uppercase/letter-spacing
- `.error-banner`: `background-color: rgba(255,107,107,0.1)`, `border-color: rgba(255,107,107,0.3)`, `border-radius: 3.4px`, `color: rgb(255,107,107)`

Compare to DESIGN.md tokens: card/button/field radius should be `2px` (`{rounded.sharp}`), card surface should be opaque `#0a0a0a` with a plain 1px hairline border and no blur.

## Overall Impression

The interaction design here is genuinely sound — a tight, well-built login form with real accessibility care (`aria-live`, `aria-invalid`, proper labels) and no cognitive-load failures. The problem isn't the form logic, it's that this page is still wearing the product's old visual identity while everything around it moved on. It reads as a different, older product the moment a user arrives here from the redesigned inscription or mon-équipe pages: 8px rounded corners and a translucent blurred card sit directly against DESIGN.md's explicit "Don't revert to rounded corners (0.5rem or larger)" and "nothing is lifted" rules. The single biggest opportunity is also the cheapest fix in this entire critique run: swap radius tokens, drop the blur, and reuse the shared `<app-button>` — most of the correct brand color/type already flows through shared SCSS variables, so this isn't a redesign, it's a cleanup.

## What's Working

- **Accessibility wiring is above the codebase's baseline**: `role="alert"`/`aria-live="polite"` on the error banner, `aria-invalid`+`aria-describedby` cross-linked to both inputs, visible (non-placeholder-only) labels, and `:focus-visible` outlines throughout — worth explicitly preserving when this page gets its redesign pass.
- **Interaction completeness for a small surface**: empty-field pre-check before hitting the network, distinct copy for wrong-credentials vs. server error, loading state (spinner + disabled button), Enter-to-submit, and autocomplete attributes are all correctly wired. Nothing structurally missing.
- **French copy is natural and idiomatic** throughout ("Bon retour", "Mot de passe oublié ?", "Pas encore inscrit(e) ?", correct gendered inclusive form) — no translation-ese.
- **One-dominant-yellow rule is respected here**, unlike the two prior pages critiqued in this same run (landing, inscription). The small fixed-size logo mark is established sitewide chrome (confirmed present identically on the redesigned `teams` admin page), and the submit button is unambiguously the single dominant content-level yellow element on screen.

## Priority Issues

**[P1] Card, input, and button radii reproduce the design system's explicitly rejected pre-redesign pattern.**
Why it matters: DESIGN.md names this exact rollback as a "Don't" — `border-radius: 8px` on `.login-card` (vs. the `2px` token) and `~4.2px` on inputs/button (vs. `2px`) is the single most visible tell that this page wasn't touched by the redesign, and it's the page every returning admin and participant hits most often.
Fix: Set `.login-card`, inputs, and `.submit-btn` to `border-radius: 2px`, matching the pattern already shipped in `my-team.component.scss` (`$radius: 2px`).
Suggested command: /impeccable polish

**[P1] Card uses a translucent, blurred "glass" material that contradicts the system's flat, no-lift depth model.**
Why it matters: DESIGN.md's Elevation & Depth section states there are no `box-shadow` declarations anywhere and nothing is "lifted" — depth comes only from opaque background-tier steps and hairline borders. `rgba(255,255,255,0.03)` + `backdrop-filter: blur(10px)` is the one surface in the shipped app that floats, and it's paired with a soft radial yellow glow behind the page (confirmed by the browser-overlay detector as `radial-spotlight-glow`) that exists nowhere else in the system.
Fix: Replace the translucent/blur background with the Raised Surface token (`#0a0a0a`) and a plain `1px solid rgba(255,255,255,0.1)` hairline border; drop `backdrop-filter` and the radial glow entirely.
Suggested command: /impeccable polish

**[P2] Submit button is hand-rolled instead of reusing the shared `<app-button>` component, and diverges in typography.**
Why it matters: every other primary CTA in the app is uppercase, `letter-spacing: 0.04em`, `font-weight: 700`, via the shared button component; login's `.submit-btn` is sentence-case, no tracking, weight 600 — a subtle but real "this control behaves differently here" signal on the most-repeated interaction in the app.
Fix: Swap `.submit-btn` for `<app-button>`, or align its CSS 1:1 with `button.component.scss`.
Suggested command: /impeccable polish

**[P2] Error banner's icon and container don't follow the system's signature status-badge pattern, at the highest-stakes moment on the page.**
Why it matters: the error icon is a solid circle (`border-radius: 50%`), directly contradicting DESIGN.md's Shapes rule that "nothing in the system is fully round"; the banner itself uses an ~3.4px radius rather than the shared tinted-bg + matching-border + `2px`-radius status-badge pattern used identically for payment/account/category state elsewhere. This is precisely the moment (failed login, before a capacity-capped paid event) where the product's disciplined, trustworthy "tactical" tone should be most reinforced — instead it's the least on-brand element on the page.
Fix: Rebuild the error banner using the shared status-badge visual pattern; square off or drop the circular icon.
Suggested command: /impeccable polish

**[P3] Login card has no responsive gutter and was not verifiable on a real mobile viewport by either assessment.**
Why it matters: `.login-page` has no `@media` query and no horizontal padding in source, and `.login-card` has no side margin below its `max-width: 400px` — on phones under ~400px wide the card's border likely sits flush against the screen edge, unlike every other page which uses the fluid `{spacing.gutter}` token. Both Assessment A and B attempted to confirm this live and were blocked by a shared-browser-session viewport-resize limitation (`resize_window` reported success but `window.innerWidth` never actually changed), so this is a source-derived, not visually confirmed, finding.
Fix: Add horizontal padding/gutter to `.login-page`, then verify on an actual mobile viewport or device before treating this as closed.
Suggested command: /impeccable critique (re-verify on mobile), then /impeccable polish

## Persona Red Flags

**Jordan (First-Timer)**: Arrives here mid-recovery (e.g. right after activating their account via the redesigned activation flow) with no visual continuity cue that this is the same product as the polished inscription flow they just came from — the rounded glass card reads as a different, older site, which can trigger a moment of "did I click the wrong link?" doubt.

**Sam (Accessibility-Dependent)**: Overall wiring is good (labels, `aria-live`, focus-visible), but placeholder text at roughly `rgba(255,255,255,0.25)` on a `rgba(255,255,255,0.05)` field background over near-black is low contrast (~2.5:1 estimated) — a legibility issue for low-vision users reading the hint text before typing, even though the real `<label>` covers the accessible-name requirement.

**Casey (Distracted Mobile User)**: The unverified zero-gutter card-to-viewport-edge issue (P3 above) combined with no dynamic-viewport-height handling for mobile browser chrome — flagged as a real risk given the source, but neither assessment could visually confirm it live in this session, so treat as needing verification rather than a confirmed defect.

## Minor Observations

- The loading spinner (`border-radius: 50%`) is technically the same "fully round" shape-rule violation as the error icon, but circular spinners are a near-universal convention even in squared-off design systems — defensible exception, not a real finding.
- No `autofocus` on the email field — one extra click before typing, a small friction point given the product's "fast, low-friction" principle.
- No password-visibility toggle — reasonable to omit for a security-conscious, police-affiliated audience; not treated as a finding.
- Already-logged-in redirect (`homeRouteFor` in the constructor) runs synchronously against the decoded JWT, so there's no flash-of-login-form for an authenticated user — works correctly, no complaint.
- Static CLI detector scan was clean (0 findings) but structurally couldn't check layout/visual-contrast categories (no puppeteer installed for URL scanning) — the 2 findings that did surface came only from the browser-injected overlay.

## Questions to Consider

- If the four fully-redesigned surfaces are the product's real visual identity now, why is login — arguably the single most-repeated interaction for returning admins and participants — the one surface still running the old shell, when most of the fix is a token swap rather than a rebuild?
- What would this screen look like if it borrowed one structural element from the "mission briefing board" metaphor (e.g. a hairline-bordered credential block) instead of defaulting to a generic translucent glass card?
- Given PRODUCT.md's principle that payment/account state must never be ambiguous, should a failed login for a participant surface any reassurance that their team registration itself isn't at risk — or is a plain "wrong password" message deliberately sufficient here?
- Is the translucent/blurred card a deliberate "security boundary" signal for auth screens specifically, or unmigrated legacy CSS? If deliberate, DESIGN.md should document it as a named exception; if not, it should be first in line for the fix.
