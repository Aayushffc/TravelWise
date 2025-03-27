import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DealService } from '../../services/deal.service';
import { FormsModule } from '@angular/forms';

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
  startPoint: string;
  endPoint: string;
  elderlyFriendly: boolean;
  internetIncluded: boolean;
  travelIncluded: boolean;
  mealsIncluded: boolean;
  sightseeingIncluded: boolean;
  stayIncluded: boolean;
  airTransfer: boolean;
  roadTransfer: boolean;
  trainTransfer: boolean;
  travelCostIncluded: boolean;
  guideIncluded: boolean;
  photographyIncluded: boolean;
  insuranceIncluded: boolean;
  visaIncluded: boolean;
  itinerary: ItineraryDay[];
  packageOptions: PackageOption[];
  mapUrl?: string;
  policies: Policy[];
  packageType: string;
  location: {
    id: number;
    name: string;
  };
}

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
  deal: Deal | null = null;
  isLoading: boolean = true;
  errorMessage: string = '';
  currentImageIndex: number = 0;
  selectedDate: string = '';
  selectedTravelers: number = 2;
  activeTab: 'overview' | 'inclusions' | 'itinerary' | 'reviews' = 'overview';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private dealService: DealService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const dealId = parseInt(params.get('id') || '0', 10);
      if (dealId) {
        this.loadDealDetails(dealId);
      } else {
        this.errorMessage = 'Invalid deal ID';
        this.isLoading = false;
      }
    });
  }

  loadDealDetails(id: number): void {
    this.isLoading = true;
    this.dealService.getDealById(id).subscribe({
      next: (deal) => {
        this.deal = deal;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading deal details:', error);
        this.errorMessage = 'Failed to load deal details.';
        this.isLoading = false;
      }
    });
  }

  nextImage(): void {
    if (this.deal?.photos) {
      this.currentImageIndex = (this.currentImageIndex + 1) % this.deal.photos.length;
    }
  }

  previousImage(): void {
    if (this.deal?.photos) {
      this.currentImageIndex = (this.currentImageIndex - 1 + this.deal.photos.length) % this.deal.photos.length;
    }
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
    this.router.navigate(['/home']);
  }

  addToWishlist(): void {
    // Implement wishlist logic
    console.log('Added to wishlist:', this.deal?.id);
  }

  share(): void {
    // Implement share logic
    console.log('Sharing deal:', this.deal?.id);
  }
}
