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
  countLoadError = false;

  constructor(private teamCountService: TeamCountService) { }

  ngOnInit(): void {
    this.teamCountService.getCount().subscribe({
      next: (data) => this.isRegistrationFull = data.isFull,
      // Fail closed (never oversell) on a network/API error, but track it
      // separately from a genuine sellout so the CTA copy doesn't falsely
      // tell every visitor during an outage that registration is closed.
      error: () => {
        this.isRegistrationFull = true;
        this.countLoadError = true;
      }
    });
  }

  scrollTo(id: string) {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' });
  }
}