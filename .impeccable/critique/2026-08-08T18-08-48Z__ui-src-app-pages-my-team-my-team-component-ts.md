---
target: my-team page
total_score: 17
max_score: 40
na_heuristics: 
p0_count: 1
p1_count: 2
timestamp: 2026-08-08T18-08-48Z
slug: ui-src-app-pages-my-team-my-team-component-ts
---
Method: dual-agent (A: a2c9bfc89c9923f9d · B: a223799f0a0d36779)

#### Design Health Score

Operate-mode surface — all 10 heuristics scored for real.

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 1 | Loading/error/save states exist, but the live pass found the system silently hiding its own state: `version`, `category`, `outfit`, `administration` all hold real saved values yet render as unselected controls with no indicator that data exists underneath. |
| 2 | Match System / Real World | 2 | French copy is natural and role-appropriate, but "Statut de paiement" conflates a status label with a 48h-lag explanation in one string. |
| 3 | User Control and Freedom | 2 | No "revert to saved"/cancel-per-field option and no unsaved-changes guard on navigation, but this is a minor ding given the small form scope. |
| 4 | Consistency and Standards | 2 | The prior clash (login vs. form using two unrelated visual systems) is resolved — top bar now shares the yellow logo mark, Lemon Milk titles, and palette with login. But the form body is a verbatim reuse of the registration form's visual language, so an Operate-mode "review my status" screen is visually indistinguishable from a first-time signup form. |
| 5 | Error Prevention | 1 | Required-field validation works for empty strings, but a newly-confirmed gap: FormControls holding values that don't match any known radio/option (`"competition"`, `"Homme"`, `"Police Nationale QA"`, etc.) are treated as valid by `Validators.required` while rendering completely blank — nothing flags the mismatch. |
| 6 | Recognition Rather Than Recall | 1 | Direct consequence of #5 — a returning participant sees Version/Catégorie/Tenue/Administration all blank with red required-asterisks next to an otherwise fully-populated form, forcing them to re-guess answers they already gave. |
| 7 | Flexibility and Efficiency of Use | 2 | No shortcuts or jump-to-section, but the task itself (2-person roster edit) is small enough that this is a minor gap. |
| 8 | Aesthetic and Minimalist Design | 2 | Every field/fieldset — payment status, an informational "Version" choice, and contact fields — gets identical yellow-bordered treatment, flattening hierarchy; no hard visual seam remains (the prior brand clash is gone). |
| 9 | Error Recovery | 1 | Inline required-field errors work and the Save button correctly disables, and a `.retry-btn` now exists for `loadError` (prior P1 fixed). But on a long single-column form there's still no error summary or scroll-to-first-error — a user who errors near the top and scrolls down sees only a disabled button with no visible reason. |
| 10 | Help and Documentation | 3 | The locked-email hint and the unpaid-badge recourse note (tel:/mailto: links) are specific, actionable copy — confirmed working, a real improvement over the prior 0/4. |
| **Total** | | **17/40** | **Poor–Fair** |

#### Design Specificity Verdict

Partially deliberate, partially still generic. The brand-convergence fix (089dddf) genuinely landed: login and mon-equipe top bars now share the same yellow "SR" mark, Lemon Milk titling, and palette, with the previous Bebas Neue/DM Sans clash gone (Assessment B's deterministic scan found zero hard-coded anti-patterns in either the `.ts` or `.html` source). But the form body itself is, by the code's own comment, "same visual language as the registration form" — meaning a returning participant's account-management/status screen looks and behaves identically to a first-time marketing signup form, with no visual signal that this is a "review your data" task rather than a "please fill this out" task. Assessment B's live-DOM pass (browser-console `impeccable` detector, injected against the rendered page) surfaced two additional real signals invisible to static analysis: a radial spotlight glow on `div.page` (worth confirming as intentional brand texture vs. accidental) and a field-hint paragraph running ~96 characters/line against an ~80-char readability target.

#### Overall Impression

Net improvement since the 13/40 snapshot, but modest, and one new issue is arguably as serious as what it replaced. Both prior P0s are now fixed and verified live: the email field is genuinely locked (readonly, distinct styling, explanatory hint) and inline validation now works correctly (required-field messages, `aria-invalid`, Save button disabling). The prior P1 "loadError is a dead end" also appears fixed (a `.retry-btn` now exists). However, live testing on the QA team surfaced a new P0-grade bug: several FormControls (`version`, `category`, `outfit`, `administration`) hold real saved values that don't match any option in the template, so they render as blank required fields next to an otherwise fully-populated form — for a participant checking their status before an event, this reads as "your registration is broken" at the exact moment the page is supposed to deliver reassurance, directly undercutting the "payment status must be unambiguous" design goal. One data point needs a flag rather than a fix: Assessment A's live login showed the QA team's payment badge as green "✓ Payé," where this team was provisioned as an unpaid test fixture — worth the parent process double-checking whether that account's paid flag changed since provisioning (e.g. from other testing activity) before trusting the "unpaid" scenario was actually exercised this pass.

#### What's Working

- The participant-1 email lock is implemented correctly and clearly: `readonly` input, distinct locked-input styling, and an explanatory hint pointing to the organizer contact path — the 089dddf fix holds up under live testing.
- Inline field-level validation now functions correctly: required-field messages, `aria-invalid`/`aria-describedby` wiring, and a Save button that genuinely disables on `form.invalid` — confirmed live, resolving the prior P0.
- Login/mon-equipe brand convergence landed: shared logo mark, typography, and palette across both surfaces, with the deterministic scanner confirming zero hard-coded anti-pattern hits in the source files.

#### Priority Issues

**[P0] Existing selections silently render as unselected when the saved value doesn't match a known option**
- **Why it matters**: Live inspection of the QA team's API payload showed `version:"competition"`, `category:"Homme"`, `outfit:"M"/"L"`, `administration:"Police Nationale QA"` — none of which match the hardcoded `value=` attributes in `my-team.component.html`. Because `Validators.required` only checks non-empty, these FormControls report valid while every corresponding radio/select renders blank with a red required-asterisk, right next to correctly-populated name/phone/email fields. A returning participant reads this as "my registration is incomplete," and any attempt to "fix" it by re-selecting risks silently overwriting a value they can't currently see.
- **Fix**: On `patchForm`, validate incoming values against the known option set and surface a visible "this value doesn't match a known option — please reselect" state rather than failing silently; add a custom validator so a genuinely non-matching value is flagged invalid instead of passing.
- **Suggested command**: `/impeccable harden`

**[P1] No error summary or scroll-to-first-error on a long single-column form**
- **Why it matters**: Confirmed live — clearing "Nom d'équipe" and scrolling to the bottom Save button shows only a greyed-out button with no banner, message, or link back to the offending field. On a ~15-field form this is a real recoverability gap (Nielsen heuristic 9), worse for screen-magnification or keyboard-only users who can't quickly re-scan the whole page.
- **Fix**: Add a persistent inline banner/summary near the Save button when `form.invalid`, or auto-scroll to the first invalid control on a blocked submit attempt.
- **Suggested command**: `/impeccable harden`

**[P1] Custom radio/checkbox focus indicator is effectively invisible**
- **Why it matters**: Live-measured computed style on a focused radio control is `outline: auto 0.667px rgb(16,16,16)` — a sub-1px, near-black outline against a dark theme. The custom `appearance:none` styling removes native focus rendering without replacing it, failing WCAG 2.4.7 (focus visible) for keyboard users navigating the Version/Catégorie/Tenue fieldsets.
- **Fix**: Add an explicit `:focus-visible` rule for `input[type=radio]`/`input[type=checkbox]` (visible outline or box-shadow in the brand yellow), matching the pattern already used for `.input`, `.button`, and `.logout-btn` (which do have working focus-visible styling).
- **Suggested command**: `/impeccable harden`

**[P2] Team name shown in two bindings that can silently diverge**
- **Why it matters**: The static yellow `<h1>{{team.name}}</h1>` header and the editable "Nom d'équipe" input are bound to different sources (`team` object vs. `form`) — typing a correction into the field doesn't update the header until save succeeds, creating a brief but confusing "two sources of truth" moment for a value the user is actively editing.
- **Fix**: Either drop the static header (the form field already surfaces the name) or bind it live to `form.get('team.team_name').value`.
- **Suggested command**: `/impeccable clarify`

**[P3] Uniform visual weight across all fields regardless of importance**
- **Why it matters**: Payment status, a purely informational "Version" choice, and legally-relevant contact fields all receive identical yellow-bordered fieldset treatment — nothing visually signals "this is data you're verifying" vs. "this is a preference" vs. "this affects your event category," which flattens the hierarchy an Operate-mode status page should have.
- **Fix**: Differentiate section emphasis — lighter treatment for optional/informational fields, stronger treatment reserved for payment/account-critical information.
- **Suggested command**: `/impeccable layout`

#### Persona Red Flags

- **Jordan (first-timer)**: Would plausibly panic seeing Version/Catégorie/Tenue/Administration render blank with red asterisks immediately after logging in to check status — nothing distinguishes a rendering mismatch from an actually-incomplete registration.
- **Sam (accessibility-dependent)**: The near-invisible focus outline on radios/checkboxes (measured live at 0.667px, near-black) leaves keyboard-only or low-vision users with no reliable visual anchor for focus position while tabbing through the roster fieldsets — a genuine WCAG failure, not a style nitpick.
- **Riley (stress-tester/edge-case abuser)**: Confirmed exploitable gap — because required-field validation only checks non-empty, a garbage value like `"Police Nationale QA"` passes silently and displays as blank-but-valid indefinitely, meaning corrupted or mismatched data (from any future admin-edit path or schema change) would be masked rather than surfaced.

#### Minor Observations

- Deterministic scan (`detect.mjs`) returned zero findings against both `my-team.component.ts` and `my-team.component.html` — clean on hard-coded anti-patterns.
- Live-DOM browser-console pass flagged two rendering-dependent signals invisible to static analysis: a radial spotlight glow on `div.page` (`#ffed00` alpha 0.06 → transparent) — confirm this is deliberate brand texture, not accidental — and `#email_1-hint.field-hint` running ~96 characters/line against an ~80-char target.
- `.retry-btn` and `.logout-btn` already have correct `:focus-visible` styling — good precedent to extend to the radio/checkbox controls flagged above.
- `field-hint` and `payment-note` both render at `rgba(255,255,255,0.6)` — worth a contrast check, since both carry important guidance text (locked-email hint, payment recourse) at reduced opacity.
- Save success/error banners render directly under the payment status, far from the Save button at the bottom of the form — a save confirmation appearing above the fold the user just scrolled past is easy to miss.
- Not re-verified this pass: prior minor findings (`getAdminLabel()` dead code, `.required { color: red }` literal, SR logo vs. "Hyrox Police 54" brand name mismatch) — carry these forward for a future pass if not already addressed.
- True mobile-viewport rendering could not be verified live (the browser tool's resize did not actually change viewport width in this session); `my-team.component.scss` has no `@media` queries at all, which is worth a dedicated responsive check outside this critique.

#### Questions to Consider

- Is the mismatched enum data (`"Homme"`, `"M"`/`"L"`, `"Police Nationale QA"`) specific to how the QA fixture was seeded, or can real participant data drift this way via an admin-edit path or a future label/schema change? If the latter is plausible, this is a live data-integrity risk, not just a test artifact.
- Given the page's own stated mode is "Operate" (task completion, not marketing), should the edit form keep reusing the registration form's full visual language, or would a lighter "review/confirm" treatment better fit a returning user checking status?
- Should "Version," "Administration," and "Catégorie" remain participant-editable post-registration at all, or are they organizer-controlled facts once paid — if the latter, locking them (like the email) would eliminate the blank-radio failure mode entirely rather than just message around it?
