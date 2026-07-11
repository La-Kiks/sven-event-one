import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../core/runtime-env';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  username: string;
  role: string;
}

export interface ActivateAccountRequest {
  token: string;
  password: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private apiUrl = `${environment.apiUrl}/api/auth`;

  constructor(private http: HttpClient, private router: Router) { }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(response => this.storeSession(response))
    );
  }

  activate(request: ActivateAccountRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/activate`, request).pipe(
      tap(response => this.storeSession(response))
    );
  }

  private storeSession(response: LoginResponse): void {
    sessionStorage.setItem('auth_token', response.token);
    sessionStorage.setItem('auth_user', JSON.stringify({
      username: response.username,
      role: response.role
    }));
  }

  logout(): void {
    sessionStorage.removeItem('auth_token');
    sessionStorage.removeItem('auth_user');
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return sessionStorage.getItem('auth_token');
  }

  isLoggedIn(): boolean {
    const token = this.getToken();
    if (!token) return false;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.exp * 1000 > Date.now();
    } catch {
      return false;
    }
  }

  getCurrentUser(): { username: string; role: string } | null {
    const user = sessionStorage.getItem('auth_user');
    return user ? JSON.parse(user) : null;
  }
}