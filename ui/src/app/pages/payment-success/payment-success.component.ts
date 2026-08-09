import { Component } from '@angular/core';
import { ModalComponent, StatusType } from '../../components/ui/modal/modal.component'
import { Router } from '@angular/router';

// LEGACY / UNREACHABLE IN PRODUCTION: registration pays via an external
// Yurplan link that does not redirect back here (see
// inscription-form.component.ts and StripeController's SuccessUrl, which
// nothing currently navigates to). Kept working in case Yurplan is ever
// dropped in favor of this app's own Stripe checkout.
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
