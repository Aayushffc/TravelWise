import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface StripeConnectStatus {
  isConnected: boolean;
  stripeAccountId?: string;
  accountStatus?: string;
  isEnabled: boolean;
  payoutsEnabled?: string;
  chargesEnabled?: string;
  detailsSubmitted?: string;
  requirements?: string;
  verificationStatus?: string;
  errorMessage?: string;
}

export interface StripeConnectAccount {
  accountId: string;
  accountLink: string;
}

export interface StripeAccountLink {
  url: string;
}

@Injectable({
  providedIn: 'root'
})
export class StripeConnectService {
  private apiUrl = `${environment.apiUrl}/api/stripeconnect`;

  constructor(private http: HttpClient) {}

  getConnectStatus(): Observable<StripeConnectStatus> {
    return this.http.get<StripeConnectStatus>(`${this.apiUrl}/status`);
  }

  createConnectAccount(): Observable<StripeConnectAccount> {
    return this.http.post<StripeConnectAccount>(`${this.apiUrl}/create-account`, {});
  }

  createAccountLink(): Observable<StripeAccountLink> {
    return this.http.post<StripeAccountLink>(`${this.apiUrl}/create-account-link`, {});
  }
}
