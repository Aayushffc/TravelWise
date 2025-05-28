import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PaymentService, CreatePaymentIntentDTO } from '../../services/payment.service';
import { BookingService } from '../../services/booking.service';
import { DealService } from '../../services/deal.service';
import { firstValueFrom } from 'rxjs';
import { DealResponseDto } from '../../models/deal.model';
import { AuthService } from '../../services/auth.service';

interface Booking {
  id: number;
  totalAmount: number;
  status: string;
  travelDate: Date;
  numberOfPeople: number;
  agencyId: number;
  dealId: number;
  userName: string;
  customerEmail?: string;
  customerName?: string;
}

@Component({
  selector: 'app-payment',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './payment.component.html',
  styleUrls: ['./payment.component.css']
})
export class PaymentComponent implements OnInit {
  booking: Booking | null = null;
  deal: DealResponseDto | null = null;
  amount: number = 0;
  isProcessing: boolean = false;
  error: string | null = null;
  success: string | null = null;
  customerEmail: string = '';
  customerName: string = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private paymentService: PaymentService,
    private bookingService: BookingService,
    private dealService: DealService,
    private authService: AuthService
  ) {}

  ngOnInit() {
    const bookingId = this.route.snapshot.params['id'];
    if (bookingId) {
      this.loadBooking(Number(bookingId));
    } else {
      this.error = 'Invalid booking ID';
      this.goBack();
    }
  }

  async loadBooking(id: number) {
    try {
      const response = await firstValueFrom(this.bookingService.getAgencyBookings());
      const bookings = Array.isArray(response) ? response : response.data;
      this.booking = bookings.find((b: Booking) => b.id === id);

      if (!this.booking) {
        throw new Error('Booking not found');
      }

      // Load deal details
      if (this.booking.dealId) {
        const dealResponse = await firstValueFrom(this.dealService.getDeals());
        const deals = Array.isArray(dealResponse) ? dealResponse : [];
        this.deal = deals.find((d: DealResponseDto) => d.id === this.booking?.dealId) || null;
        // Set initial amount to deal's price
        this.amount = this.deal?.price || this.booking.totalAmount || 0;
      } else {
        this.amount = this.booking.totalAmount || 0;
      }

      // Set customer information from booking
      this.customerEmail = this.booking.customerEmail || '';
      this.customerName = this.booking.customerName || '';
    } catch (error) {
      console.error('Error loading booking:', error);
      this.error = 'Failed to load booking details';
      this.goBack();
    }
  }

  async createPaymentRequest() {
    if (!this.amount || this.isProcessing || !this.booking) return;

    try {
      this.isProcessing = true;
      this.error = null;
      this.success = null;

      const paymentIntent: CreatePaymentIntentDTO = {
        bookingId: this.booking.id,
        amount: this.amount * 100, // Convert to cents
        currency: 'usd',
        customerEmail: this.customerEmail,
        customerName: this.customerName,
        bookingCustomerEmail: this.booking.customerEmail,
        bookingCustomerName: this.booking.customerName
      };

      // Create payment intent
      await firstValueFrom(this.paymentService.createPaymentIntent(paymentIntent));

      this.success = 'Payment request created successfully. The customer will be notified to complete the payment.';

      // Wait for 2 seconds before redirecting
      setTimeout(() => {
        this.router.navigate(['/agency-dashboard'], {
          queryParams: { paymentRequestCreated: true }
        });
      }, 2000);

    } catch (error) {
      console.error('Error creating payment request:', error);
      this.error = 'Failed to create payment request. Please try again.';
    } finally {
      this.isProcessing = false;
    }
  }

  goBack() {
    this.router.navigate(['/agency-dashboard']);
  }
}
