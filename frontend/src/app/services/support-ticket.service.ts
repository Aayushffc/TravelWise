import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface SupportTicket {
  id: number;
  name: string;
  email: string;
  problemTitle: string;
  problemDescription: string;
  status: string;
  adminResponse: string;
  createdAt: string;
  updatedAt: string;
  resolvedAt: string;
}

export interface CreateTicketDTO {
  name: string;
  email: string;
  problemTitle: string;
  problemDescription: string;
}

@Injectable({
  providedIn: 'root'
})
export class SupportTicketService {
  private apiUrl = `${environment.apiUrl}/api/supportticket`;

  constructor(private http: HttpClient) { }

  getTickets(status?: string): Observable<SupportTicket[]> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<SupportTicket[]>(this.apiUrl, { params });
  }

  getTicketById(id: number): Observable<SupportTicket> {
    return this.http.get<SupportTicket>(`${this.apiUrl}/${id}`);
  }

  createTicket(ticket: CreateTicketDTO): Observable<SupportTicket> {
    return this.http.post<SupportTicket>(this.apiUrl, ticket);
  }

  updateTicket(id: number, ticket: Partial<SupportTicket>): Observable<SupportTicket> {
    return this.http.put<SupportTicket>(`${this.apiUrl}/${id}`, ticket);
  }

  updateTicketStatus(id: number, status: string): Observable<SupportTicket> {
    return this.http.put<SupportTicket>(`${this.apiUrl}/${id}/status`, { status });
  }
}
