import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { firstValueFrom, Observable } from "rxjs";
import { environment } from "../core/runtime-env";

import { CreateTeamWithPlayersRequest } from '../models/create-team-request';
import { CreateTeamResponse } from '../models/create-team-response';

@Injectable({
    providedIn: 'root'
})

export class TeamService {

    private readonly createTeamApiUrl = `${environment.apiUrl}/api/teams/create-team`;

    constructor(private http: HttpClient) { }

    createTeam(
        payload: CreateTeamWithPlayersRequest
    ): Observable<CreateTeamResponse> {
        return this.http.post<CreateTeamResponse>(`${this.createTeamApiUrl}`, payload);
    }
} 