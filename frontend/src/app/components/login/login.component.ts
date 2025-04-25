import { Component, OnInit, AfterViewInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

declare var google: any;

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, CommonModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements AfterViewInit {
  email: string = '';
  password: string = '';
  errorMessage: string = '';
  isLoading: boolean = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngAfterViewInit() {
    // Wait for the Google script to load
    if (typeof google !== 'undefined') {
      google.accounts.id.initialize({
        client_id: '334522416360-h3ipni5mvi5g9pnds92ts48j32k7stpr.apps.googleusercontent.com',
        callback: this.handleCredentialResponse.bind(this)
      });

      google.accounts.id.renderButton(
        document.getElementById('google-btn'),
        {
          theme: 'outline',
          size: 'large',
          width: '100%',
          text: 'continue_with'
        }
      );
    } else {
      console.error('Google Identity Services script not loaded');
    }
  }

  handleCredentialResponse(response: any) {
    if (response.credential) {
      this.authService.googleLogin(response.credential).subscribe({
        next: (res) => {
          // Save the token and user info
          // this.authService.saveToken(res);

          // Check email confirmation using the raw response structure
          if (res.user?.emailConfirmed === false) {
            this.router.navigate(['/verify-email'], {
              queryParams: { email: res.user?.email }
            });
          } else {
            // Get user role and then navigate (uses token saved within googleLogin)
            this.authService.getUserRole().subscribe({
              next: (role) => {
                // Navigate to home and force reload
                this.router.navigateByUrl('/home', { skipLocationChange: true }).then(() => {
                  window.location.href = '/home';
                });
              },
              error: (err) => {
                console.error('Error getting user role:', err);
                this.router.navigate(['/home']);
              }
            });
          }
        },
        error: (err) => {
          console.error('Backend login error:', err);
          this.errorMessage = err.error?.message || 'Google login failed. Please try again.';
        }
      });
    }
  }

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
            // Show the specific error message from the backend
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
