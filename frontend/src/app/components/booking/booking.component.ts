import { Component, OnInit } from '@angular/core';
import { BookingService } from '../../services/booking.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-booking',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './booking.component.html',
  styleUrls: ['./booking.component.css']
})
export class BookingComponent implements OnInit {
  bookings: any[] = [];
  selectedBooking: any = null;
  newMessage: string = '';
  isLoading: boolean = true;
  error: string | null = null;

  constructor(private bookingService: BookingService) {}

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
    if (!this.selectedBooking || !this.newMessage.trim()) return;

    this.bookingService.sendMessage(this.selectedBooking.id, this.newMessage).subscribe({
      next: (response) => {
        this.newMessage = '';
        this.loadChatMessages(this.selectedBooking.id);
      },
      error: (err) => {
        this.error = 'Failed to send message';
      }
    });
  }

  getStatusColor(status: string): string {
    switch (status?.toLowerCase()) {
      case 'accepted':
        return 'bg-green-100 text-green-800';
      case 'rejected':
        return 'bg-red-100 text-red-800';
      case 'cancelled':
        return 'bg-gray-100 text-gray-800';
      case 'completed':
        return 'bg-blue-100 text-blue-800';
      default:
        return 'bg-yellow-100 text-yellow-800';
    }
  }
}
