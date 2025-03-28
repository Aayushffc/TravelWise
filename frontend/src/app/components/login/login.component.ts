import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, CommonModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  email: string = '';
  password: string = '';
  errorMessage: string = '';
  isLoading: boolean = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  login() {
    if (!this.email || !this.password) {
      this.errorMessage = 'Please fill in all fields';
      return;
    }

    // Add email format validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(this.email)) {
      this.errorMessage = 'Please enter a valid email address';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.authService.login({ email: this.email, password: this.password })
      .subscribe({
        next: (response) => {
          this.authService.saveToken(response);

          // Check if email is verified
          if (response.emailConfirmed === false) {
            this.router.navigate(['/verify-email'], {
              queryParams: { email: response.email }
            });
          } else {
            // Navigate based on user role
            this.authService.navigateBasedOnRole();
          }
        },
        error: (error) => {
          console.error('Login error:', error);
          this.isLoading = false;

          // Handle specific error cases
          if (error.status === 401) {
            this.errorMessage = 'Invalid email or password';
          } else if (error.status === 0) {
            this.errorMessage = 'Unable to connect to server. Please check your internet connection';
          } else if (error.error?.message) {
            this.errorMessage = error.error.message;
          } else {
            this.errorMessage = 'An unexpected error occurred. Please try again later';
          }
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

  forgotPassword() {
    if (!this.email) {
      this.errorMessage = 'Please enter your email address';
      return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(this.email)) {
      this.errorMessage = 'Please enter a valid email address';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.authService.forgotPassword(this.email).subscribe({
      next: (response) => {
        this.router.navigate(['/forgot-password'], {
          queryParams: { email: this.email }
        });
      },
      error: (error) => {
        console.error('Forgot password error:', error);
        this.isLoading = false;

        // Handle specific error cases for forgot password
        if (error.status === 404) {
          this.errorMessage = 'No account found with this email address';
        } else if (error.status === 0) {
          this.errorMessage = 'Unable to connect to server. Please check your internet connection';
        } else if (error.error?.message) {
          this.errorMessage = error.error.message;
        } else {
          this.errorMessage = 'An unexpected error occurred. Please try again later';
        }
      },
      complete: () => {
        this.isLoading = false;
      }
    });
  }
}
