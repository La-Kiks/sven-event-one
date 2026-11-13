import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  username = '';
  password = '';
  errorMessage = '';
  isLoading = false;

  constructor(private authService: AuthService, private router: Router) {
    // Redirect if already logged in
    if (this.authService.isLoggedIn()) {
      this.router.navigate([this.homeRouteFor(this.authService.getCurrentUser()?.role)]);
    }
  }

  onSubmit(): void {
    if (!this.username || !this.password) {
      this.errorMessage = 'Merci de remplir tous les champs.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.authService.login({ username: this.username, password: this.password }).subscribe({
      next: (response) => this.router.navigate([this.homeRouteFor(response.role)]),
      error: (err) => {
        this.isLoading = false;
        if (err.status === 401) {
          this.errorMessage = 'Email ou mot de passe incorrect.';
        } else if (err.status === 429) {
          this.errorMessage = err.error?.message ?? 'Trop de tentatives de connexion. Réessayez dans 15 minutes.';
        } else {
          this.errorMessage = 'Erreur serveur, veuillez réessayer.';
        }
      }
    });
  }

  private homeRouteFor(role: string | undefined): string {
    return role === 'Admin' ? '/teams' : '/mon-equipe';
  }
}