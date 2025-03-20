import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './verify-email.component.html',
  styleUrls: ['./verify-email.component.css']
})
export class VerifyEmailComponent implements OnInit {
  userEmail: string = '';
  verificationCode: string = '';
  errorMessage: string = '';
  successMessage: string = '';
  isLoading: boolean = false;
  codeRequested: boolean = false;

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    // Check if email is provided in query params
    this.route.queryParams.subscribe(params => {
      if (params['email']) {
        this.userEmail = params['email'];
      } else {
        // Try to get the email from the current user
        const user = this.authService.getCurrentUser();
        if (user && user.email) {
          this.userEmail = user.email;
        }
      }
    });
  }

  requestCode(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.authService.requestVerificationCode(this.userEmail).subscribe({
      next: () => {
        this.codeRequested = true;
        this.successMessage = 'Verification code sent to your email';
        this.isLoading = false;
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = error.error?.message || 'Failed to send verification code';
      }
    });
  }

  verifyEmail(): void {
    if (!this.verificationCode) {
      this.errorMessage = 'Please enter your verification code';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.authService.verifyEmail({
      email: this.userEmail,
      verificationCode: this.verificationCode
    }).subscribe({
      next: () => {
        this.successMessage = 'Email verified successfully';
        this.isLoading = false;

        // Navigate to home after short delay
        setTimeout(() => {
          this.router.navigate(['/home']);
        }, 2000);
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = error.error?.message || 'Failed to verify email';
      }
    });
  }
}
