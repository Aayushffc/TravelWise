import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CurrencyService, Currency } from '../../services/currency.service';
import { trigger, transition, style, animate } from '@angular/animations';

@Component({
  selector: 'app-currency-picker',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './currency-picker.component.html',
  styleUrls: ['./currency-picker.component.css'],
  animations: [
    trigger('dropdownAnimation', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(-10px)' }),
        animate('200ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ]),
      transition(':leave', [
        animate('200ms ease-in', style({ opacity: 0, transform: 'translateY(-10px)' }))
      ])
    ])
  ]
})
export class CurrencyPickerComponent implements OnInit {
  isOpen = false;
  searchQuery = '';
  currentCurrency: Currency;
  currencies: Currency[] = [];
  filteredCurrencies: Currency[] = [];

  constructor(private currencyService: CurrencyService) {
    this.currentCurrency = this.currencyService.getPopularCurrencies()[0];
  }

  ngOnInit() {
    this.currencies = this.currencyService.getPopularCurrencies();
    this.filteredCurrencies = [...this.currencies];

    this.currencyService.getCurrentCurrency().subscribe(currency => {
      this.currentCurrency = currency;
    });
  }

  toggleDropdown() {
    this.isOpen = !this.isOpen;
  }

  filterCurrencies() {
    if (!this.searchQuery) {
      this.filteredCurrencies = [...this.currencies];
      return;
    }

    const query = this.searchQuery.toLowerCase();
    this.filteredCurrencies = this.currencies.filter(
      currency =>
        currency.name.toLowerCase().includes(query) ||
        currency.code.toLowerCase().includes(query)
    );
  }

  selectCurrency(currency: Currency) {
    this.currencyService.setCurrency(currency);
    this.isOpen = false;
  }
}
