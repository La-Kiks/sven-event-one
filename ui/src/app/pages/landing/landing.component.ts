import { Component } from '@angular/core';
import { ButtonComponent } from '../../components/ui/button/button.component';
import { CardComponent } from '../../components/ui/card/card.component';
import { ScrollComponent } from '../../components/ui/scroll/scroll.component';


@Component({
  selector: 'app-landing',
  imports: [ButtonComponent, CardComponent, ScrollComponent],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss'
})
export class LandingComponent {

}
