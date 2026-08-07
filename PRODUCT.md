# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Primary users are police and law-enforcement personnel (and affiliated guests) who register in duos for the "Hyrox Police 54" fitness competition, plus event organizers/admins who track registrations, payments, and player rosters. Registration is restricted to police/law-enforcement teams, not the general public.

## Product Purpose

A registration and payment platform for "Hyrox Police 54," a Hyrox-style fitness competition (run + functional workout stations) held annually in September. Duos sign up, pay a 60€ team fee via Stripe, receive account activation by email, and can view/edit their team details until the event. Organizers track teams, players, and payment status from an admin view.

## Positioning

A police-organized charity fitness event: proceeds/participation support Orphéopolis, a charity for police officers' orphans, distinguishing it from a purely competitive or commercial Hyrox event. The charity/community angle should inform messaging and CTA framing, not just "sign up for a race."

## Operating Context

- Duo team registration (2 players per team, roles/categories captured per player) with Stripe checkout for the 60€ entry fee.
- Post-registration account activation via emailed link (participant sets password, becomes team's login).
- Participant self-service: view/edit own team (`mon-equipe`) until event day.
- Admin (organizer) view: list/manage teams and players, mark/confirm payment, resend or create activation accounts.
- Event details participants and organizers coordinate around: date (September), venue (Well & Fit, 113 Bd Emile Zola, 54520 Laxou), organizer contact (Sven Barberat).

## Capabilities and Constraints

- A team requires exactly two players; team category is derived from the two players' categories (or "mixt" if they differ), not user-entered.
- Registration has a hard capacity cap (`MaxTeams` in code) — sold-out state must be represented (landing page already reflects `isRegistrationFull`).
- Roles: `Admin` (single seeded organizer account) and `User` (participant, auto-created at team registration). No public self-registration for organizers.
- Payment is Stripe Checkout; team is marked paid via webhook, not client-confirmed.
- Terminology: "team"/"équipe" (duo), "player"/"joueur", "category" (per-player, e.g. skill/division), "outfit" (apparel size/choice), "volunteer" opt-in, mailing-list "subscribe" opt-in — all present as existing form fields.

## Brand Commitments

- Event name: "Hyrox Police 54" (French copy: "Rendez-vous en Septembre").
- Venue: Well & Fit, 113 Bd Emile Zola, 54520 Laxou.
- Entry fee: 60€ per duo team.
- Existing sponsor roster (logos in `ui/public/images/sponso-*`): BFM, Cops13, CrossFit Laxou, Fitness Park, Fitnrack, Intersport, MGP, Orphéopolis, RW, Well & Fit, FSPN, Police Nationale.
- Organizer contact: Sven Barberat (phone 06 48 73 50 15, email svenbarberat@orange.fr).

## Evidence on Hand

- Promo video embedded on the landing page (YouTube).
- Competition format copy: 1km run before each of 8 stations (Ski Erg, Sled Push, Sled Pull, Burpees Broad Jump, Rowing, Farmers Carry, Fentes/Lunges, Wall Balls), with station photos in `ui/public/images`.
- Sponsor logo assets in `ui/public/images/sponso-*`.
- Google Maps embed for venue location.
- Existing typefaces already installed in `ui/public/fonts`: Lemon Milk (display) and Cabin (text family, multiple weights). No other visual direction (palette, layout system) is confirmed as binding — that belongs to DESIGN.md, not here.

## Product Principles

1. Registration must stay fast and low-friction for a duo (two people, one form, one payment) — this is the primary conversion path.
2. Trust and legitimacy matter: police organization + named charity beneficiary (Orphéopolis) + visible sponsor roster should read as credible, not amateur.
3. Payment and account state must never be ambiguous to the participant (paid vs. unpaid, activated vs. not) or the organizer (who's registered, who's paid).
4. Admin/organizer tooling is a working tool for a small team, not a showcase — clarity and speed over polish there.
5. The event is time-boxed (single September date) and capacity-capped — the product must clearly communicate sold-out/closed state rather than silently failing.
