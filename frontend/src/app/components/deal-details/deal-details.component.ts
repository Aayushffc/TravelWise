import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DealService } from '../../services/deal.service';
import { FormsModule } from '@angular/forms';
import { Location } from '@angular/common';
import { DealResponseDto } from '../../models/deal.model';

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
    private dealService: DealService,
    private location: Location
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
        if (error.status === 500) {
          this.errorMessage = 'A server error occurred while loading the deal. Please try again later.';
        } else if (error.status === 404) {
          this.errorMessage = 'Deal not found. It may have been removed.';
        } else {
          this.errorMessage = 'Failed to load deal details. Please try again.';
        }
        this.isLoading = false;
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
