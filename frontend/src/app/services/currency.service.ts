import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';

export interface Currency {
  code: string;
  name: string;
  symbol: string;
  flag: string;
}

@Injectable({
  providedIn: 'root'
})
export class CurrencyService {
  private readonly API_KEY = '3252c000f5fd4cdc2e5ce9ec'; // You'll need to get this from https://www.exchangerate-api.com/
  private readonly BASE_URL = 'https://v6.exchangerate-api.com/v6';

  private currentCurrency = new BehaviorSubject<Currency>({
    code: 'USD',
    name: 'US Dollar',
    symbol: '$',
    flag: 'https://flagcdn.com/32x24/us.png'
  });

  private exchangeRates: { [key: string]: number } = {};

  constructor(private http: HttpClient) {
    this.loadExchangeRates();
  }

  private loadExchangeRates() {
    this.http.get(`${this.BASE_URL}/${this.API_KEY}/latest/USD`)
      .subscribe((data: any) => {
        this.exchangeRates = data.conversion_rates;
      });
  }

  getCurrentCurrency(): Observable<Currency> {
    return this.currentCurrency.asObservable();
  }

  setCurrency(currency: Currency) {
    this.currentCurrency.next(currency);
  }

  convertPrice(price: number, fromCurrency: string = 'USD', toCurrency: string): number {
    if (fromCurrency === toCurrency) return price;

    const rate = this.exchangeRates[toCurrency];
    if (!rate) return price;

    return price * rate;
  }

  getPopularCurrencies(): Currency[] {
    return [
      { code: 'USD', name: 'US Dollar', symbol: '$', flag: 'https://flagcdn.com/32x24/us.png' },
      { code: 'EUR', name: 'Euro', symbol: '€', flag: 'https://flagcdn.com/32x24/eu.png' },
      { code: 'GBP', name: 'British Pound', symbol: '£', flag: 'https://flagcdn.com/32x24/gb.png' },
      { code: 'INR', name: 'Indian Rupee', symbol: '₹', flag: 'https://flagcdn.com/32x24/in.png' },
      { code: 'JPY', name: 'Japanese Yen', symbol: '¥', flag: 'https://flagcdn.com/32x24/jp.png' },
      { code: 'AUD', name: 'Australian Dollar', symbol: 'A$', flag: 'https://flagcdn.com/32x24/au.png' },
      { code: 'CAD', name: 'Canadian Dollar', symbol: '$', flag: 'https://flagcdn.com/32x24/ca.png' },
      { code: 'CHF', name: 'Swiss Franc', symbol: 'CHF', flag: 'https://flagcdn.com/32x24/ch.png' },
      { code: 'CNY', name: 'Chinese Renminbi Yuan', symbol: '¥', flag: 'https://flagcdn.com/32x24/cn.png' },
      { code: 'SGD', name: 'Singapore Dollar', symbol: '$', flag: 'https://flagcdn.com/32x24/sg.png' },
      { code: 'BBD', name: 'Barbadian Dollar', symbol: '$', flag: 'https://flagcdn.com/32x24/bb.png' },
      { code: 'BDT', name: 'Bangladeshi Taka', symbol: '৳', flag: 'https://flagcdn.com/32x24/bd.png' },
      { code: 'BOB', name: 'Bolivian Boliviano', symbol: 'Bs.', flag: 'https://flagcdn.com/32x24/bo.png' },
      { code: 'BSD', name: 'Bahamian Dollar', symbol: '$', flag: 'https://flagcdn.com/32x24/bs.png' },
      { code: 'BWP', name: 'Botswana Pula', symbol: 'P', flag: 'https://flagcdn.com/32x24/bw.png' },
      { code: 'BZD', name: 'Belize Dollar', symbol: '$', flag: 'https://flagcdn.com/32x24/bz.png' },
      { code: 'BMD', name: 'Bermudian Dollar', symbol: '$', flag: 'https://flagcdn.com/32x24/bm.png' },
      { code: 'BND', name: 'Brunei Dollar', symbol: '$', flag: 'https://flagcdn.com/32x24/bn.png' },
      { code: 'COP', name: 'Colombian Peso', symbol: '$', flag: 'https://flagcdn.com/32x24/co.png' },
      { code: 'CRC', name: 'Costa Rican Colón', symbol: '₡', flag: 'https://flagcdn.com/32x24/cr.png' },
      { code: 'CZK', name: 'Czech Koruna', symbol: 'Kč', flag: 'https://flagcdn.com/32x24/cz.png' },
      { code: 'DKK', name: 'Danish Krone', symbol: 'kr.', flag: 'https://flagcdn.com/32x24/dk.png' },
      { code: 'DOP', name: 'Dominican Peso', symbol: '$', flag: 'https://flagcdn.com/32x24/do.png' },
      { code: 'DZD', name: 'Algerian Dinar', symbol: 'دج', flag: 'https://flagcdn.com/32x24/dz.png' },
      { code: 'EGP', name: 'Egyptian Pound', symbol: '£', flag: 'https://flagcdn.com/32x24/eg.png' },
      { code: 'ETB', name: 'Ethiopian Birr', symbol: 'Br', flag: 'https://flagcdn.com/32x24/et.png' },
      { code: 'FJD', name: 'Fijian Dollar', symbol: '$', flag: 'https://flagcdn.com/32x24/fj.png' },
      { code: 'GHS', name: 'Ghanaian Cedi', symbol: '₵', flag: 'https://flagcdn.com/32x24/gh.png' },
      { code: 'GIP', name: 'Gibraltar Pound', symbol: '£', flag: 'https://flagcdn.com/32x24/gi.png' },
      { code: 'GMD', name: 'Gambian Dalasi', symbol: 'D', flag: 'https://flagcdn.com/32x24/gm.png' },
      { code: 'GTQ', name: 'Guatemalan Quetzal', symbol: 'Q', flag: 'https://flagcdn.com/32x24/gt.png' },
      { code: 'GYD', name: 'Guyanese Dollar', symbol: '$', flag: 'https://flagcdn.com/32x24/gy.png' },
      { code: 'HKD', name: 'Hong Kong Dollar', symbol: '$', flag: 'https://flagcdn.com/32x24/hk.png' },
      { code: 'HNL', name: 'Honduran Lempira', symbol: 'L', flag: 'https://flagcdn.com/32x24/hn.png' },
      { code: 'HRK', name: 'Croatian Kuna', symbol: 'kn', flag: 'https://flagcdn.com/32x24/hr.png' },

      // Additional currencies:
      { code: 'IDR', name: 'Indonesian Rupiah', symbol: 'Rp', flag: 'https://flagcdn.com/32x24/id.png' },
      { code: 'ILS', name: 'Israeli Shekel', symbol: '₪', flag: 'https://flagcdn.com/32x24/il.png' },
      { code: 'KRW', name: 'South Korean Won', symbol: '₩', flag: 'https://flagcdn.com/32x24/kr.png' },
      { code: 'MXN', name: 'Mexican Peso', symbol: '$', flag: 'https://flagcdn.com/32x24/mx.png' },
      { code: 'MYR', name: 'Malaysian Ringgit', symbol: 'RM', flag: 'https://flagcdn.com/32x24/my.png' },
      { code: 'NOK', name: 'Norwegian Krone', symbol: 'kr', flag: 'https://flagcdn.com/32x24/no.png' },
      { code: 'NZD', name: 'New Zealand Dollar', symbol: '$', flag: 'https://flagcdn.com/32x24/nz.png' },
      { code: 'PHP', name: 'Philippine Peso', symbol: '₱', flag: 'https://flagcdn.com/32x24/ph.png' },
      { code: 'PKR', name: 'Pakistani Rupee', symbol: '₨', flag: 'https://flagcdn.com/32x24/pk.png' },
      { code: 'PLN', name: 'Polish Zloty', symbol: 'zł', flag: 'https://flagcdn.com/32x24/pl.png' },
      { code: 'RUB', name: 'Russian Ruble', symbol: '₽', flag: 'https://flagcdn.com/32x24/ru.png' },
      { code: 'SAR', name: 'Saudi Riyal', symbol: '﷼', flag: 'https://flagcdn.com/32x24/sa.png' },
      { code: 'SEK', name: 'Swedish Krona', symbol: 'kr', flag: 'https://flagcdn.com/32x24/se.png' },
      { code: 'THB', name: 'Thai Baht', symbol: '฿', flag: 'https://flagcdn.com/32x24/th.png' },
      { code: 'TRY', name: 'Turkish Lira', symbol: '₺', flag: 'https://flagcdn.com/32x24/tr.png' },
      { code: 'UAH', name: 'Ukrainian Hryvnia', symbol: '₴', flag: 'https://flagcdn.com/32x24/ua.png' },
      { code: 'VND', name: 'Vietnamese Dong', symbol: '₫', flag: 'https://flagcdn.com/32x24/vn.png' },
      { code: 'ZAR', name: 'South African Rand', symbol: 'R', flag: 'https://flagcdn.com/32x24/za.png' }
    ];
  }
}
