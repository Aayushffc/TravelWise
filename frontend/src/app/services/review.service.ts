import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Review {
  id: number;
  dealId: number;
  userId: string;
  agencyId: string;
  userName: string;
  userPhoto: string;
  text: string;
  photos: string[];
  rating: number;
  createdAt: Date;
  updatedAt: Date;
}

export interface CreateReview {
  dealId: number;
  text: string;
  photos: string[];
  rating: number;
}

export interface UpdateReview {
  text: string;
  photos: string[];
  rating: number;
}

@Injectable({
  providedIn: 'root'
})
export class ReviewService {
  private apiUrl = `${environment.apiUrl}/api/reviews`;

  constructor(private http: HttpClient) {}

  getDealReviews(dealId: number): Observable<Review[]> {
    return this.http.get<Review[]>(`${this.apiUrl}/deal/${dealId}`);
  }

  createReview(review: CreateReview): Observable<Review> {
    return this.http.post<Review>(this.apiUrl, review);
  }

  updateReview(id: number, review: UpdateReview): Observable<Review> {
    return this.http.put<Review>(`${this.apiUrl}/${id}`, review);
  }

  deleteReview(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}