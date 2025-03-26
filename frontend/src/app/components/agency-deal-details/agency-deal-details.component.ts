import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { DealService } from '../../services/deal.service';
import { Deal, ItineraryDay, PackageOption, Policy } from '../../models/deal.model';
import { FormsModule } from '@angular/forms';
import { Location } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { FileUploadService } from '../../services/file-upload.service';

@Component({
  selector: 'app-agency-deal-details',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './agency-deal-details.component.html',
  styleUrls: ['./agency-deal-details.component.css']
})
export class AgencyDealDetailsComponent implements OnInit {
  deal: Deal | null = null;
  isLoading: boolean = true;
  error: string | null = null;
  success: string | null = null;
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
      const dealId = params['id'];
      if (dealId) {
        this.loadDeal(dealId);
      }
    });
  }

  loadDeal(id: number): void {
    this.isLoading = true;
    this.dealService.getDealById(id).subscribe({
      next: (deal) => {
        this.deal = {
          ...deal,
          itinerary: deal.itinerary || [],
          packageOptions: deal.packageOptions || [],
          policies: deal.policies || [],
          photos: deal.photos || []
        };
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading deal:', error);
        this.error = 'Failed to load deal details';
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
      // Reset any unsaved changes
      const dealId = this.deal?.id;
      if (dealId) {
        this.loadDeal(dealId);
      }
    }
  }

  // Photo Management
  async handleFileInput(event: any): Promise<void> {
    const files = event.target.files;
    if (!files || !this.deal) return;

    for (let i = 0; i < files.length; i++) {
      try {
        const response = await this.fileUploadService.uploadFile(files[i], 'deals').toPromise();
        if (response?.url) {
          if (!this.deal.photos) this.deal.photos = [];
          this.deal.photos.push(response.url);
        }
      } catch (error) {
        console.error('Error uploading file:', error);
        this.error = 'Error uploading photo';
      }
    }
  }

  removePhoto(index: number): void {
    if (!this.deal?.photos) return;

    this.deal.photos.splice(index, 1);
    if (this.currentImageIndex >= this.deal.photos.length) {
      this.currentImageIndex = Math.max(0, this.deal.photos.length - 1);
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

  saveDeal(): void {
    if (!this.deal || !this.currentUserId) return;

    // Calculate discount percentage
    const discountPercentage = this.calculateDiscountPercentage();
    const dealToUpdate = {
      ...this.deal,
      userId: this.currentUserId,
      discountPercentage
    };

    this.dealService.updateDeal(this.deal.id, dealToUpdate).subscribe({
      next: (updatedDeal) => {
        this.deal = updatedDeal;
        this.success = 'Deal updated successfully';
        this.isEditing = false;
        this.loadDeal(updatedDeal.id);
      },
      error: (error) => {
        console.error('Error updating deal:', error);
        this.error = 'Failed to update deal';
      }
    });
  }

  // Itinerary Management
  addItineraryDay(): void {
    if (!this.deal) return;

    if (!this.deal.itinerary) {
      this.deal.itinerary = [];
    }

    const newDay: ItineraryDay = {
      dayNumber: this.deal.itinerary.length + 1,
      title: '',
      description: '',
      activities: []
    };

    this.deal.itinerary.push(newDay);
  }

  removeItineraryDay(index: number): void {
    if (!this.deal?.itinerary) return;

    this.deal.itinerary.splice(index, 1);
    // Renumber days
    this.deal.itinerary.forEach((day, idx) => {
      day.dayNumber = idx + 1;
    });
  }

  trackByActivity(index: number, activity: string): number {
    return index;
  }

  trackByInclusion(index: number, inclusion: string): number {
    return index;
  }

  addActivity(day: ItineraryDay): void {
    if (!day.activities) {
      day.activities = [];
    }
    day.activities.push('');
  }

  removeActivity(day: ItineraryDay, index: number): void {
    if (!day.activities) return;
    day.activities.splice(index, 1);
  }

  handleActivityKeydown(event: KeyboardEvent, day: ItineraryDay, index: number): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      if (index === day.activities.length - 1) {
        this.addActivity(day);
      }
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

    const newOption: PackageOption = {
      name: '',
      description: '',
      price: 0,
      inclusions: []
    };

    this.deal.packageOptions.push(newOption);
  }

  removePackageOption(index: number): void {
    if (!this.deal?.packageOptions) return;
    this.deal.packageOptions.splice(index, 1);
  }

  addInclusion(option: PackageOption): void {
    if (!option.inclusions) {
      option.inclusions = [];
    }
    option.inclusions.push('');
  }

  removeInclusion(option: PackageOption, index: number): void {
    if (!option.inclusions) return;
    option.inclusions.splice(index, 1);
  }

  handleInclusionKeydown(event: KeyboardEvent, option: PackageOption, index: number): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      if (index === option.inclusions.length - 1) {
        this.addInclusion(option);
      }
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

    const newPolicy: Policy = {
      title: '',
      description: ''
    };

    this.deal.policies.push(newPolicy);
  }

  removePolicy(index: number): void {
    if (!this.deal?.policies) return;
    this.deal.policies.splice(index, 1);
  }

  goBack(): void {
    this.location.back();
  }

  handleImageError(event: any): void {
    event.target.src = 'https://travelwiseapp.s3.ap-south-1.amazonaws.com/Placeholder/placeholder-mountain.jpg';
  }
}
