import { Component, Input } from '@angular/core';
import { RouterLink } from "@angular/router";
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-button',
  imports: [RouterLink, CommonModule],
  templateUrl: './button.component.html',
  styleUrl: './button.component.scss'
})
export class ButtonComponent {
  @Input() link = "";
  @Input() label = "";
  @Input() isFull = false;
  @Input() small = false;
  @Input() fullLabel = "Inscriptions complètes";
  @Input() fullLink = "";
}