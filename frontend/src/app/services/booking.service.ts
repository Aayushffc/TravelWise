import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';

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
  public selectedBooking: any = null;

  constructor(private http: HttpClient, private authService: AuthService) {}

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
    if (!this.selectedBooking) {
      throw new Error('No booking selected');
    }

    // Get the current user's ID from the auth service
    const currentUserId = this.authService.getCurrentUser()?.id;
    if (!currentUserId) {
      throw new Error('User not authenticated');
    }

    // If the current user is the agency, send to the customer (userId)
    // If the current user is the customer, send to the agency (agencyId)
    const receiverId = currentUserId === this.selectedBooking.agencyId
      ? this.selectedBooking.userId
      : this.selectedBooking.agencyId;

    // Format the message payload according to the backend DTO
    const payload = {
      receiverId,
      message,
      messageType: 'text'
    };

    console.log('Sending message with payload:', payload);

    return this.http.post(`${this.apiUrl}/${bookingId}/messages`, payload);
  }

  markMessageAsRead(messageId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/messages/${messageId}/read`, {});
  }

  getUserBookings(): Observable<any> {
    return this.http.get(`${this.apiUrl}/user-bookings`);
  }
}
