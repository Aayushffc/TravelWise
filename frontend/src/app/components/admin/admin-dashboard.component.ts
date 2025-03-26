import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';
import { AgencyApplicationService, AgencyApplicationResponseDTO } from '../../services/agency-application.service';
import { FormsModule } from '@angular/forms';
import { ManageLocationsComponent } from './manage-locations/manage-locations.component';

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
  imports: [CommonModule, RouterModule, FormsModule, ManageLocationsComponent],
  templateUrl: './admin-dashboard.component.html'
})
export class AdminDashboardComponent implements OnInit {
  activeTab = 'agencies';
  agencyApplications: AgencyApplicationResponseDTO[] = [];
  faqs: FAQ[] = [];
  isLoading = true;
  errorMessage = '';
  rejectDialogOpen = false;
  selectedApplicationId: number | null = null;
  rejectionReason = '';

  activeTabClass = 'border-indigo-500 text-indigo-600 whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm';
  inactiveTabClass = 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm';

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private agencyApplicationService: AgencyApplicationService
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
    this.errorMessage = '';
    this.agencyApplicationService.getAllApplications().subscribe({
      next: (applications) => {
        this.agencyApplications = applications;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading agency applications:', error);
        if (error.status === 403) {
          this.errorMessage = 'You do not have permission to view agency applications. Please ensure you have admin access.';
        } else {
          this.errorMessage = error.error?.message || 'Failed to load agency applications';
        }
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
    this.errorMessage = '';
    this.agencyApplicationService.approveApplication(id).subscribe({
      next: () => {
        this.loadAgencyApplications();
      },
      error: (error) => {
        console.error('Error approving agency application:', error);
        if (error.status === 403) {
          this.errorMessage = 'You do not have permission to approve applications. Please ensure you have admin access.';
        } else {
          this.errorMessage = error.error?.message || 'Failed to approve agency application';
        }
      }
    });
  }

  openRejectDialog(id: number) {
    this.selectedApplicationId = id;
    this.rejectDialogOpen = true;
  }

  closeRejectDialog() {
    this.rejectDialogOpen = false;
    this.selectedApplicationId = null;
    this.rejectionReason = '';
  }

  isValidRejectionReason(): boolean {
    const trimmedReason = this.rejectionReason.trim();
    return trimmedReason.length >= 10 && trimmedReason.length <= 500;
  }

  rejectAgency() {
    if (!this.selectedApplicationId || !this.isValidRejectionReason()) return;

    this.errorMessage = '';
    this.agencyApplicationService.rejectApplication(this.selectedApplicationId, this.rejectionReason.trim()).subscribe({
      next: () => {
        this.loadAgencyApplications();
        this.closeRejectDialog();
      },
      error: (error) => {
        console.error('Error rejecting agency application:', error);
        if (error.status === 403) {
          this.errorMessage = 'You do not have permission to reject applications. Please ensure you have admin access.';
        } else {
          this.errorMessage = error.error?.message || 'Failed to reject agency application';
        }
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
