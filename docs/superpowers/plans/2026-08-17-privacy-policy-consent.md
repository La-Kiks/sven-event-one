# Politique de confidentialité & consentement RGPD — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a public privacy-policy/mentions-légales page and fix the inscription form's consent checkbox so that opting into future-event marketing emails (`AcceptMails`) is a genuine, separate, opt-in choice instead of being forced true by the mandatory registration checkbox.

**Architecture:** Purely frontend (Angular 19 standalone components), no backend changes. A new static `LegalComponent` page is added at route `/politique-de-confidentialite`, linked from the landing page footer. The inscription form's single "subscribe" checkbox is split into two: the existing required one (now linking to the new page, no longer driving `acceptMails`) and a new optional, unchecked-by-default one that alone maps to `acceptMails` for both players.

**Tech Stack:** Angular 19 standalone components, reactive forms (`ReactiveFormsModule`), SCSS with the existing per-component `@use "variables" as *` + local `$var` pattern (no shared design-token file beyond `ui/src/styles/_variables.scss`).

## Global Constraints

- Data controller shown on the page: Sven Barberat (individual organizer, no legal entity), contact `svenbarberat@orange.fr`, phone `06 48 73 50 15`, hosting in the EU.
- Retention period shown on the page: 1 year after the event, then deleted unless the team re-registers.
- Single combined page (`/politique-de-confidentialite`, with `#mentions-legales` and `#confidentialite` anchors) — no separate `/mentions-legales` route.
- No backend/API changes — `AcceptMails` stays a plain `boolean` on `CreatePlayerDto`/`Player`; only which frontend control feeds it changes.
- The "politique de confidentialité" checkbox stays mandatory (`Validators.requiredTrue` on `subscribe`, same control name, same validation behavior). The new "emails prochaines éditions" checkbox is optional and unchecked by default.
- This project has no automated frontend test suite — deliverables are verified manually against `docker compose up --build`, per the pattern already used for other frontend-only tasks in this repo (see `docs/superpowers/plans/2026-07-13-forgot-password.md`, Task 4).

---

### Task 1: Privacy policy page, route, and footer link

**Files:**
- Create: `ui/src/app/pages/legal/legal.component.ts`
- Create: `ui/src/app/pages/legal/legal.component.html`
- Create: `ui/src/app/pages/legal/legal.component.scss`
- Modify: `ui/src/app/app.routes.ts`
- Modify: `ui/src/app/pages/landing/landing.component.ts`
- Modify: `ui/src/app/pages/landing/landing.component.html:157-160`
- Modify: `ui/src/app/pages/landing/landing.component.scss:504-518`

**Interfaces:**
- Consumes: nothing new.
- Produces (consumed by Task 2): a route at path `politique-de-confidentialite` that Task 2 links to from the inscription form.

- [ ] **Step 1: Create the `LegalComponent`**

Create `ui/src/app/pages/legal/legal.component.ts`:

```typescript
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-legal',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './legal.component.html',
  styleUrl: './legal.component.scss'
})
export class LegalComponent { }
```

Create `ui/src/app/pages/legal/legal.component.html`:

```html
<div class="legal">
    <div class="legal__container">
        <a routerLink="/" class="legal__brand" aria-label="Retour à l'accueil">
            <span class="legal__mark">54</span> Hyrox Police 54
        </a>

        <h1>Mentions légales &amp; politique de confidentialité</h1>

        <section id="mentions-legales">
            <h2>Mentions légales</h2>
            <p>Ce site est édité à titre individuel par <strong>Sven Barberat</strong>, organisateur de
                l'événement Hyrox Police 54, sans structure juridique dédiée.</p>
            <p>Contact : <a href="mailto:svenbarberat@orange.fr">svenbarberat@orange.fr</a> · 06 48 73 50 15</p>
            <p>Hébergement : les données sont hébergées au sein de l'Union Européenne.</p>
        </section>

        <section id="confidentialite">
            <h2>Politique de confidentialité</h2>

            <h3>Données collectées</h3>
            <p>Lors de l'inscription d'une équipe, nous collectons pour chacun des deux participants : nom,
                prénom, email, téléphone, catégorie, taille de tenue, ainsi que le nom de l'équipe et
                l'administration d'appartenance. Vous pouvez également indiquer si vous souhaitez être
                bénévole et si vous acceptez de recevoir des emails pour les prochaines éditions.</p>

            <h3>Finalités et base légale</h3>
            <p>Les données d'identité et de contact sont utilisées pour organiser l'événement (inscription,
                répartition par catégorie, communication liée à votre participation) : leur traitement est
                nécessaire à l'exécution de votre inscription. L'envoi d'emails sur les prochaines éditions
                repose sur votre consentement explicite et facultatif, que vous pouvez retirer à tout moment
                en nous contactant.</p>

            <h3>Destinataires</h3>
            <p><strong>Mailgun</strong> nous permet d'envoyer les emails transactionnels (activation de
                compte). <strong>Yurplan</strong>, notre billetterie partenaire, gère le paiement de
                l'inscription directement sur son propre site : nous ne lui transmettons aucune de vos
                données, le lien de paiement utilisé est un lien générique. Les informations que vous
                saisissez sur Yurplan sont régies par sa propre politique de confidentialité.</p>

            <h3>Durée de conservation</h3>
            <p>Vos données sont conservées 1 an après l'événement, puis supprimées, sauf si votre équipe se
                réinscrit à une édition suivante.</p>

            <h3>Vos droits</h3>
            <p>Conformément au RGPD, vous disposez d'un droit d'accès, de rectification, d'effacement, de
                portabilité et d'opposition sur vos données. Pour l'exercer, contactez-nous à
                <a href="mailto:svenbarberat@orange.fr">svenbarberat@orange.fr</a>. Vous disposez également
                du droit d'introduire une réclamation auprès de la CNIL
                (<a href="https://www.cnil.fr" target="_blank" rel="noopener">cnil.fr</a>).</p>
        </section>
    </div>
</div>
```

Create `ui/src/app/pages/legal/legal.component.scss`:

```scss
@use "variables" as *;

$text-secondary: rgba(255, 255, 255, 0.75);
$border: rgba(255, 255, 255, 0.1);
$gutter: clamp(1rem, 4vw, 3rem);

:host {
  display: block;
  min-height: 100vh;
  background: $background-color;
}

.legal {
  min-height: 100vh;
  padding: 3rem $gutter 4rem;
  font-family: $font-body;
  color: $text-secondary;
  box-sizing: border-box;
}

.legal__container {
  max-width: 720px;
  margin: 0 auto;
}

.legal__brand {
  display: inline-flex;
  align-items: center;
  gap: 0.6rem;
  margin-bottom: 2rem;
  color: #ffffff;
  font-family: $font-title;
  font-size: 0.95rem;
  text-decoration: none;

  &:focus-visible {
    outline: 2px solid $main-color;
    outline-offset: 3px;
  }
}

.legal__mark {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  background: $main-color;
  color: #000;
  border-radius: 0.25em;
  font-size: 0.85rem;
}

h1 {
  font-family: $font-title;
  font-size: clamp(1.6rem, 4vw, 2.2rem);
  color: #ffffff;
  margin: 0 0 2.5rem;
}

h2 {
  font-family: $font-title;
  font-size: 1.3rem;
  color: #ffffff;
  margin: 0 0 1rem;
  padding-top: 2rem;
  border-top: 1px solid $border;
}

#mentions-legales h2 {
  padding-top: 0;
  border-top: none;
}

h3 {
  font-family: $font-body;
  font-weight: 600;
  font-size: 1rem;
  color: #ffffff;
  margin: 1.5rem 0 0.5rem;
}

p {
  font-size: 0.92rem;
  line-height: 1.6;
  margin: 0 0 1rem;
}

a {
  color: $main-color;

  &:focus-visible {
    outline: 2px solid $main-color;
    outline-offset: 2px;
  }
}
```

- [ ] **Step 2: Add the route**

In `ui/src/app/app.routes.ts`, add the import:

```typescript
import { LegalComponent } from './pages/legal/legal.component';
```

and add this route entry (after `"inscription"`, before `"payment-success"`):

```typescript
    { path: "politique-de-confidentialite", component: LegalComponent },
```

- [ ] **Step 3: Link the page from the landing footer**

In `ui/src/app/pages/landing/landing.component.ts`, add `RouterLink` to the imports:

```typescript
import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ButtonComponent } from '../../components/ui/button/button.component';
import { CardComponent } from '../../components/ui/card/card.component';
import { TeamCount, TeamCountService } from '../../services/team-count.service'; // ← adjust path if needed

@Component({
  selector: 'app-landing',
  imports: [NgIf, RouterLink, ButtonComponent, CardComponent],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss'
})
```

In `ui/src/app/pages/landing/landing.component.html:157-160`, replace:

```html
<footer class="footer">
    <span>Hyrox Police 54 — organisé au profit d'Orphéopolis</span>
    <span>Well &amp; Fit, Laxou · 06 48 73 50 15</span>
</footer>
```

with:

```html
<footer class="footer">
    <span>Hyrox Police 54 — organisé au profit d'Orphéopolis</span>
    <span>Well &amp; Fit, Laxou · 06 48 73 50 15</span>
    <a routerLink="/politique-de-confidentialite">Politique de confidentialité</a>
</footer>
```

In `ui/src/app/pages/landing/landing.component.scss`, add after the `.footer` rule (around line 518):

```scss

.footer a {
  color: $text-tertiary;
  text-decoration: underline;

  &:hover {
    color: $main-color;
  }

  &:focus-visible {
    outline: 2px solid $main-color;
    outline-offset: 2px;
  }
}
```

- [ ] **Step 4: Manual verification**

Run: `docker compose up --build`

1. Open `http://localhost:<UI_PORT>/` and confirm the footer now shows a "Politique de confidentialité" link.
2. Click it → arrive on `/politique-de-confidentialite`. Confirm both the "Mentions légales" section (Sven Barberat, `svenbarberat@orange.fr`, 06 48 73 50 15, hosting in the EU) and the "Politique de confidentialité" section (data collected, purposes, Mailgun/Yurplan recipients, 1-year retention, rights + CNIL) render correctly.
3. Click the "Retour à l'accueil" brand link at the top of the page → back to `/`.
4. Resize the browser to a narrow mobile width and confirm the text stays readable (no horizontal overflow).

- [ ] **Step 5: Commit**

```bash
git add ui/src/app/pages/legal ui/src/app/app.routes.ts ui/src/app/pages/landing/landing.component.ts ui/src/app/pages/landing/landing.component.html ui/src/app/pages/landing/landing.component.scss
git commit -m "feat: add privacy policy / mentions légales page"
```

---

### Task 2: Split the inscription form's consent checkbox

**Files:**
- Modify: `ui/src/app/components/ui/inscription-form/inscription-form.component.ts`
- Modify: `ui/src/app/components/ui/inscription-form/inscription-form.component.html:332-341`
- Modify: `docs/manual-testing-guide.md`

**Interfaces:**
- Consumes: route `politique-de-confidentialite` (Task 1) for the checkbox's link.
- Produces: nothing consumed by a later task (this is the last task).

- [ ] **Step 1: Add the new form control and fix the `acceptMails` mapping**

In `ui/src/app/components/ui/inscription-form/inscription-form.component.ts`, add `RouterLink` to the imports:

```typescript
import { Component, OnDestroy, OnInit } from '@angular/core';
import { Validators, ReactiveFormsModule, FormGroup, FormControl } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { ModalComponent, StatusType } from '../modal/modal.component';
import { CreateTeamWithPlayersRequest } from '../../../models/create-team-request';
import { TeamService } from '../../../services/team.service';

const FORM_STORAGE_KEY = 'inscription-form-draft';

@Component({
  selector: 'app-inscription-form',
  standalone: true,
  imports: [ReactiveFormsModule, ModalComponent, RouterLink],
  templateUrl: './inscription-form.component.html',
  styleUrls: ['./inscription-form.component.scss']
})
```

Change the `step3` form group:

```typescript
    step3: new FormGroup({
      version: new FormControl('', Validators.required),
      administration: new FormControl('', Validators.required),
      team_name: new FormControl('', Validators.required),
      subscribe: new FormControl(false, Validators.requiredTrue),
      acceptFutureEmails: new FormControl(false),
    }),
```

Update the required-message text (still describes the `subscribe` control, now specifically the privacy-policy acknowledgment):

```typescript
    'step3.subscribe': "Merci d'accepter la politique de confidentialité pour continuer.",
```

In `submit()`, change both occurrences of `acceptMails: !!step3.subscribe` to read from the new control instead:

```typescript
          volunteer: !!step1.volounteer_a,
          acceptMails: !!step3.acceptFutureEmails,
        },
        {
          firstName: step2.firstname_b!,
          lastName: step2.name_b!,
          email: step2.email_b!,
          phoneNumber: step2.phone_b!,
          category: step2.category_b!,
          outfit: step2.outfit_b!,
          volunteer: !!step2.volounteer_b,
          acceptMails: !!step3.acceptFutureEmails
```

- [ ] **Step 2: Split the checkbox in the template**

In `ui/src/app/components/ui/inscription-form/inscription-form.component.html:332-341`, replace:

```html
                    <label class="checkbox-row" [class.checkbox-row--checked]="!!form.get('step3.subscribe')?.value">
                        <input type="checkbox" name="subscribe" value="yes" formControlName="subscribe" required
                            [attr.aria-invalid]="errorMessage('step3.subscribe') ? true : null"
                            [attr.aria-describedby]="errorMessage('step3.subscribe') ? 'subscribe-error' : null">
                        <span>Les informations de contact présentes dans ce formulaire ne seront utilisées que dans
                            le cadre de cet événement ou événement similaire. <span class="required">*</span></span>
                    </label>
                    @if (errorMessage('step3.subscribe'); as msg) {
                    <p class="field-error" id="subscribe-error">{{ msg }}</p>
                    }
```

with:

```html
                    <label class="checkbox-row" [class.checkbox-row--checked]="!!form.get('step3.subscribe')?.value">
                        <input type="checkbox" name="subscribe" value="yes" formControlName="subscribe" required
                            [attr.aria-invalid]="errorMessage('step3.subscribe') ? true : null"
                            [attr.aria-describedby]="errorMessage('step3.subscribe') ? 'subscribe-error' : null">
                        <span>J'ai pris connaissance de la
                            <a routerLink="/politique-de-confidentialite" target="_blank">politique de
                                confidentialité</a>. <span class="required">*</span></span>
                    </label>
                    @if (errorMessage('step3.subscribe'); as msg) {
                    <p class="field-error" id="subscribe-error">{{ msg }}</p>
                    }

                    <label class="checkbox-row"
                        [class.checkbox-row--checked]="!!form.get('step3.acceptFutureEmails')?.value">
                        <input type="checkbox" name="acceptFutureEmails" value="yes"
                            formControlName="acceptFutureEmails">
                        <span>Je souhaite recevoir des emails pour les prochaines éditions.</span>
                    </label>
```

- [ ] **Step 3: Update the manual testing guide**

Replace the full contents of `docs/manual-testing-guide.md`:

```markdown
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
```

- [ ] **Step 4: Manual verification**

Run: `docker compose up --build` (skip the rebuild if Task 1's stack is still running).

1. Go through `docs/manual-testing-guide.md` sections 1 and 2 end to end, confirming both checkbox behaviors and the `AcceptMails` values in the database as described.
2. Confirm the required checkbox still blocks step 3 → step 4 navigation when left unchecked (existing `Validators.requiredTrue` behavior, unchanged).
3. Confirm clicking the "politique de confidentialité" link inside the label opens the page in a new tab without toggling the checkbox.

- [ ] **Step 5: Commit**

```bash
git add ui/src/app/components/ui/inscription-form/inscription-form.component.ts ui/src/app/components/ui/inscription-form/inscription-form.component.html docs/manual-testing-guide.md
git commit -m "fix: make the future-editions email opt-in genuinely separate from the required consent checkbox"
```
