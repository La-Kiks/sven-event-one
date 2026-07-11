import { Component } from '@angular/core';
import { Validators, ReactiveFormsModule, FormGroup, FormControl } from '@angular/forms';
import { ModalComponent, StatusType } from '../modal/modal.component';
import { CreateTeamWithPlayersRequest } from '../../../models/create-team-request';
import { TeamService } from '../../../services/team.service';
import { StripeService } from '../../../services/stripe.service';


@Component({
  selector: 'app-inscription-form',
  standalone: true,
  imports: [ReactiveFormsModule, ModalComponent],
  templateUrl: './inscription-form.component.html',
  styleUrls: ['./inscription-form.component.scss']
})
export class InscriptionFormComponent {

  constructor(private teamService: TeamService, private stripeService: StripeService) { }

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
      subscribe: new FormControl('', Validators.required),
    }),
  });

  currentStep = 1;

  next() {
    const stepGroup = this.form.get(`step${this.currentStep}`);
    if (stepGroup?.valid) {
      this.currentStep++;
    } else {
      stepGroup?.markAllAsTouched();
    }
  }

  prev() {
    this.currentStep--;
  }

  submit() {
    if (this.form.invalid) {
      this.onError();
      return;
    }

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
        this.onSuccess();
        setTimeout(() => {
          window.location.href = this.url;
        }, 2000);
      },
      error: (err) => this.onError(err.error?.error)
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
      message: 'Formulaire envoyé ! Un email va vous être envoyé pour activer votre compte et suivre votre inscription.',
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
