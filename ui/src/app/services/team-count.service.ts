import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../core/runtime-env';

export interface TeamCount {
    current: number;
    max: number;
    isFull: boolean;
}

@Injectable({ providedIn: 'root' })
export class TeamCountService {
    constructor(private http: HttpClient) { }

    getCount(): Observable<TeamCount> {
        return this.http.get<TeamCount>(`${environment.apiUrl}/api/Teams/count`);
    }
}