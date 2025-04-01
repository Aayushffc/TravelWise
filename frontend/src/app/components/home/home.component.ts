import { Component, OnInit, ElementRef, ViewChild, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterModule } from '@angular/router';
import { LocationService } from '../../services/location.service';
import { DealService } from '../../services/deal.service';
import { AuthService } from '../../services/auth.service';
import { DealCardComponent } from '../deal-card/deal-card.component';

interface Location {
  id: number;
  name: string;
  description: string | null;
  imageUrl: string;
}

interface Deal {
  id: number;
  title: string;
  description: string;
  price: number;
  discountedPrice: number;
  discountPercentage: number;
  daysCount: number;
  nightsCount: number;
  photos: string[];
  rating: number | null;
  location: {
    id: number;
    name: string;
  };
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterModule, DealCardComponent],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  locations: Location[] = [];
  locationDeals: {[key: number]: Deal[]} = {};
  searchTerm: string = '';
  userName: string = '';
  isLoading: boolean = true;
  isEmailVerified: boolean = true;
  isAuthenticated: boolean = false;
  isProfileMenuOpen: boolean = false;
  userRole: string = '';
  @ViewChild('profileMenuTrigger') profileMenuTrigger!: ElementRef;

  constructor(
    private locationService: LocationService,
    private dealService: DealService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadUserInfo();
    this.loadLocations();
  }

  loadUserInfo(): void {
    this.isAuthenticated = this.authService.isAuthenticated();
    const user = this.authService.getCurrentUser();
    if (user) {
      // Use email as fallback if fullName is not available
      if (user.fullName) {
        this.userName = user.fullName.split(' ')[0];
      } else {
        this.userName = user.email.split('@')[0];
      }

      // Get user role
      this.authService.getUserRole().subscribe({
        next: (role) => {
          this.userRole = role;
        },
        error: (error) => {
          console.error('Error fetching user role:', error);
        }
      });

      // Update this part to get the latest email verification status
      this.authService.getUserProfile().subscribe({
        next: (profile) => {
          this.isEmailVerified = profile.emailConfirmed;
        },
        error: (error) => {
          console.error('Error fetching user profile:', error);
        }
      });
    }
  }

  loadLocations(): void {
    this.isLoading = true;
    this.locationService.getLocations().subscribe({
      next: (data) => {
        this.locations = data;
        // For each location, load its deals
        this.locations.forEach(location => {
          this.loadDealsForLocation(location.id);
        });
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading locations:', error);
        this.isLoading = false;
      }
    });
  }

  loadDealsForLocation(locationId: number): void {
    this.dealService.getDealsByLocation(locationId).subscribe({
      next: (deals) => {
        this.locationDeals[locationId] = deals;
      },
      error: (error) => {
        console.error(`Error loading deals for location ${locationId}:`, error);
      }
    });
  }

  scrollLocations(direction: 'left' | 'right'): void {
    const container = document.getElementById('locations-container');
    if (container) {
      const scrollAmount = 300; // Adjust scroll amount as needed
      if (direction === 'left') {
        container.scrollBy({ left: -scrollAmount, behavior: 'smooth' });
      } else {
        container.scrollBy({ left: scrollAmount, behavior: 'smooth' });
      }
    }
  }

  search(): void {
    if (this.searchTerm.trim()) {
      this.router.navigate(['/home/search'], {
        queryParams: {
          searchTerm: this.searchTerm.trim()
        }
      });
    }
  }

  viewLocation(locationId: number): void {
    this.router.navigate(['/location', locationId]);
  }

  viewDeal(dealId: number): void {
    this.router.navigate(['/deal', dealId]);
  }

  toggleProfileMenu(): void {
    this.isProfileMenuOpen = !this.isProfileMenuOpen;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.isProfileMenuOpen &&
        !this.profileMenuTrigger.nativeElement.contains(event.target)) {
      this.isProfileMenuOpen = false;
    }
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
