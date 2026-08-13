import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormGroup, FormControl, Validators, ReactiveFormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth/auth.service';
import { TeamService } from '../../services/team.service';
import { TeamDto } from '../../models/team-dto';

@Component({
  selector: 'app-my-team',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './my-team.component.html',
  styleUrls: ['./my-team.component.scss']
})
export class MyTeamComponent implements OnInit {
  isLoading = true;
  loadError = '';
  isSaving = false;
  saveSuccess = false;
  saveError = '';
  team: TeamDto | null = null;

  form = new FormGroup({
    team: new FormGroup({
      team_name: new FormControl('', Validators.required),
      version: new FormControl('', Validators.required),
      administration: new FormControl('', Validators.required),
    }),
    player1: this.buildPlayerGroup(),
    player2: this.buildPlayerGroup(),
  });

  private readonly requiredMessages: Record<string, string> = {
    'team.version': 'Merci de choisir une version.',
    'team.administration': 'Merci de sélectionner ton administration.',
    'player1.category': 'Merci de choisir une catégorie.',
    'player1.outfit': 'Merci de répondre à cette question.',
    'player2.category': 'Merci de choisir une catégorie.',
    'player2.outfit': 'Merci de répondre à cette question.',
  };

  readonly organizerPhone = '06 48 73 50 15';
  readonly organizerEmail = 'svenbarberat@orange.fr';

  constructor(public authService: AuthService, private teamService: TeamService) { }

  ngOnInit(): void {
    this.loadTeam();
  }

  loadTeam(): void {
    this.isLoading = true;
    this.loadError = '';
    this.teamService.getMyTeam().subscribe({
      next: (team) => {
        this.team = team;
        this.patchForm(team);
        this.isLoading = false;
      },
      error: () => {
        this.loadError = "Impossible de charger votre équipe. Réessayez plus tard.";
        this.isLoading = false;
      }
    });
  }

  errorMessage(path: string): string | null {
    const control = this.form.get(path);
    if (!control || !control.touched || control.valid) {
      return null;
    }
    if (control.hasError('email')) {
      return 'Adresse mail invalide.';
    }
    return this.requiredMessages[path] ?? 'Ce champ est requis.';
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const teamVal = this.form.get('team')!.value;
    const p1 = this.form.get('player1')!.value;
    const p2 = this.form.get('player2')!.value;

    this.isSaving = true;
    this.saveError = '';
    this.saveSuccess = false;

    this.teamService.updateMyTeam({
      teamDto: {
        teamName: teamVal.team_name!,
        version: teamVal.version!,
        administration: teamVal.administration!
      },
      playerDtos: [
        {
          id: p1.id!, firstName: p1.firstname!, lastName: p1.name!, email: p1.email!,
          phoneNumber: p1.phone!, category: p1.category!, outfit: p1.outfit!,
          volunteer: !!p1.volunteer, acceptMails: !!p1.acceptMails
        },
        {
          id: p2.id!, firstName: p2.firstname!, lastName: p2.name!, email: p2.email!,
          phoneNumber: p2.phone!, category: p2.category!, outfit: p2.outfit!,
          volunteer: !!p2.volunteer, acceptMails: !!p2.acceptMails
        },
      ]
    }).subscribe({
      next: () => {
        this.isSaving = false;
        this.saveSuccess = true;
        // The PUT response doesn't echo back the updated team, and re-running
        // loadTeam() would flip isLoading and hide the just-shown success
        // banner — patch the fields this form actually owns instead, so the
        // header title and version/administration pills stop showing stale
        // pre-save values.
        if (this.team) {
          this.team = {
            ...this.team,
            name: teamVal.team_name!,
            version: teamVal.version!,
            administration: teamVal.administration!,
          };
        }
      },
      error: (err) => {
        this.isSaving = false;
        this.saveError = err.error?.error ?? "Une erreur est survenue lors de l'enregistrement. Réessayez.";
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

  private buildPlayerGroup() {
    return new FormGroup({
      id: new FormControl<number | null>(null),
      name: new FormControl('', Validators.required),
      firstname: new FormControl('', Validators.required),
      email: new FormControl('', [Validators.required, Validators.email]),
      phone: new FormControl('', Validators.required),
      category: new FormControl('', Validators.required),
      outfit: new FormControl('', Validators.required),
      volunteer: new FormControl(false),
      acceptMails: new FormControl(false),
    });
  }

  private patchForm(team: TeamDto): void {
    const [p1, p2] = team.players;
    this.form.patchValue({
      team: {
        team_name: team.name,
        version: team.version,
        administration: team.administration,
      },
      player1: p1 ? {
        id: p1.id, name: p1.lastName, firstname: p1.firstName, email: p1.email,
        phone: p1.phoneNumber, category: p1.category, outfit: p1.outfit,
        volunteer: p1.volunteer, acceptMails: p1.acceptMails
      } : {},
      player2: p2 ? {
        id: p2.id, name: p2.lastName, firstname: p2.firstName, email: p2.email,
        phone: p2.phoneNumber, category: p2.category, outfit: p2.outfit,
        volunteer: p2.volunteer, acceptMails: p2.acceptMails
      } : {},
    });
  }
}
