import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { DealResponseDto } from '../../models/deal.model';

@Component({
  selector: 'app-deal-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './deal-card.component.html',
  styleUrls: ['./deal-card.component.css']
})
export class DealCardComponent {
  @Input() deal!: DealResponseDto;

  constructor(private router: Router) {}

  onDealClick(): void {
    // Navigate to the deal details page
    this.router.navigate(['/deal', this.deal.id]);
  }
}
