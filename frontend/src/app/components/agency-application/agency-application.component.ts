import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { trigger, transition, style, animate } from '@angular/animations';
import { AuthService } from '../../services/auth.service';
import { AgencyApplicationService, AgencyApplicationResponseDTO, AgencyApplicationDTO } from '../../services/agency-application.service';
import { SidebarComponent } from '../side-bar/sidebar.component';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Observable, take } from 'rxjs';

interface Message {
  type: 'success' | 'error';
  text: string;
}

interface User {
  id: string;
  email: string;
  fullName: string;
  role: string;
}

@Component({
  selector: 'app-agency-application',
  templateUrl: './agency-application.component.html',
  styleUrls: ['./agency-application.component.css'],
  animations: [
    trigger('fadeSlide', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(20px)' }),
        animate('0.3s ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ])
    ]),
    trigger('fadeIn', [
      transition(':enter', [
        style({ opacity: 0 }),
        animate('0.2s ease-out', style({ opacity: 1 }))
      ])
    ])
  ],
  imports: [SidebarComponent, CommonModule, FormsModule],
  standalone: true
})
export class AgencyApplicationComponent implements OnInit {
  user: User = {
    id: '',
    email: '',
    fullName: '',
    role: ''
  };
  application: AgencyApplicationResponseDTO | null = null;
  applicationData: AgencyApplicationDTO = {
    agencyName: '',
    address: '',
    phoneNumber: '',
    description: '',
    businessRegistrationNumber: ''
  };
  isLoading = false;
  message: Message | null = null;

  constructor(
    private router: Router,
    private authService: AuthService,
    private agencyApplicationService: AgencyApplicationService
  ) {}

  ngOnInit(): void {
    this.loadUserInfo();
    this.loadApplication();
  }

  private loadUserInfo(): void {
    this.isLoading = true;
    this.authService.user$.pipe(take(1)).subscribe({
      next: (user) => {
        if (user) {
          this.user = {
            id: user.id,
            email: user.email,
            fullName: user.fullName || user.email,
            role: user.roles?.[0] || 'User'
          };
          this.isLoading = false;
        } else {
          this.isLoading = false;
          this.showMessage('error', 'User not authenticated');
          this.router.navigate(['/login']);
        }
      },
      error: (error: Error) => {
        console.error('Error loading user info:', error);
        this.isLoading = false;
        this.showMessage('error', 'Failed to load user information');
        this.router.navigate(['/login']);
      }
    });
  }

  private loadApplication(): void {
    this.isLoading = true;
    this.agencyApplicationService.getMyApplication().subscribe({
      next: (response: AgencyApplicationResponseDTO) => {
        this.application = response;
        this.isLoading = false;
      },
      error: (error: Error) => {
        console.error('Error loading application:', error);
        this.isLoading = false;
        this.showMessage('error', 'Failed to load application data');
      }
    });
  }

  submitApplication(): void {
    if (!this.validateForm()) {
      return;
    }

    this.isLoading = true;
    this.agencyApplicationService.apply(this.applicationData).subscribe({
      next: (response: any) => {
        this.isLoading = false;
        this.showMessage('success', 'Application submitted successfully');
        this.loadApplication(); // Reload the application to get the updated status
      },
      error: (error: Error) => {
        console.error('Error submitting application:', error);
        this.isLoading = false;
        this.showMessage('error', 'Failed to submit application. Please try again.');
      }
    });
  }

  private validateForm(): boolean {
    if (!this.applicationData.agencyName ||
        !this.applicationData.businessRegistrationNumber ||
        !this.applicationData.phoneNumber ||
        !this.applicationData.address) {
      this.showMessage('error', 'Please fill in all required fields');
      return false;
    }
    return true;
  }

  private showMessage(type: 'success' | 'error', text: string): void {
    this.message = { type, text };
    setTimeout(() => {
      this.message = null;
    }, 5000);
  }

  goBack(): void {
    this.router.navigate(['/home']);
  }
}
