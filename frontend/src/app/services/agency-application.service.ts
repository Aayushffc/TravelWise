import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

export interface AgencyApplicationDTO {
  agencyName: string;
  address: string;
  phoneNumber: string;
  description?: string;
  businessRegistrationNumber: string;
}

export interface AgencyApplicationResponseDTO {
  id: number;
  userId: string;
  userEmail: string;
  userName: string;
  agencyName: string;
  address: string;
  phoneNumber: string;
  description: string;
  businessRegistrationNumber: string;
  createdAt: Date;
  reviewedAt: Date | null;
  isApproved: boolean;
  rejectionReason: string | null;
  reviewedBy: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class AgencyApplicationService {
  private apiUrl = `${environment.apiUrl}/api/agency-applications`;

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  private getHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    });
  }

  apply(application: AgencyApplicationDTO): Observable<any> {
    return this.http.post(`${this.apiUrl}/apply`, application, { headers: this.getHeaders() });
  }

  getMyApplication(): Observable<AgencyApplicationResponseDTO> {
    return this.http.get<AgencyApplicationResponseDTO>(`${this.apiUrl}/my-application`, { headers: this.getHeaders() });
  }

  getAllApplications(): Observable<AgencyApplicationResponseDTO[]> {
    return this.http.get<AgencyApplicationResponseDTO[]>(this.apiUrl, { headers: this.getHeaders() });
  }

  approveApplication(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/approve`, {}, { headers: this.getHeaders() });
  }

  rejectApplication(id: number, reason: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/reject`, { reason });
  }
}
