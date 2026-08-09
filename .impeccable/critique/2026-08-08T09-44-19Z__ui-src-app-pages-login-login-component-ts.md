---
target: login page
total_score: 28
max_score: 40
na_heuristics: 
p0_count: 1
p1_count: 1
timestamp: 2026-08-08T09-44-19Z
slug: ui-src-app-pages-login-login-component-ts
---
Method: dual-agent (A: a83333f083713d249 · B: ad958bc892e9a39f3)

#### Design Health Score

| # | Heuristic | Score | Key Issue |
|---|---|---|---|
| 1 | Visibility of system status | 3/4 | Spinner + disabled button on submit, but no `aria-live` so the state change is silent for screen readers. |
| 2 | Match between system and the real world | 3/4 | Natural French copy ("Bon retour"), but the generic "SR" logo mark has no connection to Police 54/Hyrox context. |
| 3 | User control and freedom | 2/4 | No link back to landing/registration anywhere on the page — the logo mark is static text, not a link, and there's no nav. |
| 4 | Consistency and standards | 4/4 | Standard email/password/submit/forgot-password layout; correct `autocomplete` values and `for`/`id` label wiring. |
| 5 | Error prevention | 2/4 | Only a non-empty check on submit; email field is `type="text"` so there's no format validation, inline or otherwise. |
| 6 | Recognition rather than recall | 3/4 | Labels stay visible (good), but no show/hide password toggle to let users verify what they typed. |
| 7 | Flexibility and efficiency of use | 3/4 | `(keyup.enter)` support and `autocomplete` hints aid power users/password managers, but no autofocus and no "remember me". |
| 8 | Aesthetic and minimalist design | 4/4 | Single card, no clutter, restrained palette. |
| 9 | Help recognize/diagnose/recover from errors | 2/4 | Error copy is calm and non-blaming, but the banner has no `role="alert"`/`aria-live`, no field-level highlighting, and no inline recovery link. |
| 10 | Help and documentation | 2/4 | "Mot de passe oublié ?" is present and discoverable, but there's no path for "I don't have an account yet" or any support/contact fallback. |

**Total: 28/40** (no heuristics n/a — this is an Operate surface and both #7 and #10 legitimately apply).

#### Design Specificity Verdict

Both assessments converge on the same verdict from different angles. The LLM review (Assessment A) called the page "mostly generic": strip the "Bon retour" heading and the yellow CTA, and the card — `rgba(255,255,255,0.03)` fill, `backdrop-filter: blur(10px)`, thin white-alpha borders — is an unbranded dark glassmorphic SaaS login indistinguishable from Linear/Vercel/Stripe-style dashboards. There is no reference anywhere on the page to Police 54, Hyrox, or Orphéopolis, and the logo mark is generic initials ("SR") rather than anything tied to the event brand used elsewhere in the app.

The deterministic scan (Assessment B) independently supports this: the static source-file scan (`detect.mjs` against the `.ts`/`.html`/`.scss` files) came back completely clean (exit 0, zero findings on all three files), but the **live browser overlay** — which inspects rendered/computed styles the static scan can't see — flagged two generic decorative anti-patterns on the rendered page: a `radial-spotlight-glow` (yellow `#ffed00` at ~7% opacity on `div.login-page`) and a `repeating-stripes-gradient` on `body`. Assessment B flagged genuine uncertainty about whether these are lazy generic filler or a deliberate, restrained brand accent (the yellow does match the app's brand color) — but the fact that a generic-pattern detector recognizes these effects as templated "decorative glow/stripe" idioms at all is corroborating evidence for Assessment A's core complaint: the page reaches for stock visual moves rather than anything legible as "this specific event's login." Notably, the static-vs-live split itself is informative — a page can pass a source-level generic-pattern scan while still reading as generic once rendered, which is exactly what happened here.

#### Overall Impression

Functionally solid — French copy is warm and calm, loading/disabled states are handled, `autocomplete` and label wiring are correct — but the page currently does none of the trust-building work this app needs from its front door. For a public/police charity event where legitimacy matters, a login screen with zero event branding and a silent (non-`aria-live`) error path undercuts both the emotional case (nothing to reassure a nervous first-timer) and the accessibility case (nothing to inform a screen-reader user their login failed). Both assessments agree the biggest structural risks are the accessibility gap on the error path and the complete absence of a way back to registration/landing — not the surface color/decoration choices, which are merely generic rather than broken.

#### What's Working

- Loading state properly disables the submit button and shows a spinner, preventing double-submits (`onSubmit()` sets `isLoading` in `login.component.ts`).
- Correct semantic label/`for`/`id` association and `autocomplete="email"`/`"current-password"` hints, so password managers and browser autofill work as expected — an easy thing to skip that wasn't skipped.
- A deliberate, visible focus-visible treatment on the submit button (`outline: 3px solid $main-color; outline-offset: 3px`), better than most login forms bother with.
- The error copy itself is calmly worded and non-blaming ("Email ou mot de passe incorrect." rather than anything that reads as user-blaming).

#### Priority Issues

**[P0] Error banner and validation state are invisible to assistive tech**
Why it matters: There is no `role="alert"`/`aria-live` region and no `aria-invalid`/`aria-describedby` wiring on the inputs. A screen-reader user who submits blank or wrong credentials gets zero indication anything happened — this isn't a polish gap, it blocks the login task entirely for that user, on a page that gates paid access to a public charity event.
Fix: Add `role="alert"` + `aria-live="polite"` to the error-message container, wire `aria-invalid`/`aria-describedby` onto the email/password inputs when `errorMessage` is set, and mark the decorative `!` icon `aria-hidden="true"` so it isn't announced as a stray character alongside the message.
Suggested command: /impeccable harden

**[P1] No discoverable way back to registration or the landing page**
Why it matters: The logo mark is static text, not a link, and there's no nav on the page. A first-time visitor (Jordan persona) who lands on `/login` without an account — the most likely audience for a duo-registration event — has no path forward except the browser back button, and no hint that accounts are created via team registration + an activation email rather than self-signup here.
Fix: Make the logo/header a link back to the landing page, and add a short line under the form (e.g. "Pas encore inscrit ? Inscrivez votre équipe") linking to the registration flow.
Suggested command: /impeccable layout

**[P2] De-branded, templated visual surface undercuts trust at a legitimacy-sensitive moment**
Why it matters: Corroborated by both assessments — the LLM review found no event-specific imagery or copy, and the deterministic live-detector independently flagged generic "radial spotlight glow" and "repeating stripe gradient" decorative patterns on the rendered page. For a police/charity event where the registration and landing pages already establish trust (named charity, sponsor roster, police affiliation), the login screen reverts to stock SaaS decoration with no connection to Police 54/Hyrox/Orphéopolis.
Fix: Carry over at least one concrete brand/event marker from landing/registration (event name/logo mark, or a compact trust cue referencing the charity), and reconsider whether the generic spotlight/stripe decoration is intentional brand texture or leftover template filler.
Suggested command: /impeccable adapt

**[P2] Placeholder text contrast is very low**
Why it matters: Calculated at roughly 2.2:1 (`rgba(255,255,255,0.25)` text over a near-black `rgba(255,255,255,0.05)` input fill), well under WCAG AA guidance for the hint copy ("Entrez votre email") — making it hard to read for low-vision users before they start typing. Notably the error banner and button text contrast are fine by comparison, so this is a localized fix, not a systemic palette problem.
Fix: Raise placeholder text opacity/lightness to clear at least 4.5:1 against the input background.
Suggested command: /impeccable polish

**[P3] Email input uses `type="text"` instead of `type="email"`**
Why it matters: Forgoes the `@`-optimized mobile keyboard (Casey persona) and native browser format validation, leaving all validation to the single coarse "remplir tous les champs" message regardless of whether a field is empty or malformed.
Fix: Change the input type to `email` and consider a lightweight inline format check before submit.
Suggested command: /impeccable clarify

#### Persona Red Flags

- **Jordan (first-timer):** No breadcrumb back to the event site or registration if they land here without an account, and the empty-field message gives no hint that accounts come only from team registration + an activation email — someone who never received that email has no clue what to do next beyond "Mot de passe oublié ?", which won't help since they never had a password to forget.
- **Sam (accessibility):** The error banner's total silence for assistive tech (no `aria-live`) is the standout failure — the form functionally fails for this persona on any wrong-credentials or empty-field attempt. Secondary: input focus state is a border-color swap only, visibly weaker and inconsistent with the button's outline+offset treatment.
- **Casey (mobile):** `type="text"` on the email field forgoes the `@`-key mobile keyboard layout; the component has no `@media` breakpoints at all (confirmed in the SCSS), relying entirely on a fluid `max-width: 400px` card, and `min-height: 100vh` risks the classic iOS viewport-jump-on-keyboard-open issue with no dynamic-viewport-unit fallback.

#### Minor Observations

- No `autofocus` on the email field — a near-zero-cost efficiency win left on the table.
- No show/hide password toggle.
- The static source-file scan (`.ts`/`.html`/`.scss`) was completely clean — the two detector findings only surfaced against the live-rendered page, a reminder that source-level scans and rendered-page scans catch different things.
- Cognitive-load checklist passed 7/8 items (≤4 actions, single CTA, chunked fields, low working-memory demand, immediate feedback, no distractions, progressive disclosure) — the one failure is errors being localized to a single generic top-of-form banner rather than the specific field at fault.
- Emotional journey is flat rather than reassuring-then-rewarding: successful login redirects silently with no acknowledgment moment, and the page offers no trust-building content during a moment (entering credentials) where that would matter most for this audience.

#### Questions to Consider

1. If this is the front door for a police-charity fitness event, what would visitors actually lose if "Bon retour" and the "SR" mark were swapped for any other product's name — and is that an acceptable answer for a legitimacy-sensitive login screen?
2. Given accounts only exist via team registration + activation email, is a bare email/password login even the right mental model for first-timers, or does this screen need an explicit branch for "I just registered, where's my password?"
3. Is "sighted users can see the error" an acceptable bar for a page gating paid access to a public charity event, or does the total silence for screen-reader users on a failed login warrant a real accessibility pass before this ships?
