import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AgencyApplicationService, AgencyApplicationDTO, AgencyApplicationResponseDTO } from '../../services/agency-application.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-agency-application',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './agency-application.component.html'
})
export class AgencyApplicationComponent implements OnInit {
  application: AgencyApplicationResponseDTO | null = null;
  formData: AgencyApplicationDTO = {
    agencyName: '',
    address: '',
    phoneNumber: '',
    description: '',
    businessRegistrationNumber: ''
  };
  isLoading: boolean = true;
  error: string | null = null;
  success: string | null = null;
  isEmailVerified: boolean = false;

  constructor(
    private agencyApplicationService: AgencyApplicationService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadUserInfo();
    this.fetchApplication();
  }

  loadUserInfo(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.authService.getUserProfile().subscribe({
        next: (profile) => {
          this.isEmailVerified = profile.emailConfirmed;
        },
        error: (error) => {
          console.error('Error fetching user profile:', error);
        }
      });
    }
  }

  fetchApplication(): void {
    this.isLoading = true;
    this.agencyApplicationService.getMyApplication().subscribe({
      next: (data) => {
        this.application = data;
        this.isLoading = false;
      },
      error: (error) => {
        // If no application exists, this is fine
        this.application = null;
        this.isLoading = false;
      }
    });
  }

  onSubmit(): void {
    this.error = null;
    this.success = null;

    this.agencyApplicationService.apply(this.formData).subscribe({
      next: () => {
        this.success = 'Application submitted successfully';
        this.fetchApplication();
      },
      error: (error) => {
        this.error = error.error?.message || 'Failed to submit application';
      }
    });
  }
}
