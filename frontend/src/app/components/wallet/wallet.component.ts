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
  selectedFilter: string = 'all';

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
      await this.paymentService.confirmPayment(request.stripePaymentId).toPromise();
      await this.loadPaymentRequests();
    } catch (error) {
      console.error('Error processing payment:', error);
      this.error = 'Failed to process payment';
    } finally {
      this.isLoading = false;
    }
  }

  getFilteredRequests(): PaymentResponseDTO[] {
    if (this.selectedFilter === 'all') {
      return this.paymentRequests;
    }
    return this.paymentRequests.filter(request => request.status === this.selectedFilter);
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

  goBack() {
    this.router.navigate(['/agency-dashboard']);
  }
}
