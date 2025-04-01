import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface SearchParams {
  searchTerm?: string;
  minPrice?: number;
  maxPrice?: number;
  minDays?: number;
  maxDays?: number;
  packageType?: string;
  location?: string;
  country?: string;
  continent?: string;
  agencyId?: string;
  sortBy?: string;
  page?: number;
  pageSize?: number;
  tags?: string;
  categories?: string;
  seasons?: string;
  difficultyLevel?: string;
  isInstantBooking?: boolean;
  isLastMinuteDeal?: boolean;
  validFrom?: Date;
  validUntil?: Date;
}

export interface SearchResponse {
  deals: any[];
  totalCount: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
}

@Injectable({
  providedIn: 'root'
})
export class SearchService {
  private apiUrl = `${environment.apiUrl}/api/home`;

  constructor(private http: HttpClient) { }

  searchDeals(params: SearchParams): Observable<SearchResponse> {
    let httpParams = new HttpParams();

    // Add all non-null parameters to the query
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null) {
        if (value instanceof Date) {
          httpParams = httpParams.append(key, value.toISOString());
        } else {
          httpParams = httpParams.append(key, value.toString());
        }
      }
    });

    return this.http.get<SearchResponse>(`${this.apiUrl}/search`, { params: httpParams });
  }

  recordDealClick(dealId: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/deal/${dealId}/click`, {});
  }
}
