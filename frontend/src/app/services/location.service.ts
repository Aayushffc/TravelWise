import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LocationService {
  private apiUrl = `${environment.apiUrl}/api/location`;

  constructor(private http: HttpClient) { }

  getLocations(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl);
  }

  getPopularLocations(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/popular`);
  }

  getLocationById(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  createLocation(location: any): Observable<any> {
    return this.http.post<any>(this.apiUrl, location);
  }

  updateLocation(id: number, location: any): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, location);
  }

  deleteLocation(id: number): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`);
  }

  requestCall(id: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${id}/request-call`, {});
  }
}
