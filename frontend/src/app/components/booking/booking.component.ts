import { Component, OnInit, OnDestroy } from '@angular/core';
import { BookingService } from '../../services/booking.service';
import { ChatService } from '../../services/chat.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Location } from '@angular/common';
import { trigger, transition, style, animate } from '@angular/animations';
import { AuthService } from '../../services/auth.service';
import { Subscription } from 'rxjs';
import { firstValueFrom } from 'rxjs';
import { SidebarComponent } from '../side-bar/sidebar.component';

@Component({
  selector: 'app-booking',
  standalone: true,
  imports: [CommonModule, FormsModule, SidebarComponent],
  templateUrl: './booking.component.html',
  styleUrls: ['./booking.component.css'],
  animations: [
    trigger('fadeSlide', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(10px)' }),
        animate('300ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ])
    ])
  ]
})
export class BookingComponent implements OnInit, OnDestroy {
  bookings: any[] = [];
  filteredBookings: any[] = [];
  selectedBooking: any = null;
  newMessage: string = '';
  isLoading: boolean = true;
  error: string | null = null;
  private messageSubscription: Subscription | null = null;
  isConnecting: boolean = false;
  connectionError: string | null = null;
  public authService: AuthService;
  private messagesContainer: HTMLElement | null = null;
  private isScrolling = false;
  private scrollPosition = 0;
  private isLoadingMore = false;
  activeFilter: string = 'all'; // 'all', 'pending', 'Accepted', 'rejected'
  user: any;

  constructor(
    private bookingService: BookingService,
    private chatService: ChatService,
    private location: Location,
    authService: AuthService
  ) {
    this.authService = authService;
    this.user = this.authService.getCurrentUser();
  }

  ngOnInit() {
    this.loadBookings();
    this.setupMessageSubscription();
  }

  ngAfterViewInit() {
    this.messagesContainer = document.querySelector('.messages-container');
    if (this.messagesContainer) {
      this.messagesContainer.addEventListener('scroll', this.handleScroll.bind(this));
    }
  }

  private handleScroll() {
    if (!this.messagesContainer || !this.selectedBooking || this.isLoadingMore) return;

    const { scrollTop, scrollHeight, clientHeight } = this.messagesContainer;

    // Save scroll position when loading more messages
    if (this.isScrolling) {
      this.scrollPosition = scrollHeight - this.scrollPosition;
      return;
    }

    // Load more messages when reaching the top
    if (scrollTop === 0) {
      this.isScrolling = true;
      this.scrollPosition = scrollHeight;
      this.loadMoreMessages();
    }
  }

  private scrollToBottom() {
    if (this.messagesContainer) {
      setTimeout(() => {
        if (this.messagesContainer) {
          this.messagesContainer.scrollTop = this.messagesContainer.scrollHeight;
        }
      }, 100);
    }
  }

  public async loadMoreMessages() {
    if (this.selectedBooking && !this.isLoadingMore) {
      this.isLoadingMore = true;
      try {
        const hasMore = await this.chatService.loadMoreMessages();
        if (hasMore) {
          setTimeout(() => {
            if (this.messagesContainer) {
              this.messagesContainer.scrollTop = this.scrollPosition;
            }
            this.isScrolling = false;
          }, 100);
        }
      } finally {
        this.isLoadingMore = false;
      }
    }
  }

  private setupMessageSubscription() {
    this.messageSubscription = this.chatService.messages$.subscribe(messages => {
      if (this.selectedBooking) {
        this.selectedBooking.messages = messages;
        this.scrollToBottom();
      }
    });
  }

  loadBookings() {
    this.isLoading = true;
    this.bookingService.getUserBookings().subscribe({
      next: (response) => {
        this.bookings = response;
        this.filterBookings();
        this.isLoading = false;
      },
      error: (err) => {
        this.error = 'Failed to load bookings';
        this.isLoading = false;
      }
    });
  }

  filterBookings(status: string = this.activeFilter) {
    this.activeFilter = status;
    if (status === 'all') {
      this.filteredBookings = this.bookings;
    } else {
      this.filteredBookings = this.bookings.filter(booking =>
        booking.status.toLowerCase() === status.toLowerCase()
      );
    }
  }

  getFilterCount(status: string): number {
    if (status === 'all') return this.bookings.length;
    return this.bookings.filter(booking =>
      booking.status.toLowerCase() === status.toLowerCase()
    ).length;
  }

  async selectBooking(booking: any) {
    try {
      this.isConnecting = true;
      this.connectionError = null;

      if (this.selectedBooking) {
        await this.chatService.leaveChat(this.selectedBooking.id);
      }

      this.selectedBooking = booking;
      this.bookingService.selectedBooking = booking;
      this.chatService.selectedBooking = booking;
      this.chatService.resetPagination();
      await this.chatService.joinChat(booking.id);

      // Wait for messages to load and then scroll
      setTimeout(() => {
        this.scrollToBottom();
      }, 500);
    } catch (error) {
      console.error('Error selecting booking:', error);
      this.connectionError = 'Failed to connect to chat. Please try again.';
    } finally {
      this.isConnecting = false;
    }
  }

  async loadChatMessages(bookingId: number) {
    try {
      const messages = await firstValueFrom(this.bookingService.getChatMessages(bookingId));
      if (this.selectedBooking) {
        this.selectedBooking.messages = messages;

        // Scroll to bottom after messages are loaded
        setTimeout(() => {
          if (this.messagesContainer) {
            this.messagesContainer.scrollTop = this.messagesContainer.scrollHeight;
          }
        }, 100);
      }
    } catch (error) {
      console.error('Error loading chat messages:', error);
      this.error = 'Failed to load chat messages';
    }
  }

  async sendMessage() {
    if (!this.selectedBooking || !this.newMessage.trim()) {
      return;
    }

    try {
      this.isConnecting = true;
      this.connectionError = null;
      await this.chatService.sendMessage(this.selectedBooking.id, this.newMessage);
      this.newMessage = '';
      this.scrollToBottom();
    } catch (error) {
      console.error('Error sending message:', error);
      this.connectionError = 'Failed to send message. Please try again.';
    } finally {
      this.isConnecting = false;
    }
  }

  getStatusColor(status: string): string {
    switch (status?.toLowerCase()) {
      case 'confirmed':
        return 'bg-green-100 text-green-800';
      case 'pending':
        return 'bg-yellow-100 text-yellow-800';
      case 'cancelled':
        return 'bg-red-100 text-red-800';
      case 'completed':
        return 'bg-blue-100 text-blue-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  getStatusDotColor(status: string): string {
    switch (status?.toLowerCase()) {
      case 'confirmed':
        return 'bg-green-400';
      case 'pending':
        return 'bg-yellow-400';
      case 'cancelled':
        return 'bg-red-400';
      case 'completed':
        return 'bg-blue-400';
      default:
        return 'bg-gray-400';
    }
  }

  getPaymentStatusColor(status: string): string {
    switch (status?.toLowerCase()) {
      case 'paid':
        return 'text-green-600';
      case 'pending':
        return 'text-yellow-600';
      case 'failed':
        return 'text-red-600';
      case 'partial':
        return 'text-orange-600';
      default:
        return 'text-gray-600';
    }
  }

  goBack(): void {
    this.location.back();
  }

  ngOnDestroy() {
    if (this.messageSubscription) {
      this.messageSubscription.unsubscribe();
    }
    if (this.selectedBooking) {
      this.chatService.leaveChat(this.selectedBooking.id);
    }
    if (this.messagesContainer) {
      this.messagesContainer.removeEventListener('scroll', this.handleScroll.bind(this));
    }
  }
}
