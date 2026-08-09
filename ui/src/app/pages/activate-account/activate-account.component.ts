import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';

@Component({
  selector: 'app-activate-account',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './activate-account.component.html',
  styleUrls: ['./activate-account.component.scss']
})
export class ActivateAccountComponent implements OnInit {
  readonly organizerPhone = '06 48 73 50 15';
  readonly organizerEmail = 'svenbarberat@orange.fr';

  token = '';
  password = '';
  confirmPassword = '';
  errorMessage = '';
  tokenRejected = false;
  isLoading = false;
  missingToken = false;

  constructor(
    private route: ActivatedRoute,
    private authService: AuthService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    this.missingToken = !this.token;
  }

  onSubmit(): void {
    this.tokenRejected = false;

    if (!this.password || !this.confirmPassword) {
      this.errorMessage = 'Merci de renseigner un mot de passe.';
      return;
    }
    if (this.password.length < 8) {
      this.errorMessage = 'Le mot de passe doit contenir au moins 8 caractères.';
      return;
    }
    if (this.password !== this.confirmPassword) {
      this.errorMessage = 'Les mots de passe ne correspondent pas.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.authService.activate({ token: this.token, password: this.password }).subscribe({
      next: () => this.router.navigate(['/mon-equipe']),
      error: (err) => {
        this.isLoading = false;
        // A server rejection (bad/expired token) is a different failure mode than a
        // client-side validation miss: it can't be fixed by retyping the password, so
        // it gets its own message with a real recovery path instead of reusing the
        // generic banner.
        if (err.error?.error) {
          this.tokenRejected = true;
          this.errorMessage = err.error.error;
        } else {
          this.tokenRejected = true;
          this.errorMessage = 'Ce lien d\'activation est invalide ou a expiré.';
        }
      }
    });
  }
}
