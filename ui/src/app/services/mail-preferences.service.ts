import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../core/runtime-env";

@Injectable({
    providedIn: 'root'
})
export class MailPreferencesService {

    private readonly apiUrl = `${environment.apiUrl}/api/mail-preferences`;

    constructor(private http: HttpClient) { }

    unsubscribe(email: string, sig: string): Observable<unknown> {
        return this.http.post<unknown>(`${this.apiUrl}/unsubscribe`, { email, sig });
    }
}
