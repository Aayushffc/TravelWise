import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DealResponseDto } from '../../models/deal.model';
import { Router } from '@angular/router';

@Component({
  selector: 'app-manage-deal-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './manage-deal-card.component.html',
  styleUrls: ['./manage-deal-card.component.css']
})
export class ManageDealCardComponent {
  @Input() deal!: DealResponseDto;
  @Output() delete = new EventEmitter<number>();
  @Output() toggleStatus = new EventEmitter<{ id: number, isActive: boolean }>();

  constructor(private router: Router) {}

  onCardClick(): void {
    // Navigate to agency deal details (correct route based on app.routes.ts)
    this.router.navigate(['/agency/agency-deal-details', this.deal.id]);
  }

  onToggleStatus(event: Event): void {
    event.stopPropagation();

    // Ensure isActive is a boolean
    const currentStatus = this.deal.isActive === true;
    const newStatus = !currentStatus;

    // Ask for confirmation before toggling
    if (confirm(`Are you sure you want to ${currentStatus ? 'deactivate' : 'activate'} "${this.deal.title}"?`)) {
      this.toggleStatus.emit({ id: this.deal.id, isActive: newStatus });
    }
  }

  onDelete(event: Event): void {
    event.stopPropagation();

    // Ask for confirmation before deletion with warning about permanence
    if (confirm(`⚠️ Warning: This action is permanent and cannot be undone!\n\nAre you sure you want to delete "${this.deal.title}"?`)) {
      this.delete.emit(this.deal.id);
    }
  }

  formatDate(date: Date | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString();
  }

  getLocationName(): string {
    // Get the location name from the proper nested object
    if (this.deal.location && this.deal.location.name) {
      return this.deal.location.name;
    }

    return 'Unknown Location';
  }
}
