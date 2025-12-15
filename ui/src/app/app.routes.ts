import { Routes } from '@angular/router';
import { LandingComponent } from './pages/landing/landing.component';
import { InscriptionComponent } from './pages/inscription/inscription.component';

export const routes: Routes = [
    { path: "", component: LandingComponent },
    { path: "inscription", component: InscriptionComponent },
];
