import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth/auth.service';
import { environment } from '../../../environment';
import { Router } from '@angular/router';

interface Player {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  category: string;
  outfit: string;
  volunteer: boolean;
  teamName: string;
}

type SortField = 'lastName' | 'email' | 'phoneNumber' | 'category' | 'outfit' | 'volunteer' | 'teamName';
type SortDir = 'asc' | 'desc';

@Component({
  selector: 'app-players',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './players.component.html',
  styleUrls: ['./players.component.scss']
})
export class PlayersComponent implements OnInit {
  players: Player[] = [];
  sorted: Player[] = [];
  isLoading = true;
  error = '';
  currentUser: { username: string; role: string } | null = null;

  sortField: SortField = 'lastName';
  sortDir: SortDir = 'asc';

  constructor(
    private http: HttpClient,
    public authService: AuthService,
    private router: Router
  ) {
    this.currentUser = this.authService.getCurrentUser();
  }

  ngOnInit(): void {
    this.http.get<Player[]>(`${environment.apiUrl}/api/Players`).subscribe({
      next: (data) => {
        this.players = data;
        this.applySort();
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Failed to load players.';
        this.isLoading = false;
      }
    });
  }

  sort(field: SortField): void {
    if (this.sortField === field) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortField = field;
      this.sortDir = 'asc';
    }
    this.applySort();
  }

  applySort(): void {
    this.sorted = [...this.players].sort((a, b) => {
      const valA = a[this.sortField];
      const valB = b[this.sortField];

      if (typeof valA === 'boolean') {
        return this.sortDir === 'asc'
          ? Number(valB) - Number(valA)
          : Number(valA) - Number(valB);
      }

      const cmp = String(valA).localeCompare(String(valB));
      return this.sortDir === 'asc' ? cmp : -cmp;
    });
  }

  getSortIcon(field: SortField): string {
    if (this.sortField !== field) return '↕';
    return this.sortDir === 'asc' ? '↑' : '↓';
  }

  goToTeams(): void {
    this.router.navigate(['/teams']);
  }
}