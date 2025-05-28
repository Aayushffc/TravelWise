import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PaymentService, PaymentResponseDTO } from '../../services/payment.service';
import { BookingService } from '../../services/booking.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-payments',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule
  ],
  templateUrl: './payments.component.html',
  styleUrls: ['./payments.component.css']
})
export class PaymentsComponent implements OnInit {
  payments: any[] = [];
  filteredPayments: any[] = [];
  selectedStatus: 'all' | 'completed' | 'pending' | 'failed' = 'all';
  isLoading = true;
  stats = {
    totalEarnings: 0,
    completedPayments: 0,
    pendingPayments: 0,
    failedPayments: 0
  };
  user: any = null;
  isAgency: boolean = false;
  stripeConnectUrl: string = '';

  constructor(
    private paymentService: PaymentService,
    private bookingService: BookingService,
    private authService: AuthService
  ) {}

  async ngOnInit() {
    try {
      this.user = await this.authService.getCurrentUser();
      this.isAgency = this.user && (this.user.role === 'Agency' || this.user.roles?.includes('Agency'));
      if (this.isAgency) {
        await this.loadAgencyPayments();
        await this.loadStripeConnectUrl();
      } else {
        await this.loadPayments();
      }
      await this.loadStats();
    } catch (error) {
      console.error('Error initializing payments:', error);
    } finally {
      this.isLoading = false;
    }
  }

  loadPayments(): void {
    this.isLoading = true;
    this.paymentService.getPayments().subscribe({
      next: (payments) => {
        this.payments = payments;
        this.filterPayments();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading payments:', error);
        this.isLoading = false;
      }
    });
  }

  loadAgencyPayments(): void {
    this.isLoading = true;
    this.paymentService.getAgencyPayments(this.user.id).subscribe({
      next: (payments) => {
        this.payments = payments;
        this.filterPayments();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading agency payments:', error);
        this.isLoading = false;
      }
    });
  }

  loadStats(): void {
    this.paymentService.getPaymentStats().subscribe({
      next: (stats) => {
        this.stats = stats;
      },
      error: (error) => {
        console.error('Error loading payment stats:', error);
      }
    });
  }

  filterPayments(): void {
    if (this.selectedStatus === 'all') {
      this.filteredPayments = this.payments;
    } else {
      this.filteredPayments = this.payments.filter(
        payment => payment.status === this.selectedStatus
      );
    }
  }

  onStatusFilterChange(status: 'all' | 'completed' | 'pending' | 'failed'): void {
    this.selectedStatus = status;
    this.filterPayments();
  }

  getStatusColor(status: string): string {
    switch (status?.toLowerCase()) {
      case 'completed':
        return 'bg-green-100 text-green-800';
      case 'pending':
        return 'bg-yellow-100 text-yellow-800';
      case 'failed':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  getStatusDotColor(status: string): string {
    switch (status?.toLowerCase()) {
      case 'completed':
        return 'bg-green-500';
      case 'pending':
        return 'bg-yellow-500';
      case 'failed':
        return 'bg-red-500';
      default:
        return 'bg-gray-500';
    }
  }

  processPayment(payment: PaymentResponseDTO): void {
    this.paymentService.processPayment(payment.stripePaymentId).subscribe({
      next: (updatedPayment) => {
        const index = this.payments.findIndex(p => p.id === payment.id);
        if (index !== -1) {
          this.payments[index] = updatedPayment as PaymentResponseDTO;
          this.filterPayments();
          this.loadStats();
        }
      },
      error: (error) => {
        console.error('Error processing payment:', error);
      }
    });
  }

  acceptPayment(payment: PaymentResponseDTO): void {
    // Accept/confirm payment logic for agency (call backend API if needed)
    this.processPayment(payment);
  }

  connectStripeAccount(): void {
    if (this.stripeConnectUrl) {
      window.location.href = this.stripeConnectUrl;
    }
  }

  async loadStripeConnectUrl() {
    // This should call your backend to get the Stripe Connect onboarding link for the agency
    // For now, just a placeholder. Replace with actual API call if needed.
    this.stripeConnectUrl = '/api/stripe/connect/onboarding';
  }

  refundPayment(payment: PaymentResponseDTO): void {
    this.paymentService.refundPayment(payment.id, { reason: 'Agency refund' }).subscribe({
      next: () => {
        this.loadAgencyPayments();
        this.loadStats();
      },
      error: (error) => {
        console.error('Error refunding payment:', error);
      }
    });
  }

  viewPaymentDetails(payment: PaymentResponseDTO): void {
    // Implement payment details view logic
    console.log('View payment details:', payment);
  }
}
