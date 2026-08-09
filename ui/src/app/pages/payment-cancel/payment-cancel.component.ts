import { Component } from '@angular/core';
import { ModalComponent, StatusType } from '../../components/ui/modal/modal.component'
import { Router } from '@angular/router';

// LEGACY / UNREACHABLE IN PRODUCTION: see the comment on PaymentSuccessComponent
// — registration pays via an external Yurplan link that never redirects here.
@Component({
  selector: 'app-payment-cancel',
  imports: [ModalComponent],
  templateUrl: './payment-cancel.component.html',
  styleUrl: './payment-cancel.component.scss'
})
export class PaymentCancelComponent {
  constructor(private router: Router) { }

  transactionStatus: StatusType = 'error';
  showModal = true;
  modalMessage = "Paiment annulé, veuillez réessayer ultériement.";

  closeModal(): void {
    this.showModal = false;
    this.router.navigate(['/']);
  }

}
