# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

Sports event registration app ("Sports Reservation" / "sport-challenge"): teams of 2 players sign up, pay via Stripe, and get tracked by an admin. Two parts:

- `backend/SportsReservationAPI` — ASP.NET Core 8 Web API (C#), EF Core + SQL Server, JWT auth, Stripe integration.
- `ui` — Angular 19 standalone-component app.

Both are wired together via the root `docker-compose.yml` plus a SQL Server container. Env vars for local/docker runs live in `.env` (see `.env.sample` for the required keys: ports, DB creds, Stripe keys, JWT key/issuer, admin creds, API/frontend base URLs).

## Commands

### Backend (`backend/SportsReservationAPI`)

Run all commands from that directory.

```bash
dotnet restore
dotnet build
dotnet run                          # runs with appsettings + .env (DotNetEnv) loaded via Configuration/EnvLoader.cs
dotnet ef migrations add <Name>     # add a migration (Migrations/ folder)
dotnet ef database update           # apply migrations manually (also auto-applied on app startup, see Program.cs)
```

There is no test project in this repo currently.

### Frontend (`ui`)

Run all commands from that directory.

```bash
npm install
npm start        # ng serve, http://localhost:4200
npm run build    # ng build -> dist/
npm run watch    # ng build --watch --configuration development
npm test         # ng test (Karma/Jasmine)
```

To run a single spec, use Angular CLI's Karma filtering, e.g. `ng test --include='**/inscription-form.component.spec.ts'`.

### Full stack via Docker

```bash
docker compose up --build
```

Builds `ui` (served via nginx, see `ui/Dockerfile` + `ui/nginx.conf`) and `backend/SportsReservationAPI` (see its `Dockerfile`), plus a `mssql/server:2022-latest` container. Ports/credentials come from `.env` (`UI_PORT`, `API_PORT`, `DB_PORT`, etc.).

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
- `environment.ts` / `environment.prod.ts` hold `apiUrl` and `stripePublishableKey` — both currently point at the same production API host and both are marked `production: true`; there is no dev-specific environment file, so local frontend dev talks to the deployed backend unless you edit `environment.ts` directly.
- UI building blocks live under `components/ui/*` (button, card, modal, error, success, inscription-form); page-level components live under `pages/*`.
