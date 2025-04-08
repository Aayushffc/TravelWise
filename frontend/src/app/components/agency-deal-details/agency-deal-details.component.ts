import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { DealService } from '../../services/deal.service';
import { FormsModule } from '@angular/forms';
import { Location } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { DealResponseDto, ItineraryDay, PackageOption, Policy } from '../../models/deal.model';
import { DragDropModule, CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { LocationService } from '../../services/location.service';

@Component({
  selector: 'app-agency-deal-details',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, DragDropModule],
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
  packageTypes = ['Adventure',
    'Luxury',
    'Jungle Safari',
    'Standard',
    'Family Friendly',
    'Cultural',
    'Beach',
    'Mountain',
    'City Tour',
    'Wildlife',
    'Pilgrimage',
    'Honeymoon'];
  selectedFiles: File[] = [];
  currentUserId: string | null = null;
  isImageSliding: boolean = false;
  slideDirection: 'left' | 'right' = 'left';
  isDragging: boolean = false;
  difficultyLevels = ['EASY', 'MODERATE', 'CHALLENGING', 'DIFFICULT'];
  seasons = ['SPRING', 'SUMMER', 'AUTUMN', 'WINTER', 'ALL SEASONS'];
  locations: any[] = [];

  constructor(
    private route: ActivatedRoute,
    private dealService: DealService,
    private location: Location,
    private authService: AuthService,
    private locationService: LocationService
  ) {}

  ngOnInit(): void {
    this.currentUserId = this.authService.getCurrentUser()?.id || null;
    this.loadLocations();
    this.route.params.subscribe(params => {
      this.dealId = params['id'];
      this.loadDeal();
    });
  }

  loadLocations(): void {
    this.locationService.getLocations().subscribe({
      next: (locations) => {
        this.locations = locations;
      },
      error: (error) => {
        console.error('Error loading locations:', error);
      }
    });
  }

  loadDeal(): void {
    this.isLoading = true;
    this.dealService.getDealById(Number(this.dealId)).subscribe({
      next: (deal) => {
        this.deal = deal;
        if (this.deal) {
          // Initialize arrays if they don't exist
          this.deal.photos = this.deal.photos || [];
          this.deal.tags = this.deal.tags || [];
          this.deal.seasons = this.deal.seasons || [];
          this.deal.languages = this.deal.languages || [];
          this.deal.requirements = this.deal.requirements || [];
          this.deal.restrictions = this.deal.restrictions || [];
          this.deal.packageOptions = this.deal.packageOptions || [];
          this.deal.policies = this.deal.policies || [];

          // Load location details if we have a locationId
          if (this.deal.locationId) {
            this.locationService.getLocationById(this.deal.locationId).subscribe({
              next: (location) => {
                if (this.deal) {
                  this.deal.location = location;
                }
                this.isLoading = false;
              },
              error: (error) => {
                console.error('Error loading location:', error);
                this.isLoading = false;
              }
            });
          } else {
            this.isLoading = false;
          }
        } else {
          this.isLoading = false;
        }
      },
      error: (error) => {
        console.error('Error loading deal:', error);
        this.error = 'Failed to load deal details. Please try again.';
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

    // Ensure all arrays are initialized
    this.deal.tags = this.deal.tags || [];
    this.deal.seasons = this.deal.seasons || [];
    this.deal.languages = this.deal.languages || [];
    this.deal.requirements = this.deal.requirements || [];
    this.deal.restrictions = this.deal.restrictions || [];

    // Remove any empty strings from arrays
    this.deal.tags = this.deal.tags.filter(t => t.trim());
    this.deal.seasons = this.deal.seasons.filter(s => s.trim());
    this.deal.languages = this.deal.languages.filter(l => l.trim());
    this.deal.requirements = this.deal.requirements.filter(r => r.trim());
    this.deal.restrictions = this.deal.restrictions.filter(r => r.trim());

    // Update the deal's timestamp
    this.deal.updatedAt = new Date();

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
        console.error('Error updating deal:', error);
        this.error = 'Failed to update deal. Please try again.';
        this.isLoading = false;

        setTimeout(() => {
          this.error = '';
        }, 3000);
      }
    });
  }

  goBack(): void {
    this.location.back();
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.handleFiles(Array.from(files));
    }
  }

  handleFileInput(event: Event): void {
    const element = event.target as HTMLInputElement;
    const files = element.files;
    if (files && files.length > 0) {
      this.handleFiles(Array.from(files));
    }
  }

  private handleFiles(files: File[]): void {
    const imageFiles = files.filter(file => file.type.startsWith('image/'));

    if (!this.deal) return;
    if (!this.deal.photos) {
      this.deal.photos = [];
    }

    imageFiles.forEach(file => {
      const reader = new FileReader();
      reader.onload = (e: ProgressEvent<FileReader>) => {
        if (e.target?.result && this.deal) {
          this.deal.photos?.push(e.target.result as string);
        }
      };
      reader.readAsDataURL(file);
    });
  }

  removePhoto(index: number): void {
    if (this.deal?.photos) {
      this.deal.photos = this.deal.photos.filter((_, i) => i !== index);
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

    const newDay: ItineraryDay = {
      dayNumber: this.deal.itinerary.length + 1,
      title: `Day ${this.deal.itinerary.length + 1}`,
      description: '',
      activities: []
    };

    this.deal.itinerary.push(newDay);
  }

  removeItineraryDay(index: number): void {
    if (!this.deal?.itinerary) return;
    this.deal.itinerary.splice(index, 1);
    // Renumber days
    this.deal.itinerary.forEach((day, i) => {
      day.dayNumber = i + 1;
      day.title = `Day ${i + 1}`;
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

  // Tags Management
  addTag(): void {
    if (!this.deal) return;
    if (!this.deal.tags) {
      this.deal.tags = [];
    }
    this.deal.tags.push('');
  }

  removeTag(index: number): void {
    if (this.deal?.tags) {
      this.deal.tags.splice(index, 1);
    }
  }

  handleTagInput(event: Event, index: number): void {
    const input = event.target as HTMLInputElement;
    if (this.deal?.tags) {
      this.deal.tags[index] = input.value;
    }
  }

  // Languages Management
  addLanguage(): void {
    if (!this.deal) return;
    if (!this.deal.languages) {
      this.deal.languages = [];
    }
    this.deal.languages.push('');
  }

  removeLanguage(index: number): void {
    if (this.deal?.languages) {
      this.deal.languages.splice(index, 1);
    }
  }

  handleLanguageInput(event: Event, index: number): void {
    const input = event.target as HTMLInputElement;
    if (this.deal?.languages) {
      this.deal.languages[index] = input.value;
    }
  }

  // Requirements Management
  addRequirement(): void {
    if (!this.deal) return;
    if (!this.deal.requirements) {
      this.deal.requirements = [];
    }
    this.deal.requirements.push('');
  }

  removeRequirement(index: number): void {
    if (this.deal?.requirements) {
      this.deal.requirements.splice(index, 1);
    }
  }

  handleRequirementInput(event: Event, index: number): void {
    const input = event.target as HTMLInputElement;
    if (this.deal?.requirements) {
      this.deal.requirements[index] = input.value;
    }
  }

  // Restrictions Management
  addRestriction(): void {
    if (!this.deal) return;
    if (!this.deal.restrictions) {
      this.deal.restrictions = [];
    }
    this.deal.restrictions.push('');
  }

  removeRestriction(index: number): void {
    if (this.deal?.restrictions) {
      this.deal.restrictions.splice(index, 1);
    }
  }

  handleRestrictionInput(event: Event, index: number): void {
    const input = event.target as HTMLInputElement;
    if (this.deal?.restrictions) {
      this.deal.restrictions[index] = input.value;
    }
  }

  // Add a method to format dates for the template
  formatDate(date: Date | string | undefined): string {
    if (!date) return '';
    return new Date(date).toISOString().split('T')[0];
  }

  // Add a method to validate map URL
  validateMapUrl(url: string): boolean {
    try {
      new URL(url);
      return true;
    } catch {
      return false;
    }
  }

  onPhotoReorder(event: CdkDragDrop<string[]>): void {
    if (!this.deal) return;
    if (!this.deal.photos) {
      this.deal.photos = [];
    }
    moveItemInArray(this.deal.photos, event.previousIndex, event.currentIndex);
    if (this.currentImageIndex === event.previousIndex) {
      this.currentImageIndex = event.currentIndex;
    } else if (this.currentImageIndex === event.currentIndex) {
      this.currentImageIndex = event.previousIndex;
    }
  }

  // Update price calculation
  updateDiscountedPrice(): void {
    if (!this.deal) return;
    if (this.deal.price && this.deal.discountPercentage) {
      this.deal.discountedPrice = this.deal.price * (1 - this.deal.discountPercentage / 100);
    }
  }

  // Seasons Management
  addSeason(): void {
    if (!this.deal) return;
    if (!this.deal.seasons) {
      this.deal.seasons = [];
    }
    this.deal.seasons.push('SPRING');
  }

  removeSeason(index: number): void {
    if (this.deal?.seasons) {
      this.deal.seasons.splice(index, 1);
    }
  }

  handleSeasonInput(event: Event, index: number): void {
    const input = event.target as HTMLInputElement;
    if (this.deal?.seasons) {
      this.deal.seasons[index] = input.value;
    }
  }
}
