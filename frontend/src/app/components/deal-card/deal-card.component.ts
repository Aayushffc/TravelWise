import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

interface Deal {
  id: number;
  title: string;
  description: string;
  price: number;
  discountedPrice?: number;
  discountPercentage?: number;
  daysCount: number;
  nightsCount: number;
  photos?: string[];
  rating?: number | null | undefined;
  location?: {
    id: number;
    name: string;
  };
}

@Component({
  selector: 'app-deal-card',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './deal-card.component.html',
  styleUrls: ['./deal-card.component.css']
})
export class DealCardComponent {
  @Input() deal!: Deal;
  @Input() redirectUrl: string = '/location-details';
  @Output() dealAction = new EventEmitter<any>();

  onDealClick(): void {
    this.dealAction.emit(this.deal);
  }
}
