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
  email: string;
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
  showPaymentForm: boolean = false;

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
      this.customerEmail = this.booking.email || '';
      this.customerName = this.booking.userName || '';
    } catch (error) {
      console.error('Error loading booking:', error);
      this.error = 'Failed to load booking details';
      this.goBack();
    }
  }

  createPaymentRequest() {
    if (!this.amount || this.amount <= 0) {
      this.error = 'Please enter a valid amount';
      return;
    }

    if (!this.booking?.id) {
      this.error = 'Invalid booking information';
      return;
    }

    this.isProcessing = true;
    this.error = null;
    this.success = null;

    const paymentData: CreatePaymentIntentDTO = {
      amount: this.amount, // Amount is already in cents from the form
      currency: 'usd',
      customerEmail: this.customerEmail,
      customerName: this.customerName,
      bookingId: this.booking.id,
      description: `Payment for booking #${this.booking.id}`
    };

    this.paymentService.createPaymentIntent(paymentData).subscribe({
      next: (response) => {
        this.isProcessing = false;
        this.success = 'Payment request created successfully';
        // Handle successful payment creation
        if (response.clientSecret) {
          // Handle Stripe payment flow
          this.handleStripePayment(response.clientSecret);
        }
      },
      error: (err) => {
        this.isProcessing = false;
        if (err.error) {
          if (err.error.message) {
            this.error = err.error.message;
          } else if (err.error.error) {
            this.error = err.error.error;
          } else if (err.error.details) {
            this.error = err.error.details;
          } else {
            this.error = 'Failed to create payment request. Please try again.';
          }
        } else {
          this.error = 'Failed to create payment request. Please try again.';
        }
        console.error('Payment request error:', err);
      }
    });
  }

  private handleStripePayment(clientSecret: string) {
    // Implement Stripe payment handling logic here
    // This could include redirecting to Stripe's hosted payment page
    // or implementing a custom payment form
    console.log('Handling Stripe payment with client secret:', clientSecret);
    // For now, we'll just show a success message
    this.success = 'Payment request created successfully. The customer will be notified to complete the payment.';
    this.showPaymentForm = false;

    // Wait for 2 seconds before redirecting
    setTimeout(() => {
      this.router.navigate(['/agency-dashboard'], {
        queryParams: { paymentRequestCreated: true }
      });
    }, 2000);
  }

  showPaymentRequestForm() {
    this.showPaymentForm = true;
  }

  closePaymentForm() {
    this.showPaymentForm = false;
  }

  formatDate(date: Date | string): string {
    return new Date(date).toLocaleDateString();
  }

  formatAmount(amount: number): string {
    return (amount / 100).toFixed(2);
  }

  goBack() {
    this.router.navigate(['/agency-dashboard']);
  }
}
