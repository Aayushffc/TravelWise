import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { WishlistService } from '../../services/wishlist.service';
import { DealResponseDto } from '../../models/deal.model';
import { AuthService, AuthResponse } from '../../services/auth.service';
import { SidebarComponent } from '../side-bar/sidebar.component';
import { LocationService } from '../../services/location.service';
import { trigger, transition, style, animate, query, stagger } from '@angular/animations';

interface Location {
  id: number;
  name: string;
}

@Component({
  selector: 'app-wishlist',
  standalone: true,
  imports: [CommonModule, RouterModule, SidebarComponent, FormsModule],
  templateUrl: './wishlist.component.html',
  styleUrls: ['./wishlist.component.css'],
  animations: [
    trigger('listAnimation', [
      transition('* => *', [
        query(':enter', [
          style({ opacity: 0, transform: 'translateY(20px)' }),
          stagger(100, [
            animate('300ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
          ])
        ], { optional: true })
      ])
    ])
  ]
})
export class WishlistComponent implements OnInit {
  wishlistItems: DealResponseDto[] = [];
  filteredItems: DealResponseDto[] = [];
  isLoading = true;
  error: string | null = null;
  isLoggedIn = false;
  user: AuthResponse | null = null;
  locations: string[] = [];
  selectedLocation: string = '';

  constructor(
    private wishlistService: WishlistService,
    private authService: AuthService,
    private locationService: LocationService,
    private router: Router
  ) {}

  ngOnInit() {
    this.isLoggedIn = this.authService.isAuthenticated();
    if (!this.isLoggedIn) {
      this.router.navigate(['/login']);
      return;
    }
    this.user = this.authService.getCurrentUser();
    this.loadWishlist();
  }

  goBack() {
    this.router.navigate(['/home']);
  }

  loadWishlist() {
    this.isLoading = true;
    this.wishlistService.getWishlist().subscribe({
      next: async (items) => {
        this.wishlistItems = await this.enrichDealsWithLocationNames(items);
        this.filteredItems = [...this.wishlistItems];
        this.extractUniqueLocations();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading wishlist:', error);
        this.error = 'Failed to load wishlist';
        this.isLoading = false;
      }
    });
  }

  private async enrichDealsWithLocationNames(deals: DealResponseDto[]): Promise<DealResponseDto[]> {
    const enrichedDeals = await Promise.all(
      deals.map(async (deal) => {
        if (deal.locationId) {
          try {
            const locations = await this.locationService.getLocations().toPromise() as Location[];
            const location = locations?.find(loc => loc.id === deal.locationId);
            if (location) {
              return {
                ...deal,
                location: {
                  id: location.id,
                  name: location.name
                }
              };
            }
          } catch (error) {
            console.error(`Error fetching location name for ID ${deal.locationId}:`, error);
          }
        }
        return {
          ...deal,
          location: {
            id: deal.locationId,
            name: 'Unknown Location'
          }
        };
      })
    );
    return enrichedDeals;
  }

  private extractUniqueLocations() {
    const uniqueLocations = new Set(this.wishlistItems.map(item => item.location?.name || 'Unknown Location'));
    this.locations = Array.from(uniqueLocations).sort();
  }

  filterByLocation(location: string) {
    this.selectedLocation = location;
    if (location === '') {
      this.filteredItems = [...this.wishlistItems];
    } else {
      this.filteredItems = this.wishlistItems.filter(item => item.location?.name === location);
    }
  }

  removeFromWishlist(dealId: number) {
    this.wishlistService.removeFromWishlist(dealId).subscribe({
      next: () => {
        this.wishlistItems = this.wishlistItems.filter(item => item.id !== dealId);
        this.filteredItems = this.filteredItems.filter(item => item.id !== dealId);
        this.extractUniqueLocations();
      },
      error: (error) => {
        console.error('Error removing from wishlist:', error);
        this.error = 'Failed to remove from wishlist';
      }
    });
  }

  getDiscountPercentage(price: number, discountedPrice: number | null): number {
    if (!discountedPrice) return 0;
    return Math.round(((price - discountedPrice) / price) * 100);
  }
}

