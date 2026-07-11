import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router } from '@angular/router';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {

  constructor(private authService: AuthService, private router: Router) { }

  canActivate(route: ActivatedRouteSnapshot): boolean {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
      return false;
    }

    const requiredRole = route.data?.['role'] as string | undefined;
    const role = this.authService.getCurrentUser()?.role;

    if (requiredRole && role !== requiredRole) {
      // Logged in, wrong role — send them to their own home instead of /login to avoid a loop.
      this.router.navigate([role === 'Admin' ? '/teams' : '/mon-equipe']);
      return false;
    }

    return true;
  }
}
