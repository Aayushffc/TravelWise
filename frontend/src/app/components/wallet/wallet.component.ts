import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { PaymentService, PaymentResponseDTO } from '../../services/payment.service';
import { AuthService } from '../../services/auth.service';
import { SidebarComponent } from '../side-bar/sidebar.component';
import { FormsModule } from '@angular/forms';
import { take } from 'rxjs';

interface User {
  id: string;
  email: string;
  fullName: string;
  role: string;
}

@Component({
  selector: 'app-wallet',
  standalone: true,
  imports: [CommonModule, SidebarComponent, FormsModule],
  templateUrl: './wallet.component.html',
  styleUrls: ['./wallet.component.css']
})
export class WalletComponent implements OnInit {
  paymentRequests: PaymentResponseDTO[] = [];
  user: User = {
    id: '',
    email: '',
    fullName: '',
    role: ''
  };
  isLoading: boolean = false;
  error: string | null = null;
  success: string | null = null;
  selectedFilter: string = 'all';
  showPaymentModal: boolean = false;
  selectedPayment: PaymentResponseDTO | null = null;

  filters = [
    { label: 'All', value: 'all' },
    { label: 'Pending', value: 'requires_payment_method' },
    { label: 'Completed', value: 'succeeded' },
    { label: 'Failed', value: 'failed' }
  ];

  constructor(
    private paymentService: PaymentService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadUserInfo();
    this.loadPaymentRequests();
  }

  async loadUserInfo() {
    try {
      const userInfo = await this.authService.getCurrentUser();
      if (userInfo) {
        this.user = {
          id: userInfo.id,
          email: userInfo.email,
          fullName: userInfo.fullName || userInfo.email,
          role: userInfo.roles?.[0] || 'User'
        };
      }
    } catch (error) {
      console.error('Error loading user info:', error);
      this.router.navigate(['/login']);
    }
  }

  async loadPaymentRequests() {
    try {
      this.isLoading = true;
      this.error = null;
      const requests = await this.paymentService.getPaymentRequests().toPromise();
      this.paymentRequests = requests || [];
    } catch (error) {
      console.error('Error loading payment requests:', error);
      this.error = 'Failed to load payment requests';
    } finally {
      this.isLoading = false;
    }
  }

  async processPayment(request: PaymentResponseDTO) {
    try {
      this.isLoading = true;
      this.error = null;
      this.success = null;

      // Create payment intent
      const paymentIntent = await this.paymentService.createPaymentIntent({
        bookingId: request.bookingId,
        amount: request.amount,
        currency: request.currency,
        customerEmail: this.user.email,
        customerName: this.user.fullName,
        bookingCustomerEmail: request.bookingCustomerEmail,
        bookingCustomerName: request.bookingCustomerName
      }).toPromise();

      if (!paymentIntent) {
        throw new Error('Failed to create payment intent');
      }

      // Initialize Stripe payment
      await this.paymentService.initializeStripePayment(paymentIntent.clientSecret);

      // Confirm payment
      await this.paymentService.confirmPayment(paymentIntent.paymentIntentId).toPromise();

      this.success = 'Payment processed successfully!';
      await this.loadPaymentRequests();
    } catch (error) {
      console.error('Error processing payment:', error);
      this.error = error instanceof Error ? error.message : 'Failed to process payment';
    } finally {
      this.isLoading = false;
    }
  }

  async downloadInvoice(paymentId: string) {
    try {
      const blob = await this.paymentService.downloadInvoice(paymentId).toPromise();
      if (blob) {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `invoice-${paymentId}.pdf`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
      }
    } catch (error) {
      console.error('Error downloading invoice:', error);
      this.error = 'Failed to download invoice';
    }
  }

  getFilteredRequests(): PaymentResponseDTO[] {
    if (this.selectedFilter === 'all') {
      return this.paymentRequests;
    }
    return this.paymentRequests.filter(request => request.status === this.selectedFilter);
  }

  getPendingPaymentsCount(): number {
    return this.paymentRequests.filter(request => request.status === 'requires_payment_method').length;
  }

  getCompletedPaymentsCount(): number {
    return this.paymentRequests.filter(request => request.status === 'succeeded').length;
  }

  getFailedPaymentsCount(): number {
    return this.paymentRequests.filter(request => request.status === 'failed').length;
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'requires_payment_method':
        return 'status-pending';
      case 'succeeded':
        return 'status-completed';
      case 'failed':
        return 'status-failed';
      case 'refunded':
        return 'status-refunded';
      default:
        return '';
    }
  }

  showPaymentForm(payment: PaymentResponseDTO) {
    this.selectedPayment = payment;
    this.showPaymentModal = true;
  }

  closePaymentModal() {
    this.showPaymentModal = false;
    this.selectedPayment = null;
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
