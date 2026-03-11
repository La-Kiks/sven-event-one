import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth/auth.service';
import { environment } from '../../../environment';

interface Team {
  id: number;
  name: string;
}

@Component({
  selector: 'app-teams',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './teams.component.html',
  styleUrls: ['./teams.component.scss']
})
export class TeamsComponent implements OnInit {
  teams: Team[] = [];
  isLoading = true;
  error = '';
  currentUser: { username: string; role: string } | null = null;

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
}