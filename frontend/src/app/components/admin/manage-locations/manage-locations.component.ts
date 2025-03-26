import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocationService } from '../../../services/location.service';
import { FileUploadService } from '../../../services/file-upload.service';

interface Location {
  id: number;
  name: string;
  description?: string;
  imageUrl?: string;
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
  templateUrl: './manage-locations.component.html'
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

  locationForm: Partial<Location> = {
    name: '',
    description: '',
    imageUrl: '',
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
        },
        error: (error) => {
          this.error = 'Failed to upload image';
          this.isUploading = false;
        }
      });
    }
  }

  openCreateModal(): void {
    this.locationForm = {
      name: '',
      description: '',
      imageUrl: '',
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
      },
      error: (error) => {
        this.error = 'Failed to create location';
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
      },
      error: (error) => {
        this.error = 'Failed to update location';
      }
    });
  }

  deleteLocation(id: number): void {
    if (confirm('Are you sure you want to delete this location?')) {
      this.locationService.deleteLocation(id).subscribe({
        next: () => {
          this.success = 'Location deleted successfully';
          this.loadLocations();
        },
        error: (error) => {
          this.error = 'Failed to delete location';
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
      },
      error: (error) => {
        this.error = 'Failed to update location status';
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
      },
      error: (error) => {
        this.error = 'Failed to update location popularity';
      }
    });
  }
}
