import { Component, OnInit } from '@angular/core';
import { BookingService } from '../../services/booking.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Location } from '@angular/common';
import { trigger, transition, style, animate } from '@angular/animations';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-booking',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './booking.component.html',
  styleUrls: ['./booking.component.css'],
  animations: [
    trigger('fadeSlide', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(10px)' }),
        animate('300ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ])
    ])
  ]
})
export class BookingComponent implements OnInit {
  bookings: any[] = [];
  selectedBooking: any = null;
  newMessage: string = '';
  isLoading: boolean = true;
  error: string | null = null;

  constructor(private bookingService: BookingService, private location: Location, private authService: AuthService) {}

  ngOnInit() {
    this.loadBookings();
  }

  loadBookings() {
    this.isLoading = true;
    this.bookingService.getUserBookings().subscribe({
      next: (response) => {
        this.bookings = response;
        this.isLoading = false;
      },
      error: (err) => {
        this.error = 'Failed to load bookings';
        this.isLoading = false;
      }
    });
  }

  selectBooking(booking: any) {
    this.selectedBooking = booking;
    this.bookingService.selectedBooking = booking;
    this.loadChatMessages(booking.id);
  }

  loadChatMessages(bookingId: number) {
    this.bookingService.getChatMessages(bookingId).subscribe({
      next: (messages) => {
        if (this.selectedBooking) {
          this.selectedBooking.messages = messages;
        }
      },
      error: (err) => {
        this.error = 'Failed to load chat messages';
      }
    });
  }

  sendMessage() {
    if (!this.selectedBooking || !this.newMessage.trim()) {
      console.error('No booking selected or empty message');
      return;
    }

    console.log('Sending message for booking:', this.selectedBooking.id);
    console.log('Message content:', this.newMessage);

    this.bookingService.selectedBooking = this.selectedBooking;
    this.bookingService.sendMessage(this.selectedBooking.id, this.newMessage).subscribe({
      next: (response) => {
        console.log('Message sent successfully:', response);
        this.newMessage = '';
        this.loadChatMessages(this.selectedBooking.id);
      },
      error: (err) => {
        console.error('Error sending message:', err);
        this.error = 'Failed to send message: ' + (err.error?.message || 'Unknown error');
      }
    });
  }

  getStatusColor(status: string): string {
    switch (status?.toLowerCase()) {
      case 'confirmed':
        return 'bg-green-100 text-green-800';
      case 'pending':
        return 'bg-yellow-100 text-yellow-800';
      case 'cancelled':
        return 'bg-red-100 text-red-800';
      case 'completed':
        return 'bg-blue-100 text-blue-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  getStatusDotColor(status: string): string {
    switch (status?.toLowerCase()) {
      case 'confirmed':
        return 'bg-green-400';
      case 'pending':
        return 'bg-yellow-400';
      case 'cancelled':
        return 'bg-red-400';
      case 'completed':
        return 'bg-blue-400';
      default:
        return 'bg-gray-400';
    }
  }

  getPaymentStatusColor(status: string): string {
    switch (status?.toLowerCase()) {
      case 'paid':
        return 'text-green-600';
      case 'pending':
        return 'text-yellow-600';
      case 'failed':
        return 'text-red-600';
      case 'partial':
        return 'text-orange-600';
      default:
        return 'text-gray-600';
    }
  }

  goBack(): void {
    this.location.back();
  }
}
