import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DealResponseDto } from '../../models/deal.model';
import { Router } from '@angular/router';
import { DealService } from '../../services/deal.service';
import { LocationService } from '../../services/location.service';

@Component({
  selector: 'app-manage-deal-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './manage-deal-card.component.html',
  styleUrls: ['./manage-deal-card.component.css']
})
export class ManageDealCardComponent implements OnInit {
  @Input() deal!: DealResponseDto;
  @Output() delete = new EventEmitter<number>();
  @Output() toggleStatus = new EventEmitter<{ id: number, isActive: boolean }>();
  @Output() error = new EventEmitter<string>();

  locationName: string = 'Loading...';

  constructor(
    private router: Router,
    private dealService: DealService,
    private locationService: LocationService
  ) {}

  ngOnInit(): void {
    this.loadLocationName();
  }

  private loadLocationName(): void {
    if (this.deal.locationId) {
      this.locationService.getLocationById(this.deal.locationId).subscribe({
        next: (location) => {
          this.locationName = location.name || 'Location not specified';
        },
        error: (error) => {
          console.error('Error loading location:', error);
          this.locationName = 'Location not specified';
        }
      });
    }
  }

  getLocationName(): string {
    return this.locationName;
  }

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
      this.dealService.toggleDealStatus(this.deal.id, newStatus)
        .subscribe({
          next: () => {
            this.toggleStatus.emit({ id: this.deal.id, isActive: newStatus });
          },
          error: (err) => {
            console.error('Error toggling status:', err);
            this.error.emit(`Failed to ${currentStatus ? 'deactivate' : 'activate'} deal. Server error occurred.`);
          }
        });
    }
  }

  onDelete(event: Event): void {
    event.stopPropagation();

    // Ask for confirmation before deletion with warning about permanence
    if (confirm(`⚠️ Warning: This action is permanent and cannot be undone!\n\nAre you sure you want to delete "${this.deal.title}"?`)) {
      this.dealService.deleteDeal(this.deal.id)
        .subscribe({
          next: () => {
            this.delete.emit(this.deal.id);
          },
          error: (err) => {
            console.error('Error deleting deal:', err);
            this.error.emit('Failed to delete deal. Server error occurred.');
          }
        });
    }
  }

  formatDate(date: Date | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString();
  }
}
