import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { DealResponseDto, DealCreateDto, DealUpdateDto, DealToggleStatusDto } from '../../models/deal.model';
import { Location as AngularLocation } from '@angular/common';
import { Location } from '../../models/location.model';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { DealService } from '../../services/deal.service';
import { tap } from 'rxjs/operators';
import { ManageDealCardComponent } from '../manage-deal-card/manage-deal-card.component';
import { trigger, transition, style, animate } from '@angular/animations';
import { FileUploadService } from '../../services/file-upload.service';
import { DeleteConfirmationComponent } from '../delete-confirmation/delete-confirmation.component';

@Component({
  selector: 'app-manage-deals',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ManageDealCardComponent,
    DeleteConfirmationComponent
  ],
  templateUrl: './manage-deals.component.html',
  styleUrls: ['./manage-deals.component.css', './manage-deals-css.component.css'],
  animations: [
    trigger('fadeInOut', [
      transition(':enter', [
        style({ opacity: 0, height: 0 }),
        animate('300ms ease-out', style({ opacity: 1, height: '*' }))
      ]),
      transition(':leave', [
        style({ opacity: 1, height: '*' }),
        animate('300ms ease-in', style({ opacity: 0, height: 0 }))
      ])
    ])
  ]
})
export class ManageDealsComponent implements OnInit {
  deals: DealResponseDto[] = [];
  isLoading = false;
  error: string | null = null;
  formError: string | null = null; // Error for the form
  locations: Location[] = [];
  selectedStatus: string | null = null;
  selectedLocation: number | null = null;
  searchTerm: string = '';
  isActiveFilter: boolean | null = null;
  dateRange: { start: Date | null; end: Date | null } = {
    start: null,
    end: null
  };
  dealForm: FormGroup;
  showModal = false; // Renamed from isFormVisible
  selectedDeal: DealResponseDto | null = null;
  isEditMode = false; // Renamed from isEditing
  editingDealId: number | null = null;
  showFilters = false;
  stats = {
    total: 0,
    active: 0,
    inactive: 0
  };
  photos: any[] = [];
  isDraggingOver = false;
  isSubmitting = false;
  activeTab = 'basic'; // For tabbed form navigation
  private fileUploadService: FileUploadService;
  showDeleteConfirmation = false;
  dealToDelete: { id: number, title: string } | null = null;

  // Categories for the form
  categories: string[] = [
    'Adventure',
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
    'Honeymoon'
  ];

  constructor(
    private http: HttpClient,
    private router: Router,
    private authService: AuthService,
    private fb: FormBuilder,
    private dealService: DealService,
    private location: AngularLocation,
    fileUploadService: FileUploadService
  ) {
    this.dealForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(3)]],
      locationId: [null, Validators.required],
      price: [0, [Validators.required, Validators.min(0)]],
      discountPercentage: [0, [Validators.min(0), Validators.max(100)]],
      description: ['', Validators.required],
      photos: [[]],
      packageType: ['', Validators.required],
      isActive: [true],
      tags: [[]],
      isInstantBooking: [false],
      isLastMinuteDeal: [false],
      daysCount: [1, [Validators.required, Validators.min(1)]],
      nightsCount: [0, [Validators.required, Validators.min(0)]],
      elderlyFriendly: [false],
      internetIncluded: [false],
      travelIncluded: [false],
      mealsIncluded: [false],
      sightseeingIncluded: [false],
      stayIncluded: [false],
      airTransfer: [false],
      roadTransfer: [false],
      trainTransfer: [false],
      travelCostIncluded: [false],
      guideIncluded: [false],
      photographyIncluded: [false],
      insuranceIncluded: [false],
      visaIncluded: [false],
      difficultyLevel: ['Easy'],
      maxGroupSize: [10, [Validators.min(1)]],
      minGroupSize: [1, [Validators.min(1)]],
      validFrom: [new Date().toISOString().split('T')[0], Validators.required],
      validUntil: [new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toISOString().split('T')[0], Validators.required]
    });
    this.fileUploadService = fileUploadService;
  }

  ngOnInit(): void {
    this.loadData();
    this.loadLocations();
  }

  loadData(): void {
    this.isLoading = true;
    this.error = null;

    const currentUser = this.authService.getCurrentUser();
    if (!currentUser || !currentUser.id) {
      this.error = 'User not authenticated. Please log in again.';
      this.isLoading = false;
      return;
    }

    this.dealService.getDealsByUserId(currentUser.id)
      .pipe(
        tap(deals => {
          this.updateStats(deals);
          this.deals = deals;
          this.isLoading = false;
        })
      )
      .subscribe({
        next: () => {},
        error: (err) => {
          console.error('Failed to load deals:', err);
          if (err.status === 500) {
            this.error = 'Server error occurred. The team has been notified.';
          } else if (err.status === 401 || err.status === 403) {
            this.error = 'You are not authorized to view these deals. Please log in again.';
          } else if (err.status === 404) {
            this.error = 'No deals found for your account.';
          } else {
            this.error = 'Failed to load deals. Please try again later.';
          }
          this.isLoading = false;
          this.deals = [];
        }
      });
  }

  loadLocations(): void {
    this.http.get<Location[]>(`${environment.apiUrl}/api/Location`)
      .subscribe({
        next: (locations) => {
          this.locations = locations;
        },
        error: (err) => {
          console.error('Failed to load locations:', err);
        }
      });
  }

  updateStats(deals: DealResponseDto[]): void {
    this.stats = {
      total: deals.length,
      active: deals.filter(d => d.isActive).length,
      inactive: deals.filter(d => !d.isActive).length
    };
  }

  applyFilter(): void {
    let filteredData = [...this.deals];

    if (this.selectedLocation) {
      filteredData = filteredData.filter(deal => deal.locationId === this.selectedLocation);
    }

    if (this.searchTerm) {
      const searchLower = this.searchTerm.toLowerCase();
      filteredData = filteredData.filter(deal =>
        deal.title.toLowerCase().includes(searchLower) ||
        (deal.description?.toLowerCase() || '').includes(searchLower)
      );
    }

    if (this.isActiveFilter !== null) {
      filteredData = filteredData.filter(deal => deal.isActive === this.isActiveFilter);
    }

    if (this.dateRange.start) {
      filteredData = filteredData.filter(deal => new Date(deal.createdAt) >= this.dateRange.start!);
    }

    if (this.dateRange.end) {
      filteredData = filteredData.filter(deal => new Date(deal.createdAt) <= this.dateRange.end!);
    }

    this.deals = filteredData;
    this.updateStats(filteredData);
  }

  resetFilters(): void {
    this.selectedStatus = null;
    this.selectedLocation = null;
    this.searchTerm = '';
    this.isActiveFilter = null;
    this.dateRange = { start: null, end: null };
    this.loadData();
  }

  toggleDealStatus(deal: DealResponseDto): void {
    const toggleDto: DealToggleStatusDto = {
      isActive: !deal.isActive
    };

    this.dealService.toggleDealStatus(deal.id, !deal.isActive)
      .subscribe({
        next: () => {
          deal.isActive = !deal.isActive;
          this.updateStats(this.deals);
        },
        error: (err) => {
          console.error('Failed to toggle deal status:', err);
        }
      });
  }

  editDeal(deal: DealResponseDto): void {
    this.selectedDeal = deal;
    this.isEditMode = true;
    this.showModal = true;
    this.photos = deal.photos || [];
    this.activeTab = 'basic';
    this.formError = null;

    this.dealForm.patchValue({
      title: deal.title,
      description: deal.description,
      price: deal.price,
      discountPercentage: deal.discountPercentage || 0,
      category: deal.packageType || '',
      isActive: deal.isActive,
      tags: deal.tags || [],
      isInstantBooking: (deal as any).isInstantBooking || false,
      isLastMinuteDeal: (deal as any).isLastMinuteDeal || false,
      photos: deal.photos || []
    });
  }

  createDeal(): void {
    this.selectedDeal = null;
    this.isEditMode = false;
    this.showModal = true;
    this.photos = [];
    this.activeTab = 'basic';
    this.formError = null;

    this.dealForm.reset({
      title: '',
      description: '',
      price: 0,
      discountPercentage: 0,
      category: '',
      isActive: true,
      tags: [],
      isInstantBooking: false,
      isLastMinuteDeal: false,
      photos: []
    });
  }

  async submitDeal() {
    if (this.dealForm.valid) {
      this.isSubmitting = true;
      this.formError = null;

      try {
        // Upload photos first
        const uploadedPhotoUrls: string[] = [];
        for (const photo of this.photos) {
          if (photo.file) {
            const response = await this.fileUploadService.uploadFile(photo.file, 'deals').toPromise();
            if (response?.url) {
              uploadedPhotoUrls.push(response.url);
            }
          } else if (photo.url) {
            uploadedPhotoUrls.push(photo.url);
          }
        }

        const price = Number(this.dealForm.get('price')?.value);
        const discountPercentage = Number(this.dealForm.get('discountPercentage')?.value) || 0;
        const discountedPrice = price - (price * discountPercentage / 100);

        const dealData = {
          ...this.dealForm.value,
          photos: uploadedPhotoUrls,
          discountedPrice: discountedPrice,
          // Add default itinerary
          itinerary: [
            {
              dayNumber: 1,
              title: "Day 1",
              description: "First day of the trip",
              activities: ["Arrival", "Check-in", "Orientation"]
            }
          ],
          // Ensure boolean fields are properly set
          elderlyFriendly: this.dealForm.get('elderlyFriendly')?.value ?? false,
          internetIncluded: this.dealForm.get('internetIncluded')?.value ?? false,
          travelIncluded: this.dealForm.get('travelIncluded')?.value ?? false,
          mealsIncluded: this.dealForm.get('mealsIncluded')?.value ?? false,
          sightseeingIncluded: this.dealForm.get('sightseeingIncluded')?.value ?? false,
          stayIncluded: this.dealForm.get('stayIncluded')?.value ?? false,
          airTransfer: this.dealForm.get('airTransfer')?.value ?? false,
          roadTransfer: this.dealForm.get('roadTransfer')?.value ?? false,
          trainTransfer: this.dealForm.get('trainTransfer')?.value ?? false,
          travelCostIncluded: this.dealForm.get('travelCostIncluded')?.value ?? false,
          guideIncluded: this.dealForm.get('guideIncluded')?.value ?? false,
          photographyIncluded: this.dealForm.get('photographyIncluded')?.value ?? false,
          insuranceIncluded: this.dealForm.get('insuranceIncluded')?.value ?? false,
          visaIncluded: this.dealForm.get('visaIncluded')?.value ?? false,
          // Set timestamps
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          // Ensure required fields are properly set
          locationId: Number(this.dealForm.get('locationId')?.value),
          daysCount: Number(this.dealForm.get('daysCount')?.value),
          nightsCount: Number(this.dealForm.get('nightsCount')?.value),
          price: price,
          description: this.dealForm.get('description')?.value,
          packageType: this.dealForm.get('packageType')?.value,
          title: this.dealForm.get('title')?.value
        };

        const requestData = dealData;

        const apiUrl = `${environment.apiUrl}/api/Deal`;
        const request$ = this.isEditMode
          ? this.http.put(`${apiUrl}/${this.editingDealId}`, requestData)
          : this.http.post(apiUrl, requestData);

        request$.subscribe({
          next: () => {
            this.closeModal();
            this.loadData();
            this.isSubmitting = false;
          },
          error: (error) => {
            console.error('Error submitting deal:', error);
            this.formError = error.error?.message || 'Failed to submit deal. Please try again.';
            this.isSubmitting = false;
          }
        });
      } catch (error) {
        console.error('Error uploading photos:', error);
        this.formError = 'Failed to upload photos. Please try again.';
        this.isSubmitting = false;
      }
    } else {
      this.formError = 'Please fill in all required fields correctly.';
      this.dealForm.markAllAsTouched();
    }
  }

  deleteDeal(dealId: number): void {
    // This method now just receives the ID from the card component
    // The actual delete API call is handled in the card component
    const dealIndex = this.deals.findIndex(d => d.id === dealId);
    if (dealIndex !== -1) {
      this.deals.splice(dealIndex, 1);
      this.updateStats(this.deals);
    }
  }

  viewDealDetails(deal: DealResponseDto): void {
    this.router.navigate(['/agency/deal-details', deal.id]);
  }

  getStatusColor(status: string): string {
    switch (status) {
      case 'Active':
        return 'bg-green-100 text-green-800';
      case 'Pending':
        return 'bg-yellow-100 text-yellow-800';
      case 'Rejected':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  getLocationName(locationId: number): string {
    return this.locations.find(l => l.id === locationId)?.name || 'Unknown';
  }

  getLocationNameFromForm(): string {
    const locationId = this.dealForm.get('locationId')?.value;
    return this.getLocationName(locationId);
  }

  // Improved tag handling
  getTags(): string[] {
    return this.dealForm.get('tags')?.value || [];
  }

  addTag(event: any, inputElement: HTMLInputElement): void {
    const value = inputElement.value?.trim();
    if (value) {
      const currentTags = this.getTags();
      if (!currentTags.includes(value)) {
        this.dealForm.patchValue({
          tags: [...currentTags, value]
        });
      }
      inputElement.value = '';
    }
  }

  removeTag(index: number): void {
    const currentTags = this.getTags();
    currentTags.splice(index, 1);
    this.dealForm.patchValue({
      tags: currentTags
    });
  }

  goBack(): void {
    this.location.back();
  }

  onToggleStatus(event: { id: number, isActive: boolean }): void {
    this.dealService.toggleDealStatus(event.id, event.isActive)
      .subscribe({
        next: () => {
          const deal = this.deals.find(d => d.id === event.id);
          if (deal) {
            deal.isActive = event.isActive;
            this.updateStats(this.deals);
          }
        },
        error: (err) => {
          console.error('Failed to toggle deal status:', err);
        }
      });
  }

  toggleFilters(): void {
    this.showFilters = !this.showFilters;
  }

  showError(message: string): void {
    this.error = message;
    // Auto hide the error after 5 seconds
    setTimeout(() => {
      this.error = null;
    }, 5000);
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDraggingOver = true;
  }

  onDropPhotos(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDraggingOver = false;

    if (event.dataTransfer?.files) {
      this.handleFileInput(event.dataTransfer.files);
    }
  }

  onPhotosSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      this.handleFileInput(input.files);
    }
  }

  handleFileInput(files: FileList): void {
    for (let i = 0; i < files.length; i++) {
      const file = files[i];
      if (file.type.startsWith('image/') && file.size <= 10 * 1024 * 1024) { // 10MB limit
        const reader = new FileReader();
        reader.onload = (e) => {
          if (e.target?.result) {
            this.photos.push({
              url: e.target.result as string,
              file: file
            });
            // Update the form value
            this.dealForm.patchValue({
              photos: this.photos
            });
          }
        };
        reader.readAsDataURL(file);
      }
    }
  }

  removePhoto(index: number, event: Event): void {
    event.stopPropagation();
    this.photos.splice(index, 1);
    // Update the form value
    this.dealForm.patchValue({
      photos: this.photos
    });
  }

  closeModal(): void {
    this.showModal = false;
    this.formError = null;
  }

  clearFormError(): void {
    this.formError = null;
  }

  // Add new method for creating location
  createLocation(locationName: string): void {
    if (!locationName.trim()) return;

    const newLocation = {
      name: locationName.trim(),
      description: '',
      address: '',
      city: '',
      state: '',
      country: '',
      postalCode: '',
      latitude: 0,
      longitude: 0,
      isActive: true
    };

    this.http.post<Location>(`${environment.apiUrl}/api/Location`, newLocation)
      .subscribe({
        next: (location) => {
          this.locations.push(location);
          this.dealForm.patchValue({ locationId: location.id });
        },
        error: (err) => {
          console.error('Failed to create location:', err);
          this.formError = 'Failed to create location. Please try again.';
        }
      });
  }

  onDeleteDeal(dealInfo: { id: number, title: string }): void {
    this.dealToDelete = dealInfo;
    this.showDeleteConfirmation = true;
  }

  onDeleteConfirmed(): void {
    if (this.dealToDelete) {
      this.dealService.deleteDeal(this.dealToDelete.id)
        .subscribe({
          next: () => {
            this.loadData();
            this.showDeleteConfirmation = false;
            this.dealToDelete = null;
          },
          error: (err) => {
            console.error('Error deleting deal:', err);
            this.showDeleteConfirmation = false;
            this.dealToDelete = null;
          }
        });
    }
  }

  onDeleteCancelled(): void {
    this.showDeleteConfirmation = false;
    this.dealToDelete = null;
  }
}
