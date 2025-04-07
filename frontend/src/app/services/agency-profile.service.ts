import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AgencyProfileService {
  private apiUrl = `${environment.apiUrl}/api/agency-profiles`;

  constructor(private http: HttpClient) {}

  getMyAgencyProfile(): Observable<any> {
    return this.http.get(`${this.apiUrl}/my-profile`);
  }

  createAgencyProfile(profileData: any): Observable<any> {
    return this.http.post(this.apiUrl, profileData);
  }

  updateAgencyProfile(id: number, profileData: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, profileData);
  }

  updateOnlineStatus(isOnline: boolean): Observable<any> {
    return this.http.post(`${this.apiUrl}/online-status`, { isOnline });
  }

  updateLastActive(): Observable<any> {
    return this.http.post(`${this.apiUrl}/last-active`, {});
  }

  getAgencyProfile(id: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/${id}`);
  }
}
