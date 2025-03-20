import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent {
  currentStep: number = 1;
  firstName: string = '';
  lastName: string = '';
  email: string = '';
  password: string = '';
  confirmPassword: string = '';
  userName: string = '';
  errorMessage: string = '';
  isLoading: boolean = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  nextStep() {
    if (!this.firstName || !this.lastName || !this.email || !this.password || !this.confirmPassword) {
      this.errorMessage = 'Please fill in all fields';
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.errorMessage = 'Passwords do not match';
      return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(this.email)) {
      this.errorMessage = 'Please enter a valid email address';
      return;
    }

    this.errorMessage = '';
    this.currentStep = 2;
  }

  register() {
    if (!this.userName) {
      this.errorMessage = 'Please choose a username';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const userData = {
      firstName: this.firstName,
      lastName: this.lastName,
      email: this.email,
      password: this.password,
      userName: this.userName
    };

    this.authService.register(userData).subscribe({
      next: (response) => {
        if (response.token) {
          this.router.navigate(['/home']);
        } else {
          this.router.navigate(['/verify-email'], {
            queryParams: { email: this.email }
          });
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = error.error?.message || 'An error occurred during registration';
      },
      complete: () => {
        this.isLoading = false;
      }
    });
  }

  // Add Google login method
  loginWithGoogle() {
    this.authService.loginWithGoogle();
  }
}
