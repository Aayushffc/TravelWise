import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Payment {
  id: string;
  bookingId: string;
  amount: number;
  status: 'pending' | 'completed' | 'failed';
  paymentMethod: string;
  customerName: string;
  createdAt: string;
  updatedAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private apiUrl = `${environment.apiUrl}/payments`;

  constructor(private http: HttpClient) {}

  getPayments(): Observable<Payment[]> {
    return this.http.get<Payment[]>(this.apiUrl);
  }

  getPaymentById(id: string): Observable<Payment> {
    return this.http.get<Payment>(`${this.apiUrl}/${id}`);
  }

  getPaymentsByBookingId(bookingId: string): Observable<Payment[]> {
    return this.http.get<Payment[]>(`${this.apiUrl}/booking/${bookingId}`);
  }

  processPayment(id: string): Observable<Payment> {
    return this.http.post<Payment>(`${this.apiUrl}/${id}/process`, {});
  }

  refundPayment(id: string): Observable<Payment> {
    return this.http.post<Payment>(`${this.apiUrl}/${id}/refund`, {});
  }

  getPaymentStats(): Observable<{
    totalEarnings: number;
    completedPayments: number;
    pendingPayments: number;
    failedPayments: number;
  }> {
    return this.http.get<{
      totalEarnings: number;
      completedPayments: number;
      pendingPayments: number;
      failedPayments: number;
    }>(`${this.apiUrl}/stats`);
  }
}
