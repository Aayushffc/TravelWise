import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface FAQ {
  id: number;
  question: string;
  answer: string;
  category: string;
  orderIndex: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface FAQCreateDTO {
  question: string;
  category: string;
}

@Injectable({
  providedIn: 'root'
})
export class FAQService {
  private apiUrl = `${environment.apiUrl}/api/faq`;

  constructor(private http: HttpClient) { }

  getFAQs(): Observable<FAQ[]> {
    return this.http.get<FAQ[]>(this.apiUrl);
  }

  searchFAQs(query: string): Observable<FAQ[]> {
    return this.http.get<FAQ[]>(`${this.apiUrl}/search`, {
      params: { query }
    });
  }

  submitQuestion(faq: FAQCreateDTO): Observable<FAQ> {
    return this.http.post<FAQ>(`${this.apiUrl}/question`, faq);
  }

  answerQuestion(id: number, answer: string): Observable<FAQ> {
    return this.http.put<FAQ>(`${this.apiUrl}/${id}/answer`, { answer });
  }
}
