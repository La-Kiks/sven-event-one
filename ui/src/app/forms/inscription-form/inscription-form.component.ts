import { Component } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
@Component({
  selector: 'app-inscription-form',
  imports: [ReactiveFormsModule],
  templateUrl: './inscription-form.component.html',
  styleUrl: './inscription-form.component.scss'
})
export class InscriptionFormComponent {
  fb = new FormBuilder();

  form = this.fb.group({
    name: ['', Validators.required],
    firstname: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    team: ['', Validators.required],
    version: ['', Validators.required],
    soloduo: ['', Validators.required],
    outfit: ['', Validators.required],
    subscribe: ['', Validators.required],
  });

  submit() {
    if (this.form.invalid) {
      return;
    }
    console.log(this.form.value);
    // Send this to API 
  }
}
