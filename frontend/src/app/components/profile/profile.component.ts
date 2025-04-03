import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
import { trigger, transition, style, animate } from '@angular/animations';
import { SidebarComponent } from '../side-bar/sidebar.component';

interface Message {
  type: 'success' | 'error';
  text: string;
}

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, SidebarComponent],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css'],
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
        animate('0.3s ease-out', style({ opacity: 1 }))
      ])
    ])
  ]
})
export class ProfileComponent implements OnInit {
  user: any = {};
  isEditing: boolean = false;
  isLoading: boolean = true;
  message: Message | null = null;
  passwordData = {
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  };

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadUserProfile();
  }

  loadUserProfile(): void {
    this.isLoading = true;
    this.authService.getUserProfile().subscribe({
      next: (data) => {
        this.user = data;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading profile:', error);
        this.showMessage('error', 'Failed to load profile');
        this.isLoading = false;
      }
    });
  }

  saveProfile(): void {
    this.isLoading = true;
    this.authService.updateProfile(this.user).subscribe({
      next: () => {
        this.showMessage('success', 'Profile updated successfully');
        this.isEditing = false;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error updating profile:', error);
        this.showMessage('error', error.error?.message || 'Failed to update profile');
        this.isLoading = false;
      }
    });
  }

  changePassword(): void {
    if (this.passwordData.newPassword !== this.passwordData.confirmPassword) {
      this.showMessage('error', 'New passwords do not match');
      return;
    }

    this.isLoading = true;
    this.authService.changePassword(
      this.passwordData.currentPassword,
      this.passwordData.newPassword
    ).subscribe({
      next: () => {
        this.showMessage('success', 'Password changed successfully');
        this.passwordData = {
          currentPassword: '',
          newPassword: '',
          confirmPassword: ''
        };
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error changing password:', error);
        this.showMessage('error', error.error?.message || 'Failed to change password');
        this.isLoading = false;
      }
    });
  }

  showMessage(type: 'success' | 'error', text: string) {
    this.message = { type, text };
    setTimeout(() => {
      this.message = null;
    }, 5000);
  }

  goBack(): void {
    this.router.navigate(['/home']);
  }
}
