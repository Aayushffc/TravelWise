import { Component, OnInit, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { PaymentService, PaymentResponseDTO, CreatePaymentIntentDTO } from '../../services/payment.service';
import { AuthService } from '../../services/auth.service';
import { SidebarComponent } from '../side-bar/sidebar.component';
import { FormsModule } from '@angular/forms';
import { take } from 'rxjs';
import { loadStripe } from '@stripe/stripe-js';
import { environment } from '../../../environments/environment';

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
  @ViewChild('cardElement') cardElement!: ElementRef;
  paymentRequests: PaymentResponseDTO[] = [];
  user: User = {
    id: '',
    email: '',
    fullName: '',
    role: ''
  };
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  selectedFilter: string = 'all';
  showPaymentModal: boolean = false;
  selectedPayment: PaymentResponseDTO | null = null;
  stripe: any;
  elements: any;
  card: any;

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

  async ngOnInit() {
    await this.loadStripe();
    await this.loadUserInfo();
    this.loadPaymentRequests();
  }

  async loadStripe() {
    try {
      this.stripe = await loadStripe(environment.stripePublicKey);
      this.elements = this.stripe.elements();

      // Create card element with custom styling
      this.card = this.elements.create('card', {
        style: {
          base: {
            fontSize: '16px',
            color: '#32325d',
            fontFamily: '"Helvetica Neue", Helvetica, sans-serif',
            fontSmoothing: 'antialiased',
            '::placeholder': {
              color: '#aab7c4'
            }
          },
          invalid: {
            color: '#fa755a',
            iconColor: '#fa755a'
          }
        }
      });

      // Mount the card element
      this.card.mount(this.cardElement.nativeElement);

      // Handle validation errors
      this.card.addEventListener('change', (event: any) => {
        this.errorMessage = event.error ? event.error.message : '';
      });
    } catch (error) {
      console.error('Error loading Stripe:', error);
      this.errorMessage = 'Failed to initialize payment system';
    }
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

  loadPaymentRequests() {
    this.isLoading = true;
    this.errorMessage = '';

    this.paymentService.getPaymentRequests().subscribe({
      next: (requests) => {
        this.paymentRequests = requests;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading payment requests:', error);
        this.errorMessage = 'Failed to load payment requests';
        this.isLoading = false;
      }
    });
  }

  async processPayment(payment: PaymentResponseDTO) {
    try {
      this.isLoading = true;
      this.errorMessage = '';
      this.successMessage = '';

      // Create payment intent
      const createIntentDto: CreatePaymentIntentDTO = {
        bookingId: payment.bookingId,
        amount: payment.amount,
        currency: payment.currency,
        customerEmail: payment.customerEmail,
        customerName: payment.customerName
      };

      const intentResponse = await this.paymentService.createPaymentIntent(createIntentDto).toPromise();
      if (!intentResponse) {
        throw new Error('Failed to create payment intent');
      }

      // Confirm the payment with Stripe
      const { error, paymentIntent } = await this.stripe.confirmCardPayment(
        intentResponse.clientSecret,
        {
          payment_method: {
            card: this.card,
            billing_details: {
              email: payment.customerEmail,
              name: payment.customerName
            }
          }
        }
      );

      if (error) {
        throw new Error(error.message);
      }

      if (paymentIntent.status === 'succeeded') {
        // Confirm the payment on our backend
        await this.paymentService.confirmPayment(
          intentResponse.paymentIntentId,
          paymentIntent.payment_method
        ).toPromise();

        this.successMessage = 'Payment completed successfully!';
        this.selectedPayment = null;
        this.card.clear();
        this.loadPaymentRequests(); // Refresh the list
      } else if (paymentIntent.status === 'requires_payment_method') {
        throw new Error('Payment failed. Please try again with a different card.');
      } else if (paymentIntent.status === 'requires_confirmation') {
        // Retry the confirmation
        const { error: confirmError } = await this.stripe.confirmCardPayment(
          intentResponse.clientSecret
        );
        if (confirmError) {
          throw new Error(confirmError.message);
        }
      }
    } catch (error: any) {
      console.error('Payment error:', error);
      this.errorMessage = error.message || 'Payment failed';
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
      this.errorMessage = 'Failed to download invoice';
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

  async showPaymentForm(payment: PaymentResponseDTO) {
    this.selectedPayment = payment;
    this.showPaymentModal = true;
    this.errorMessage = '';
    this.successMessage = '';

    // Wait for the modal to be visible
    setTimeout(() => {
      this.card.mount(this.cardElement.nativeElement);
    }, 100);
  }

  closePaymentModal() {
    this.showPaymentModal = false;
    this.selectedPayment = null;
    this.errorMessage = '';
    this.successMessage = '';
    this.card.clear();
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString();
  }

  formatAmount(amount: number): string {
    return amount.toFixed(2);
  }

  goBack() {
    this.router.navigate(['/agency-dashboard']);
  }
}
