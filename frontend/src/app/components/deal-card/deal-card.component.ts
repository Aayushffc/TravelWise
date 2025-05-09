import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { DealResponseDto } from '../../models/deal.model';
import { CurrencyService, Currency } from '../../services/currency.service';

@Component({
  selector: 'app-deal-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './deal-card.component.html',
  styleUrls: ['./deal-card.component.css']
})
export class DealCardComponent implements OnInit {
  @Input() deal!: DealResponseDto;
  currentCurrency: Currency = {
    code: 'USD',
    name: 'US Dollar',
    symbol: '$',
    flag: 'https://flagcdn.com/32x24/us.png'
  };

  constructor(
    private router: Router,
    private currencyService: CurrencyService
  ) {}

  ngOnInit() {
    this.currencyService.getCurrentCurrency().subscribe(currency => {
      this.currentCurrency = currency;
    });
  }

  onDealClick(): void {
    // Navigate to the deal details page
    this.router.navigate(['/deal', this.deal.id]);
  }

  getConvertedPrice(price: number): number {
    return this.currencyService.convertPrice(price, 'USD', this.currentCurrency.code);
  }
}
