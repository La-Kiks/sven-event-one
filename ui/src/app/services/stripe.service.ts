import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { firstValueFrom } from "rxjs";
import { environment } from "../../environment";

@Injectable({
    providedIn: 'root'
})

export class StripeService {

    constructor(private http: HttpClient) { }

    async redirectToCheckout(): Promise<void> {
        const session = await firstValueFrom(
            this.http.post<{ url: string }>(
                `${environment.apiUrl}/stripe/create-checkout-session`,
                'body'
            )
        );

        window.location.href = session.url;
    }
}