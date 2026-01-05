import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-inscription-success',
  imports: [],
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

}
