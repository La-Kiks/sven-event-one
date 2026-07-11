# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

Sports event registration app ("Sports Reservation" / "sport-challenge"): teams of 2 players sign up, pay via Stripe, and get tracked by an admin. Two parts:

- `backend/SportsReservationAPI` — ASP.NET Core 8 Web API (C#), EF Core + SQL Server, JWT auth, Stripe integration.
- `ui` — Angular 19 standalone-component app.

Both are wired together via the root `docker-compose.yml` plus a SQL Server container. Env vars for local/docker runs live in `.env` (see `.env.sample` for the required keys: ports, DB creds, Stripe keys, Mailgun keys, JWT key/issuer, admin creds, API/frontend base URLs).

## Commands

**This project is only ever run via Docker Compose — in local dev and in prod.** The user does not run `ng`, `npm`, or `dotnet` commands directly; don't suggest them as the way to run/build/test the app, and don't run them yourself (e.g. to "verify a build") without checking first.

```bash
docker compose up --build      # build + run frontend, backend, and SQL Server
```

Builds `ui` (Angular, built inside the image and served via nginx — see `ui/Dockerfile` + `ui/nginx.conf`) and `backend/SportsReservationAPI` (see its `Dockerfile`), plus a `mssql/server:2022-latest` container. Ports/credentials come from `.env` (`UI_PORT`, `API_PORT`, `DB_PORT`, etc. — see `.env.sample`). EF Core migrations are applied automatically on backend startup (see Program.cs), there's no separate migrate step.

There is no test project/suite in this repo currently.

## Backend architecture

- **Program.cs** wires everything: loads `.env` into configuration via `Configuration/EnvLoader.cs` (maps `ENV_VAR` names to config paths like `ApiKeys:Stripe:SecretKey`, `ApiKeys:Mail:*`, `ConnectionStrings:ReservationDatabase:*`, `Jwt:*`), binds `ApiSettings` (`Models/ApiSettings.cs`), configures EF Core SqlServer with retry-on-failure, FluentValidation, JWT bearer auth, a single CORS policy (`FrontendPolicy`) restricted to `FRONTEND_BASE_URL`, and Swagger (dev only). **EF Core migrations are applied automatically on startup** (`dbContext.Database.Migrate()`), not manually per-deploy. Migrations in this repo are hand-written (not generated via `dotnet ef`, since the user doesn't run `dotnet` commands) — when adding one, update the migration `.cs`/`.Designer.cs` pair **and** `Migrations/ReservationContextModelSnapshot.cs` together, following the existing files as a template.
- **Models** are split into per-entity folders under `Models/`: `Team/`, `Player/`, `User/`, each holding the entity plus its DTOs and FluentValidation validators (e.g. `CreateTeamDtoValidator`). `Models/ReservationContext.cs` is the single `DbContext` (`Teams`, `Players`, `Users`) and holds the project's only `OnModelCreating` override (the `User` ↔ `Team` 1:1 relationship — filtered unique index on `TeamId` since SQL Server unique indexes only allow one NULL, `SetNull` on delete so removing a team doesn't fail if it has a linked account). `Models/ApiSettings.cs` is the strongly-typed config surface (Stripe keys, Mail keys, DB settings, base URLs).
- **Roles**: `User.Role` is `"Admin"` (the single seeded admin account) or `"User"` (a participant account, auto-created at team registration — see below). `[Authorize(Roles = "Admin")]` gates all admin-only endpoints (team list/detail/delete/payment, players list); `[Authorize(Roles = "User")]` gates the participant's own-team endpoints. Don't add a bare `[Authorize]` for new endpoints — decide which role it's for.
- **Controllers → Services** pattern: controllers are thin, business logic lives in `Services/*Service.cs`, injected as scoped services.
  - `TeamsController` / `TeamService`: team CRUD. Registration has a hard cap enforced in the controller (`MaxTeams` constant) in addition to any DB constraint — check this constant when changing capacity, it's not config-driven. `create-team` and `count` are public. `my-team` GET/PUT are participant-only (resolved from the JWT's `NameIdentifier` → `User.TeamId`, never trust a team id from the client for this). `create-account` (admin) creates-or-refreshes a participant account for a team missing one — same code path used for the initial registration flow and for backfilling/resending.
  - `AuthController` / `AuthService`: `/login` (BCrypt-verified against `Users`, guards against an empty `PasswordHash` on not-yet-activated accounts) and `/activate` (verifies a `VerificationToken`, sets the password, logs the user in immediately). Token includes `NameIdentifier`, `Name`, `Role` claims, 8h expiry. There is no self-registration endpoint for **admin** accounts — those are seeded out-of-band (see the commented-out `seed` endpoint in `AuthController.cs`); participant accounts self-register implicitly via team creation.
  - `UserService`: owns the participant-account lifecycle — building a pending account (unsaved, for `TeamService` to attach via EF navigation fixup in the same `SaveChanges` as the team), verifying a token + setting the password, and the admin create-or-refresh flow (throws `AccountAlreadyActivatedException` → 409 if already verified, rather than silently resetting a working account).
  - `MailService`: thin Mailgun HTTP client (form-encoded POST + Basic auth, no SDK). No-ops with a logged warning if `MAILGUN_API_KEY`/`MAILGUN_DOMAIN` are blank — unlike the Stripe webhook secret, mail config is **not** validated at startup, since sending an activation email is best-effort and must never block team registration.
  - `StripeController` / `StripeService`: creates a Stripe Checkout session for a team (`create-checkout-session/{teamId}`) and handles the `checkout.session.completed` webhook (`webhook`), which marks the team as paid via `TeamService.MarkTeamAsPaidAsync`. Webhook signature is verified against `STRIPE_WEBHOOK_SECRET`.
- A team requires **exactly two players** (`TeamService.CreateTeamWithPlayersAsync` / `UpdateMyTeamAsync` throw `ValidationException` otherwise); team `Category` is derived from the two players' categories (same category, or `"mixt"` if they differ) rather than being user-supplied — recomputed server-side on both create and participant-edit. Participant 1 (`PlayerDtos[0]`) is always the one whose email becomes the team's login (`User.Username`); when re-deriving "who is participant 1" for an existing team (e.g. the admin create-account flow), use `team.Players.OrderBy(p => p.Id).First()` — player list order from EF isn't otherwise guaranteed stable.

## Frontend architecture

- Angular 19 standalone components (no NgModules), routes declared in `app.routes.ts`. Public pages: landing, inscription (team signup), payment-success/cancel, login, `activer-compte` (email verification + set-password, single step), not-found. Role-gated pages use `AuthGuard` (`services/auth/auth.guard.ts`) with `data: { role: 'Admin' | 'User' }` on the route: `teams`/`players` require `Admin`, `mon-equipe` (participant's own team, view/edit) requires `User`. The guard sends a logged-in user with the wrong role to their own home (`/teams` or `/mon-equipe`) instead of `/login`, to avoid a redirect loop.
- `AuthService` (`services/auth/auth.service.ts`) stores the JWT + user info in **sessionStorage** (not localStorage) and treats login state as "token present and not expired" by decoding the JWT payload client-side — no refresh flow. `login()` and `activate()` (used by the `activer-compte` page) both funnel through the same private `storeSession()`. `login.component.ts`'s post-login redirect branches on `role` (`Admin` → `/teams`, else → `/mon-equipe`) — the login form itself is shared by both roles, there's no separate participant login page.
- `AuthInterceptor` (`services/auth/auth.interceptor.ts`) attaches `Authorization: Bearer <token>` to outgoing requests and force-logs-out on any `401` response.
- Config (`apiUrl`, `stripePublishableKey`) is injected at **container runtime**, not baked in at build time: `ui/public/env.template.js` is copied into the image as-is by the Angular build, then `ui/docker-entrypoint.d/40-generate-runtime-env.sh` runs `envsubst` on it at nginx container startup (using `API_BASE_URL` / `STRIPE_PUBLISHABLE_KEY` from `docker-compose.yml` → `.env`) to produce `env.js`, which `index.html` loads before Angular bootstraps and which sets `window.__env`. App code imports `environment` from `app/core/runtime-env.ts`, which reads `window.__env` and falls back to the dev defaults in `src/environment.ts` if a key is missing (e.g. no `env.js`, or a var wasn't set). Changing the API URL or Stripe key for a given environment (local/prod) means changing `.env`, not rebuilding the image or editing `environment.ts`.
- UI building blocks live under `components/ui/*` (button, card, modal, error, success, inscription-form); page-level components live under `pages/*`.
