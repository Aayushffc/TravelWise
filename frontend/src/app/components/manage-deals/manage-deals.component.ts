import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DealService } from '../../services/deal.service';
import { LocationService } from '../../services/location.service';
import { Router } from '@angular/router';
import { Location as NgLocation } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { FileUploadService } from '../../services/file-upload.service';

interface Location {
  id: number;
  name: string;
  description?: string;
  imageUrl?: string;
  isPopular: boolean;
  isActive: boolean;
  clickCount: number;
  requestCallCount: number;
}

interface Deal {
  id: number;
  title: string;
  description: string;
  price: number;
  discountedPrice: number;
  discountPercentage: number;
  rating: number;
  daysCount: number;
  nightsCount: number;
  startPoint: string;
  endPoint: string;
  duration: string;
  locationId: number;
  location?: Location;
  photos: string[];
  packageType: string;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
  userId: string;

  // Facilities
  elderlyFriendly: boolean;
  internetIncluded: boolean;
  travelIncluded: boolean;
  mealsIncluded: boolean;
  sightseeingIncluded: boolean;
  stayIncluded: boolean;
  airTransfer: boolean;
  roadTransfer: boolean;
  trainTransfer: boolean;
  travelCostIncluded: boolean;
  guideIncluded: boolean;
  photographyIncluded: boolean;
  insuranceIncluded: boolean;
  visaIncluded: boolean;

  // Additional details
  itinerary: any[];
  packageOptions: any[];
  mapUrl: string;
  policies: any[];
}

@Component({
  selector: 'app-manage-deals',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './manage-deals.component.html',
  styles: [`
    .photo-upload-preview {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
      gap: 1rem;
      margin-top: 1rem;
    }
    .photo-preview-item {
      position: relative;
      aspect-ratio: 1;
      overflow: hidden;
      border-radius: 0.5rem;
      border: 2px solid #e5e7eb;
    }
    .photo-preview-item img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }
    .photo-preview-item button {
      position: absolute;
      top: 0.25rem;
      right: 0.25rem;
      padding: 0.25rem;
      background-color: rgba(239, 68, 68, 0.9);
      border-radius: 9999px;
      color: white;
      transition: all 0.2s;
    }
    .photo-preview-item button:hover {
      background-color: rgba(220, 38, 38, 0.9);
    }
    .drop-zone {
      border: 2px dashed #e5e7eb;
      border-radius: 0.5rem;
      padding: 2rem;
      text-align: center;
      transition: all 0.2s;
      cursor: pointer;
    }
    .drop-zone.drag-over {
      border-color: #6366f1;
      background-color: rgba(99, 102, 241, 0.05);
    }
  `]
})
export class ManageDealsComponent implements OnInit {
  @ViewChild('fileInput') fileInput!: ElementRef;

  deals: Deal[] = [];
  locations: Location[] = [];
  isLoading: boolean = true;
  error: string | null = null;
  success: string | null = null;
  showCreateModal: boolean = false;
  showEditModal: boolean = false;
  showLocationModal: boolean = false;
  selectedDeal: Deal | null = null;
  newLocation: { name: string } = { name: '' };
  currentUserId: string | null = null;
  uploadProgress: number = 0;
  maxPhotos: number = 10;
  maxFileSize: number = 10 * 1024 * 1024; // 10MB

  selectedFiles: File[] = [];
  photoPreviewUrls: string[] = [];

  dealForm: Partial<Deal> = {
    title: '',
    description: '',
    price: 0,
    discountedPrice: 0,
    discountPercentage: 0,
    rating: 0,
    daysCount: 0,
    nightsCount: 0,
    startPoint: '',
    endPoint: '',
    duration: '',
    locationId: 0,
    photos: [],
    packageType: '',
    isActive: true,

    // Facilities
    elderlyFriendly: false,
    internetIncluded: false,
    travelIncluded: false,
    mealsIncluded: false,
    sightseeingIncluded: false,
    stayIncluded: false,
    airTransfer: false,
    roadTransfer: false,
    trainTransfer: false,
    travelCostIncluded: false,
    guideIncluded: false,
    photographyIncluded: false,
    insuranceIncluded: false,
    visaIncluded: false,

    // Additional details
    itinerary: [],
    packageOptions: [],
    mapUrl: '',
    policies: []
  };

  constructor(
    private dealService: DealService,
    private locationService: LocationService,
    private router: Router,
    private location: NgLocation,
    private authService: AuthService,
    private fileUploadService: FileUploadService
  ) {}

  ngOnInit(): void {
    this.loadDeals();
    this.loadLocations();
    this.getCurrentUserId();
  }

  private getCurrentUserId(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.currentUserId = user.id;
    } else {
      this.error = 'User not authenticated';
      this.router.navigate(['/login']);
    }
  }

  loadDeals(): void {
    this.isLoading = true;
    this.dealService.getDeals().subscribe({
      next: (deals) => {
        this.deals = deals;
        this.isLoading = false;
      },
      error: (error) => {
        this.error = 'Failed to load deals';
        this.isLoading = false;
        console.error('Load deals error:', error);
      }
    });
  }

  loadLocations(): void {
    this.locationService.getLocations().subscribe({
      next: (locations) => {
        this.locations = locations;
      },
      error: (error) => {
        this.error = 'Failed to load locations';
        console.error('Load locations error:', error);
      }
    });
  }

  openCreateModal(): void {
    this.dealForm = {
      title: '',
      description: '',
      price: 0,
      locationId: 0,
      duration: '',
      packageType: '',
      isActive: true,
      photos: []
    };
    this.showCreateModal = true;
  }

  openEditModal(deal: Deal): void {
    this.selectedDeal = deal;
    this.dealForm = { ...deal };
    this.showEditModal = true;
  }

  openLocationModal(): void {
    this.newLocation = { name: '' };
    this.showLocationModal = true;
  }

  closeModals(): void {
    this.showCreateModal = false;
    this.showEditModal = false;
    this.showLocationModal = false;
    this.selectedDeal = null;
    this.newLocation = { name: '' };
    this.selectedFiles = [];
    this.photoPreviewUrls = [];
    this.uploadProgress = 0;
  }

  goBack(): void {
    this.location.back();
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    const dropZone = event.target as HTMLElement;
    dropZone.classList.add('drag-over');
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    const dropZone = event.target as HTMLElement;
    dropZone.classList.remove('drag-over');
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    const dropZone = event.target as HTMLElement;
    dropZone.classList.remove('drag-over');

    const files = event.dataTransfer?.files;
    if (files) {
      this.handleFiles(event);
    }
  }

  openFileInput(): void {
    this.fileInput.nativeElement.click();
  }

  handleFiles(event: any): void {
    const files = event.target.files;
    if (!files) return;

    // Check if adding new files would exceed the limit
    if (this.selectedFiles.length + files.length > this.maxPhotos) {
      this.error = `You can only upload up to ${this.maxPhotos} photos`;
      return;
    }

    // Process each file
    Array.from(files).forEach((file: any) => {
      // Validate file type
      if (!['image/jpeg', 'image/png', 'image/gif'].includes(file.type)) {
        this.error = 'Only JPEG, PNG and GIF files are allowed';
        return;
      }

      // Validate file size (5MB)
      if (file.size > 5 * 1024 * 1024) {
        this.error = 'File size should not exceed 5MB';
        return;
      }

      // Add to selected files
      this.selectedFiles.push(file);

      // Create preview URL
      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.photoPreviewUrls.push(e.target.result);
      };
      reader.readAsDataURL(file);
    });

    // Reset input
    event.target.value = '';
  }

  removePhoto(index: number): void {
    this.selectedFiles.splice(index, 1);
    this.photoPreviewUrls.splice(index, 1);
  }

  async uploadPhotos(): Promise<string[]> {
    if (!this.selectedFiles.length) return [];

    const uploadedUrls: string[] = [];
    let totalProgress = 0;

    for (let i = 0; i < this.selectedFiles.length; i++) {
      try {
        const response = await this.fileUploadService.uploadFile(this.selectedFiles[i], 'deals').toPromise();
        if (response?.url) {
          uploadedUrls.push(response.url);
          totalProgress = ((i + 1) / this.selectedFiles.length) * 100;
          this.uploadProgress = Math.round(totalProgress);
        }
      } catch (error) {
        console.error('Error uploading file:', error);
        this.error = 'Error uploading photos. Please try again.';
        return [];
      }
    }

    return uploadedUrls;
  }

  async createDeal(): Promise<void> {
    try {
      if (!this.currentUserId) {
        this.error = 'User not authenticated';
        this.router.navigate(['/login']);
        return;
      }

      if (!this.validateForm()) {
        return;
      }

      // First upload all photos
      const uploadedUrls: string[] = [];
      let totalProgress = 0;

      // Upload photos one by one
      for (let i = 0; i < this.selectedFiles.length; i++) {
        try {
          const response = await this.fileUploadService.uploadFile(this.selectedFiles[i], 'deals').toPromise();
          if (!response?.url) {
            throw new Error('Failed to get upload URL');
          }
          uploadedUrls.push(response.url);
          totalProgress = ((i + 1) / this.selectedFiles.length) * 100;
          this.uploadProgress = Math.round(totalProgress);
        } catch (error) {
          console.error('Error uploading file:', error);
          this.error = 'Error uploading photos. Please try again.';
          return;
        }
      }

      // Create a properly structured deal object
      const dealToCreate = {
        title: this.dealForm.title,
        description: this.dealForm.description,
        price: this.dealForm.price,
        discountedPrice: this.dealForm.discountedPrice,
        discountPercentage: this.dealForm.discountPercentage,
        rating: this.dealForm.rating || 0,
        daysCount: this.dealForm.daysCount,
        nightsCount: this.dealForm.nightsCount,
        startPoint: this.dealForm.startPoint,
        endPoint: this.dealForm.endPoint,
        duration: this.dealForm.duration,
        locationId: this.dealForm.locationId,
        photos: uploadedUrls,
        packageType: this.dealForm.packageType,
        isActive: this.dealForm.isActive ?? true,
        userId: this.currentUserId,

        // Facilities
        elderlyFriendly: this.dealForm.elderlyFriendly ?? false,
        internetIncluded: this.dealForm.internetIncluded ?? false,
        travelIncluded: this.dealForm.travelIncluded ?? false,
        mealsIncluded: this.dealForm.mealsIncluded ?? false,
        sightseeingIncluded: this.dealForm.sightseeingIncluded ?? false,
        stayIncluded: this.dealForm.stayIncluded ?? false,
        airTransfer: this.dealForm.airTransfer ?? false,
        roadTransfer: this.dealForm.roadTransfer ?? false,
        trainTransfer: this.dealForm.trainTransfer ?? false,
        travelCostIncluded: this.dealForm.travelCostIncluded ?? false,
        guideIncluded: this.dealForm.guideIncluded ?? false,
        photographyIncluded: this.dealForm.photographyIncluded ?? false,
        insuranceIncluded: this.dealForm.insuranceIncluded ?? false,
        visaIncluded: this.dealForm.visaIncluded ?? false,

        // Additional details
        itinerary: this.dealForm.itinerary ?? [],
        packageOptions: this.dealForm.packageOptions ?? [],
        mapUrl: this.dealForm.mapUrl ?? '',
        policies: this.dealForm.policies ?? []
      };

      // Create the deal with the structured data
      const dealResponse = await this.dealService.createDeal(dealToCreate).toPromise();
      if (!dealResponse) {
        throw new Error('Failed to create deal');
      }

      // Success handling
      this.deals.push(dealResponse);
      this.closeModals();
      this.success = 'Deal created successfully!';
      this.loadDeals(); // Refresh the deals list

      // Reset file-related state
      this.selectedFiles = [];
      this.photoPreviewUrls = [];
      this.uploadProgress = 0;
    } catch (error) {
      console.error('Error creating deal:', error);
      this.error = 'Error creating deal. Please try again.';
    }
  }

  private validateForm(): boolean {
    if (!this.dealForm.title ||
        !this.dealForm.locationId ||
        !this.dealForm.price ||
        !this.dealForm.discountedPrice ||
        !this.dealForm.daysCount ||
        !this.dealForm.nightsCount ||
        !this.dealForm.startPoint ||
        !this.dealForm.endPoint ||
        !this.dealForm.packageType ||
        !this.dealForm.description ||
        this.selectedFiles.length === 0) {
      this.error = 'Please fill in all required fields and select at least one photo';
      return false;
    }

    if (this.dealForm.price && this.dealForm.discountedPrice && this.dealForm.price > 0) {
      this.dealForm.discountPercentage = Math.round(
        ((this.dealForm.price - this.dealForm.discountedPrice) / this.dealForm.price) * 100
      );
    }

    return true;
  }

  updateDeal(): void {
    if (!this.selectedDeal?.id || !this.dealForm.title || !this.dealForm.locationId || !this.dealForm.price ||
        !this.dealForm.discountedPrice || !this.dealForm.daysCount || !this.dealForm.nightsCount ||
        !this.dealForm.startPoint || !this.dealForm.endPoint || !this.dealForm.packageType ||
        !this.dealForm.description || !this.dealForm.photos || this.dealForm.photos.length === 0) {
      this.error = 'Please fill in all required fields and upload at least one photo';
      return;
    }

    // Calculate discount percentage
    this.dealForm.discountPercentage = Math.round(
      ((this.dealForm.price - this.dealForm.discountedPrice) / this.dealForm.price) * 100
    );

    if (this.currentUserId) {
      this.dealForm.userId = this.currentUserId;
      this.dealService.updateDeal(this.selectedDeal.id, this.dealForm as any).subscribe({
        next: () => {
          this.success = 'Deal updated successfully';
          this.closeModals();
          this.loadDeals();
        },
        error: (error) => {
          this.error = 'Failed to update deal';
          console.error('Update deal error:', error);
        }
      });
    }
  }

  deleteDeal(id: number): void {
    if (confirm('Are you sure you want to delete this deal?')) {
      this.dealService.deleteDeal(id).subscribe({
        next: () => {
          this.success = 'Deal deleted successfully';
          this.loadDeals();
        },
        error: (error) => {
          this.error = 'Failed to delete deal';
        }
      });
    }
  }

  createLocation(): void {
    if (!this.newLocation.name) {
      this.error = 'Please enter a location name';
      return;
    }

    this.locationService.createLocation({
      name: this.newLocation.name,
      isPopular: false,
      isActive: true
    }).subscribe({
      next: () => {
        this.success = 'Location created successfully';
        this.closeModals();
        this.loadLocations();
      },
      error: (error) => {
        this.error = 'Failed to create location';
      }
    });
  }

  getLocationName(locationId: number): string {
    const location = this.locations.find(l => l.id === locationId);
    return location ? location.name : 'Unknown Location';
  }
}
