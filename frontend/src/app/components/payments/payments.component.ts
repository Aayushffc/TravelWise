import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PaymentService, PaymentResponseDTO } from '../../services/payment.service';
import { BookingService } from '../../services/booking.service';
import { AuthService } from '../../services/auth.service';
import { StripeConnectService, StripeConnectStatus } from '../../services/stripe-connect.service';

interface Payment {
  id: string;
  bookingId: number;
  amount: number;
  status: 'pending' | 'completed' | 'failed';
  paymentMethod: string;
  customerName: string;
  createdAt: string;
}

interface Stats {
  totalEarnings: number;
  completedPayments: number;
  pendingPayments: number;
  failedPayments: number;
}

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
  selectedStatus = 'all';
  isLoading = true;
  stats: Stats = {
    totalEarnings: 0,
    completedPayments: 0,
    pendingPayments: 0,
    failedPayments: 0
  };
  user: any = null;
  isAgency = false;
  stripeConnectStatus: StripeConnectStatus | null = null;
  errorMessage: string | null = null;

  constructor(
    private paymentService: PaymentService,
    private bookingService: BookingService,
    private authService: AuthService,
    private stripeConnectService: StripeConnectService
  ) {}

  async ngOnInit() {
    try {
      this.isLoading = true;
      this.user = await this.authService.getCurrentUser();
      this.isAgency = this.user && (this.user.role === 'Agency' || this.user.roles?.includes('Agency'));

      if (this.isAgency) {
        // First check Stripe Connect status
        await this.loadStripeConnectStatus();

        // Only load payments and stats if connected to Stripe
        if (this.stripeConnectStatus?.isConnected && this.stripeConnectStatus?.isEnabled) {
          await this.loadAgencyPayments();
          await this.loadStats();
        }
      } else {
        // For non-agency users, just load their payments
        await this.loadPayments();
        await this.loadStats();
      }
    } catch (error) {
      console.error('Error initializing payments:', error);
      this.errorMessage = 'Failed to load payment information. Please try again.';
    } finally {
      this.isLoading = false;
    }
  }

  private async loadPayments() {
    try {
      this.isLoading = true;
      const response = await this.paymentService.getPayments().toPromise();
      if (response) {
        this.payments = response.map(p => ({
          id: p.id,
          bookingId: p.bookingId,
          amount: p.amount,
          status: p.status,
          paymentMethod: p.paymentMethod || 'Unknown',
          customerName: p.customerName || 'Unknown',
          createdAt: p.createdAt.toString()
        }));
        this.filteredPayments = this.payments;
        this.calculateStats();
      }
    } catch (error) {
      console.error('Error loading payments:', error);
      this.errorMessage = 'Failed to load payments. Please try again.';
    } finally {
      this.isLoading = false;
    }
  }

  loadAgencyPayments(): void {
    this.isLoading = true;
    this.paymentService.getAgencyPayments(this.user.id).subscribe({
      next: (payments) => {
        this.payments = payments.map(p => ({
          id: p.id.toString(),
          bookingId: p.bookingId,
          amount: p.amount,
          status: p.status as 'pending' | 'completed' | 'failed',
          paymentMethod: p.paymentMethod || 'Unknown',
          customerName: p.customerName || 'Unknown',
          createdAt: p.createdAt.toString()
        }));
        this.filterPayments();
        this.calculateStats();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading agency payments:', error);
        this.errorMessage = 'Failed to load agency payments. Please try again.';
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
        this.errorMessage = 'Failed to load payment statistics. Please try again.';
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

  onStatusFilterChange(status: string) {
    this.selectedStatus = status;
    this.filterPayments();
  }

  getStatusColor(status: string): string {
    switch (status.toLowerCase()) {
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

  getStatusIcon(status: string): string {
    switch (status.toLowerCase()) {
      case 'completed':
        return 'fas fa-check-circle';
      case 'pending':
        return 'fas fa-clock';
      case 'failed':
        return 'fas fa-times-circle';
      default:
        return 'fas fa-question-circle';
    }
  }

  getStatusDotColor(status: string): string {
    switch (status.toLowerCase()) {
      case 'completed':
        return 'bg-green-400';
      case 'pending':
        return 'bg-yellow-400';
      case 'failed':
        return 'bg-red-400';
      default:
        return 'bg-gray-400';
    }
  }

  async connectStripeAccount() {
    try {
      this.isLoading = true;
      this.errorMessage = null;
      const response = await this.stripeConnectService.createConnectAccount().toPromise();
      if (response) {
        window.location.href = response.accountLink;
      }
    } catch (error) {
      console.error('Error connecting Stripe account:', error);
      this.errorMessage = 'Failed to connect Stripe account. Please try again.';
    } finally {
      this.isLoading = false;
    }
  }

  async loadStripeConnectStatus() {
    if (!this.isAgency) return;

    try {
      this.isLoading = true;
      this.errorMessage = null;
      const response = await this.stripeConnectService.getConnectStatus().toPromise();
      this.stripeConnectStatus = response || null;

      // If connected but not enabled, try to create an account link
      if (this.stripeConnectStatus?.isConnected && !this.stripeConnectStatus?.isEnabled) {
        const accountLink = await this.stripeConnectService.createAccountLink().toPromise();
        if (accountLink) {
          window.location.href = accountLink.url;
        }
      }
    } catch (error) {
      console.error('Error loading Stripe Connect status:', error);
      this.errorMessage = 'Failed to load Stripe connection status. Please try again.';
      this.stripeConnectStatus = null;
    } finally {
      this.isLoading = false;
    }
  }

  async processPayment(payment: Payment) {
    try {
      this.isLoading = true;
      this.errorMessage = null;
      const response = await this.paymentService.processPayment(payment.id).toPromise();
      if (response) {
        // Refresh the payments list
        await this.loadAgencyPayments();
      }
    } catch (error) {
      console.error('Error processing payment:', error);
      this.errorMessage = 'Failed to process payment. Please try again.';
    } finally {
      this.isLoading = false;
    }
  }

  async refundPayment(payment: Payment) {
    try {
      this.isLoading = true;
      this.errorMessage = null;
      await this.paymentService.refundPayment(parseInt(payment.id), { reason: 'Refund requested by agency' }).toPromise();
      // Refresh the payments list
      await this.loadAgencyPayments();
    } catch (error) {
      console.error('Error refunding payment:', error);
      this.errorMessage = 'Failed to refund payment. Please try again.';
    } finally {
      this.isLoading = false;
    }
  }

  viewPaymentDetails(payment: Payment) {
    // TODO: Implement payment details view
    console.log('View payment details:', payment);
  }

  private calculateStats() {
    this.stats = {
      totalEarnings: this.payments
        .filter(p => p.status === 'completed')
        .reduce((sum, p) => sum + p.amount, 0),
      completedPayments: this.payments.filter(p => p.status === 'completed').length,
      pendingPayments: this.payments.filter(p => p.status === 'pending').length,
      failedPayments: this.payments.filter(p => p.status === 'failed').length
    };
  }

  getRequirementsList(requirementsJson: string | undefined): string[] {
    if (!requirementsJson) return [];
    try {
      const requirements = JSON.parse(requirementsJson);
      const allRequirements = [
        ...(requirements.currently_due || []),
        ...(requirements.eventually_due || []),
        ...(requirements.past_due || []),
        ...(requirements.pending_verification || [])
      ];
      return allRequirements;
    } catch (error) {
      console.error('Error parsing requirements:', error);
      return [];
    }
  }
}

