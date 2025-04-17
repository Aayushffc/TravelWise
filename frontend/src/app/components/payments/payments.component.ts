import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PaymentService, Payment } from '../../services/payment.service';
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
  payments: Payment[] = [];
  filteredPayments: Payment[] = [];
  selectedStatus: 'all' | 'completed' | 'pending' | 'failed' = 'all';
  isLoading = true;
  stats = {
    totalEarnings: 0,
    completedPayments: 0,
    pendingPayments: 0,
    failedPayments: 0
  };
  user: any = null;

  constructor(
    private paymentService: PaymentService,
    private bookingService: BookingService,
    private authService: AuthService
  ) {}

  async ngOnInit() {
    try {
      this.user = await this.authService.getCurrentUser();
      await this.loadPayments();
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

  processPayment(payment: Payment): void {
    this.paymentService.processPayment(payment.id).subscribe({
      next: (updatedPayment) => {
        const index = this.payments.findIndex(p => p.id === payment.id);
        if (index !== -1) {
          this.payments[index] = updatedPayment;
          this.filterPayments();
          this.loadStats();
        }
      },
      error: (error) => {
        console.error('Error processing payment:', error);
      }
    });
  }

  refundPayment(payment: Payment): void {
    this.paymentService.refundPayment(payment.id).subscribe({
      next: (updatedPayment) => {
        const index = this.payments.findIndex(p => p.id === payment.id);
        if (index !== -1) {
          this.payments[index] = updatedPayment;
          this.filterPayments();
          this.loadStats();
        }
      },
      error: (error) => {
        console.error('Error refunding payment:', error);
      }
    });
  }

  viewPaymentDetails(payment: Payment): void {
    // Implement payment details view logic
    console.log('View payment details:', payment);
  }
}
