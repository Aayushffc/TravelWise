import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
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

  private handleError(error: HttpErrorResponse) {
    let errorMessage = 'An error occurred';
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = error.error.message;
    } else {
      // Server-side error
      errorMessage = error.error?.message || error.message;
    }
    console.error('Review service error:', error);
    return throwError(() => new Error(errorMessage));
  }

  getDealReviews(dealId: number): Observable<Review[]> {
    return this.http.get<Review[]>(`${this.apiUrl}/deal/${dealId}`).pipe(
      catchError(this.handleError)
    );
  }

  createReview(review: CreateReview): Observable<Review> {
    return this.http.post<Review>(this.apiUrl, review).pipe(
      catchError(this.handleError)
    );
  }

  updateReview(id: number, review: UpdateReview): Observable<Review> {
    return this.http.put<Review>(`${this.apiUrl}/${id}`, review).pipe(
      catchError(error => {
        console.error('Error updating review:', error);
        return this.handleError(error);
      })
    );
  }

  deleteReview(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`, {
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('token')}`
      }
    }).pipe(
      catchError(this.handleError)
    );
  }
}
