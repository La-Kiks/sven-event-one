---
target: not-found
total_score: 23
max_score: 36
na_heuristics: 7
p0_count: 0
p1_count: 3
timestamp: 2026-08-16T07-41-32Z
slug: ui-src-app-pages-not-found-not-found-component-ts
---
Method: dual-agent (A: a1bdb0f95b0cd58aa · B: a85c7949afd1324ff)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 4 | 404 status is unambiguous instantly; nothing async to track |
| 2 | Match Between System and Real World | 4 | Plain French, ties recovery to real event context |
| 3 | User Control and Freedom | 2 | Only two exits (home, new registration) — no path back to login for a returning user |
| 4 | Consistency and Standards | 1 | Abandons the shared login/activate-account/forgot-password card template and logo-mark entirely |
| 5 | Error Prevention | 2 | The registration CTA doesn't check capacity before offering itself, unlike the identical button on landing |
| 6 | Recognition Rather Than Recall | 3 | Actions are visible, labeled buttons — no icons, no memorization required |
| 7 | Flexibility and Efficiency of Use | n/a | Small single-purpose recovery page; genuinely nothing to be flexible/efficient about |
| 8 | Aesthetic and Minimalist Design | 3 | Clean and focused, but two equal-weight yellow CTAs create a "which one" tension |
| 9 | Error Recovery | 3 | Clear plain-language recovery, but doesn't route the most likely visitor (a lapsed registrant) to the right action |
| 10 | Help and Documentation | 1 | The product has a help/contact precedent (landing's organizer footer) that's simply absent here |
| **Total** | | **23/36** | **Acceptable (64%)** |

Heuristic 7 scored n/a (genuinely nothing to be flexible/efficient about on a small recovery page) — the other 9 were scored for real, including heuristic 10, since the product has an established help/contact pattern this page could reasonably carry.

## Design Specificity Verdict

**LLM assessment**: Mixed, and unevenly so. The copy is genuinely authored for this product — "Rendez-vous en Septembre pour Hyrox Police 54 !" ties the dead-end directly back to the event date rather than a generic "oops," and the typography correctly stays on-brand. But the structure is not authored for this product at all: every other auxiliary page (login, activate-account, forgot-password) shares an identical, deliberate `.login-page > .login-card > .card-header` template with a "54" logo-mark linking home — this page uses a completely separate `.not-found > .content` wrapper with no card, no logo-mark, no footer, no contact info. Swap the copy for any other product's tagline and this page's shape would be indistinguishable from a stock Angular 404 template.

**Deterministic scan**: `detect.mjs --json ui/src/app/pages/not-found` returned exit code 0, zero static findings. The live browser-injected detector found **1 anti-pattern**: `radial-spotlight-glow` on `div.not-found` — the same intentional brand-yellow decorative gradient pattern already present on login/forgot-password/activate-account/teams/players. Mechanically correct per the detector's rule, but not a real defect — it's consistent, deliberate reuse of the app's established background motif.

**Visual overlays**: Console evidence: 1 anti-pattern found. Wildcard-route coverage was verified across 4 distinct bogus paths (including one with spaces, an encoded `<script>` tag, a query string, and a fragment) — all correctly rendered the not-found component with no console errors or injection artifacts.

**Process note**: Mobile-viewport checks again hit the known `resize_window` limitation in this environment — both agents independently confirmed `window.innerWidth` stayed pinned at 1280px despite the tool reporting a successful resize. All mobile-specific findings below are source-derived only, not visually confirmed.

## Overall Impression

The copy on this page is some of the most product-aware writing in the app — it turns a dead end into a re-engagement hook. But the page was visibly built separately from the rest of the auxiliary-page family: it drops the shared card template, the logo-mark, and the capacity-aware registration button that every sibling page already knows how to do correctly. For a trust-sensitive, police-organized charity product, the moment a visitor is already slightly unsettled by a broken link is exactly the wrong moment to also feel like they've fallen off the edge of a less-finished site.

## What's Working

1. **Event-specific recovery copy** — turns the dead-end into a re-engagement moment instead of a generic apology.
2. **Correct type-system usage** — Lemon Milk uppercase for the ghost "404" and heading, Cabin for body copy, a detail that's easy to get wrong on an error page and wasn't.
3. **Shares the site's background motif** — the radial-gradient hazard-yellow blob matches the same pattern used across login/forgot-password/activate-account/teams/players.

## Priority Issues

**[P1] The registration CTA has no idea whether registration is actually open**
- **Why it matters**: The identical button on the landing page gates on `isRegistrationFull` (via `TeamCountService`) and falls back to the documented sold-out `.btn--full` state. This page's button passes none of those bindings. PRODUCT.md explicitly requires the product to "clearly communicate sold-out/closed state rather than silently failing" — the 404 page is the one place in the app where that promise is currently broken.
- **Fix**: Inject `TeamCountService` into `NotFoundComponent`, mirror landing's `ngOnInit` subscription, and pass the same `[isFull]`/`[fullLabel]`/`[fullLink]` bindings through.
- **Suggested command**: `/impeccable harden`

**[P1] Two competing solid-yellow CTAs break the One Signal Rule**
- **Why it matters**: Both buttons use the default solid/yellow variant — DESIGN.md is explicit that yellow should ration to "one dominant element per screen," and the product's own precedent (landing's ghost-variant "Se connecter" next to its solid primary CTA) shows the intended pattern. Two same-weight yellow buttons dilute the exact signal the system is built around.
- **Fix**: Make "Inscrire mon équipe" the ghost variant, keeping "Retour à l'accueil" (the safer default recovery action) as the solid primary.
- **Suggested command**: `/impeccable polish`

**[P1] Page doesn't use the app's own shared auxiliary-page template**
- **Why it matters**: Login, activate-account, and forgot-password all wrap content in the same `.login-page > .login-card > .card-header` structure with a "54" logo-mark linking home. This page uses a completely separate structure with no card, no border, no logo-mark — the strongest, most concrete consistency violation on the page, and the main reason it reads as category-interchangeable.
- **Fix**: Reuse the shared card/header markup and logo-mark, matching the three sibling auxiliary pages.
- **Suggested command**: `/impeccable polish`

**[P2] No login path for the most likely visitor**
- **Why it matters**: A stale activation link or Yurplan payment redirect describes someone who is very likely already registered, not a first-time signup — yet only "Retour à l'accueil" and "Inscrire mon équipe" are offered, funneling a returning participant toward re-registration instead of login, risking confusion or a duplicate-signup attempt against a capacity-capped roster.
- **Fix**: Add a lighter-weight "Se connecter" link, mirroring the register-link/forgot-password-link pattern already used on the login page.
- **Suggested command**: `/impeccable clarify`

**[P3] The "404" numeral uses a hardcoded font-size with no fluid clamp** *(source-derived, not visually confirmed on mobile this run)*
- **Why it matters**: `.code { font-size: 10rem }` has no `clamp()`, unlike DESIGN.md's own Display token which is specifically fluid so large type doesn't break on narrow viewports — the one hardcoded exception to that rule found on this page.
- **Fix**: `font-size: clamp(5rem, 20vw, 10rem)` or similar.
- **Suggested command**: `/impeccable polish`

## Persona Red Flags

**Jordan (confused first-timer, followed a stale/broken link)** — the primary persona here, since the realistic paths into this page (stale activation link, stale payment redirect) describe a lapsed registrant, not a stranger: no logo-mark or card cue to reassure them they're still on the real site; no "Se connecter" option at all, pushed instead toward new registration; two identically-styled yellow buttons give no "start here" signal.

**Casey (distracted mobile user, link shared via text/social)** — findings CSS-derived only, not visually confirmed: the fixed `10rem` "404" font-size is the one place on this page departing from the system's mobile-safe fluid-type convention; the smaller "Inscrire mon équipe" button computes to roughly ~35px tall, under the 44×44px minimum touch target, though `flex-wrap` at least stacks the two buttons rather than letting them overflow.

## Minor Observations

- The `.code` "404" text is low-contrast decorative text that a screen reader will announce redundantly before the `<h1>` restates the same status — consider `aria-hidden="true"`.
- No footer or organizer contact info on this page, unlike landing's footer (phone/email + charity line) — the trust/charity signal present almost everywhere else in the product is entirely absent here.
- Browser tab title doesn't reflect "404," but this is consistent app-wide (no route uses Angular's `Title` service), not a 404-specific gap.

## Questions to Consider

- If the most likely visitor here is a lapsed registrant, is "Inscrire mon équipe" actually the right second CTA, or just the button component's default reused without thinking through who's on this page?
- This page shares zero structural DNA with login/activate-account/forgot-password — was that deliberate, or did it simply not get the same pass those three did?
- Registration is capacity-capped and landing already knows how to represent "sold out" gracefully — should the 404 page be the one surface in the app that can promise a signup flow it has no way of knowing is still open?
