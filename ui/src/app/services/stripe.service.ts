import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { firstValueFrom } from "rxjs";
import { environment } from "../core/runtime-env";

// LEGACY / UNUSED IN PRODUCTION: registration currently pays via an external
// Yurplan link (see inscription-form.component.ts), not this service. Kept as
// a working fallback in case Yurplan is ever dropped.
@Injectable({
    providedIn: 'root'
})

export class StripeService {

    private readonly createCheckoutApiUrl = `${environment.apiUrl}/api/stripe/create-checkout-session`;

    constructor(private http: HttpClient) { }

    async redirectToCheckout(teamId: number): Promise<void> {
        const session = await firstValueFrom(
            this.http.post<{ url: string }>(
                `${this.createCheckoutApiUrl}/${teamId}`,
                {}
            )
        );

        window.location.href = session.url;
    }
}