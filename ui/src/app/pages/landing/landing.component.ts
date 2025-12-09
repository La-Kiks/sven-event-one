import { Component } from '@angular/core';
import { ButtonComponent } from '../../components/ui/button/button.component';
import { CardComponent } from '../../components/ui/card/card.component';


@Component({
  selector: 'app-landing',
  imports: [ButtonComponent, CardComponent],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss'
})
export class LandingComponent {

}
