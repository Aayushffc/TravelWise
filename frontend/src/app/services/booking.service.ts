import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';

export interface CreateBookingDTO {
  agencyId: string;
  dealId: number;
  numberOfPeople: number;
  email: string;
  phoneNumber: string;
  bookingMessage: string;
  travelDate?: Date;
  specialRequirements?: string;
}

@Injectable({
  providedIn: 'root'
})
export class BookingService {
  private apiUrl = `${environment.apiUrl}/api/bookings`;

  constructor(private http: HttpClient) {}

  createBooking(bookingData: CreateBookingDTO): Observable<any> {
    return this.http.post(`${this.apiUrl}/create`, bookingData);
  }

  getAgencyBookings(): Observable<any> {
    return this.http.get(`${this.apiUrl}/my-bookings`);
  }

  getBookingDetails(id: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/${id}`);
  }

  acceptBooking(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/accept`, {});
  }

  rejectBooking(id: number, reason: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/reject`, { reason });
  }

  cancelBooking(id: number, reason: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/cancel`, { reason });
  }

  completeBooking(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/complete`, {});
  }

  getChatMessages(bookingId: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/${bookingId}/messages`);
  }

  sendMessage(bookingId: number, message: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${bookingId}/messages`, { message });
  }

  markMessageAsRead(messageId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/messages/${messageId}/read`, {});
  }

  getUserBookings(): Observable<any> {
    return this.http.get(`${this.apiUrl}/user-bookings`);
  }
}
