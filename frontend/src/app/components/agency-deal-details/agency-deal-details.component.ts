import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { DealService } from '../../services/deal.service';
import { Deal } from '../../models/deal.model';
import { FormsModule } from '@angular/forms';
import { Location } from '@angular/common';

@Component({
  selector: 'app-agency-deal-details',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './agency-deal-details.component.html',
  styleUrls: ['./agency-deal-details.component.css']
})
export class AgencyDealDetailsComponent implements OnInit {
  deal: Deal | null = null;
  isLoading: boolean = true;
  error: string | null = null;
  success: string | null = null;
  currentImageIndex: number = 0;
  isEditing: boolean = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private dealService: DealService,
    private location: Location
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      const dealId = params['id'];
      if (dealId) {
        this.loadDeal(dealId);
      }
    });
  }

  loadDeal(id: number): void {
    this.isLoading = true;
    this.dealService.getDealById(id).subscribe({
      next: (deal) => {
        this.deal = deal;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading deal:', error);
        this.error = 'Failed to load deal details';
        this.isLoading = false;
      }
    });
  }

  nextImage(): void {
    if (this.deal?.photos && this.currentImageIndex < this.deal.photos.length - 1) {
      this.currentImageIndex++;
    }
  }

  previousImage(): void {
    if (this.currentImageIndex > 0) {
      this.currentImageIndex--;
    }
  }

  selectImage(index: number): void {
    this.currentImageIndex = index;
  }

  toggleEdit(): void {
    this.isEditing = !this.isEditing;
  }

  saveDeal(): void {
    if (!this.deal) return;

    this.dealService.updateDeal(this.deal.id, this.deal).subscribe({
      next: (updatedDeal) => {
        this.deal = updatedDeal;
        this.success = 'Deal updated successfully';
        this.isEditing = false;
      },
      error: (error) => {
        console.error('Error updating deal:', error);
        this.error = 'Failed to update deal';
      }
    });
  }

  goBack(): void {
    console.log('Going back using Location service');
    this.location.back();
  }

  handleImageError(event: any): void {
    event.target.src = 'https://travelwiseapp.s3.ap-south-1.amazonaws.com/Placeholder/placeholder-mountain.jpg';
  }
}
