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

@Component({
  selector: 'app-manage-deals',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ManageDealCardComponent
  ],
  templateUrl: './manage-deals.component.html',
  styleUrls: ['./manage-deals.component.css'],
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
  showFilters = false;
  stats = {
    total: 0,
    active: 0,
    pending: 0,
    inactive: 0
  };
  photos: any[] = [];
  isDraggingOver = false;
  isSubmitting = false;
  activeTab = 'basic'; // For tabbed form navigation

  // Categories for the form
  categories: string[] = [
    'Food & Dining',
    'Entertainment',
    'Shopping',
    'Travel',
    'Health & Wellness',
    'Services',
    'Other'
  ];

  constructor(
    private http: HttpClient,
    private router: Router,
    private authService: AuthService,
    private fb: FormBuilder,
    private dealService: DealService,
    private location: AngularLocation
  ) {
    this.dealForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(3)]],
      locationId: [null],
      price: [0, [Validators.required, Validators.min(0)]],
      discountPercentage: [0, [Validators.min(0), Validators.max(100)]],
      description: ['', Validators.required],
      photos: [[]],
      category: ['', Validators.required],
      isActive: [true],
      tags: [[]],
      isInstantBooking: [false],
      isLastMinuteDeal: [false]
    });
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
      pending: deals.filter(d => d.status === 'Pending').length,
      inactive: deals.filter(d => !d.isActive).length
    };
  }

  applyFilter(): void {
    let filteredData = [...this.deals];

    if (this.selectedStatus) {
      filteredData = filteredData.filter(deal => deal.status === this.selectedStatus);
    }

    if (this.selectedLocation) {
      filteredData = filteredData.filter(deal => deal.locationId === this.selectedLocation);
    }

    if (this.searchTerm) {
      const searchLower = this.searchTerm.toLowerCase();
      filteredData = filteredData.filter(deal =>
        deal.title.toLowerCase().includes(searchLower) ||
        deal.description.toLowerCase().includes(searchLower)
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

  submitDeal(): void {
    if (this.dealForm.invalid) {
      // Mark all fields as touched to trigger validation messages
      Object.keys(this.dealForm.controls).forEach(key => {
        this.dealForm.get(key)?.markAsTouched();
      });

      this.formError = 'Please fill in all required fields.';
      return;
    }

    this.isSubmitting = true;
    this.formError = null;

    const formData = this.dealForm.value;
    const currentUser = this.authService.getCurrentUser();

    if (!currentUser?.id) {
      this.formError = 'User authentication error. Please log in again.';
      this.isSubmitting = false;
      return;
    }

    // Setup minimum required fields based on API
    const dealData: any = {
      title: formData.title,
      description: formData.description,
      price: formData.price,
      discountPercentage: formData.discountPercentage || 0,
      packageType: formData.category, // Map category to packageType
      isActive: formData.isActive,
      photos: this.photos.map(p => p.url || p), // Extract URLs from photo objects
      locationId: formData.locationId || 1, // Default to 1 if not set
      daysCount: 1, // Default values to satisfy API requirements
      nightsCount: 1,
      userId: currentUser.id,
      itinerary: [{
        dayNumber: 1,
        title: 'Day 1',
        description: 'Itinerary details will be added later.',
        activities: ['Activity details will be added later.']
      }]
    };

    // Add optional fields only if they have values
    if (formData.tags && formData.tags.length > 0) {
      dealData.tags = formData.tags;
    }

    if (formData.isInstantBooking) {
      dealData.isInstantBooking = true;
    }

    if (formData.isLastMinuteDeal) {
      dealData.isLastMinuteDeal = true;
    }

    // For updates, ensure we have an ID
    if (this.isEditMode && this.selectedDeal) {
      dealData.id = this.selectedDeal.id;
    }

    const request = this.isEditMode && this.selectedDeal
      ? this.dealService.updateDeal(this.selectedDeal.id, dealData)
      : this.dealService.createDeal(dealData);

    request.subscribe({
      next: () => {
        this.isSubmitting = false;
        this.showModal = false;
        this.loadData();
      },
      error: (err) => {
        this.isSubmitting = false;
        console.error('Failed to save deal:', err);

        // Display a user-friendly error message
        if (err.status === 400) {
          // For validation errors
          if (err.error && err.error.errors) {
            const errorMessages = [];
            for (const key in err.error.errors) {
              const messages = Array.isArray(err.error.errors[key])
                ? err.error.errors[key]
                : [err.error.errors[key]];
              errorMessages.push(...messages);
            }
            this.formError = errorMessages.join(' ');
          } else {
            this.formError = 'Please check your input and try again.';
          }
        } else if (err.status === 401 || err.status === 403) {
          this.formError = 'You are not authorized to perform this action.';
        } else {
          this.formError = 'An error occurred while saving the deal. Please try again later.';
        }
      }
    });
  }

  deleteDeal(dealId: number): void {
    if (confirm('Are you sure you want to delete this deal?')) {
      this.http.delete(`${environment.apiUrl}/api/Deal/${dealId}`)
        .subscribe({
          next: () => {
            this.loadData();
          },
          error: (err) => {
            console.error('Failed to delete deal:', err);
          }
        });
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
}
