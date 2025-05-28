import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { loadStripe, Stripe } from '@stripe/stripe-js';

export interface Payment {
  id: string;
  bookingId: number;
  amount: number;
  status: 'pending' | 'completed' | 'failed';
  paymentMethod: string;
  customerName: string;
  createdAt: Date;
  updatedAt: string;
  booking: {
    id: number;
    travelDate: Date;
    numberOfPeople: number;
    agencyName: string;
  };
}

export interface CreatePaymentIntentDTO {
  bookingId: number;
  amount: number;
  currency: string;
  customerEmail?: string;
  customerName?: string;
  bookingCustomerEmail?: string;
  bookingCustomerName?: string;
  description?: string;
}

export interface PaymentIntentResponseDTO {
  clientSecret: string;
  paymentIntentId: string;
  amount: number;
  currency: string;
  status: string;
}

export interface PaymentResponseDTO {
  id: number;
  stripePaymentId: string;
  bookingId: number;
  agencyId: number;
  amount: number;
  currency: string;
  status: string;
  paymentMethod?: string;
  customerId?: string;
  customerEmail?: string;
  customerName?: string;
  bookingCustomerEmail?: string;
  bookingCustomerName?: string;
  errorMessage?: string;
  refundReason?: string;
  createdAt: Date;
  updatedAt?: Date;
  paidAt?: Date;
  refundedAt?: Date;
}

export interface RefundPaymentDTO {
  reason: string;
}

export interface PaymentStats {
  totalEarnings: number;
  completedPayments: number;
  pendingPayments: number;
  failedPayments: number;
}

export interface PaymentIntentRequest {
  bookingId: number;
  amount: number;
  currency: string;
}

export interface PaymentRequest {
  id: number;
  bookingId: number;
  amount: number;
  status: 'pending' | 'completed' | 'failed';
  createdAt: Date;
}

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private apiUrl = `${environment.apiUrl}/api/payment`;
  private stripePromise: Promise<Stripe | null>;

  constructor(private http: HttpClient) {
    this.stripePromise = loadStripe(environment.stripePublicKey);
  }

  getPayments(): Observable<Payment[]> {
    return this.http.get<Payment[]>(this.apiUrl);
  }

  getPaymentById(id: string): Observable<Payment> {
    return this.http.get<Payment>(`${this.apiUrl}/${id}`);
  }

  getPaymentsByBookingId(bookingId: number): Observable<Payment[]> {
    return this.http.get<Payment[]>(`${this.apiUrl}/booking/${bookingId}`);
  }

  processPayment(paymentIntentId: string): Observable<PaymentResponseDTO> {
    return this.http.post<PaymentResponseDTO>(`${this.apiUrl}/confirm/${paymentIntentId}`, {});
  }

  getPaymentStats(): Observable<PaymentStats> {
    return this.http.get<PaymentStats>(`${this.apiUrl}/stats`);
  }

  createPaymentIntent(dto: CreatePaymentIntentDTO): Observable<PaymentIntentResponseDTO> {
    return this.http.post<PaymentIntentResponseDTO>(`${this.apiUrl}/create-intent`, dto);
  }

  confirmPayment(paymentIntentId: string): Observable<PaymentResponseDTO> {
    return this.http.post<PaymentResponseDTO>(`${this.apiUrl}/confirm/${paymentIntentId}`, {});
  }

  getPayment(paymentIntentId: string): Observable<PaymentResponseDTO> {
    return this.http.get<PaymentResponseDTO>(`${this.apiUrl}/${paymentIntentId}`);
  }

  getPaymentRequests(): Observable<PaymentResponseDTO[]> {
    return this.http.get<PaymentResponseDTO[]>(`${this.apiUrl}/requests`);
  }

  getAgencyPayments(agencyId: number): Observable<PaymentResponseDTO[]> {
    return this.http.get<PaymentResponseDTO[]>(`${this.apiUrl}/agency/${agencyId}`);
  }

  refundPayment(paymentId: number, dto: RefundPaymentDTO): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/refund/${paymentId}`, dto);
  }

  async initializeStripePayment(clientSecret: string) {
    const stripe = await this.stripePromise;
    if (!stripe) {
      throw new Error('Stripe failed to initialize');
    }

    const { error } = await stripe.confirmCardPayment(clientSecret);
    if (error) {
      throw new Error(error.message);
    }
  }

  downloadInvoice(paymentId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${paymentId}/invoice`, { responseType: 'blob' });
  }
}
