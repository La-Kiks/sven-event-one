# Sport Challenge Police 54

Registration site for a Hyrox-style team sports competition. Teams of 2 sign up, pay via Stripe, and get tracked by an event organizer through an admin panel.

## Tech stack

- **Backend** — ASP.NET Core 8 (C#), Entity Framework Core + SQL Server, JWT auth, Stripe, Mailgun
- **Frontend** — Angular 19 (standalone components), served via nginx
- **Orchestration** — Docker Compose (frontend, backend, and a SQL Server container)

## Getting started

This project is only ever run via Docker Compose — there's no need to install .NET, Node, or Angular CLI locally.

### Prerequisites

- Docker + Docker Compose

### Setup

1. Copy `.env.sample` to `.env` and fill in real values (see below).
2. Build and run everything:
   ```bash
   docker compose up --build
   ```
3. The frontend is served at `http://localhost:<UI_PORT>` and the API at `http://localhost:<API_PORT>` (ports come from your `.env`).

Database migrations are applied automatically on backend startup — no separate migrate step. In development (`ENVIRONMENT=Development`), an admin account is also seeded automatically from `ADMIN_USERNAME`/`ADMIN_PASSWORD` if it doesn't already exist.

### Environment variables

All configuration lives in `.env` (gitignored — never commit real secrets). See `.env.sample` for the full list with placeholder values:

| Group | Variables | Notes |
|---|---|---|
| Ports | `API_PORT`, `UI_PORT`, `DB_PORT` | Host ports for each container |
| Database | `RESERVATION_DB_SERVER`, `RESERVATION_DB_NAME`, `DB_USER`, `DB_PASSWORD` | SQL Server connection |
| Stripe | `STRIPE_SECRET_KEY`, `STRIPE_PUBLISHABLE_KEY`, `STRIPE_WEBHOOK_SECRET`, `STRIPE_PRODUCT_PRICE_DUO` | Payment processing |
| Mailgun | `MAILGUN_API_KEY`, `MAILGUN_DOMAIN`, `MAILGUN_BASE_URL`, `MAIL_FROM_ADDRESS`, `MAIL_FROM_NAME` | Account activation emails — leave the key/domain blank locally to disable sending without breaking registration |
| URLs | `API_BASE_URL`, `FRONTEND_BASE_URL` | Used for CORS and building links (e.g. activation emails) |
| JWT | `JWT_KEY`, `JWT_ISSUER` | Auth token signing |
| Admin | `ADMIN_USERNAME`, `ADMIN_PASSWORD` | Auto-seeded in dev only; production admin is seeded out of band |

## Features

- **Public registration** — a 3-step form to sign up a 2-player team, followed by Stripe Checkout.
- **Participant self-service** — team registration auto-creates an account for participant 1; they receive an activation email (Mailgun) to verify their address and set a password in one step, then can log in to view/edit their team and check payment status at `/mon-equipe`.
- **Admin panel** — `/teams` (list, detail, delete, toggle payment status, create/resend a participant account) and `/players` (cross-team player list), both behind admin-only auth.

## Project structure

```
backend/SportsReservationAPI/   ASP.NET Core API (Controllers → Services → EF Core)
ui/                              Angular 19 frontend
docker-compose.yml               Orchestrates frontend + backend + SQL Server
.env.sample                      Template for required environment variables
```

## Development notes

See [`CLAUDE.md`](CLAUDE.md) for a deeper architecture walkthrough (auth/role model, EF Core migration conventions, frontend runtime config injection, etc.). There is no automated test suite in this repo — changes are verified by rebuilding via `docker compose up --build` and exercising the running app.
