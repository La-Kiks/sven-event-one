# Manual testing guide

Walks through the full participant lifecycle against the running dev stack (`docker compose up --build`). Assumes `.env` has `ENVIRONMENT=Development` and `ADMIN_USERNAME`/`ADMIN_PASSWORD` set (the admin account is auto-seeded on startup — see `CLAUDE.md`).

## 1. Register a team

1. Open `http://localhost:<UI_PORT>/inscription` and fill in the 3-step form for two players.
2. On step 3, confirm there are two separate consent checkboxes: "J'ai pris connaissance de la politique de confidentialité" (required — the form won't advance without it; its link opens `/politique-de-confidentialite` in a new tab) and "Je souhaite recevoir des emails pour les prochaines éditions" (optional, unchecked by default). Leave the optional one unchecked.
3. Submit. Expect a success modal mentioning an activation email.

## 2. Check the privacy policy page and the consent split

1. From `/`, scroll to the footer and click "Politique de confidentialité". Expect to land on `/politique-de-confidentialite` with both the "Mentions légales" and "Politique de confidentialité" sections, showing the correct contact details (Sven Barberat, svenbarberat@orange.fr, 06 48 73 50 15).
2. Query the database and confirm `AcceptMails` is `0`/`false` for both players of the team registered in step 1 (left the optional box unchecked):

```bash
MSYS_NO_PATHCONV=1 docker exec sports-reservation-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P '<your DB_PASSWORD from .env>' -d SportsReservationDB -C \
  -Q "SELECT FirstName, LastName, AcceptMails FROM Players ORDER BY Id DESC"
```

3. Register a second team, this time checking "Je souhaite recevoir des emails pour les prochaines éditions". Re-run the query above and confirm `AcceptMails` is `1`/`true` for both of that team's players.

## 3. Find the activation link

If `MAILGUN_API_KEY`/`MAILGUN_DOMAIN` are set in `.env` and the participant's email is an authorized recipient on your Mailgun sandbox, check that inbox directly and skip to step 4.

Otherwise, fetch the token from the database:

```bash
MSYS_NO_PATHCONV=1 docker exec sports-reservation-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P '<your DB_PASSWORD from .env>' -d SportsReservationDB -C \
  -Q "SELECT Username, VerificationToken FROM Users WHERE VerificationToken IS NOT NULL"
```

Build the URL yourself: `http://localhost:<UI_PORT>/activer-compte?token=<VerificationToken>`.

## 4. Activate

1. Open the URL from step 3.
2. Set a password (8+ characters) and submit.
3. Expect a redirect to `/mon-equipe` showing the team you just registered.

## 5. Reset a forgotten password

1. Log out, go to `/login`, click "Mot de passe oublié ?".
2. Submit the email from step 1. Expect the same generic confirmation message every time, whether or not the email exists.
3. Fetch the new `VerificationToken` from the DB (same query as step 3), open `/activer-compte?token=<token>`, set a new password.
4. Log in with the new password. Confirm the old password no longer works.

## 6. Edit your team

1. Change a field (e.g. team name, a player's category).
2. Save. Expect a success message and the change to persist on refresh.
3. Confirm the payment status badge is present but not clickable (participants can't toggle it).

## 7. Check the admin view

1. Log out (top bar).
2. Log in at `/login` with your `ADMIN_USERNAME`/`ADMIN_PASSWORD`.
3. Expect a redirect to `/teams` (not `/mon-equipe`).
4. Find the team from step 1, open its detail panel, confirm your step 6 edit is reflected and the account shows as "Activé".
5. Toggle the payment badge, confirm it updates.

## 8. Confirm role gating

1. While still logged in as admin, navigate directly to `/mon-equipe`. Expect a redirect back to `/teams` (not `/login` — wrong role, not logged out).
2. Log out, log back in with the participant credentials from step 4.
3. Navigate directly to `/teams`. Expect a redirect back to `/mon-equipe`.
