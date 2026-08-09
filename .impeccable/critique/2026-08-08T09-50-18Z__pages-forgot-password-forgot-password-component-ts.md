---
target: forgot-password page
total_score: 16
max_score: 40
na_heuristics: 
p0_count: 2
p1_count: 2
timestamp: 2026-08-08T09-50-18Z
slug: pages-forgot-password-forgot-password-component-ts
---
Method: dual-agent (A: assessment-a-forgot-password · B: assessment-b-forgot-password)

#### Design Health Score

| # | Heuristic | Score | Justification |
|---|---|---|---|
| 1 | Visibility of system status | 2/4 | Loading spinner + disabled button on submit is present, but the error banner has no `aria-live`/`role="alert"` and doesn't clear reactively while the user edits the input. |
| 2 | Match between system and the real world | 2/4 | Copy itself is plausible French, but the page's red/black visual identity clashes with "this is the same police-charity event site" expectation set by `/login`. |
| 3 | User control and freedom | 1/4 | On success, `*ngIf="!successMessage"` removes the entire form (`forgot-password.component.html:15-26`) — no path to correct a mistyped email without leaving and returning to the page. |
| 4 | Consistency and standards | 1/4 | `forgot-password.component.scss` hardcodes `#0d0d0d`/`#dc2626` and imports Bebas Neue + DM Sans from Google Fonts (line 1), instead of the `$main-color`/`$font-title`/`$font-body` tokens the sibling `login.component.scss` consumes via `@use "variables" as *;`. Also lacks the `:focus-visible` rules `login.component.scss` defines. |
| 5 | Error prevention | 1/4 | `<input type="text">`, not `type="email"` (`forgot-password.component.html:18`) — no native format check or mobile email keyboard; client guard is a bare truthy check (`!this.email`), so whitespace-only input reaches the network. |
| 6 | Recognition rather than recall | 3/4 | Single-field form, nothing to remember. |
| 7 | Flexibility and efficiency of use | 2/4 | `(keyup.enter)` submit and `autocomplete="email"` are good touches, but there's no `<form>` element (weakens password-manager autofill) and no quick re-submit path after an error. |
| 8 | Aesthetic and minimalist design | 2/4 | Layout is clean/minimal, but the palette is a jarring non-sequitur against the rest of the app — confirmed live (red badge/CTA vs. yellow everywhere else) and independently by the automated live-detector flagging a decorative radial-glow and repeating-stripe gradient baked into this same stylesheet. |
| 9 | Help users recognize, diagnose, and recover from errors | 1/4 | Submitting a malformed email (e.g. "asdf") triggers a backend 400 validation failure, but the component collapses every non-429 error into "Erreur serveur, veuillez réessayer." — factually wrong and gives advice (retry) that will fail identically every time. |
| 10 | Help and documentation | 1/4 | No link-expiry statement, no spam-folder hint, no support/contact fallback anywhere on the page. |

**Total: 16/40** (all 10 heuristics applicable; none N/A)

#### Design Specificity Verdict

This page is a leftover, un-migrated template — not a deliberate design. `forgot-password.component.scss` hardcodes a red/black palette (`#0d0d0d`, `#dc2626`) and pulls in "Bebas Neue" + "DM Sans" from Google Fonts at runtime, while the sibling `login.component.scss` correctly consumes the app's actual brand tokens (`$main-color` = `#ffed00` yellow, `$font-title` = "Lemon", `$font-body` = "Cabin"). Live screenshots confirm this isn't a paper diff: `/login` renders a yellow badge and yellow CTA; `/mot-de-passe-oublie` renders the same layout in red. The independent automated live-detector run corroborates this from a different angle — it flagged a radial "spotlight glow" and a repeating-stripe gradient as decorative anti-patterns on this exact page's `.login-page`/`body` elements. One assessment speculated these might originate from shared CSS rather than the component itself; direct inspection of `forgot-password.component.scss` (lines 8–28) shows that's not the case — both effects are defined locally in this component's own stylesheet. The same red/Bebas-Neue pattern also appears in `activate-account.component.scss`, so this is systemic across at least two auth-adjacent pages, not a one-off slip.

The one place real intentionality shows up is the success copy itself: *"Si un compte existe pour cet email, un lien a été envoyé."* — an honest, well-calibrated anti-enumeration message that doesn't read as evasive. That is the single considered decision on the page; the visual system, error-message accuracy, and focus states all read as inherited-and-never-revisited.

#### Overall Impression

The person landing on this page is a locked-out participant, likely anxious about missing the event days away. They click through from a yellow/black branded `/login` page into a red-and-black page for a security-sensitive action — precisely the kind of visual discontinuity that trains users toward "did I land somewhere wrong?" suspicion at the worst possible moment. The functional skeleton is sound (empty-field guard, Enter-to-submit, loading state, a real distinct 429 message from the backend) and the core success copy is genuinely well-crafted, honest about the intentional email-enumeration ambiguity. But nearly every supporting detail — color system, font choice, error-message accuracy, keyboard focus visibility, retry path after a mistake — was never brought in line with the rest of the app or with what a stressed user actually needs from this screen.

#### What's Working

1. The anti-enumeration success message (*"Si un compte existe pour cet email, un lien a été envoyé."*) is well-calibrated — reassuring without being cagey, which is the hard part of designing copy for a deliberately ambiguous backend response.
2. Functional basics all work: empty-field guard, Enter-to-submit, a disabled/spinner loading state, and a real, specific 429 message surfaced from the backend rather than folded into a generic wall of text.
3. Minimal, single-field IA — the form asks for exactly one thing and nothing more.

#### Priority Issues

**[P0] Off-brand visual identity (color, fonts, and decorative anti-patterns)**
**Why it matters:** A participant moves from the yellow/black branded `/login` page into a red/black page with a second, unrelated font family (Bebas Neue + DM Sans, loaded from an external CDN) for a security-sensitive password-reset action. This visual break undermines trust exactly when reassurance matters most, and it repeats in `activate-account.component.scss` — a systemic gap, not a one-off.
**Fix:** Replace hardcoded hex values and `font-family` declarations in `forgot-password.component.scss` with the shared `$main-color`/`$font-title`/`$font-body`/`$background-color` tokens (mirror `login.component.scss`'s `@use "variables" as *;` pattern); drop the redundant Google Fonts `@import`; audit `activate-account.component.scss` for the same fix.
**Suggested command:** `/impeccable adapt`

**[P0] Misleading error copy for invalid/malformed email**
**Why it matters:** Submitting something like "asdf" triggers a backend 400 validation failure, but `forgot-password.component.ts:37-42` collapses every non-429 error status into "Erreur serveur, veuillez réessayer." — telling the user to retry an action that will fail identically every time, and misrepresenting a client input problem as a server outage.
**Fix:** Branch the error handler on status (400 validation vs. 5xx/network) and show copy that actually helps ("vérifiez le format de votre email") for the validation case.
**Suggested command:** `/impeccable harden`

**[P1] No visible keyboard focus indicator**
**Why it matters:** Confirmed live via keyboard Tab — the submit button and "Retour à la connexion" link show zero visual change on focus. `login.component.scss` defines explicit `:focus-visible { outline: 3px solid $main-color; }` on both equivalent elements; this page has no `:focus-visible` rules at all. This is a WCAG 2.4.7 failure on a page whose only two interactive elements are a button and a link.
**Fix:** Port the `:focus-visible` outline rules from `login.component.scss` onto `.submit-btn` and `.back-link a`; add `role="alert"`/`aria-live="polite"` to `.error-banner` and `aria-hidden="true"` to the bare "!" icon glyph.
**Suggested command:** `/impeccable harden`

**[P1] Dead-end success state with no retry path**
**Why it matters:** On success, `*ngIf="!successMessage"` removes the entire form block (`forgot-password.component.html:15-26`), leaving only static text and a single "back to login" link. Given the backend's silent per-email cooldown and always-generic success message, a wrong-address mistake is otherwise invisible until the user simply never receives an email — the UI should make "try a different address" trivial, not require leaving and re-entering the page.
**Fix:** Keep the form reachable after success (or add an explicit "pas la bonne adresse ? réessayer" affordance) instead of hiding it entirely.
**Suggested command:** `/impeccable clarify`

**[P2] Stale, non-reactive error state**
**Why it matters:** `errorMessage` is only cleared inside `onSubmit()` (`forgot-password.component.ts:29`), not on input change. Confirmed live: after the empty-field error fires, typing "asdf" leaves the old error message on screen unchanged until the next submit — adding confusion at exactly the moment the user is trying to self-correct.
**Fix:** Clear `errorMessage` on `(ngModelChange)`, or move to a reactive form whose `valueChanges` drives error-state reset.
**Suggested command:** `/impeccable polish`

#### Persona Red Flags

- **Sam (accessibility-dependent):** No visible focus ring on the submit button or back-link (confirmed live via keyboard Tab); the error banner has no `role="alert"`/`aria-live`, so a screen-reader user gets no proactive announcement that submission failed; the bare "!" icon glyph isn't `aria-hidden`, so it may be read aloud oddly by some screen readers.
- **Riley (stress-tester/edge-case prober):** Submitting "asdf" produces "Erreur serveur, veuillez réessayer." — wrong and useless. Whitespace-only input (`"   "`) passes the client's truthy check and hits the network unnecessarily. Reaching the success screen and then hitting browser-back drops the user back to a blank, memory-less form with no record a request was already sent.
- **Jordan (first-timer, plausibly the primary persona here — a locked-out participant days before the event):** Lands on a page that visually doesn't match the yellow/black branding just seen on `/login`, which can read as "did I land somewhere wrong?" at exactly the moment they're already anxious about losing access before the competition.

#### Minor Observations

- Success and error states share nearly identical layout/typography weight — no distinct color or iconography differentiates "good news" from "bad news" at a glance; only the red border on the error banner differs, and success has no banner styling of its own.
- No stated link-expiry window in the success copy ("ce lien est valable 24h," etc.) — a small addition that would head off a second wave of anxiety ("did I already miss my window?").
- No mention of checking spam/junk folders, a near-universal real-world failure point for transactional email.
- The 429 message and the generic "server error" message render in the identical visual treatment, so a legitimately rate-limited user and a user hitting a real bug get the same visual urgency despite very different situations.
- The in-card logo mark reads "SR" ("Sports Reservation," likely a scaffold placeholder) while the browser tab title reads "Sport Challenge Police 54" — a small brand-naming mismatch, directly visible on this page.
- Automated static detection (CLI) came back clean on both `forgot-password.component.ts` and `.html` — no anti-patterns in markup/logic structure; the only automated findings were visual/decorative (radial glow, stripe gradient), both traced to this component's own stylesheet.
- No automated or manual finding flagged the ambiguous-success-message design itself as a problem — both assessments treat it as an intentional, well-executed product decision, not a bug.

#### Questions to Consider

1. This page and `activate-account` are running a completely different color/font system than the rest of the app — was that ever a deliberate choice, or did nobody open this file since it was first scaffolded?
2. The copy is careful never to reveal whether an email exists — so why does an obviously malformed email ("asdf") get told "server error, try again" instead of a message that would actually help them fix it?
3. If a stressed participant mistypes their email and lands on the generic success screen, what's the intended way for them to try again — is "leave the page and come back" really the flow we want three days before a competition?
