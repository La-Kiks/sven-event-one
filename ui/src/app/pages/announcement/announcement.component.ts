import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';
import { environment } from '../../core/runtime-env';

interface AnnouncementSendResult {
  total: number;
  sent: number;
  failed: number;
  failures: { email: string; error: string }[];
}

@Component({
  selector: 'app-announcement',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './announcement.component.html',
  styleUrl: './announcement.component.scss'
})
export class AnnouncementComponent {
  readonly maxSubjectLength = 200;
  readonly maxMessageLength = 10000;

  currentUser: { username: string; role: string } | null = null;

  subject = '';
  message = '';

  testEmail = '';
  isSendingTest = false;
  testSuccess = '';
  testError = '';

  isLoadingCount = false;
  recipientCount: number | null = null;
  showConfirm = false;
  isSending = false;
  sendError = '';
  result: AnnouncementSendResult | null = null;

  private readonly baseUrl = `${environment.apiUrl}/api/announcements`;

  constructor(private http: HttpClient, public authService: AuthService) {
    this.currentUser = this.authService.getCurrentUser();
  }

  get canSend(): boolean {
    return this.subject.trim().length > 0 && this.message.trim().length > 0;
  }

  sendTest(): void {
    if (!this.canSend || !this.testEmail.trim()) return;
    this.isSendingTest = true;
    this.testSuccess = '';
    this.testError = '';

    const toEmail = this.testEmail.trim();
    this.http.post(`${this.baseUrl}/test`, { subject: this.subject, message: this.message, toEmail }).subscribe({
      next: () => {
        this.isSendingTest = false;
        this.testSuccess = `Test envoyé à ${toEmail}.`;
      },
      error: (err) => {
        this.isSendingTest = false;
        this.testError = err.status === 400
          ? 'Vérifiez l\'adresse email, l\'objet et le message.'
          : (err.error?.error ?? "L'email de test n'a pas pu être envoyé.");
      }
    });
  }

  openConfirm(): void {
    if (!this.canSend) return;
    this.isLoadingCount = true;
    this.sendError = '';
    this.result = null;

    this.http.get<{ count: number }>(`${this.baseUrl}/recipients`).subscribe({
      next: (res) => {
        this.isLoadingCount = false;
        this.recipientCount = res.count;
        this.showConfirm = true;
      },
      error: () => {
        this.isLoadingCount = false;
        this.sendError = 'Impossible de récupérer le nombre de destinataires. Réessayez.';
      }
    });
  }

  cancelConfirm(): void {
    this.showConfirm = false;
  }

  sendToAll(): void {
    this.isSending = true;
    this.sendError = '';

    this.http.post<AnnouncementSendResult>(`${this.baseUrl}/send`, { subject: this.subject, message: this.message }).subscribe({
      next: (res) => {
        this.isSending = false;
        this.showConfirm = false;
        this.result = res;
      },
      error: () => {
        this.isSending = false;
        this.sendError = "L'envoi a échoué. Vérifiez les logs avant de réessayer : une partie des emails a pu partir.";
      }
    });
  }
}
