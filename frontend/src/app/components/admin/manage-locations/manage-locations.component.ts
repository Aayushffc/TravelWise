import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocationService } from '../../../services/location.service';
import { FileUploadService } from '../../../services/file-upload.service';
import { trigger, transition, style, animate, query, stagger } from '@angular/animations';

interface Location {
  id: number;
  name: string;
  description?: string;
  imageUrl?: string;
  country?: string;
  continent?: string;
  currency?: string;
  isPopular: boolean;
  isActive: boolean;
  clickCount: number;
  requestCallCount: number;
  createdAt: Date;
  updatedAt: Date;
}

@Component({
  selector: 'app-manage-locations',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './manage-locations.component.html',
  animations: [
    trigger('listAnimation', [
      transition('* => *', [
        query(':enter', [
          style({ opacity: 0, transform: 'translateY(20px)' }),
          stagger(100, [
            animate('0.3s ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
          ])
        ], { optional: true })
      ])
    ]),
    trigger('fadeInOut', [
      transition(':enter', [
        style({ opacity: 0 }),
        animate('0.3s ease-out', style({ opacity: 1 }))
      ]),
      transition(':leave', [
        animate('0.3s ease-in', style({ opacity: 0 }))
      ])
    ])
  ]
})
export class ManageLocationsComponent implements OnInit {
  locations: Location[] = [];
  isLoading: boolean = true;
  error: string | null = null;
  success: string | null = null;
  showCreateModal: boolean = false;
  showEditModal: boolean = false;
  selectedLocation: Location | null = null;
  selectedFile: File | null = null;
  isUploading: boolean = false;
  continents: string[] = ['Africa', 'Asia', 'Europe', 'North America', 'South America', 'Oceania', 'Antarctica'];
  readonly DESCRIPTION_MAX_LENGTH = 40;

  locationForm: Partial<Location> = {
    name: '',
    description: '',
    imageUrl: '',
    country: '',
    continent: '',
    currency: '',
    isPopular: false,
    isActive: true
  };

  constructor(
    private locationService: LocationService,
    private fileUploadService: FileUploadService
  ) {}

  ngOnInit(): void {
    this.loadLocations();
  }

  truncateDescription(description: string | undefined): string {
    if (!description) return 'No description';
    if (description.length <= this.DESCRIPTION_MAX_LENGTH) return description;
    return description.substring(0, this.DESCRIPTION_MAX_LENGTH) + '...';
  }

  loadLocations(): void {
    this.isLoading = true;
    this.locationService.getLocations().subscribe({
      next: (locations) => {
        this.locations = locations;
        this.isLoading = false;
      },
      error: (error) => {
        this.error = 'Failed to load locations';
        this.isLoading = false;
      }
    });
  }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.selectedFile = file;
      this.isUploading = true;
      this.fileUploadService.uploadFile(file, 'locations').subscribe({
        next: (response) => {
          this.locationForm.imageUrl = response.url;
          this.isUploading = false;
          this.success = 'Image uploaded successfully';
          setTimeout(() => this.success = null, 3000);
        },
        error: (error) => {
          this.error = 'Failed to upload image';
          this.isUploading = false;
          setTimeout(() => this.error = null, 3000);
        }
      });
    }
  }

  openCreateModal(): void {
    this.locationForm = {
      name: '',
      description: '',
      imageUrl: '',
      country: '',
      continent: '',
      currency: '',
      isPopular: false,
      isActive: true
    };
    this.selectedFile = null;
    this.showCreateModal = true;
  }

  openEditModal(location: Location): void {
    this.selectedLocation = location;
    this.locationForm = { ...location };
    this.selectedFile = null;
    this.showEditModal = true;
  }

  closeModals(): void {
    this.showCreateModal = false;
    this.showEditModal = false;
    this.selectedLocation = null;
    this.selectedFile = null;
  }

  createLocation(): void {
    if (!this.locationForm.name) {
      this.error = 'Please enter a location name';
      return;
    }

    this.locationService.createLocation(this.locationForm).subscribe({
      next: () => {
        this.success = 'Location created successfully';
        this.closeModals();
        this.loadLocations();
        setTimeout(() => this.success = null, 3000);
      },
      error: (error) => {
        this.error = 'Failed to create location';
        setTimeout(() => this.error = null, 3000);
      }
    });
  }

  updateLocation(): void {
    if (!this.selectedLocation?.id || !this.locationForm.name) {
      this.error = 'Please fill in all required fields';
      return;
    }

    this.locationService.updateLocation(this.selectedLocation.id, this.locationForm).subscribe({
      next: () => {
        this.success = 'Location updated successfully';
        this.closeModals();
        this.loadLocations();
        setTimeout(() => this.success = null, 3000);
      },
      error: (error) => {
        this.error = 'Failed to update location';
        setTimeout(() => this.error = null, 3000);
      }
    });
  }

  deleteLocation(id: number): void {
    if (confirm('Are you sure you want to delete this location?')) {
      this.locationService.deleteLocation(id).subscribe({
        next: () => {
          this.success = 'Location deleted successfully';
          this.loadLocations();
          setTimeout(() => this.success = null, 3000);
        },
        error: (error) => {
          this.error = 'Failed to delete location';
          setTimeout(() => this.error = null, 3000);
        }
      });
    }
  }

  toggleLocationStatus(location: Location): void {
    const updatedLocation = {
      ...location,
      isActive: !location.isActive
    };

    this.locationService.updateLocation(location.id, updatedLocation).subscribe({
      next: () => {
        this.success = `Location ${updatedLocation.isActive ? 'activated' : 'deactivated'} successfully`;
        this.loadLocations();
        setTimeout(() => this.success = null, 3000);
      },
      error: (error) => {
        this.error = 'Failed to update location status';
        setTimeout(() => this.error = null, 3000);
      }
    });
  }

  togglePopularStatus(location: Location): void {
    const updatedLocation = {
      ...location,
      isPopular: !location.isPopular
    };

    this.locationService.updateLocation(location.id, updatedLocation).subscribe({
      next: () => {
        this.success = `Location ${updatedLocation.isPopular ? 'marked as popular' : 'removed from popular'} successfully`;
        this.loadLocations();
        setTimeout(() => this.success = null, 3000);
      },
      error: (error) => {
        this.error = 'Failed to update location popularity';
        setTimeout(() => this.error = null, 3000);
      }
    });
  }
}
