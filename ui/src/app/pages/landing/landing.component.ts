import { Component, OnInit } from '@angular/core';
import { ButtonComponent } from '../../components/ui/button/button.component';
import { CardComponent } from '../../components/ui/card/card.component';
import { TeamCountService } from '../../services/team-count.service'; // ← adjust path if needed

@Component({
  selector: 'app-landing',
  imports: [ButtonComponent, CardComponent],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss'
})
export class LandingComponent implements OnInit {
  isRegistrationFull = false;

  constructor(private teamCountService: TeamCountService) { }

  ngOnInit(): void {
    this.teamCountService.getCount().subscribe({
      next: (data) => this.isRegistrationFull = data.isFull,
      error: () => this.isRegistrationFull = false
    });
  }

  scrollTo(id: string) {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' });
  }
}