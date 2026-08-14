---
name: Hyrox Police 54
description: A tactical, high-contrast black/yellow registration system for a police-organized Hyrox duo competition.
colors:
  hazard-yellow: "#ffed00"
  void-black: "#000000"
  surface-raised: "#0a0a0a"
  surface-card: "#111111"
  field-bg: "#141414"
  field-bg-locked: "#1c1c1c"
  border-hairline: "rgba(255, 255, 255, 0.1)"
  border-field: "rgba(255, 255, 255, 0.14)"
  text-primary: "#ffffff"
  text-secondary: "rgba(255, 255, 255, 0.55)"
  text-tertiary: "rgba(255, 255, 255, 0.4)"
  success: "#4ade80"
  success-bg: "rgba(34, 197, 94, 0.15)"
  error: "#ff6b6b"
  error-bg: "rgba(255, 107, 107, 0.1)"
  danger: "#f87171"
  category-man: "#93c5fd"
  category-woman: "#f9a8d4"
  category-mixed: "#d8b4fe"
typography:
  display:
    fontFamily: "Lemon Milk, Segoe UI, Roboto, Arial, sans-serif"
    fontSize: "clamp(2.8rem, 9vw, 7rem)"
    fontWeight: 400
    lineHeight: 0.92
    letterSpacing: "-0.01em"
  headline:
    fontFamily: "Lemon Milk, Segoe UI, Roboto, Arial, sans-serif"
    fontSize: "clamp(1.8rem, 4vw, 2.6rem)"
    fontWeight: 400
    lineHeight: 1.05
  label:
    fontFamily: "Lemon Milk, Segoe UI, Roboto, Arial, sans-serif"
    fontSize: "0.78rem"
    fontWeight: 400
    letterSpacing: "0.16em"
  meta:
    fontFamily: "Cabin, Segoe UI, Roboto, Arial, sans-serif"
    fontSize: "0.85rem"
    fontWeight: 400
  body:
    fontFamily: "Cabin, Segoe UI, Roboto, Arial, sans-serif"
    fontSize: "1rem"
    fontWeight: 400
    lineHeight: 1.55
  caption:
    fontFamily: "Cabin, Segoe UI, Roboto, Arial, sans-serif"
    fontSize: "0.8rem"
    fontWeight: 400
    letterSpacing: "0.14em"
rounded:
  sharp: "2px"
  mark: "3px"
  control: "0.25em"
spacing:
  gutter: "clamp(1rem, 4vw, 3rem)"
  section: "clamp(3rem, 7vw, 5.5rem)"
  card-padding: "clamp(1.25rem, 3vw, 2.25rem)"
components:
  button-primary:
    backgroundColor: "{colors.hazard-yellow}"
    textColor: "#000000"
    rounded: "{rounded.sharp}"
    padding: "1.05rem 2.2rem"
  button-primary-hover:
    backgroundColor: "{colors.hazard-yellow}"
    textColor: "#000000"
  button-ghost:
    backgroundColor: "transparent"
    textColor: "rgba(255, 255, 255, 0.6)"
    rounded: "{rounded.sharp}"
    padding: "0.55rem 0.9rem"
  chip-active:
    backgroundColor: "{colors.hazard-yellow}"
    textColor: "#000000"
    rounded: "{rounded.sharp}"
    padding: "0.7rem 1.1rem"
  chip-inactive:
    backgroundColor: "transparent"
    textColor: "rgba(255, 255, 255, 0.75)"
    rounded: "{rounded.sharp}"
    padding: "0.7rem 1.1rem"
  card-primary:
    backgroundColor: "{colors.surface-card}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.sharp}"
---

# Design System: Hyrox Police 54

## Overview

**Creative North Star: "The Tactical Briefing"**

The system reads like a mission briefing board: a void-black ground, hazard-yellow highlight ink, everything squared off and legible at a glance. Nothing floats, nothing softens the edges — surfaces are stamped, not lifted, and the yellow is rationed so that when it appears (a CTA, a key stat, an active state) it reads as a directive, not decoration.

This is a deliberate rejection of the product's previous look, which used soft ~0.5rem rounded corners throughout. The redesign squares that off to a near-universal 2px radius as a statement of precision and seriousness, fitting an event organized by and for law-enforcement personnel in support of a police-officer charity (Orphéopolis). Disciplined and angular over friendly and soft.

**Key Characteristics:**
- Void-black ground with hazard-yellow used sparingly, never as a background fill for large areas
- Near-universal 2px corner radius — angular by design, not an oversight
- Flat throughout — depth comes from hairline borders and background-tier steps, never shadows
- All-caps Lemon Milk display type paired with restrained, lowercase Cabin body copy
- Grids separated by 1px hairlines rather than gaps (the "filet" pattern): the seam is the background showing through, not a border

## Colors

Void-black ground, hazard-yellow signal, and a tightly rationed set of status/category hues that only ever appear inside small badges — never as a fill.

### Primary
- **Hazard Yellow** (`#ffed00`): CTAs, active states, key stats/numbers, focus outlines, active chip fill. The rarest color on any given screen by design — its scarcity is what makes it read as urgent.

### Neutral
- **Void Black** (`#000000`): page ground.
- **Raised Surface** (`#0a0a0a`): the first layer up from the ground — panels, tiles, form cards, the step rail (inscription/admin).
- **Card Surface** (`#111111`): landing's format cards specifically (one step brighter than Raised Surface, reserved for imagery-bearing cards).
- **Field Surface** (`#141414`): input/select backgrounds.
- **Locked Field Surface** (`#1c1c1c`): readonly inputs (e.g. the participant-1 login email on `mon-équipe`) — visibly inert, `cursor: not-allowed`.
- **Hairline Border** (`rgba(255,255,255,0.1)`): card/table/tile borders and the "filet" grid-seam pattern.
- **Field Border** (`rgba(255,255,255,0.14)`): input/select/checkbox-row borders.
- **Primary Text** (`#ffffff`): headings, primary body text.
- **Secondary Text** (`rgba(255,255,255,0.55)`): supporting paragraphs, descriptions.
- **Tertiary Text** (`rgba(255,255,255,0.4)`): labels, meta, eyebrow copy, placeholder-adjacent text.

### Status & Category (badges only)
- **Success** (`#4ade80` on `rgba(34,197,94,0.15)`): paid, account activated, volunteer confirmation.
- **Error** (`#ff6b6b` on `rgba(255,107,107,0.1)`): unpaid, invalid field.
- **Danger** (`#f87171`, border `rgba(220,38,38,0.35)`): destructive actions (delete team).
- **Category — Homme** (`#93c5fd` on `rgba(59,130,246,0.12)`).
- **Category — Femme** (`#f9a8d4` on `rgba(236,72,153,0.12)`).
- **Category — Mixte** (`#d8b4fe` on `rgba(168,85,247,0.12)`).

### Named Rules
**The One Signal Rule.** Hazard Yellow never fills a large area — no yellow section backgrounds except the two full-bleed CTA bandeaux on the landing page, which are the intentional exception that proves the rule (maximum-urgency moments only). Everywhere else it's ink on black: text, borders, small fills.

## Typography

**Display Font:** Lemon Milk (with Segoe UI, Roboto, Arial fallback)
**Body Font:** Cabin (with Segoe UI, Roboto, Arial fallback)

**Character:** Lemon Milk is used exclusively uppercase, for anything that needs to command attention — it's blocky and geometric, never used for running text. Cabin carries every sentence a user actually reads; the pairing is deliberately lopsided toward Cabin for legibility, with Lemon Milk rationed to headlines, labels, and numerals.

### Hierarchy
- **Display** (400, `clamp(2.8rem, 9vw, 7rem)`, line-height 0.92): the landing hero H1 only.
- **Headline** (400, `clamp(1.8rem, 4vw, 2.6rem)`, line-height 1.05): page-level H1 on form/admin/mon-équipe screens.
- **Section Title** (400, `clamp(1.5rem, 4.5vw, 3.2rem)` on landing / `1.5rem` elsewhere): section headers, always uppercase.
- **Body** (400, 1rem, line-height 1.55, Cabin): paragraphs, field values.
- **Meta** (400, 0.85rem, Cabin, sentence case): compact UI chrome text that isn't a paragraph but also isn't a tracked/uppercase label — nav-tab text, the logged-in username, checkbox-row copy. The tell: it reads as a short phrase, not a single tracked word.
- **Label** (400, 0.72–0.78rem, letter-spacing 0.14–0.2em, uppercase, Tertiary Text color): eyebrows, form field captions, table headers, badge text.

### Named Rules
**The Uppercase-Only Rule.** Lemon Milk never appears in sentence case or lowercase — if it's Lemon Milk, it's uppercase with tracking. Cabin never appears in all-caps for running text.

## Layout

Single real breakpoint at **860px**; below it, sticky rails go static, multi-column layouts stack, and admin tables switch to horizontal scroll inside a bordered `overflow-x: auto` wrapper rather than compressing columns. Grids throughout use `repeat(auto-fit, minmax(...))` so they reflow without a second breakpoint (stations 260px, partenaires 140px, form fields 220px, status tiles 240px).

Content max-widths are role-specific, not a single global container: 1240px (landing), 1180px (inscription/admin), 980px (mon-équipe), 900px (admin teams table — deliberately wider than the base 1180px to fit its extra column). Page gutters and section spacing both use clamp-based fluid scales (`{spacing.gutter}`, `{spacing.section}`) rather than fixed breakpoint jumps.

**The Filet Rule.** Grids that present a set of like items (key stats, status tiles, partner logos) use `display: grid; gap: 1px; background: {colors.border-hairline}` with opaque children — the 1px "filet" is the page background showing through the gap, not a drawn border. This reads as more precise than individual card borders and is a signature texture of the system.

## Elevation & Depth

Flat and precise — nothing is "lifted." There are no `box-shadow` declarations anywhere in the shipped CSS. Depth is conveyed entirely through two mechanisms: hairline 1px borders, and a five-step background tier (Void Black → Raised Surface `#0a0a0a` → Card Surface `#111` → Field Surface `#141414` → Locked Field Surface `#1c1c1c`), each step reserved for a specific role rather than used interchangeably. Components feel stamped or printed onto the surface, not floating above it.

### Named Rules
**The No-Lift Rule.** Interactive elements communicate state through color and border-color shifts (`filter: brightness(1.1)` on primary-button hover, border-color → Hazard Yellow on focus/active) — never through `transform`, shadow, or scale. The one scripted exception is the admin detail panel's slide-in `translateX`, which is navigational motion, not a hover/lift effect.

## Shapes

Two coexisting radius patterns, both angular — nothing in the system is fully round (pill/circle):

- **Fixed 2px** (`{rounded.sharp}`) on the system's primary, high-visibility surfaces: buttons, cards, inputs, chips, status badges, checkboxes. This is the one to reach for by default.
- **Relative 0.25em** (`{rounded.control}`) on compact UI chrome sized off its own font — nav tabs, the brand mark, modal close buttons, small pill-style controls across every page (present in 6+ component files: nav bars, modals, auth pages). It scales with the control's text size instead of staying fixed, which is why it's a separate token rather than a rounding error.
- The brand mark specifically lands close to **3px** (`{rounded.mark}`) at its shipped size — the same relative-radius family as `{rounded.control}`, called out separately only because it's the one radius value a reader will eyeball directly next to the logo.

Rule of thumb: if the component is a primary/full-size element (button, card, field), use `{rounded.sharp}`. If it's compact chrome whose size is driven by its own font-size (a tab, a small icon button, a badge-adjacent control), use `{rounded.control}`.

## Components

### Buttons
- **Shape:** 2px radius (`{rounded.sharp}`) throughout.
- **Primary:** Hazard Yellow fill, black text, bold, uppercase, letter-spacing 0.04em, generous padding (1.05rem 2.2rem). `filter: brightness(1.1)` on hover — no color or shadow change, just a brightness lift.
- **Ghost:** transparent fill, `rgba(255,255,255,0.6)` text, 1px `rgba(255,255,255,0.15)` border, sentence case (not uppercase) — reserved for secondary actions like "Se connecter" or "Retour". Hover: border and text shift to Hazard Yellow / white.
- **Dark:** black fill with Hazard Yellow text — used for CTAs sitting on top of a yellow bandeau, where a plain yellow button would disappear.
- **Full/disabled variant:** `rgba(255,255,255,0.06)` fill, muted text, dashed-feeling yellow-tinted border (`rgba(255,237,0,0.25)`) — used for the sold-out registration state.
- **Focus:** 2px solid Hazard Yellow outline, 2px offset, on every variant without exception.

### Chips (radio/toggle replacements)
- **Style:** inactive = transparent fill, `rgba(255,255,255,0.18)` border, `rgba(255,255,255,0.75)` text; active = Hazard Yellow fill, black bold text.
- **Purpose:** chips replace native radio buttons everywhere a user picks one of a few options (category, tenue, version) — the underlying `<input type="radio">` stays in the DOM for accessibility/forms, visually hidden. Minimum 44px touch target, `flex-wrap` group layout.
- **State:** `:focus-within` gets the same 2px Hazard Yellow outline as buttons.

### Cards / Containers
- **Corner Style:** 2px radius, no exception.
- **Background:** Raised Surface (`#0a0a0a`) for form/admin/status cards; Card Surface (`#111`) specifically for landing's imagery-bearing format cards.
- **Shadow Strategy:** none — see Elevation & Depth.
- **Border:** 1px Hairline Border (`rgba(255,255,255,0.1)`) on every card, no exceptions.
- **Internal Padding:** `{spacing.card-padding}` (`clamp(1.25rem, 3vw, 2.25rem)`) for form-step cards; tighter fixed padding (~1rem) for compact tiles like KPI/status bands.

### Inputs / Fields
- **Style:** Field Surface (`#141414`) background, 1px Field Border (`rgba(255,255,255,0.14)`), 2px radius, white text, no placeholder styling tricks.
- **Focus:** border-color shifts to Hazard Yellow, no outline ring, no glow.
- **Error:** border-color shifts to Error (`#ff6b6b`); helper text below in the same red, 0.78rem.
- **Readonly/locked:** Locked Field Surface (`#1c1c1c`), muted text (`rgba(255,255,255,0.45)`), `cursor: not-allowed` — used specifically for the team's login email, which can't be changed post-registration.
- **Label pairing:** label and its required-asterisk sit on the same line via a flex wrapper — never stacked with the asterisk trailing awkwardly.

### Navigation
- **Landing header:** sticky, translucent black (`rgba(0,0,0,0.82)`) with `backdrop-filter: blur(10px)`, bottom hairline. Anchor links hidden below 900px; CTA always visible. `flex-wrap: wrap` is mandatory here for mobile.
- **Admin header:** same visual language (brand mark, tab-style nav buttons, hairline bottom border) but on a solid ground rather than translucent/sticky. **Must also wrap on mobile** — this was shipped without `flex-wrap` initially and clipped the sign-out control off-screen entirely on narrow viewports; treat any un-wrapped flex header as a bug, not a style choice.
- **Active tab:** `rgba(255,255,255,0.05)` fill, `rgba(255,255,255,0.15)` border, white text. Inactive: `rgba(255,255,255,0.4)` text, transparent.

### Status Badge (signature component)
Small, uppercase, letter-spaced (0.08em), 2px-radius pill-adjacent tag at 0.7rem — never a full pill/circle shape, always the shared 2px radius. Built from a tinted 12–15%-opacity background of the status color, a matching ~25–30%-opacity border, and the full-strength status color as text. Used identically for payment state, account state, and player category across admin and participant screens — one visual pattern, reused everywhere status needs to be scannable at a glance.

## Do's and Don'ts

### Do:
- **Do** use `{rounded.sharp}` (2px) for primary/full-size components (buttons, cards, fields) and `{rounded.control}` (0.25em) for compact font-sized chrome (tabs, small pills, modal controls) — never 4px, 8px, or 0 for either case.
- **Do** wrap every flex header (`flex-wrap: wrap`) — this system has no secondary breakpoint bailout, so an un-wrapped header is a broken header on mobile, not a rare edge case.
- **Do** use the filet grid pattern (`gap: 1px` on a hairline-colored background, opaque children) for any new set of like items — stat rows, tile grids, comparison rows.
- **Do** ration Hazard Yellow: one dominant yellow element per screen (a CTA, a hero word, an active state), not several competing for attention.
- **Do** pair a Lemon Milk uppercase label with Cabin body text in every new component; never introduce a third typeface.

### Don't:
- **Don't** add box-shadows, glows, or `transform: scale`/lift-on-hover anywhere — depth is tonal and bordered, not physical, with the single named exception of the admin panel's slide-in translateX.
- **Don't** fill a large surface area with Hazard Yellow outside the two landing CTA bandeaux — it reads as alarm fatigue, not urgency, the moment it stops being rare.
- **Don't** revert to rounded corners (0.5rem or larger) anywhere — that's the explicitly rejected previous identity.
- **Don't** style a new status/category value without the badge pattern (tinted bg + matching border + full-strength text) — a plain colored-text label breaks the established scanning pattern.
- **Don't** introduce a second breakpoint below 860px as a fallback for a layout that should have wrapped or stacked instead — the system's mobile strategy is reflow (`auto-fit`/`minmax`, `flex-wrap`), not additional fixed breakpoints.
