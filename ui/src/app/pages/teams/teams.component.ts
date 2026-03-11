import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';
import { environment } from '../../../environment';

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
  isPaid: boolean;
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
  teams: Team[] = [];
  isLoading = true;
  error = '';
  currentUser: { username: string; role: string } | null = null;

  // Panel state
  selectedTeam: Team | null = null;
  isPanelOpen = false;
  isPanelLoading = false;
  panelError = '';

  constructor(private http: HttpClient, public authService: AuthService) {
    this.currentUser = this.authService.getCurrentUser();
  }

  ngOnInit(): void {
    this.http.get<Team[]>(`${environment.apiUrl}/api/Teams/teams`).subscribe({
      next: (data) => {
        this.teams = data;
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
    setTimeout(() => this.selectedTeam = null, 300);
  }
}