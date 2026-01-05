import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ModalComponent } from "../../components/ui/modal/modal.component";

type StatusType = 'none' | 'success' | 'error';

@Component({
  selector: 'app-inscription-success',
  imports: [ModalComponent],
  templateUrl: './inscription-success.component.html',
  styleUrl: './inscription-success.component.scss'
})
export class InscriptionSuccessComponent implements OnInit {
  userData: any;

  constructor(private router: Router) {
    const navigation = this.router.getCurrentNavigation();
    this.userData = navigation?.extras?.state?.['userData'];
  }

  ngOnInit(): void {
    if (!this.userData) {
      console.log('No user data !');
      // this.router.navigate(['/inscription']);
    }
  }

  goToPayment(): void {
    console.log('Go to payment !');
    // this.router.navigate(['/payment']);
  }


  // Modal section
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
    this.openModal({ message: 'Transaction réussie !', status: 'success' });
  }

  onError(): void {
    this.openModal({ message: 'Transaction échouée...', status: 'error' });
  }
}
