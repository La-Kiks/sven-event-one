# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

Sports event registration app ("Sports Reservation" / "sport-challenge"): teams of 2 players sign up, pay via Stripe, and get tracked by an admin. Two parts:

- `backend/SportsReservationAPI` — ASP.NET Core 8 Web API (C#), EF Core + SQL Server, JWT auth, Stripe integration.
- `ui` — Angular 19 standalone-component app.

Both are wired together via the root `docker-compose.yml` plus a SQL Server container. Env vars for local/docker runs live in `.env` (see `.env.sample` for the required keys: ports, DB creds, Stripe keys, JWT key/issuer, admin creds, API/frontend base URLs).

## Commands

**This project is only ever run via Docker Compose — in local dev and in prod.** The user does not run `ng`, `npm`, or `dotnet` commands directly; don't suggest them as the way to run/build/test the app, and don't run them yourself (e.g. to "verify a build") without checking first.

```bash
docker compose up --build      # build + run frontend, backend, and SQL Server
```

Builds `ui` (Angular, built inside the image and served via nginx — see `ui/Dockerfile` + `ui/nginx.conf`) and `backend/SportsReservationAPI` (see its `Dockerfile`), plus a `mssql/server:2022-latest` container. Ports/credentials come from `.env` (`UI_PORT`, `API_PORT`, `DB_PORT`, etc. — see `.env.sample`). EF Core migrations are applied automatically on backend startup (see Program.cs), there's no separate migrate step.

There is no test project/suite in this repo currently.

## Backend architecture

- **Program.cs** wires everything: loads `.env` into configuration via `Configuration/EnvLoader.cs` (maps `ENV_VAR` names to config paths like `ApiKeys:Stripe:SecretKey`, `ConnectionStrings:ReservationDatabase:*`, `Jwt:*`), binds `ApiSettings` (`Models/ApiSettings.cs`), configures EF Core SqlServer with retry-on-failure, FluentValidation, JWT bearer auth, a single CORS policy (`FrontendPolicy`) restricted to `FRONTEND_BASE_URL`, and Swagger (dev only). **EF Core migrations are applied automatically on startup** (`dbContext.Database.Migrate()`), not manually per-deploy.
- **Models** are split into per-entity folders under `Models/`: `Team/`, `Player/`, `User/`, each holding the entity plus its DTOs and FluentValidation validators (e.g. `CreateTeamDtoValidator`). `Models/ReservationContext.cs` is the single `DbContext` (`Teams`, `Players`, `Users`). `Models/ApiSettings.cs` is the strongly-typed config surface (Stripe keys, DB settings, base URLs).
- **Controllers → Services** pattern: controllers are thin, business logic lives in `Services/*Service.cs`, injected as scoped services.
  - `TeamsController` / `TeamService`: team CRUD. Registration has a hard cap enforced in the controller (`MaxTeams` constant) in addition to any DB constraint — check this constant when changing capacity, it's not config-driven. `create-team` and `count` are public; get/list/delete/payment-status endpoints are `[Authorize]`-protected (admin JWT).
  - `AuthController` / `AuthService`: login only, issues JWT via BCrypt-verified credentials against the `Users` table. Token includes `NameIdentifier`, `Name`, `Role` claims, 8h expiry. There is no self-registration endpoint — admin users are seeded out-of-band (see the commented-out `seed` endpoint in `AuthController.cs`).
  - `StripeController` / `StripeService`: creates a Stripe Checkout session for a team (`create-checkout-session/{teamId}`) and handles the `checkout.session.completed` webhook (`webhook`), which marks the team as paid via `TeamService.MarkTeamAsPaidAsync`. Webhook signature is verified against `STRIPE_WEBHOOK_SECRET`.
- A team requires **exactly two players** (`TeamService.CreateTeamWithPlayersAsync` throws `ValidationException` otherwise); team `Category` is derived from the two players' categories (same category, or `"mixt"` if they differ) rather than being user-supplied.

## Frontend architecture

- Angular 19 standalone components (no NgModules), routes declared in `app.routes.ts`. Public pages: landing, inscription (team signup), payment-success/cancel, login, not-found. Admin-only pages (`teams`, `players`) are gated by `AuthGuard` (`services/auth/auth.guard.ts`).
- `AuthService` (`services/auth/auth.service.ts`) stores the JWT + user info in **sessionStorage** (not localStorage) and treats login state as "token present and not expired" by decoding the JWT payload client-side — no refresh flow.
- `AuthInterceptor` (`services/auth/auth.interceptor.ts`) attaches `Authorization: Bearer <token>` to outgoing requests and force-logs-out on any `401` response.
- Config (`apiUrl`, `stripePublishableKey`) is injected at **container runtime**, not baked in at build time: `ui/public/env.template.js` is copied into the image as-is by the Angular build, then `ui/docker-entrypoint.d/40-generate-runtime-env.sh` runs `envsubst` on it at nginx container startup (using `API_BASE_URL` / `STRIPE_PUBLISHABLE_KEY` from `docker-compose.yml` → `.env`) to produce `env.js`, which `index.html` loads before Angular bootstraps and which sets `window.__env`. App code imports `environment` from `app/core/runtime-env.ts`, which reads `window.__env` and falls back to the dev defaults in `src/environment.ts` if a key is missing (e.g. no `env.js`, or a var wasn't set). Changing the API URL or Stripe key for a given environment (local/prod) means changing `.env`, not rebuilding the image or editing `environment.ts`.
- UI building blocks live under `components/ui/*` (button, card, modal, error, success, inscription-form); page-level components live under `pages/*`.
