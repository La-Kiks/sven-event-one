import { Component } from '@angular/core';
import { Validators, ReactiveFormsModule, FormGroup, FormControl } from '@angular/forms';
@Component({
  selector: 'app-inscription-form',
  imports: [ReactiveFormsModule],
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
      outfit_a: new FormControl('', Validators.required),
      volounteer_a: new FormControl(''),
    }),
    step2: new FormGroup({
      name_b: new FormControl('', Validators.required),
      firstname_b: new FormControl('', Validators.required),
      email_b: new FormControl('', [Validators.required, Validators.email]),
      phone_b: new FormControl('', [Validators.required]),
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
      return;
    }
    console.log(this.form.value);
    // Send this to API 
  }
}
