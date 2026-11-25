import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MailPreferencesService } from '../../services/mail-preferences.service';

@Component({
  selector: 'app-unsubscribe',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './unsubscribe.component.html',
  styleUrls: ['../activate-account/activate-account.component.scss']
})
export class UnsubscribeComponent implements OnInit {
  readonly organizerEmail = 'svenbarberat@orange.fr';

  email = '';
  sig = '';
  missingParams = false;
  isLoading = false;
  done = false;
  errorMessage = '';
  linkRejected = false;

  constructor(
    private route: ActivatedRoute,
    private mailPreferences: MailPreferencesService
  ) { }

  ngOnInit(): void {
    this.email = this.route.snapshot.queryParamMap.get('email') ?? '';
    this.sig = this.route.snapshot.queryParamMap.get('sig') ?? '';
    this.missingParams = !this.email || !this.sig;
  }

  onConfirm(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.linkRejected = false;

    this.mailPreferences.unsubscribe(this.email, this.sig).subscribe({
      next: () => {
        this.isLoading = false;
        this.done = true;
      },
      error: (err) => {
        this.isLoading = false;
        if (err.status === 400) {
          this.linkRejected = true;
          this.errorMessage = err.error?.error ?? 'Ce lien de désabonnement est invalide.';
        } else {
          this.errorMessage = 'Une erreur est survenue. Réessayez dans quelques instants.';
        }
      }
    });
  }
}
