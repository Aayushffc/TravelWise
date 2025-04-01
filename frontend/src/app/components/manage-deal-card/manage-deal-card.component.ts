import { Component, Input, Output, EventEmitter, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Deal } from '../../models/deal.model';

@Component({
  selector: 'app-manage-deal-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './manage-deal-card.component.html',
  styleUrls: ['./manage-deal-card.component.css']
})
export class ManageDealCardComponent {
  @Input() deal!: Deal;
  @Output() edit = new EventEmitter<Deal>();
  @Output() delete = new EventEmitter<number>();
  @Output() toggleStatus = new EventEmitter<{ id: number, isActive: boolean }>();
  @Output() view = new EventEmitter<Deal>();

  onEdit(event: Event): void {
    event.stopPropagation();
    this.edit.emit(this.deal);
  }

  onDelete(event: Event): void {
    event.stopPropagation();
    this.delete.emit(this.deal.id);
  }

  onToggleStatus(event: Event): void {
    event.stopPropagation();
    console.log("Current deal isActive:", this.deal.isActive);
    console.log("Current deal isActive type:", typeof this.deal.isActive);

    // Ensure isActive is a boolean
    const currentStatus = this.deal.isActive === true;
    const newStatus = !currentStatus;

    console.log("Current status (normalized):", currentStatus);
    console.log("New status to emit:", newStatus);

    this.toggleStatus.emit({ id: this.deal.id, isActive: newStatus });
  }

  onView(event: Event): void {
    event.stopPropagation();
    this.view.emit(this.deal);
  }

  formatDate(date: Date | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString();
  }
}
