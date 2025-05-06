import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { DealResponseDto } from '../models/deal.model';

@Injectable({
  providedIn: 'root'
})
export class WishlistService {
  private apiUrl = `${environment.apiUrl}/api/Deal`;

  constructor(private http: HttpClient) { }

  addToWishlist(dealId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${dealId}/wishlist`, {});
  }

  removeFromWishlist(dealId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${dealId}/wishlist`);
  }

  getWishlist(): Observable<DealResponseDto[]> {
    return this.http.get<DealResponseDto[]>(`${this.apiUrl}/wishlist`);
  }

  isInWishlist(dealId: number): Observable<boolean> {
    return this.http.get<boolean>(`${this.apiUrl}/${dealId}/wishlist`);
  }
}
