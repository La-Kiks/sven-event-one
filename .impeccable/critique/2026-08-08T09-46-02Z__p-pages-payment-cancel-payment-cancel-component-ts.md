---
target: payment-cancel page
total_score: 10
max_score: 36
na_heuristics: 7
p0_count: 1
p1_count: 2
timestamp: 2026-08-08T09-46-02Z
slug: p-pages-payment-cancel-payment-cancel-component-ts
---
Method: dual-agent (A: ad1fcf1bf193e8921 · B: a8d3bbb3bbb752459)

#### Design Health Score

| # | Heuristic | Score | Notes |
|---|---|---|---|
| 1 | Visibility of system status | 1/4 | States "cancelled" but says nothing about the team's actual state (still exists, unpaid) or what happens next |
| 2 | Match between system & real world | 1/4 | Copy has two typos ("Paiment" → Paiement, "ultériement" → ultérieurement) and reads cold/error-toned for a charity/colleague event |
| 3 | User control and freedom | 1/4 | Only exit is "Fermer," which routes to `/` (landing) — no path back to checkout or to the team |
| 4 | Consistency and standards | 3/4 | Internally consistent with `payment-success`'s identical modal pattern; red-X is a recognizable convention |
| 5 | Error prevention | 1/4 | No guidance on why a checkout gets cancelled or how to avoid repeating it |
| 6 | Recognition rather than recall | 1/4 | Nothing on screen reminds the user that login → mon-equipe shows payment status |
| 7 | Flexibility and efficiency of use | N/A | Single-purpose, one-shot landing screen — no expert/repeat-use dimension applies |
| 8 | Aesthetic and minimalist design | 1/4 | Minimal, but by omission (empty SCSS, no brand) not by intent — reads unfinished, not spare |
| 9 | Help recognize/diagnose/recover from errors | 1/4 | Names the event, offers zero recovery action or reassurance the spot/payment is safe |
| 10 | Help and documentation | 0/4 | `mon-equipe` already has an established "contact an organizer" pattern for unpaid teams — its total absence here is an avoidable gap, not an inapplicable dimension |

**Total: 10/36** (heuristic 7 excluded as N/A; 9 applicable heuristics × 4 = 36 max)

#### Design Specificity Verdict

Generic/templated, not bespoke. Both assessments independently confirmed `payment-cancel.component.scss` is genuinely empty (not a read error) and the CLI static scan came back clean (`[]`, exit 0) on `.ts`/`.html`/`.scss` — precisely because there is almost no page-specific surface to scan. Every visible pixel comes from the shared `ModalComponent`, reused verbatim from `payment-success` with only the `status`/`message` inputs swapped. Outside the modal: no header, no logo, no "HYROX POLICE 54" wordmark, none of the yellow/black brand identity the landing page establishes. The result is a dark-gray card adrift in a flat black void — indistinguishable from a generic SaaS error toast, with zero design intent applied to what the brief frames as a recovery moment.

#### Overall Impression

The component's own code is trivially clean (both agents agree: empty SCSS, one-line HTML, no static anti-patterns), but that cleanliness is a symptom, not a virtue — it means essentially no design work was done here at all. Assessment A's cross-repo trace surfaced the real severity: this isn't just an underdesigned screen, it's a screen with no functional way to complete its own stated purpose. `StripeService.redirectToCheckout(teamId)` exists in the codebase but is called from nowhere — not from this page, not from `mon-equipe` (the only page a returning unpaid participant can reach), which shows a static "✗ Non payé" badge with no pay button. The registration flow itself currently redirects to an external Yurplan URL rather than this app's Stripe Checkout, raising a real question about whether `/payment-cancel` is reachable through any current live user action at all. Combined with copy typos, a stock red-X failure icon, and a black void with no brand presence, the emotional tone lands as "something broke" rather than the brief's target of "no worries, try again."

#### What's Working

1. The underlying architecture is genuinely reassuring by design — teams are created *before* Stripe Checkout (`TeamService`), so a cancelled payment never destroys the registration. The problem is purely that this truth is never communicated on screen.
2. One shared `ModalComponent`/`StatusType` skeleton (icon + message + close) serves both success and cancel states — a clean, low-overhead structural pattern that's easy to build real content into.
3. Both the static CLI scan and manual code read agree the component's own markup/logic is minimal and free of anti-patterns — a clean base to build on, not a base that needs untangling.

#### Priority Issues

**[P0] No retry-payment mechanism exists anywhere downstream of this page**
**Why it matters**: This page's entire reason to exist is to get an abandoned payer back to checkout. `StripeService.redirectToCheckout(teamId)` is defined in the Angular app but called from nowhere — not from `payment-cancel`, not from `mon-equipe` (the only screen a returning unpaid participant can reach later), which shows just a static "✗ Non payé" badge and a note to contact the organizer. A user who cancels checkout has no in-app way to ever try paying again.
**Fix**: Wire an actual "Réessayer le paiement" CTA into this page (and ideally into `mon-equipe`'s unpaid state) that calls `redirectToCheckout` for the team, using the same `[url]` slot `ModalComponent` already supports (currently unbound) or a dedicated button.
**Suggested command**: `/impeccable shape`

**[P1] Copy and iconography actively contradict the "reassuring" brief**
**Why it matters**: The message has two typos ("Paiment," "ultériement"), is terse and error-toned, and pairs with a stock red-X failure icon on a bare black backdrop — the opposite of "nothing broke, you didn't lose your spot." For a charity event run by/for colleagues, this reads as a system failure at the exact moment reassurance matters most.
**Fix**: Rewrite the copy to explicitly state the team/registration is intact and unpaid (not lost), fix the typos, and reconsider whether a hard red-X is the right icon for "you can still finish this" vs. an actual error.
**Suggested command**: `/impeccable clarify`

**[P1] Zero brand presence — composition reads as broken, not minimal**
**Why it matters**: An empty page-level SCSS file and no header/logo/brand treatment leaves a small card floating in a large black void, especially stark on wide viewports. This is the least brand-invested screen in the product at the moment users are most likely to need reassurance.
**Fix**: Apply the site's established yellow/black + Lemon Milk/Cabin identity to this page's layout (even minimally — a header/wordmark, consistent background treatment) so it reads as an intentional part of the product rather than an unfinished fallback.
**Suggested command**: `/impeccable polish`

**[P2] Accessibility gaps in the modal**
**Why it matters**: The accessibility tree exposes only a plain text node and a button — no `role="dialog"`/`aria-modal`, no heading, no live-region announcement of the status change, and no confirmed focus management on open. A screen-reader user gets no structural signal this is a status page at all.
**Fix**: Add `role="dialog"`, `aria-modal="true"`, a heading element, and programmatic focus-move into the modal in `ModalComponent` (shared, so this fix also benefits `payment-success`).
**Suggested command**: `/impeccable harden`

**[P2] Navigation is a dead end, not a recovery path**
**Why it matters**: "Fermer" routes to the landing page, not to login/mon-equipe/checkout. A user checking on their payment must self-navigate landing → "Se connecter" → login → mon-equipe, and even then finds no way to pay (see P0). "Fermer" (Close) is also an odd verb for a full navigation away from the flow — it primes the user to think they're dismissing an overlay, not leaving entirely.
**Fix**: Replace the single "Fermer" action with an explicit primary path back into the payment/registration flow, and relabel to match the actual destination.
**Suggested command**: `/impeccable layout`

#### Persona Red Flags

- **Jordan (first-timer)**: Just tried to pay for a charity police event, lands on a black screen with a red X and a typo'd sentence, no button to retry. Likely to conclude the site is broken or their spot/money is at risk — may abandon rather than retry, or contact the organizer directly, which is exactly the support load this screen should be preventing.
- **Sam (accessibility)**: No heading, no dialog role, no `aria-modal`, no live-region — a screen-reader user gets a bare "text, then button" with no structural context that this is a status page, and no evidence focus moves into the modal on load.
- **Riley (stress-tester)**: Bookmarking, refreshing, or returning to this URL after multiple failed attempts produces an identical generic message with no team/session reference — no way to tell if it reflects the latest attempt, and repeated "Fermer" clicks just loop back to the landing page with nothing resolved.

#### Minor Observations

- `.modal-close-btn:hover { background-color: filter(brightness(1.25)); }` in `modal.component.scss` is invalid CSS (`filter()` is not a valid `background-color` value) — the hover state silently does nothing, so the one interactive element on the page has no hover feedback.
- `payment-success` shares the identical structural and copy gaps (same "Paiment" typo, no next-step CTA), suggesting this is a systemic gap across both transactional pages rather than something specific to cancel.
- Contrast/legibility itself is fine (white text on `#262626`, black text on `#ffed00` button) — the accessibility issues found are structural/semantic, not color-contrast.
- Assessment B's live browser scan flagged a `flat-type-hierarchy` finding (16/20/24px, ratio 1.5:1) via the runtime detector — likely a false positive as far as this component is concerned: `payment-cancel`'s own template is a single one-line wrapper around `<app-modal>`, so this type-scale signal almost certainly originates in the shared modal/button/global styles rather than in page-specific code. Worth checking if fixed globally, not scoped to this page.
- Both assessments independently confirmed the `.scss` file is genuinely empty and the CLI static scan is clean (exit 0, `[]`) on all three files — agreement that the component's own code has no static anti-patterns, only an absence of design work.

#### Questions to Consider

1. If a participant cancels checkout, returns later, logs in, and sees "✗ Non payé" on `mon-equipe` — where, concretely, do they click to pay? Today's answer appears to be nowhere; is that a known gap?
2. Given registration currently redirects to an external Yurplan URL rather than this app's own Stripe Checkout, is `/payment-cancel` reachable by real users at all right now — and if not, is polishing it the right use of design effort versus whatever screen the external flow actually returns to?
3. This event's pitch is camaraderie among colleagues doing a charity challenge together — why does the moment most likely to produce anxiety (a failed payment) get the least design investment in the product (empty SCSS, borrowed error icon, unreassuring copy)?
