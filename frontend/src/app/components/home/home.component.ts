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
          this.cdr.detectChanges();
        }
      });
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
        // Load deals for the first 3 locations initially
        const initialLocations = this.locations.slice(0, 3);
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
      const nextBatch = remainingLocations.slice(0, 3);
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
}
