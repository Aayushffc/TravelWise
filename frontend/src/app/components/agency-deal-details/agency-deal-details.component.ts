import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { DealService } from '../../services/deal.service';
import { FormsModule } from '@angular/forms';
import { Location } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { FileUploadService } from '../../services/file-upload.service';
import { DealResponseDto } from '../../models/deal.model';

// Use these interfaces for the internal components only
interface ItineraryDay {
  dayNumber: number;
  title: string;
  description: string;
  activities: string[];
}

interface PackageOption {
  name: string;
  description: string;
  price: number;
  inclusions: string[];
}

interface Policy {
  title: string;
  description: string;
}

@Component({
  selector: 'app-agency-deal-details',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './agency-deal-details.component.html',
  styleUrls: ['./agency-deal-details.component.css']
})
export class AgencyDealDetailsComponent implements OnInit {
  dealId: string = '';
  deal: DealResponseDto | null = null;
  isLoading: boolean = true;
  error: string = '';
  successMessage: string = '';
  currentImageIndex: number = 0;
  isEditing: boolean = false;
  packageTypes = ['STANDARD', 'PREMIUM', 'LUXURY'];
  selectedFiles: File[] = [];
  currentUserId: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private dealService: DealService,
    private location: Location,
    private authService: AuthService,
    private fileUploadService: FileUploadService
  ) {}

  ngOnInit(): void {
    this.currentUserId = this.authService.getCurrentUser()?.id || null;
    this.route.params.subscribe(params => {
      this.dealId = params['id'];
      this.loadDeal();
    });
  }

  loadDeal(): void {
    this.isLoading = true;
    this.dealService.getDealById(Number(this.dealId)).subscribe({
      next: (deal) => {
        this.deal = deal;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading deal:', error);
        if (error.status === 500) {
          this.error = 'A server error occurred while loading the deal. Please try again later.';
        } else if (error.status === 404) {
          this.error = 'Deal not found. It may have been removed.';
        } else {
          this.error = 'Failed to load deal details. Please try again.';
        }
        this.isLoading = false;
      }
    });
  }

  nextImage(): void {
    if (this.deal?.photos && this.currentImageIndex < this.deal.photos.length - 1) {
      this.currentImageIndex++;
    }
  }

  previousImage(): void {
    if (this.currentImageIndex > 0) {
      this.currentImageIndex--;
    }
  }

  selectImage(index: number): void {
    this.currentImageIndex = index;
  }

  toggleEdit(): void {
    this.isEditing = !this.isEditing;
    if (!this.isEditing) {
      this.loadDeal();
    }
  }

  saveDeal(): void {
    if (!this.deal) return;

    this.isLoading = true;
    this.dealService.updateDeal(Number(this.dealId), this.deal).subscribe({
      next: () => {
        this.successMessage = 'Deal updated successfully!';
        this.isEditing = false;
        this.isLoading = false;

        setTimeout(() => {
          this.successMessage = '';
        }, 3000);
      },
      error: (error) => {
        this.error = 'Failed to update deal. Please try again.';
        this.isLoading = false;
        console.error('Error updating deal:', error);

        setTimeout(() => {
          this.error = '';
        }, 3000);
      }
    });
  }

  goBack(): void {
    this.location.back();
  }

  handleFileInput(event: any): void {
    const files = event.target.files;
    if (files && this.deal) {
      for (let i = 0; i < files.length; i++) {
        const file = files[i];
        const reader = new FileReader();

        reader.onload = (e: any) => {
          if (this.deal && this.deal.photos) {
            this.deal.photos.push(e.target.result);
          } else if (this.deal) {
            this.deal.photos = [e.target.result];
          }
        };

        reader.readAsDataURL(file);
      }
    }
  }

  removePhoto(index: number): void {
    if (this.deal && this.deal.photos) {
      this.deal.photos.splice(index, 1);

      if (this.currentImageIndex >= this.deal.photos.length) {
        this.currentImageIndex = Math.max(0, this.deal.photos.length - 1);
      }
    }
  }

  calculateDiscountPercentage(): number {
    if (!this.deal) return 0;
    if (this.deal.price && this.deal.discountedPrice && this.deal.price > 0) {
      return Math.round(
        ((this.deal.price - this.deal.discountedPrice) / this.deal.price) * 100
      );
    }
    return 0;
  }

  // Itinerary Management
  addItineraryDay(): void {
    if (!this.deal) return;

    if (!this.deal.itinerary) {
      this.deal.itinerary = [];
    }

    const newDayNumber = this.deal.itinerary.length + 1;

    this.deal.itinerary.push({
      dayNumber: newDayNumber,
      title: `Day ${newDayNumber}`,
      description: '',
      activities: ['']
    });
  }

  removeItineraryDay(index: number): void {
    if (this.deal && this.deal.itinerary) {
      this.deal.itinerary.splice(index, 1);

      this.deal.itinerary.forEach((day, i) => {
        day.dayNumber = i + 1;
      });
    }
  }

  trackByActivity(index: number, activity: string): number {
    return index;
  }

  trackByInclusion(index: number, inclusion: string): number {
    return index;
  }

  addActivity(day: ItineraryDay): void {
    day.activities.push('');
  }

  removeActivity(day: ItineraryDay, index: number): void {
    day.activities.splice(index, 1);
  }

  handleActivityKeydown(event: KeyboardEvent, day: ItineraryDay, index: number): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.addActivity(day);

      setTimeout(() => {
        const inputs = document.querySelectorAll('input[placeholder="Activity"]');
        if (inputs && inputs.length > index + 1) {
          (inputs[index + 1] as HTMLInputElement).focus();
        }
      }, 0);
    }
  }

  handleActivityInput(event: Event, day: ItineraryDay, index: number): void {
    const input = event.target as HTMLInputElement;
    if (day.activities) {
      day.activities[index] = input.value;
    }
  }

  // Package Options Management
  addPackageOption(): void {
    if (!this.deal) return;

    if (!this.deal.packageOptions) {
      this.deal.packageOptions = [];
    }

    this.deal.packageOptions.push({
      name: '',
      description: '',
      price: 0,
      inclusions: ['']
    });
  }

  removePackageOption(index: number): void {
    if (this.deal && this.deal.packageOptions) {
      this.deal.packageOptions.splice(index, 1);
    }
  }

  addInclusion(option: PackageOption): void {
    option.inclusions.push('');
  }

  removeInclusion(option: PackageOption, index: number): void {
    option.inclusions.splice(index, 1);
  }

  handleInclusionKeydown(event: KeyboardEvent, option: PackageOption, index: number): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.addInclusion(option);

      setTimeout(() => {
        const inputs = document.querySelectorAll('input[placeholder="Inclusion"]');
        if (inputs && inputs.length > index + 1) {
          (inputs[index + 1] as HTMLInputElement).focus();
        }
      }, 0);
    }
  }

  handleInclusionInput(event: Event, option: PackageOption, index: number): void {
    const input = event.target as HTMLInputElement;
    if (option.inclusions) {
      option.inclusions[index] = input.value;
    }
  }

  // Policy Management
  addPolicy(): void {
    if (!this.deal) return;

    if (!this.deal.policies) {
      this.deal.policies = [];
    }

    this.deal.policies.push({
      title: '',
      description: ''
    });
  }

  removePolicy(index: number): void {
    if (this.deal && this.deal.policies) {
      this.deal.policies.splice(index, 1);
    }
  }

  handleImageError(event: any): void {
    event.target.src = 'assets/images/placeholder.jpg';
  }

  getLocationName(): string {
    if (!this.deal) return '';
    if (typeof this.deal.location === 'string') {
      return this.deal.location;
    } else if (this.deal.location && typeof this.deal.location === 'object') {
      return this.deal.location.name || '';
    }
    return '';
  }
}
