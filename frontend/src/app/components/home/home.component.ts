import { Component, OnInit, ElementRef, ViewChild, HostListener, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterModule } from '@angular/router';
import { LocationService } from '../../services/location.service';
import { DealService } from '../../services/deal.service';
import { AuthService } from '../../services/auth.service';
import { DealCardComponent } from '../deal-card/deal-card.component';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize, map, take } from 'rxjs/operators';
import { DealResponseDto } from '../../models/deal.model';

interface Location {
  id: number;
  name: string;
  description: string | null;
  imageUrl: string;
  isPopular?: boolean;
  clickCount?: number;
}

// Using DealResponseDto from models instead of local interface
type Deal = DealResponseDto;

interface Destination {
  id: string;
  name: string;
  slug: string;
  imageUrl: string;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterModule, DealCardComponent],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
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

  // Animated placeholder properties
  placeholderText: string = 'Search for destination';
  private placeholderIndex: number = 0;
  private isDeleting: boolean = false;
  private typingSpeed: number = 150; // Slower speed for smoother animation
  private destinations: string[] = ['destination', 'Locations', 'Dubai', 'Kashmir', 'Bali', 'Paris'];
  private typingInterval: any;
  currentText: string = '';
  private baseText: string = 'Search for ';
  isFocused: boolean = false;

  popularDestinations: Destination[] = [
    { id: '1', name: 'DUBAI', slug: 'dubai', imageUrl: 'assets/destinations/dubai.jpg' },
    { id: '2', name: 'SINGAPORE', slug: 'singapore', imageUrl: 'assets/destinations/singapore.jpg' },
    { id: '3', name: 'THAILAND', slug: 'thailand', imageUrl: 'assets/destinations/thailand.jpg' },
    { id: '4', name: 'ANDAMAN', slug: 'andaman', imageUrl: 'assets/destinations/andaman.jpg' },
    { id: '5', name: 'INDIA', slug: 'india', imageUrl: 'assets/destinations/india.jpg' },
    { id: '6', name: 'LADAKH', slug: 'ladakh', imageUrl: 'assets/destinations/ladakh.jpg' },
    { id: '7', name: 'HONGKONG', slug: 'hongkong', imageUrl: 'assets/destinations/hongkong.jpg' },
    { id: '8', name: 'SRILANKA', slug: 'srilanka', imageUrl: 'assets/destinations/srilanka.jpg' },
    { id: '9', name: 'BALI', slug: 'bali', imageUrl: 'assets/destinations/bali.jpg' }
  ];

  newsletterEmail: string = '';
  currentYear: number = new Date().getFullYear();

  constructor(
    private locationService: LocationService,
    private dealService: DealService,
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.checkAuthAndLoadData();
    this.startPlaceholderAnimation();
  }

  ngOnDestroy(): void {
    if (this.typingInterval) {
      clearInterval(this.typingInterval);
    }
  }

  private startPlaceholderAnimation(): void {
    this.typingInterval = setInterval(() => {
      const currentDestination = this.destinations[this.placeholderIndex];

      if (this.isDeleting) {
        // Delete text
        this.currentText = this.currentText.slice(0, -1);

        if (this.currentText.length === 0) {
          this.isDeleting = false;
          this.placeholderIndex = (this.placeholderIndex + 1) % this.destinations.length;
          // Add a small delay before starting to type the next word
          setTimeout(() => {
            this.currentText = '';
          }, 500);
        }
      } else {
        // Add text
        if (this.currentText.length < currentDestination.length) {
          this.currentText = currentDestination.slice(0, this.currentText.length + 1);
        } else {
          // Pause before starting to delete
          setTimeout(() => {
            this.isDeleting = true;
          }, 2000);
        }
      }

      this.cdr.detectChanges();
    }, this.typingSpeed);
  }

  checkAuthAndLoadData(): void {
    this.isAuthenticated = this.authService.isAuthenticated();

    if (this.isAuthenticated) {
      this.loadUserInfo();
      this.loadLocations();
    } else {
      this.router.navigate(['/login']);
    }
  }

  loadUserInfo(): void {
    const user = this.authService.getCurrentUser();
    console.log('Current user from auth service:', user);

    if (user) {
      // Handle the user data based on the response structure
      let userName = 'User';

      if (user.firstName) {
        userName = user.firstName;
        console.log('Using firstName:', userName);
      }
      // If no firstName, use fullName if available
      else if (user.fullName) {
        userName = user.fullName;
      }
      // If no firstName or fullName, try to construct from firstName and lastName
      else if (user.firstName && user.lastName) { // This case might be redundant now but kept for safety
        userName = `${user.firstName} ${user.lastName}`;
      }
      // If no name available, use email
      else if (user.email) {
        userName = user.email.split('@')[0];
      }

      this.userName = userName;

      // Get user role and profile in parallel
      forkJoin({
        role: this.authService.getUserRole().pipe(
          catchError(error => {
            console.error('Error fetching user role:', error);
            return of('');
          })
        ),
        profile: this.authService.getUserProfile().pipe(
          catchError(error => {
            console.error('Error fetching user profile:', error);
            return of({ emailConfirmed: true });
          })
        )
      }).pipe(take(1)).subscribe({
        next: (data) => {
          this.userRole = data.role;
          this.isEmailVerified = data.profile.emailConfirmed;
          console.log('User role and profile updated:', { role: this.userRole, emailVerified: this.isEmailVerified });
          this.cdr.detectChanges();
        },
        error: (error) => {
          console.error('Error in forkJoin:', error);
        }
      });
    } else {
      console.log('No user found in auth service');
    }
  }

  loadLocations(): void {
    this.isLoading = true;
    this.cdr.detectChanges();

    this.locationService.getLocations().pipe(
      take(1),
      finalize(() => {
        this.isLoading = false;
        this.cdr.detectChanges();
      })
    ).subscribe({
      next: (data) => {
        this.locations = data;
        // Load deals for the first 12 locations initially instead of just 3
        const initialLocations = this.locations.slice(0, 12);
        this.loadDealsForLocations(initialLocations);
      },
      error: (error) => {
        console.error('Error loading locations:', error);
      }
    });
  }

  loadDealsForLocations(locations: Location[]): void {
    if (locations.length === 0) return;

    const dealRequests = locations.map(location =>
      this.dealService.getDealsByLocation(location.id).pipe(
        catchError(error => {
          console.error(`Error loading deals for location ${location.id}:`, error);
          return of([]);
        }),
        map(deals => ({ locationId: location.id, deals }))
      )
    );

    forkJoin(dealRequests).pipe(take(1)).subscribe({
      next: (results) => {
        results.forEach(result => {
          this.locationDeals[result.locationId] = result.deals;
        });
        this.cdr.detectChanges();
      }
    });
  }

  loadMoreDeals(): void {
    // Load deals for the next batch of locations
    const loadedLocationIds = Object.keys(this.locationDeals).map(Number);
    const remainingLocations = this.locations.filter(
      location => !loadedLocationIds.includes(location.id)
    );

    if (remainingLocations.length > 0) {
      // Load the next 12 locations instead of just 3
      const nextBatch = remainingLocations.slice(0, 12);
      this.loadDealsForLocations(nextBatch);
    }
  }

  scrollLocations(direction: 'left' | 'right'): void {
    const container = document.getElementById('locations-container');
    if (container) {
      const scrollAmount = 600; // Increased from 300 to 600 for more scroll distance
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
    this.cdr.detectChanges();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.isProfileMenuOpen &&
        !this.profileMenuTrigger.nativeElement.contains(event.target)) {
      this.isProfileMenuOpen = false;
      this.cdr.detectChanges();
    }
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  subscribeNewsletter() {
    if (this.newsletterEmail) {
      // TODO: Implement newsletter subscription logic
      console.log('Newsletter subscription for:', this.newsletterEmail);
      this.newsletterEmail = ''; // Reset the form
      // Show success message to user
    }
  }

  onFocus(): void {
    this.isFocused = true;
  }

  onBlur(): void {
    this.isFocused = false;
  }
}
