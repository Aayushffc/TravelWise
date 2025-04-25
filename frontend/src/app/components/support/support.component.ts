import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Location } from '@angular/common';
import { trigger, transition, style, animate } from '@angular/animations';
import { FAQService, FAQ } from '../../services/faq.service';
import { SupportTicketService, SupportTicket, CreateTicketDTO } from '../../services/support-ticket.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-support',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './support.component.html',
  styleUrls: ['./support.component.css'],
  animations: [
    trigger('fadeSlide', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(20px)' }),
        animate('300ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ]),
      transition(':leave', [
        animate('300ms ease-in', style({ opacity: 0, transform: 'translateY(20px)' }))
      ])
    ])
  ]
})
export class SupportComponent implements OnInit {
  activeTab: 'faq' | 'tickets' = 'faq';
  faqs: FAQ[] = [];
  tickets: SupportTicket[] = [];
  searchQuery: string = '';
  isLoading: boolean = false;
  showNewTicketForm: boolean = false;
  newTicket: CreateTicketDTO = {
    name: '',
    email: '',
    problemTitle: '',
    problemDescription: ''
  };

  constructor(
    private location: Location,
    private faqService: FAQService,
    private supportTicketService: SupportTicketService
  ) { }

  ngOnInit(): void {
    this.loadFAQs();
    this.loadTickets();
  }

  goBack(): void {
    this.location.back();
  }

  async loadFAQs(): Promise<void> {
    try {
      this.isLoading = true;
      const response = await firstValueFrom(this.faqService.getFAQs());
      if (response && Array.isArray(response)) {
        this.faqs = response;
      } else {
        console.warn('Invalid FAQ response format:', response);
        this.faqs = [];
      }
    } catch (error) {
      console.error('Error loading FAQs:', error);
      this.faqs = [];
    } finally {
      this.isLoading = false;
    }
  }

  async searchFAQs(): Promise<void> {
    if (!this.searchQuery.trim()) {
      this.loadFAQs();
      return;
    }

    try {
      this.isLoading = true;
      const response = await firstValueFrom(this.faqService.searchFAQs(this.searchQuery));
      if (response && Array.isArray(response)) {
        this.faqs = response;
      } else {
        console.warn('Invalid FAQ search response format:', response);
        this.faqs = [];
      }
    } catch (error) {
      console.error('Error searching FAQs:', error);
      this.faqs = [];
    } finally {
      this.isLoading = false;
    }
  }

  async loadTickets(): Promise<void> {
    try {
      this.isLoading = true;
      const response = await firstValueFrom(this.supportTicketService.getTickets());
      if (response && Array.isArray(response)) {
        this.tickets = response;
      } else {
        console.warn('Invalid tickets response format:', response);
        this.tickets = [];
      }
    } catch (error) {
      console.error('Error loading tickets:', error);
      this.tickets = [];
    } finally {
      this.isLoading = false;
    }
  }

  async submitTicket(): Promise<void> {
    if (!this.validateTicket()) return;

    try {
      this.isLoading = true;
      const response = await firstValueFrom(this.supportTicketService.createTicket(this.newTicket));
      if (response) {
        this.resetTicketForm();
        await this.loadTickets();
      }
    } catch (error) {
      console.error('Error submitting ticket:', error);
    } finally {
      this.isLoading = false;
    }
  }

  validateTicket(): boolean {
    return (
      this.newTicket.name.trim() !== '' &&
      this.newTicket.email.trim() !== '' &&
      this.newTicket.problemTitle.trim() !== '' &&
      this.newTicket.problemDescription.trim() !== ''
    );
  }

  resetTicketForm(): void {
    this.newTicket = {
      name: '',
      email: '',
      problemTitle: '',
      problemDescription: ''
    };
    this.showNewTicketForm = false;
  }

  getStatusColor(status: string): string {
    switch (status.toLowerCase()) {
      case 'open':
        return 'bg-yellow-100 text-yellow-800';
      case 'in progress':
        return 'bg-blue-100 text-blue-800';
      case 'resolved':
        return 'bg-green-100 text-green-800';
      case 'closed':
        return 'bg-gray-100 text-gray-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }
}
