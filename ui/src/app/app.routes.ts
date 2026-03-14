import { Routes } from '@angular/router';
import { LandingComponent } from './pages/landing/landing.component';
import { InscriptionComponent } from './pages/inscription/inscription.component';
import { PaymentSuccessComponent } from './pages/payment-success/payment-success.component';
import { PaymentCancelComponent } from './pages/payment-cancel/payment-cancel.component';
import { LoginComponent } from './pages/login/login.component';
import { TeamsComponent } from './pages/teams/teams.component';
import { PlayersComponent } from './pages/players/players.component';
import { AuthGuard } from './services/auth/auth.guard';
import { NotFoundComponent } from './pages/not-found/not-found.component';

export const routes: Routes = [
    { path: "", component: LandingComponent },
    { path: "inscription", component: InscriptionComponent },
    { path: "payment-success", component: PaymentSuccessComponent },
    { path: "payment-cancel", component: PaymentCancelComponent },
    { path: "login", component: LoginComponent },
    { path: "teams", component: TeamsComponent, canActivate: [AuthGuard] },
    { path: "players", component: PlayersComponent, canActivate: [AuthGuard] },
    { path: '**', component: NotFoundComponent },
];

