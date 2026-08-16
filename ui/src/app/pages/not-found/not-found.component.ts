import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonComponent } from '../../components/ui/button/button.component';
import { TeamCountService } from '../../services/team-count.service';

@Component({
  selector: 'app-not-found',
  imports: [ButtonComponent, RouterLink],
  templateUrl: './not-found.component.html',
  styleUrl: './not-found.component.scss'
})
export class NotFoundComponent implements OnInit {
  isRegistrationFull = false;

  constructor(private teamCountService: TeamCountService) { }

  ngOnInit(): void {
    this.teamCountService.getCount().subscribe({
      next: (data) => {
        this.isRegistrationFull = data.isFull;
      },
      // Fail closed (never oversell) on a network/API error, matching landing's
      // behavior for the same button.
      error: () => {
        this.isRegistrationFull = true;
      }
    });
  }
}
