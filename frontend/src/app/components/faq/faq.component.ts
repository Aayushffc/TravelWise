import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { CommonModule } from '@angular/common';

interface FAQ {
  id: number;
  question: string;
  answer: string;
  orderIndex: number;
}

interface FAQCategory {
  category: string;
  questions: FAQ[];
}

@Component({
  selector: 'app-faq',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './faq.component.html'
})
export class FAQComponent implements OnInit {
  faqCategories: FAQCategory[] = [];
  expandedFAQs: number[] = [];

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.loadFAQs();
  }

  loadFAQs() {
    this.http.get<FAQCategory[]>(`${environment.apiUrl}/api/faq`)
      .subscribe({
        next: (data) => {
          this.faqCategories = data;
        },
        error: (error) => {
          console.error('Error loading FAQs:', error);
        }
      });
  }

  toggleFAQ(faq: FAQ) {
    const index = this.expandedFAQs.indexOf(faq.id);
    if (index === -1) {
      this.expandedFAQs.push(faq.id);
    } else {
      this.expandedFAQs.splice(index, 1);
    }
  }
}
