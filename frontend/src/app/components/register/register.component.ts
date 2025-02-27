import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, CommonModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent {
  currentStep: number = 1;
  fullName: string = '';
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
    if (!this.fullName || !this.email || !this.password || !this.confirmPassword) {
      this.errorMessage = 'Please fill in all fields';
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.errorMessage = 'Passwords do not match';
      return;
    }

    // Basic email validation
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
      fullName: this.fullName,
      email: this.email,
      password: this.password,
      userName: this.userName
    };

    this.authService.register(userData).subscribe({
      next: (response) => {
        // Store the token if the API returns one
        if (response.token) {
          localStorage.setItem('token', response.token);
        }
        // Navigate to login or dashboard
        this.router.navigate(['/login']);
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = error.error.message || 'An error occurred during registration';
      },
      complete: () => {
        this.isLoading = false;
      }
    });
  }
}
