import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { firstValueFrom, Observable } from "rxjs";
import { environment } from "../core/runtime-env";

import { CreateTeamWithPlayersRequest, UpdateTeamWithPlayersRequest } from '../models/create-team-request';
import { CreateTeamResponse } from '../models/create-team-response';
import { TeamDto } from '../models/team-dto';

@Injectable({
    providedIn: 'root'
})

export class TeamService {

    private readonly apiUrl = `${environment.apiUrl}/api/teams`;

    constructor(private http: HttpClient) { }

    createTeam(
        payload: CreateTeamWithPlayersRequest
    ): Observable<CreateTeamResponse> {
        return this.http.post<CreateTeamResponse>(`${this.apiUrl}/create-team`, payload);
    }

    getMyTeam(): Observable<TeamDto> {
        return this.http.get<TeamDto>(`${this.apiUrl}/my-team`);
    }

    updateMyTeam(payload: UpdateTeamWithPlayersRequest): Observable<unknown> {
        return this.http.put(`${this.apiUrl}/my-team`, payload);
    }

    createAccount(teamId: number): Observable<unknown> {
        return this.http.post(`${this.apiUrl}/${teamId}/create-account`, {});
    }
}
