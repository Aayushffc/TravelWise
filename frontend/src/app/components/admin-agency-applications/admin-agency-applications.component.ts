import { Component, OnInit } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AgencyApplicationService, AgencyApplicationResponseDTO } from '../../services/agency-application.service';

@Component({
  selector: 'app-admin-agency-applications',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-agency-applications.component.html'
})
export class AdminAgencyApplicationsComponent implements OnInit {
  applications: AgencyApplicationResponseDTO[] = [];
  isLoading: boolean = true;
  error: string | null = null;
  success: string | null = null;
  selectedApplication: AgencyApplicationResponseDTO | null = null;
  rejectDialogOpen: boolean = false;
  rejectionReason: string = '';

  constructor(
    private agencyApplicationService: AgencyApplicationService,
    private location: Location
  ) {}

  ngOnInit(): void {
    this.fetchApplications();
  }

  goBack(): void {
    this.location.back();
  }

  fetchApplications(): void {
    this.isLoading = true;
    this.agencyApplicationService.getAllApplications().subscribe({
      next: (data) => {
        this.applications = data;
        this.isLoading = false;
      },
      error: (error) => {
        this.error = 'Failed to fetch applications';
        this.isLoading = false;
      }
    });
  }

  handleApprove(id: number): void {
    this.agencyApplicationService.approveApplication(id).subscribe({
      next: () => {
        this.success = 'Application approved successfully';
        this.fetchApplications();
      },
      error: (error) => {
        this.error = error.error?.message || 'Failed to approve application';
      }
    });
  }

  openRejectDialog(application: AgencyApplicationResponseDTO): void {
    this.selectedApplication = application;
    this.rejectDialogOpen = true;
  }

  closeRejectDialog(): void {
    this.rejectDialogOpen = false;
    this.selectedApplication = null;
    this.rejectionReason = '';
  }

  isValidRejectionReason(): boolean {
    const trimmedReason = this.rejectionReason.trim();
    return trimmedReason.length >= 10 && trimmedReason.length <= 500;
  }

  handleReject(): void {
    if (!this.selectedApplication || !this.isValidRejectionReason()) return;

    this.agencyApplicationService.rejectApplication(this.selectedApplication.id, this.rejectionReason.trim()).subscribe({
      next: () => {
        this.success = 'Application rejected successfully';
        this.closeRejectDialog();
        this.fetchApplications();
      },
      error: (error) => {
        this.error = error.error?.message || 'Failed to reject application';
      }
    });
  }
}
