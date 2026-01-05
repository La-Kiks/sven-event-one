import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SuccessComponent } from "../success/success.component";
import { ErrorComponent } from "../error/error.component";

type StatusType = 'none' | 'success' | 'error';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule, SuccessComponent, ErrorComponent],
  templateUrl: './modal.component.html',
  styleUrl: './modal.component.scss'
})
export class ModalComponent {
  @Input() status: StatusType = 'none'
  @Input() message: string = '';
  @Input() isVisible: boolean = false;
  @Output() close = new EventEmitter<void>();

  onClose(): void {
    this.close.emit();
  }

  onBackgroundClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onClose();
    }
  }
}
