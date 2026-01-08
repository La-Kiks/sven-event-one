import { Component } from '@angular/core';
import { Validators, ReactiveFormsModule, FormGroup, FormControl } from '@angular/forms';
import { ModalComponent, StatusType } from '../modal/modal.component';

@Component({
  selector: 'app-inscription-form',
  imports: [ReactiveFormsModule, ModalComponent],
  templateUrl: './inscription-form.component.html',
  styleUrl: './inscription-form.component.scss'
})
export class InscriptionFormComponent {

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
    console.log(this.form.value);
    this.onSuccess();
    // Send this to API & redirect to inscription/success - maybe with call back
  }

  transactionStatus: StatusType = 'none';
  showModal = false;
  modalMessage = "";

  openModal(params: { message: string, status: StatusType }): void {
    const { message, status } = params;
    this.transactionStatus = status;
    this.modalMessage = message;
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
  }

  onSuccess(): void {
    this.openModal({ message: 'Forumlaire envoyé !', status: 'success' });
  }

  onError(): void {
    this.openModal({ message: 'Erreur... Essayez encore ! Si le problème persiste prenez contact avec un organisateur. Merci de votre compréhension.', status: 'error' });
  }
}
