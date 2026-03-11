import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { firstValueFrom } from "rxjs";
import { environment } from "../../environment";

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