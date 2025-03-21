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

    this.isLoading = true;
    this.errorMessage = '';

    this.authService.login({ email: this.email, password: this.password })
      .subscribe({
        next: (response) => {
          console.log('Login successful, saving token...');
          this.authService.saveToken(response);

          // Check if email is verified
          if (response.emailConfirmed === false) {
            console.log('Email not verified, redirecting to verification page...');
            this.router.navigate(['/verify-email'], {
              queryParams: { email: response.email }
            });
          } else {
            console.log('Email verified, starting role-based navigation...');
            // Navigate based on user role
            this.authService.navigateBasedOnRole();
          }
        },
        error: (error) => {
          console.error('Login error:', error);
          this.isLoading = false;
          this.errorMessage = error.error?.message || 'An error occurred during login';
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

    this.isLoading = true;
    this.errorMessage = '';

    this.authService.forgotPassword(this.email).subscribe({
      next: (response) => {
        this.router.navigate(['/forgot-password'], {
          queryParams: { email: this.email }
        });
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = error.error?.message || 'An error occurred while processing your request';
      },
      complete: () => {
        this.isLoading = false;
      }
    });
  }
}
