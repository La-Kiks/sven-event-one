import { Component } from '@angular/core';
import { ModalComponent, StatusType } from '../../components/ui/modal/modal.component'
import { Router } from '@angular/router';

@Component({
  selector: 'app-payment-success',
  imports: [ModalComponent],
  templateUrl: './payment-success.component.html',
  styleUrl: './payment-success.component.scss'
})
export class PaymentSuccessComponent {

  constructor(private router: Router) { }

  transactionStatus: StatusType = 'success';
  showModal = true;
  modalMessage = "Paiment réussi, à bientôt !";

  closeModal(): void {
    this.showModal = false;
    this.router.navigate(['/']);
  }

}
