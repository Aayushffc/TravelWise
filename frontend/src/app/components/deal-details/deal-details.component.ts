import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DealService } from '../../services/deal.service';
import { FormsModule } from '@angular/forms';
import { Location } from '@angular/common';
import { DealResponseDto } from '../../models/deal.model';
import { LocationService } from '../../services/location.service';
import { AuthService } from '../../services/auth.service';
import { AgencyProfileService } from '../../services/agency-profile.service';

// Use DealResponseDto directly instead of custom Deal interface
type Deal = DealResponseDto;

interface ItineraryDay {
  dayNumber: number;
  title: string;
  description: string;
  activities: string[];
}

interface PackageOption {
  name: string;
  description: string;
  price: number;
  inclusions: string[];
}

interface Policy {
  title: string;
  description: string;
}

@Component({
  selector: 'app-deal-details',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './deal-details.component.html',
  styleUrls: ['./deal-details.component.css']
})
export class DealDetailsComponent implements OnInit {
  deal: DealResponseDto | null = null;
  isLoading = true;
  error: string | null = null;
  currentImageIndex = 0;
  isWishlisted = false;
  locationName = 'Loading...';
  isLoggedIn = false;
  selectedDate: string = '';
  selectedTravelers: number = 2;
  activeTab: 'overview' | 'inclusions' | 'itinerary' | 'reviews' = 'overview';
  agencyProfile: any = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private dealService: DealService,
    private locationService: LocationService,
    private authService: AuthService,
    private location: Location,
    private agencyProfileService: AgencyProfileService
  ) {}

  ngOnInit(): void {
    this.isLoggedIn = this.authService.isAuthenticated();
    const dealId = this.route.snapshot.paramMap.get('id');
    if (dealId) {
      this.loadDealDetails(parseInt(dealId));
    } else {
      this.error = 'Invalid deal ID';
      this.isLoading = false;
    }
  }

  private loadDealDetails(dealId: number): void {
    this.isLoading = true;
    this.error = null;

    this.dealService.getDealById(dealId).subscribe({
      next: (deal) => {
        this.deal = deal;
        this.loadLocationName(deal.locationId);
        if (deal.userId) {
          this.loadAgencyProfile(deal.userId);
        }
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading deal:', err);
        this.error = 'Failed to load deal details. Please try again later.';
        this.isLoading = false;
      }
    });
  }

  private loadLocationName(locationId: number): void {
    this.locationService.getLocationById(locationId).subscribe({
      next: (location) => {
        this.locationName = location.name;
      },
      error: (err) => {
        console.error('Error loading location:', err);
        this.locationName = 'Location not specified';
      }
    });
  }

  private loadAgencyProfile(userId: string): void {
    const profileId = parseInt(userId, 10);
    if (isNaN(profileId)) {
      console.error('Invalid user ID:', userId);
      return;
    }
    this.agencyProfileService.getAgencyProfile(profileId).subscribe({
      next: (profile) => {
        this.agencyProfile = profile;
      },
      error: (err) => {
        console.error('Error loading agency profile:', err);
      }
    });
  }

  nextImage(): void {
    if (this.deal?.photos && this.deal.photos.length > 0) {
      this.currentImageIndex = (this.currentImageIndex + 1) % this.deal.photos.length;
    }
  }

  previousImage(): void {
    if (this.deal?.photos && this.deal.photos.length > 0) {
      this.currentImageIndex = (this.currentImageIndex - 1 + this.deal.photos.length) % this.deal.photos.length;
    }
  }

  setCurrentImage(index: number): void {
    this.currentImageIndex = index;
  }

  toggleWishlist(): void {
    if (!this.isLoggedIn) {
      this.router.navigate(['/login']);
      return;
    }
    this.isWishlisted = !this.isWishlisted;
    // TODO: Implement wishlist API call
  }

  shareDeal(): void {
    const url = window.location.href;
    navigator.clipboard.writeText(url).then(() => {
      // Show success message
      alert('Link copied to clipboard!');
    }).catch(err => {
      console.error('Failed to copy link:', err);
    });
  }

  getDiscountPercentage(): number {
    if (!this.deal?.price || !this.deal?.discountedPrice) return 0;
    return Math.round(((this.deal.price - this.deal.discountedPrice) / this.deal.price) * 100);
  }

  getIncludedFeatures(): { icon: string, label: string }[] {
    const features = [];
    if (this.deal?.elderlyFriendly) features.push({ icon: '👵', label: 'Elderly Friendly' });
    if (this.deal?.internetIncluded) features.push({ icon: '🌐', label: 'Internet Included' });
    if (this.deal?.travelIncluded) features.push({ icon: '🚗', label: 'Travel Included' });
    if (this.deal?.mealsIncluded) features.push({ icon: '🍽️', label: 'Meals Included' });
    if (this.deal?.sightseeingIncluded) features.push({ icon: '🏛️', label: 'Sightseeing Included' });
    if (this.deal?.stayIncluded) features.push({ icon: '🏨', label: 'Stay Included' });
    if (this.deal?.airTransfer) features.push({ icon: '✈️', label: 'Air Transfer' });
    if (this.deal?.roadTransfer) features.push({ icon: '🚌', label: 'Road Transfer' });
    if (this.deal?.trainTransfer) features.push({ icon: '🚂', label: 'Train Transfer' });
    if (this.deal?.travelCostIncluded) features.push({ icon: '💰', label: 'Travel Cost Included' });
    if (this.deal?.guideIncluded) features.push({ icon: '👨‍🏫', label: 'Guide Included' });
    if (this.deal?.photographyIncluded) features.push({ icon: '📸', label: 'Photography Included' });
    if (this.deal?.insuranceIncluded) features.push({ icon: '🛡️', label: 'Insurance Included' });
    if (this.deal?.visaIncluded) features.push({ icon: '📝', label: 'Visa Included' });
    return features;
  }

  getPackageOptions(): { name: string, price: number, inclusions: string[] }[] {
    return this.deal?.packageOptions || [];
  }

  getItinerary(): { dayNumber: number, title: string, description: string, activities: string[] }[] {
    return this.deal?.itinerary || [];
  }

  getPolicies(): { title: string, description: string }[] {
    return this.deal?.policies || [];
  }

  setActiveTab(tab: 'overview' | 'inclusions' | 'itinerary' | 'reviews'): void {
    this.activeTab = tab;
  }

  checkAvailability(): void {
    // Implement availability check logic
    console.log('Checking availability for:', {
      date: this.selectedDate,
      travelers: this.selectedTravelers
    });
  }

  goBack(): void {
    this.location.back();
  }

  addToWishlist(): void {
    // Implement wishlist logic
    console.log('Added to wishlist:', this.deal?.id);
  }

  share(): void {
    // Implement share logic
    console.log('Sharing deal:', this.deal?.id);
  }

  bookNow(): void {
    // Navigate to the booking page with the deal ID
    this.router.navigate(['/booking', this.deal?.id]);
  }
}
