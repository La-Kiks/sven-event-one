import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.scss']
})
export class ForgotPasswordComponent {
  email = '';
  errorMessage = '';
  successMessage = '';
  isLoading = false;

  constructor(private authService: AuthService) { }

  onSubmit(): void {
    if (!this.email) {
      this.errorMessage = 'Merci de renseigner votre email.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.authService.forgotPassword(this.email).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.successMessage = response.message;
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.status === 429
          ? (err.error?.error ?? 'Trop de tentatives, réessayez plus tard.')
          : 'Erreur serveur, veuillez réessayer.';
      }
    });
  }
}
