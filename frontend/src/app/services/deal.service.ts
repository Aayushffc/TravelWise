import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Deal, DealResponseDto } from '../models/deal.model';

@Injectable({
  providedIn: 'root'
})
export class DealService {
  private apiUrl = `${environment.apiUrl}/api/Deal`;

  constructor(private http: HttpClient) { }

  getDeals(): Observable<DealResponseDto[]> {
    return this.http.get<DealResponseDto[]>(this.apiUrl);
  }

  getDealsByLocation(locationId: number): Observable<DealResponseDto[]> {
    return this.http.get<DealResponseDto[]>(`${this.apiUrl}?locationId=${locationId}`);
  }

  getDealById(id: number): Observable<DealResponseDto> {
    return this.http.get<DealResponseDto>(`${this.apiUrl}/${id}`);
  }

  createDeal(deal: any): Observable<DealResponseDto> {
    return this.http.post<DealResponseDto>(this.apiUrl, deal);
  }

  updateDeal(id: number, deal: any): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, deal);
  }

  deleteDeal(id: number): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`);
  }

  searchDeals(
    searchTerm?: string,
    minPrice?: number,
    maxPrice?: number,
    minDays?: number,
    maxDays?: number,
    packageType?: string
  ): Observable<DealResponseDto[]> {
    let queryParams = '';

    if (searchTerm) queryParams += `searchTerm=${searchTerm}&`;
    if (minPrice) queryParams += `minPrice=${minPrice}&`;
    if (maxPrice) queryParams += `maxPrice=${maxPrice}&`;
    if (minDays) queryParams += `minDays=${minDays}&`;
    if (maxDays) queryParams += `maxDays=${maxDays}&`;
    if (packageType) queryParams += `packageType=${packageType}&`;

    // Remove trailing & if exists
    queryParams = queryParams.endsWith('&')
      ? queryParams.slice(0, -1)
      : queryParams;

    return this.http.get<DealResponseDto[]>(`${this.apiUrl}/search?${queryParams}`);
  }

  getDealsByUserId(userId: string): Observable<DealResponseDto[]> {
    return this.http.get<DealResponseDto[]>(`${this.apiUrl}/user/${userId}`);
  }

  toggleDealStatus(id: number, isActive: boolean): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}/toggle-status`, { isActive });
  }
}
