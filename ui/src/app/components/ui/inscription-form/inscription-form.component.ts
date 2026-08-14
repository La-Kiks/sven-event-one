import { Component, OnDestroy, OnInit } from '@angular/core';
import { Validators, ReactiveFormsModule, FormGroup, FormControl } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { ModalComponent, StatusType } from '../modal/modal.component';
import { CreateTeamWithPlayersRequest } from '../../../models/create-team-request';
import { TeamService } from '../../../services/team.service';

const FORM_STORAGE_KEY = 'inscription-form-draft';

@Component({
  selector: 'app-inscription-form',
  standalone: true,
  imports: [ReactiveFormsModule, ModalComponent],
  templateUrl: './inscription-form.component.html',
  styleUrls: ['./inscription-form.component.scss']
})
export class InscriptionFormComponent implements OnInit, OnDestroy {

  constructor(
    private teamService: TeamService,
    private route: ActivatedRoute,
    private router: Router
  ) { }

  form = new FormGroup({
    step1: new FormGroup({
      name_a: new FormControl('', Validators.required),
      firstname_a: new FormControl('', Validators.required),
      email_a: new FormControl('', [Validators.required, Validators.email]),
      phone_a: new FormControl('', [Validators.required]),
      category_a: new FormControl('', Validators.required),
      outfit_a: new FormControl('', Validators.required),
      volounteer_a: new FormControl(''),
    }),
    step2: new FormGroup({
      name_b: new FormControl('', Validators.required),
      firstname_b: new FormControl('', Validators.required),
      email_b: new FormControl('', [Validators.required, Validators.email]),
      phone_b: new FormControl('', [Validators.required]),
      category_b: new FormControl('', Validators.required),
      outfit_b: new FormControl('', Validators.required),
      volounteer_b: new FormControl(''),
    }),
    step3: new FormGroup({
      version: new FormControl('', Validators.required),
      administration: new FormControl('', Validators.required),
      team_name: new FormControl('', Validators.required),
      subscribe: new FormControl(false, Validators.requiredTrue),
    }),
  });

  readonly totalSteps = 4;
  readonly steps = [1, 2, 3, 4];
  currentStep = 1;
  isSubmitting = false;

  readonly categoryLabels: Record<string, string> = { man: 'Homme', woman: 'Femme', mixt: 'Mixte' };
  readonly outfitLabels: Record<string, string> = {
    yes: "Oui, j'ai ma tenue.",
    lend: "Oui, j'ai besoin qu'on m'en prête une.",
    no: 'Non.'
  };
  readonly versionLabels: Record<string, string> = { short: 'Courte', long: 'Longue' };
  readonly administrationLabels: Record<string, string> = {
    none: 'Autre',
    gendarmerie: 'Gendarmerie',
    militaire: 'Militaire',
    penitancier: 'Pénitancier',
    municipale: 'Police Municipale',
    nationale: 'Police Nationale',
    pompier: 'Pompier',
  };

  private readonly requiredMessages: Record<string, string> = {
    'step1.category_a': 'Merci de choisir une catégorie.',
    'step1.outfit_a': 'Merci de répondre à cette question.',
    'step2.category_b': 'Merci de choisir une catégorie.',
    'step2.outfit_b': 'Merci de répondre à cette question.',
    'step3.version': 'Merci de choisir une version.',
    'step3.administration': 'Merci de sélectionner ton administration.',
    'step3.subscribe': "Merci d'accepter cette condition pour continuer.",
  };

  private querySub?: Subscription;

  ngOnInit(): void {
    this.restoreDraft();

    this.querySub = this.route.queryParamMap.subscribe(params => {
      const step = Number(params.get('step')) || 1;
      this.currentStep = Math.min(Math.max(step, 1), this.totalSteps);
    });

    this.form.valueChanges.subscribe(value => {
      sessionStorage.setItem(FORM_STORAGE_KEY, JSON.stringify(value));
    });
  }

  ngOnDestroy(): void {
    this.querySub?.unsubscribe();
  }

  private restoreDraft(): void {
    const saved = sessionStorage.getItem(FORM_STORAGE_KEY);
    if (!saved) {
      return;
    }
    try {
      this.form.patchValue(JSON.parse(saved));
    } catch {
      sessionStorage.removeItem(FORM_STORAGE_KEY);
    }
  }

  private goToStep(step: number): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { step },
      queryParamsHandling: 'merge'
    });
  }

  next() {
    const stepGroup = this.form.get(`step${this.currentStep}`);
    if (stepGroup?.valid) {
      this.goToStep(this.currentStep + 1);
    } else {
      stepGroup?.markAllAsTouched();
      this.scrollToFirstInvalid();
    }
  }

  private scrollToFirstInvalid(): void {
    // Wait a tick so the @if-driven error messages/invalid classes have rendered.
    setTimeout(() => {
      const target = document.querySelector<HTMLElement>(
        '.form-card .field__input--invalid, .form-card .field-error'
      );
      target?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      target?.focus?.();
    });
  }

  prev() {
    this.goToStep(this.currentStep - 1);
  }

  editStep(step: number) {
    this.goToStep(step);
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

  label(map: Record<string, string>, value: string | null | undefined): string {
    return value ? (map[value] ?? value) : '';
  }

  submit() {
    if (this.form.invalid) {
      this.onError();
      return;
    }
    if (this.isSubmitting) {
      return;
    }

    this.isSubmitting = true;

    const step1 = this.form.get('step1')!.value;
    const step2 = this.form.get('step2')!.value;
    const step3 = this.form.get('step3')!.value;

    const payload: CreateTeamWithPlayersRequest = {
      teamDto: {
        teamName: step3.team_name!,
        version: step3.version!,
        administration: step3.administration!
      },
      playerDtos: [
        {
          firstName: step1.firstname_a!,
          lastName: step1.name_a!,
          email: step1.email_a!,
          phoneNumber: step1.phone_a!,
          category: step1.category_a!,
          outfit: step1.outfit_a!,
          volunteer: !!step1.volounteer_a,
          acceptMails: !!step3.subscribe,
        },
        {
          firstName: step2.firstname_b!,
          lastName: step2.name_b!,
          email: step2.email_b!,
          phoneNumber: step2.phone_b!,
          category: step2.category_b!,
          outfit: step2.outfit_b!,
          volunteer: !!step2.volounteer_b,
          acceptMails: !!step3.subscribe
        }
      ]
    };

    this.teamService.createTeam(payload).subscribe({
      next: () => {
        this.isSubmitting = false;
        sessionStorage.removeItem(FORM_STORAGE_KEY);
        this.onSuccess();
        setTimeout(() => {
          window.location.href = this.url;
        }, 2000);
      },
      error: (err) => {
        this.isSubmitting = false;
        this.onError(err.error?.error);
      }
    });

  }

  transactionStatus: StatusType = 'none';
  showModal = false;
  modalMessage = "";
  url = 'https://yp.events/9f201d18-648c-44ab-9933-c4494c0b4afe/HYROX-POLICE-NATIONALE-54';


  openModal(params: { message: string, url: string, status: StatusType }): void {
    const { message, url, status } = params;
    this.transactionStatus = status;
    this.modalMessage = message;
    this.url = url;
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
  }

  onSuccess(): void {
    this.openModal({
      message: 'Formulaire envoyé ! Un email va vous être envoyé pour activer votre compte et suivre votre inscription. Vous allez maintenant être redirigé(e) vers notre billetterie partenaire Yurplan pour régler les 60€ de l\'inscription.',
      url: this.url,
      status: 'success'
    });
  }

  onError(serverMessage?: string): void {
    this.openModal({
      message: serverMessage
        ?? 'Erreur... Essayez encore ! Si le problème persiste prenez contact avec un organisateur. Merci de votre compréhension.',
      url: '',
      status: 'error'
    });
  }

}
