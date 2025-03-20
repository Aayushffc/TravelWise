import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class DealService {
  private apiUrl = `${environment.apiUrl}/api/deal`;

  constructor(private http: HttpClient) { }

  getDeals(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl);
  }

  getDealsByLocation(locationId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}?locationId=${locationId}`);
  }

  getDealById(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  searchDeals(
    searchTerm?: string,
    minPrice?: number,
    maxPrice?: number,
    minDays?: number,
    maxDays?: number,
    packageType?: string
  ): Observable<any[]> {
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

    return this.http.get<any[]>(`${this.apiUrl}/search?${queryParams}`);
  }
}
