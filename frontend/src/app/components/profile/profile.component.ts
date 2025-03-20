import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './profile.component.html'
})
export class ProfileComponent implements OnInit {
  user: any = {};
  isEditing = false;
  isAgency = false;
  passwordForm = {
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  };

  constructor(
    private authService: AuthService,
    private http: HttpClient
  ) {}

  ngOnInit() {
    this.loadUserProfile();
  }

  loadUserProfile() {
    this.authService.getUserProfile().subscribe({
      next: (data) => {
        this.user = data;
        this.isAgency = data.roles?.includes('Agency') || false;
      },
      error: (error) => {
        console.error('Error loading profile:', error);
      }
    });
  }

  saveProfile() {
    this.http.put(`${environment.apiUrl}/api/auth/profile`, this.user).subscribe({
      next: () => {
        this.isEditing = false;
        // Show success message
      },
      error: (error) => {
        console.error('Error updating profile:', error);
        // Show error message
      }
    });
  }

  changePassword() {
    if (this.passwordForm.newPassword !== this.passwordForm.confirmPassword) {
      // Show error message
      return;
    }

    this.http.post(`${environment.apiUrl}/api/auth/change-password`, {
      currentPassword: this.passwordForm.currentPassword,
      newPassword: this.passwordForm.newPassword
    }).subscribe({
      next: () => {
        // Show success message
        this.passwordForm = {
          currentPassword: '',
          newPassword: '',
          confirmPassword: ''
        };
      },
      error: (error) => {
        console.error('Error changing password:', error);
        // Show error message
      }
    });
  }

  verifyPhone() {
    // Implement phone verification logic
  }

  requestAgencyUpgrade() {
    this.http.post(`${environment.apiUrl}/api/auth/request-agency-upgrade`, {}).subscribe({
      next: () => {
        // Show success message
      },
      error: (error) => {
        console.error('Error requesting agency upgrade:', error);
        // Show error message
      }
    });
  }
}
