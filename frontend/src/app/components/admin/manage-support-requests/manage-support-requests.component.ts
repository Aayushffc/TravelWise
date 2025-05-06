import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SupportTicketService, SupportTicket } from '../../../services/support-ticket.service';

@Component({
  selector: 'app-manage-support-requests',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './manage-support-requests.component.html',
  styleUrls: ['./manage-support-requests.component.css']
})
export class ManageSupportRequestsComponent implements OnInit {
  supportRequests: SupportTicket[] = [];
  isLoading = true;
  errorMessage = '';

  constructor(private supportTicketService: SupportTicketService) {}

  ngOnInit() {
    this.loadSupportRequests();
  }

  loadSupportRequests() {
    this.isLoading = true;
    this.supportTicketService.getTickets().subscribe({
      next: (tickets) => {
        this.supportRequests = tickets;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading support tickets:', error);
        this.errorMessage = 'Failed to load support tickets';
        this.isLoading = false;
      }
    });
  }

  updateRequestStatus(ticketId: number, status: string) {
    this.supportTicketService.updateTicketStatus(ticketId, status).subscribe({
      next: () => {
        this.loadSupportRequests();
      },
      error: (error) => {
        console.error('Error updating ticket status:', error);
        this.errorMessage = 'Failed to update ticket status';
      }
    });
  }

  respondToRequest(ticket: SupportTicket) {
    if (!ticket.adminResponse?.trim()) {
      this.errorMessage = 'Please enter a response before sending';
      return;
    }

    this.supportTicketService.updateTicket(ticket.id, {
      adminResponse: ticket.adminResponse.trim(),
      status: 'RESOLVED'
    }).subscribe({
      next: () => {
        this.loadSupportRequests();
      },
      error: (error) => {
        console.error('Error responding to ticket:', error);
        this.errorMessage = 'Failed to send response';
      }
    });
  }
}
