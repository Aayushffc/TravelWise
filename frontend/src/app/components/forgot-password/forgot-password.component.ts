import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './forgot-password.component.html'
})
export class ForgotPasswordComponent {
  email: string = '';
  isLoading: boolean = false;
  message: string = '';

  constructor(private http: HttpClient) {}

  onSubmit() {
    if (!this.email) return;

    this.isLoading = true;
    this.http.post(`${environment.apiUrl}/api/auth/forgot-password`, { email: this.email })
      .subscribe({
        next: (response: any) => {
          this.message = response.message;
          this.isLoading = false;
        },
        error: (error) => {
          this.message = 'An error occurred. Please try again later.';
          this.isLoading = false;
          console.error('Error:', error);
        }
      });
  }
}
