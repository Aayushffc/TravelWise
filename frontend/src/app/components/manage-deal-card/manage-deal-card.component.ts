import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DealResponseDto } from '../../models/deal.model';
import { Router } from '@angular/router';
import { DealService } from '../../services/deal.service';
import { LocationService } from '../../services/location.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-manage-deal-card',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './manage-deal-card.component.html',
  styleUrls: ['./manage-deal-card.component.css']
})
export class ManageDealCardComponent implements OnInit {
  @Input() deal!: DealResponseDto;
  @Output() edit = new EventEmitter<number>();
  @Output() delete = new EventEmitter<{ id: number, title: string }>();
  @Output() toggleStatus = new EventEmitter<{ id: number, isActive: boolean }>();
  @Output() error = new EventEmitter<string>();

  locationName: string = 'Loading...';
  showDeletePopup = false;
  deleteConfirmationText = '';

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
    this.router.navigate(['/agency/agency-deal-details', this.deal.id]);
  }

  onToggleStatus(event: Event): void {
    event.stopPropagation();

    const currentStatus = this.deal.isActive === true;
    const newStatus = !currentStatus;

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

  onDelete(event: Event): void {
    event.stopPropagation();
    this.delete.emit({ id: this.deal.id, title: this.deal.title });
  }

  closeDeletePopup(): void {
    this.showDeletePopup = false;
    this.deleteConfirmationText = '';
  }

  confirmDelete(): void {
    if (this.deleteConfirmationText === 'Delete') {
      this.delete.emit({ id: this.deal.id, title: this.deal.title });
      this.closeDeletePopup();
    }
  }

  formatDate(date: Date | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString();
  }
}
