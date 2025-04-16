import { Component, OnInit, AfterViewInit, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';
import { SocialAuthService, GoogleLoginProvider, SocialUser } from '@abacritt/angularx-social-login';

declare var google: any;

// Add this type definition
type PasswordRequirementKey = 'minLength' | 'hasNumber' | 'hasSpecial' | 'hasUppercase' | 'hasLowercase';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent implements OnInit, AfterViewInit {
  currentStep: number = 1;
  firstName: string = '';
  lastName: string = '';
  email: string = '';
  password: string = '';
  confirmPassword: string = '';
  userName: string = '';
  errorMessage: string = '';
  isLoading: boolean = false;
  passwordVisible: boolean = false;
  confirmPasswordVisible: boolean = false;

  // Add these new properties
  passwordRequirements: Record<PasswordRequirementKey, boolean> = {
    minLength: false,
    hasNumber: false,
    hasSpecial: false,
    hasUppercase: false,
    hasLowercase: false
  };

  // Update the requirements array type in the template
  passwordRequirementItems: Array<{ key: PasswordRequirementKey; text: string }> = [
    { key: 'minLength', text: '8+ chars' },
    { key: 'hasNumber', text: 'number' },
    { key: 'hasSpecial', text: 'special char' },
    { key: 'hasUppercase', text: 'uppercase' },
    { key: 'hasLowercase', text: 'lowercase' }
  ];

  constructor(
    private authService: AuthService,
    private router: Router,
    private socialAuthService: SocialAuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    // Initialization logic if needed
  }

  ngAfterViewInit() {
    // Wait for the Google script to load
    if (typeof google !== 'undefined') {
      try {
        google.accounts.id.initialize({
          client_id: '334522416360-h3ipni5mvi5g9pnds92ts48j32k7stpr.apps.googleusercontent.com', // Use your actual client ID
          callback: this.handleCredentialResponse.bind(this)
        });

        google.accounts.id.renderButton(
          document.getElementById('google-signup-btn'),
          {
            theme: 'outline',
            size: 'large',
            width: '100%',
            text: 'signup_with' // Changed text to indicate sign up
          }
        );
      } catch (error) {
        console.error('Error initializing Google Identity Services:', error);
        this.errorMessage = 'Could not initialize Google Sign-Up.';
        this.cdr.detectChanges(); // Update view with error message
      }
    } else {
      console.error('Google Identity Services script not loaded');
      this.errorMessage = 'Google Sign-Up is unavailable.';
      this.cdr.detectChanges(); // Update view with error message
    }
  }

  handleCredentialResponse(response: any) {
    if (response.credential) {
      this.isLoading = true;
      this.errorMessage = '';
      this.cdr.detectChanges();

      this.authService.googleLogin(response.credential).subscribe({
        next: (res) => { // 'res' is the raw backend response { token: '...', user: {...} }
          this.isLoading = false;
          this.cdr.detectChanges();

          // User is authenticated via Google, now navigate
          if (res.user?.emailConfirmed === false) {
            this.router.navigate(['/verify-email'], {
              queryParams: { email: res.user?.email }
            });
          } else {
            // Email confirmed or doesn't require confirmation via Google path
            this.router.navigateByUrl('/home', { replaceUrl: true });
          }
        },
        error: (err) => {
          this.isLoading = false;
          console.error('Google Sign-Up/Login error:', err);
          this.errorMessage = err.error?.message || 'Google Sign-Up failed. Please try again.';
          this.cdr.detectChanges();
        }
      });
    }
  }

  // Add this method to check password requirements
  checkPasswordStrength() {
    const password = this.password;

    this.passwordRequirements = {
      minLength: password.length >= 8,
      hasNumber: /\d/.test(password),
      hasSpecial: /[!@#$%^&*(),.?":{}|<>]/.test(password),
      hasUppercase: /[A-Z]/.test(password),
      hasLowercase: /[a-z]/.test(password)
    };
  }

  nextStep() {
    if (!this.firstName || !this.lastName || !this.email || !this.password || !this.confirmPassword) {
      this.errorMessage = 'Please fill in all fields';
      return;
    }

    // Add password requirements check
    const allRequirementsMet = Object.values(this.passwordRequirements).every(req => req);
    if (!allRequirementsMet) {
      this.errorMessage = 'Please meet all password requirements';
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
      next: (response: any) => {
        if (response.token) {
          // Create a proper auth response object
          const authResponse = {
            id: response.user.id,
            token: response.token,
            email: response.user.email,
            firstName: response.user.firstName,
            lastName: response.user.lastName,
            fullName: response.user.fullName,
            emailConfirmed: response.user.emailConfirmed
          };

          // Store the auth state
          this.authService.saveToken(authResponse);

          // Navigate to home and replace the current history entry
          this.router.navigate(['/home'], { replaceUrl: true });
        } else {
          this.router.navigate(['/verify-email'], {
            queryParams: { email: this.email },
            replaceUrl: true
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

  signInWithGoogle(): void {
    this.socialAuthService.signIn(GoogleLoginProvider.PROVIDER_ID).then((user: SocialUser) => {
      this.authService.googleLogin(user.idToken).subscribe({
        next: (res) => {
          if (res.emailConfirmed === false) {
            this.router.navigate(['/verify-email'], {
              queryParams: { email: res.email }
            });
          } else {
            this.authService.navigateBasedOnRole();
          }
        },
        error: (err) => {
          this.errorMessage = 'Google login failed. Please try again.';
          console.error(err);
        }
      });
    });
  }
}
