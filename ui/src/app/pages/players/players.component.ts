import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth/auth.service';
import { environment } from '../../core/runtime-env';
import { Router, RouterModule } from '@angular/router';

interface Player {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  category: string;
  outfit: string;
  volunteer: boolean;
  teamId: number;
  teamName: string;
}

type SortField = 'lastName' | 'email' | 'phoneNumber' | 'category' | 'outfit' | 'volunteer' | 'teamName';
type SortDir = 'asc' | 'desc';
type VolunteerFilter = 'all' | 'yes' | 'no';

@Component({
  selector: 'app-players',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './players.component.html',
  styleUrls: ['./players.component.scss']
})
export class PlayersComponent implements OnInit {
  players: Player[] = [];
  filtered: Player[] = [];
  sorted: Player[] = [];
  isLoading = true;
  error = '';
  currentUser: { username: string; role: string } | null = null;

  sortField: SortField = 'lastName';
  sortDir: SortDir = 'asc';

  searchQuery = '';
  volunteerFilter: VolunteerFilter = 'all';
  categoryFilter = 'all';

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
        this.applyFilters();
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Failed to load players.';
        this.isLoading = false;
      }
    });
  }

  get categories(): string[] {
    return Array.from(new Set(this.players.map(p => p.category))).sort();
  }

  onSearchChange(value: string): void {
    this.searchQuery = value;
    this.applyFilters();
  }

  setVolunteerFilter(filter: VolunteerFilter): void {
    this.volunteerFilter = filter;
    this.applyFilters();
  }

  setCategoryFilter(category: string): void {
    this.categoryFilter = category;
    this.applyFilters();
  }

  applyFilters(): void {
    const query = this.searchQuery.trim().toLowerCase();

    this.filtered = this.players.filter(p => {
      if (this.volunteerFilter === 'yes' && !p.volunteer) return false;
      if (this.volunteerFilter === 'no' && p.volunteer) return false;
      if (this.categoryFilter !== 'all' && p.category !== this.categoryFilter) return false;
      if (!query) return true;

      const haystack = `${p.firstName} ${p.lastName} ${p.teamName} ${p.email}`.toLowerCase();
      return haystack.includes(query);
    });

    this.applySort();
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

  onSortKeydown(event: KeyboardEvent, field: SortField): void {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.sort(field);
    }
  }

  applySort(): void {
    this.sorted = [...this.filtered].sort((a, b) => {
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

  getAriaSort(field: SortField): 'ascending' | 'descending' | 'none' {
    if (this.sortField !== field) return 'none';
    return this.sortDir === 'asc' ? 'ascending' : 'descending';
  }

  exportCsv(): void {
    const headers = ['Last name', 'First name', 'Team', 'Category', 'Outfit', 'Email', 'Phone', 'Volunteer'];
    const rows = this.sorted.map(p => [
      p.lastName,
      p.firstName,
      p.teamName,
      p.category,
      p.outfit,
      p.email,
      p.phoneNumber,
      p.volunteer ? 'Yes' : 'No'
    ]);

    const csv = [headers, ...rows]
      .map(row => row.map(field => this.escapeCsvField(field)).join(','))
      .join('\r\n');

    const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `players-${new Date().toISOString().slice(0, 10)}.csv`;
    link.click();
    URL.revokeObjectURL(url);
  }

  private escapeCsvField(field: string): string {
    return /[",\r\n]/.test(field) ? `"${field.replace(/"/g, '""')}"` : field;
  }

  goToTeams(): void {
    this.router.navigate(['/teams']);
  }
}