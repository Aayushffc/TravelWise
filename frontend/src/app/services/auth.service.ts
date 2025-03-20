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
  refreshToken?: string;
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
  private refreshTokenTimeout: any;
  user$ = this.userSubject.asObservable();

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    // Check for existing auth on startup
    this.initAuthFromStorage();
    this.startRefreshTokenTimer();
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
            this.startRefreshTokenTimer();
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
        this.startRefreshTokenTimer();
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
    this.startRefreshTokenTimer();
  }

  // Refresh token
  refreshToken(): Observable<any> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh-token`, {}, { withCredentials: true })
      .pipe(
        tap(response => {
          this.saveToken(response);
          this.startRefreshTokenTimer();
        }),
        catchError(error => {
          this.logout();
          return throwError(() => error);
        })
      );
  }

  // Start the refresh token timer
  private startRefreshTokenTimer() {
    // Parse the JWT token to get expiration time
    if (!this.userSubject.value?.token) return;

    try {
      const token = this.userSubject.value.token;
      const jwtToken = JSON.parse(atob(token.split('.')[1]));
      const expires = new Date(jwtToken.exp * 1000);

      // Set a timeout to refresh 1 minute before token expires
      const timeout = expires.getTime() - Date.now() - (60 * 1000);

      // Only set the timer if the token expires in the future
      if (timeout > 0) {
        this.refreshTokenTimeout = setTimeout(() => {
          this.refreshToken().subscribe();
        }, timeout);
      }
    } catch (error) {
      console.error('Error starting refresh token timer', error);
    }
  }

  // Stop the refresh token timer
  private stopRefreshTokenTimer() {
    clearTimeout(this.refreshTokenTimeout);
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
    this.stopRefreshTokenTimer();
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

  getUserProfile(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/profile`);
  }
}
