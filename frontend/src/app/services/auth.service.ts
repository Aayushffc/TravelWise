import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
interface RegisterData {
  fullName: string;
  email: string;
  password: string;
  userName: string;
}

interface LoginData {
  email: string;
  password: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = environment.apiUrl + '/api/auth'; // Adjust port as needed

  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    });
  }

  // User Registration
  register(data: RegisterData): Observable<any> {
    const headers = this.getHeaders();
    return this.http.post(`${this.apiUrl}/register`, data, { headers });
  }

  // User Login
  login(data: LoginData): Observable<any> {
    const headers = this.getHeaders();
    return this.http.post(`${this.apiUrl}/login`, data, { headers });
  }

  // Store Token
  saveToken(token: string): void {
    localStorage.setItem('authToken', token);
  }

  // Get Token
  getToken(): string | null {
    return localStorage.getItem('authToken');
  }

  // Logout
  logout(): void {
    localStorage.removeItem('authToken');
  }
}
