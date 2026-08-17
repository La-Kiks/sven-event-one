import { Routes } from '@angular/router';
import { LandingComponent } from './pages/landing/landing.component';
import { InscriptionComponent } from './pages/inscription/inscription.component';
import { LegalComponent } from './pages/legal/legal.component';
import { PaymentSuccessComponent } from './pages/payment-success/payment-success.component';
import { PaymentCancelComponent } from './pages/payment-cancel/payment-cancel.component';
import { LoginComponent } from './pages/login/login.component';
import { TeamsComponent } from './pages/teams/teams.component';
import { PlayersComponent } from './pages/players/players.component';
import { AuthGuard } from './services/auth/auth.guard';
import { NotFoundComponent } from './pages/not-found/not-found.component';
import { ActivateAccountComponent } from './pages/activate-account/activate-account.component';
import { MyTeamComponent } from './pages/my-team/my-team.component';
import { ForgotPasswordComponent } from './pages/forgot-password/forgot-password.component';

export const routes: Routes = [
    { path: "", component: LandingComponent },
    { path: "inscription", component: InscriptionComponent },
    { path: "politique-de-confidentialite", component: LegalComponent },
    { path: "payment-success", component: PaymentSuccessComponent },
    { path: "payment-cancel", component: PaymentCancelComponent },
    { path: "login", component: LoginComponent },
    { path: "mot-de-passe-oublie", component: ForgotPasswordComponent },
    { path: "activer-compte", component: ActivateAccountComponent },
    { path: "mon-equipe", component: MyTeamComponent, canActivate: [AuthGuard], data: { role: 'User' } },
    { path: "teams", component: TeamsComponent, canActivate: [AuthGuard], data: { role: 'Admin' } },
    { path: "players", component: PlayersComponent, canActivate: [AuthGuard], data: { role: 'Admin' } },
    { path: '**', component: NotFoundComponent },
];
