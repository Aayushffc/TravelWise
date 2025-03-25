import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DealService } from '../../services/deal.service';
import { LocationService } from '../../services/location.service';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

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
  locationId: number;
  location?: Location;
  duration: number;
  packageType: string;
  imageUrl?: string;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
  userId: string;
}

@Component({
  selector: 'app-manage-deals',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './manage-deals.component.html'
})
export class ManageDealsComponent implements OnInit {
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

  dealForm: Partial<Deal> = {
    title: '',
    description: '',
    price: 0,
    locationId: 0,
    duration: 0,
    packageType: '',
    imageUrl: '',
    isActive: true
  };

  constructor(
    private dealService: DealService,
    private locationService: LocationService,
    private router: Router,
    private authService: AuthService
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
      }
    });
  }

  openCreateModal(): void {
    this.dealForm = {
      title: '',
      description: '',
      price: 0,
      locationId: 0,
      duration: 0,
      packageType: '',
      imageUrl: '',
      isActive: true
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
  }

  createDeal(): void {
    if (!this.dealForm.title || !this.dealForm.locationId || !this.dealForm.price || !this.dealForm.duration || !this.dealForm.packageType) {
      this.error = 'Please fill in all required fields';
      return;
    }

    if (this.currentUserId) {
      this.dealForm.userId = this.currentUserId;
      this.dealService.createDeal(this.dealForm as any).subscribe({
        next: () => {
          this.success = 'Deal created successfully';
          this.closeModals();
          this.loadDeals();
        },
        error: (error) => {
          this.error = 'Failed to create deal';
        }
      });
    }
  }

  updateDeal(): void {
    if (!this.selectedDeal?.id || !this.dealForm.title || !this.dealForm.locationId || !this.dealForm.price || !this.dealForm.duration || !this.dealForm.packageType) {
      this.error = 'Please fill in all required fields';
      return;
    }

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
