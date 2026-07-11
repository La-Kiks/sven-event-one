import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';
import { environment } from '../../core/runtime-env';

interface Player {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  category: string;
  outfit: string;
  volunteer: boolean;
  acceptMails: boolean;
}

interface Team {
  id: number;
  name: string;
  version: string;
  administration: string;
  category: string; // ← added
  isPaid: boolean;
  hasAccount: boolean;
  accountVerified: boolean;
  players: Player[];
}

@Component({
  selector: 'app-teams',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './teams.component.html',
  styleUrls: ['./teams.component.scss']
})
export class TeamsComponent implements OnInit {
  sortField: 'name' | 'version' | 'administration' | 'category' = 'name'; // ← category added
  sortDir: 'asc' | 'desc' = 'asc';
  sortedTeams: Team[] = [];
  teams: Team[] = [];
  isLoading = true;
  error = '';
  currentUser: { username: string; role: string } | null = null;

  // Panel state
  selectedTeam: Team | null = null;
  isPanelOpen = false;
  isPanelLoading = false;
  panelError = '';

  // Delete state
  showDeleteConfirm = false;
  isDeleting = false;
  deleteError = '';

  // Account creation state
  isCreatingAccount = false;
  createAccountMessage = '';
  createAccountError = '';

  constructor(private http: HttpClient, public authService: AuthService) {
    this.currentUser = this.authService.getCurrentUser();
  }

  ngOnInit(): void {
    this.http.get<Team[]>(`${environment.apiUrl}/api/Teams/teams`).subscribe({
      next: (data) => {
        this.teams = data;
        this.applySort();
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Failed to load teams.';
        this.isLoading = false;
      }
    });
  }

  openPanel(team: Team): void {
    this.isPanelOpen = true;
    this.isPanelLoading = true;
    this.panelError = '';
    this.selectedTeam = null;
    this.showDeleteConfirm = false;
    this.deleteError = '';
    this.createAccountMessage = '';
    this.createAccountError = '';

    this.http.get<Team>(`${environment.apiUrl}/api/Teams/${team.id}`).subscribe({
      next: (data) => {
        this.selectedTeam = data;
        this.isPanelLoading = false;
      },
      error: () => {
        this.panelError = 'Failed to load team details.';
        this.isPanelLoading = false;
      }
    });
  }

  closePanel(): void {
    this.isPanelOpen = false;
    this.showDeleteConfirm = false;
    this.deleteError = '';
    setTimeout(() => this.selectedTeam = null, 300);
  }

  confirmDelete(): void {
    this.showDeleteConfirm = true;
    this.deleteError = '';
  }

  cancelDelete(): void {
    this.showDeleteConfirm = false;
    this.deleteError = '';
  }

  deleteTeam(): void {
    if (!this.selectedTeam) return;
    this.isDeleting = true;
    this.deleteError = '';

    this.http.delete(`${environment.apiUrl}/api/Teams/${this.selectedTeam.id}`).subscribe({
      next: () => {
        this.teams = this.teams.filter(t => t.id !== this.selectedTeam!.id);
        this.applySort();
        this.closePanel();
        this.isDeleting = false;
      },
      error: () => {
        this.deleteError = 'Failed to delete team. Please try again.';
        this.isDeleting = false;
      }
    });
  }

  getAdminLabel(value: string): string {
    const labels: Record<string, string> = {
      none: 'Autre',
      gendarmerie: 'Gendarmerie',
      militaire: 'Militaire',
      penitancier: 'Pénitancier',
      municipale: 'Police Municipale',
      nationale: 'Police Nationale',
      pompier: 'Pompier'
    };
    return labels[value] ?? value;
  }

  getCategoryLabel(value: string): string {
    const labels: Record<string, string> = {
      man: 'Homme',
      woman: 'Femme',
      mixt: 'Mixte'
    };
    return labels[value] ?? value;
  }

  sort(field: 'name' | 'version' | 'administration' | 'category'): void {
    if (this.sortField === field) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortField = field;
      this.sortDir = 'asc';
    }
    this.applySort();
  }

  applySort(): void {
    this.sortedTeams = [...this.teams].sort((a, b) => {
      const cmp = String(a[this.sortField]).localeCompare(String(b[this.sortField]));
      return this.sortDir === 'asc' ? cmp : -cmp;
    });
  }

  getSortIcon(field: 'name' | 'version' | 'administration' | 'category'): string {
    if (this.sortField !== field) return '↕';
    return this.sortDir === 'asc' ? '↑' : '↓';
  }

  togglePayment(team: Team): void {
    this.http.patch(`${environment.apiUrl}/api/Teams/${team.id}/payment`, {
      isPaid: !team.isPaid
    }).subscribe({
      next: () => {
        team.isPaid = !team.isPaid;
        if (this.selectedTeam?.id === team.id)
          this.selectedTeam.isPaid = team.isPaid;
      },
      error: () => console.error('Failed to update payment status')
    });
  }

  createAccount(team: Team): void {
    this.isCreatingAccount = true;
    this.createAccountMessage = '';
    this.createAccountError = '';

    this.http.post(`${environment.apiUrl}/api/Teams/${team.id}/create-account`, {}).subscribe({
      next: () => {
        this.isCreatingAccount = false;
        this.createAccountMessage = "Email d'activation envoyé.";
        team.hasAccount = true;
        if (this.selectedTeam?.id === team.id) this.selectedTeam.hasAccount = true;
      },
      error: (err) => {
        this.isCreatingAccount = false;
        this.createAccountError = err.status === 409
          ? 'Ce compte est déjà activé.'
          : "Échec de l'envoi de l'email d'activation.";
      }
    });
  }
}