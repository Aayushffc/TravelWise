import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink, RouterModule } from '@angular/router';
import { LocationService } from '../../services/location.service';
import { DealService } from '../../services/deal.service';
import { DealCardComponent } from '../deal-card/deal-card.component';
import { FormsModule } from '@angular/forms';
import { DealResponseDto } from '../../models/deal.model';

interface Location {
  id: number;
  name: string;
  description: string;
  imageUrl: string;
  isPopular: boolean;
  isActive: boolean;
}

// Using DealResponseDto from models
type Deal = DealResponseDto;

@Component({
  selector: 'app-location-details',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, DealCardComponent],
  templateUrl: './location-details.component.html',
  styleUrls: ['./location-details.component.css']
})
export class LocationDetailsComponent implements OnInit {
  location: Location | null = null;
  deals: Deal[] = [];
  isLoading: boolean = true;
  errorMessage: string = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private locationService: LocationService,
    private dealService: DealService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const locationId = parseInt(params.get('id') || '0', 10);
      if (locationId) {
        this.loadLocationDetails(locationId);
      } else {
        this.errorMessage = 'Invalid location ID';
        this.isLoading = false;
      }
    });
  }

  loadLocationDetails(id: number): void {
    this.isLoading = true;
    this.locationService.getLocationById(id).subscribe({
      next: (location) => {
        this.location = location;
        this.loadDealsForLocation(id);
      },
      error: (error) => {
        console.error('Error loading location details:', error);
        this.errorMessage = 'Failed to load location details.';
        this.isLoading = false;
      }
    });
  }

  loadDealsForLocation(locationId: number): void {
    this.dealService.getDealsByLocation(locationId).subscribe({
      next: (deals) => {
        this.deals = deals;
        this.isLoading = false;
      },
      error: (error) => {
        console.error(`Error loading deals for location ${locationId}:`, error);
        this.errorMessage = 'Failed to load deals for this location.';
        this.isLoading = false;
      }
    });
  }

  viewDeal(dealId: number): void {
    this.router.navigate(['/deal', dealId]);
  }

  goBack(): void {
    this.router.navigate(['/home']);
  }
}
