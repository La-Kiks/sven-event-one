import { Routes } from '@angular/router';
import { LandingComponent } from './pages/landing/landing.component';
import { InscriptionComponent } from './pages/inscription/inscription.component';
import { InscriptionSuccessComponent } from './pages/inscription-success/inscription-success.component';

export const routes: Routes = [
    { path: "", component: LandingComponent },
    { path: "inscription", component: InscriptionComponent },
    { path: "inscription/success", component: InscriptionSuccessComponent }
];

// { path: "payment-success", component: PaymentSuccessComponent},
// { path: "payment-cancel", component: PaymentCancelComponent}
