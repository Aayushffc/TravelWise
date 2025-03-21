import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';

interface AgencyApplication {
  id: number;
  userId: string;
  userName: string;
  email: string;
  status: string;
  submittedAt: Date;
}

interface FAQ {
  id: number;
  question: string;
  answer: string;
  category: string;
  isActive: boolean;
  createdAt: Date;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-dashboard.component.html'
})
export class AdminDashboardComponent implements OnInit {
  activeTab = 'agencies';
  agencyApplications: AgencyApplication[] = [];
  faqs: FAQ[] = [];
  isLoading = true;
  errorMessage = '';

  activeTabClass = 'border-indigo-500 text-indigo-600 whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm';
  inactiveTabClass = 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm';

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.loadAgencyApplications();
    this.loadFAQs();
  }

  setActiveTab(tab: string) {
    this.activeTab = tab;
  }

  loadAgencyApplications() {
    this.isLoading = true;
    this.http.get<AgencyApplication[]>(`${environment.apiUrl}/api/admin/agency-applications`)
      .subscribe({
        next: (applications) => {
          this.agencyApplications = applications;
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error loading agency applications:', error);
          this.errorMessage = 'Failed to load agency applications';
          this.isLoading = false;
        }
      });
  }

  loadFAQs() {
    this.isLoading = true;
    this.http.get<FAQ[]>(`${environment.apiUrl}/api/admin/faqs`)
      .subscribe({
        next: (faqs) => {
          this.faqs = faqs;
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error loading FAQs:', error);
          this.errorMessage = 'Failed to load FAQs';
          this.isLoading = false;
        }
      });
  }

  approveAgency(id: number) {
    this.http.post(`${environment.apiUrl}/api/admin/agency-applications/${id}/approve`, {})
      .subscribe({
        next: () => {
          this.loadAgencyApplications();
        },
        error: (error) => {
          console.error('Error approving agency application:', error);
          this.errorMessage = 'Failed to approve agency application';
        }
      });
  }

  rejectAgency(id: number) {
    this.http.post(`${environment.apiUrl}/api/admin/agency-applications/${id}/reject`, {})
      .subscribe({
        next: () => {
          this.loadAgencyApplications();
        },
        error: (error) => {
          console.error('Error rejecting agency application:', error);
          this.errorMessage = 'Failed to reject agency application';
        }
      });
  }

  toggleFAQStatus(id: number) {
    this.http.post(`${environment.apiUrl}/api/admin/faqs/${id}/toggle-status`, {})
      .subscribe({
        next: () => {
          this.loadFAQs();
        },
        error: (error) => {
          console.error('Error toggling FAQ status:', error);
          this.errorMessage = 'Failed to update FAQ status';
        }
      });
  }

  logout() {
    this.authService.logout();
  }
}
