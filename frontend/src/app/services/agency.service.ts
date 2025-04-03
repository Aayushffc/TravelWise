import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AgencyApplication {
  id: string;
  status: 'pending' | 'approved' | 'rejected';
  agencyName: string;
  registrationNumber: string;
  contactPhone: string;
  businessEmail: string;
  businessAddress: string;
  businessDescription: string;
  createdAt: Date;
  updatedAt: Date;
}

export interface ApplicationFormData {
  agencyName: string;
  registrationNumber: string;
  contactPhone: string;
  businessEmail: string;
  businessAddress: string;
  businessDescription: string;
}

@Injectable({
  providedIn: 'root'
})
export class AgencyService {
  private apiUrl = `${environment.apiUrl}/api/agency-applications`;

  constructor(private http: HttpClient) {}

  getApplication(): Observable<AgencyApplication> {
    return this.http.get<AgencyApplication>(`${this.apiUrl}/my-application`);
  }

  submitApplication(data: ApplicationFormData): Observable<AgencyApplication> {
    return this.http.post<AgencyApplication>(`${this.apiUrl}/my-application`, data);
  }
}
