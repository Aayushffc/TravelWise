import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, BehaviorSubject, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { Router } from '@angular/router';
import { catchError, tap } from 'rxjs/operators';

interface RegisterData {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  userName: string;
}

interface LoginData {
  email: string;
  password: string;
}

interface AuthResponse {
  token: string;
  email: string;
  firstName?: string;
  lastName?: string;
  fullName?: string;
  emailConfirmed?: boolean;
}

interface EmailVerificationData {
  email: string;
  verificationCode: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = environment.apiUrl + '/api/auth';
  private userSubject = new BehaviorSubject<AuthResponse | null>(null);
  user$ = this.userSubject.asObservable();

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    // Check for existing auth on startup
    this.initAuthFromStorage();
  }

  private initAuthFromStorage(): void {
    const savedAuth = localStorage.getItem('authToken');
    if (savedAuth) {
      try {
        const authData = JSON.parse(savedAuth);
        this.userSubject.next(authData);
      } catch (e) {
        localStorage.removeItem('authToken');
      }
    }
  }

  private getHeaders(): HttpHeaders {
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    });
  }

  // User Registration
  register(data: RegisterData): Observable<any> {
    const headers = this.getHeaders();
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, data, { headers })
      .pipe(
        tap(response => {
          if (response.token) {
            this.saveToken(response);
          }
        }),
        catchError(error => {
          console.error('Registration error', error);
          return throwError(() => error);
        })
      );
  }

  // User Login
  login(data: LoginData): Observable<any> {
    const headers = this.getHeaders();
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, data, {
      headers,
      withCredentials: true // Important for cookies
    }).pipe(
      tap(response => {
        this.saveToken(response);
      }),
      catchError(error => {
        console.error('Login error', error);
        return throwError(() => error);
      })
    );
  }

  // Google OAuth Login
  loginWithGoogle(): void {
    // Redirect to the Google login URL on the backend
    window.location.href = `${this.apiUrl}/google-login`;
  }

  // Handle OAuth Callback
  handleAuthCallback(token: string): void {
    const authData: AuthResponse = {
      token,
      email: '',  // This will be extracted from the token
    };
    this.saveToken(authData);
  }

  // Store Token
  saveToken(response: AuthResponse): void {
    localStorage.setItem('authToken', JSON.stringify(response));
    this.userSubject.next(response);
  }

  // Get Token
  getToken(): string | null {
    const user = this.userSubject.value;
    return user ? user.token : null;
  }

  // Check if user is authenticated
  isAuthenticated(): boolean {
    return this.userSubject.value !== null;
  }

  // Logout
  logout(): void {
    this.http.post(`${this.apiUrl}/logout`, {}, { withCredentials: true }).subscribe();
    localStorage.removeItem('authToken');
    this.userSubject.next(null);
    this.router.navigate(['/login']);
  }

  // Get current user
  getCurrentUser(): AuthResponse | null {
    return this.userSubject.value;
  }

  // Request Email Verification Code
  requestVerificationCode(email: string): Observable<any> {
    const headers = this.getHeaders();
    return this.http.post(`${this.apiUrl}/request-verification-code`, { email }, { headers })
      .pipe(
        catchError(error => {
          console.error('Verification code request error', error);
          return throwError(() => error);
        })
      );
  }

  // Verify Email
  verifyEmail(data: EmailVerificationData): Observable<any> {
    const headers = this.getHeaders();
    return this.http.post(`${this.apiUrl}/verify-email`, data, { headers })
      .pipe(
        tap(response => {
          // Update the user's email verification status if we get a successful response
          const currentUser = this.userSubject.value;
          if (currentUser) {
            currentUser.emailConfirmed = true;
            this.saveToken(currentUser);
          }
        }),
        catchError(error => {
          console.error('Email verification error', error);
          return throwError(() => error);
        })
      );
  }

  // Get current user's email verification status
  isEmailVerified(): boolean {
    const user = this.userSubject.value;
    return user ? !!user.emailConfirmed : false;
  }

  // Get user profile from backend
  getUserProfile(): Observable<AuthResponse> {
    const headers = this.getHeaders();
    return this.http.get<AuthResponse>(`${this.apiUrl}/profile`, { headers })
      .pipe(
        tap(response => {
          this.saveToken(response);
        }),
        catchError(error => {
          console.error('Error fetching user profile:', error);
          return throwError(() => error);
        })
      );
  }
}
