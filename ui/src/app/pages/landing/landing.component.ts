import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ButtonComponent } from '../../components/ui/button/button.component';
import { CardComponent } from '../../components/ui/card/card.component';
import { TeamCount, TeamCountService } from '../../services/team-count.service'; // ← adjust path if needed

@Component({
  selector: 'app-landing',
  imports: [NgIf, RouterLink, ButtonComponent, CardComponent],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss'
})
export class LandingComponent implements OnInit, AfterViewInit, OnDestroy {
  isRegistrationFull = false;
  countLoadError = false;
  hasScrolledPastHero = false;
  teamCount: TeamCount | null = null;

  @ViewChild('hero') private heroRef?: ElementRef<HTMLElement>;
  private heroObserver?: IntersectionObserver;

  constructor(private teamCountService: TeamCountService) { }

  ngOnInit(): void {
    this.teamCountService.getCount().subscribe({
      next: (data) => {
        this.isRegistrationFull = data.isFull;
        this.teamCount = data;
      },
      // Fail closed (never oversell) on a network/API error, but track it
      // separately from a genuine sellout so the CTA copy doesn't falsely
      // tell every visitor during an outage that registration is closed.
      error: () => {
        this.isRegistrationFull = true;
        this.countLoadError = true;
      }
    });
  }

  ngAfterViewInit(): void {
    if (!this.heroRef) return;
    // Show the persistent CTA only once the hero (with its own CTA) has
    // scrolled out of view, so the two don't both show at once at the top.
    this.heroObserver = new IntersectionObserver(
      ([entry]) => this.hasScrolledPastHero = !entry.isIntersecting,
      { threshold: 0 }
    );
    this.heroObserver.observe(this.heroRef.nativeElement);
  }

  ngOnDestroy(): void {
    this.heroObserver?.disconnect();
  }

  scrollTo(id: string) {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' });
  }
}