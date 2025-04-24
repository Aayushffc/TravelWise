import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { BehaviorSubject, Observable, firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';
import { BookingService } from './booking.service';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private hubConnection: HubConnection;
  private messagesSubject = new BehaviorSubject<any[]>([]);
  public messages$ = this.messagesSubject.asObservable();
  public selectedBooking: any = null;
  private isConnecting = false;
  private readonly MESSAGES_PER_PAGE = 20;
  private currentPage = 1;
  private hasMoreMessages = true;
  private allMessages: any[] = [];

  constructor(private authService: AuthService, private bookingService: BookingService) {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/chatHub`, {
        accessTokenFactory: () => {
          const token = this.authService.getToken();
          if (!token) {
            return this.authService.refreshToken().toPromise();
          }
          return Promise.resolve(token);
        }
      })
      .withAutomaticReconnect()
      .build();

    this.setupConnectionHandlers();
    this.startConnection();
  }

  private setupConnectionHandlers() {
    this.hubConnection.onreconnecting((error) => {
      console.log('SignalR reconnecting...', error);
      this.isConnecting = true;
    });

    this.hubConnection.onreconnected((connectionId) => {
      console.log('SignalR reconnected with ID:', connectionId);
      this.isConnecting = false;
    });

    this.hubConnection.onclose((error) => {
      console.log('SignalR connection closed', error);
      this.isConnecting = false;
      if (error) {
        console.error('Connection closed due to error:', error);
        setTimeout(() => this.startConnection(), 5000);
      }
    });

    this.hubConnection.on('ReceiveMessage', (message) => {
      console.log('Received message:', message);
      // Convert received message time to IST
      message.sentAt = this.convertToIST(message.sentAt);
      this.allMessages.push(message);
      this.updateDisplayedMessages();
    });

    this.hubConnection.on('BookingAccepted', (data) => {
      console.log('Booking accepted:', data);
    });

    this.hubConnection.on('BookingRejected', (data) => {
      console.log('Booking rejected:', data);
    });

    this.hubConnection.on('BookingCancelled', (data) => {
      console.log('Booking cancelled:', data);
    });

    this.hubConnection.on('BookingCompleted', (data) => {
      console.log('Booking completed:', data);
    });
  }

  private convertToIST(dateString: string): string {
    const date = new Date(dateString);
    // Add 5 hours and 30 minutes to convert to IST
    date.setHours(date.getHours() + 5);
    date.setMinutes(date.getMinutes() + 30);
    return date.toISOString();
  }

  private updateDisplayedMessages() {
    // Sort all messages by timestamp
    this.allMessages.sort((a, b) =>
      new Date(a.sentAt).getTime() - new Date(b.sentAt).getTime()
    );

    // Get the current page of messages (latest 20 messages)
    const startIndex = Math.max(0, this.allMessages.length - (this.currentPage * this.MESSAGES_PER_PAGE));
    const endIndex = this.allMessages.length;
    const displayedMessages = this.allMessages.slice(startIndex, endIndex);

    this.messagesSubject.next(displayedMessages);
  }

  private async startConnection() {
    if (this.isConnecting || this.hubConnection.state === HubConnectionState.Connected) {
      console.log('Connection already in progress or connected');
      return;
    }

    this.isConnecting = true;
    try {
      console.log('Starting SignalR connection...');
      await this.hubConnection.start();
      console.log('SignalR Connected, State:', this.hubConnection.state);
      this.isConnecting = false;
    } catch (err) {
      console.error('Error while starting connection:', err);
      this.isConnecting = false;
      throw err;
    }
  }

  private async ensureConnection(): Promise<void> {
    const state = this.hubConnection.state as HubConnectionState;
    if (state === HubConnectionState.Connected) {
      return;
    }

    if (state === HubConnectionState.Disconnected) {
      await this.startConnection();
    }

    let attempts = 0;
    const maxAttempts = 5;
    while (attempts < maxAttempts) {
      if ((this.hubConnection.state as HubConnectionState) === HubConnectionState.Connected) {
        return;
      }
      await new Promise(resolve => setTimeout(resolve, 1000));
      attempts++;
    }

    throw new Error('Failed to establish connection after multiple attempts');
  }

  public async joinChat(bookingId: number) {
    try {
      await this.ensureConnection();
      await this.hubConnection.invoke('JoinBookingChat', bookingId);
      this.resetPagination();
      await this.loadInitialMessages(bookingId);
    } catch (err) {
      console.error('Error joining chat:', err);
      throw err;
    }
  }

  private async loadInitialMessages(bookingId: number) {
    try {
      const messages = await firstValueFrom(this.bookingService.getChatMessages(bookingId));
      // Convert all message times to IST
      messages.forEach((message: { sentAt: string }) => {
        message.sentAt = this.convertToIST(message.sentAt);
      });
      this.allMessages = messages;
      this.updateDisplayedMessages();
    } catch (error) {
      console.error('Error loading initial messages:', error);
    }
  }

  public async leaveChat(bookingId: number) {
    try {
      await this.ensureConnection();
      await this.hubConnection.invoke('LeaveBookingChat', bookingId);
      this.messagesSubject.next([]);
    } catch (err) {
      console.error('Error leaving chat:', err);
      throw err;
    }
  }

  public async loadMoreMessages(): Promise<boolean> {
    if (!this.hasMoreMessages) return false;

    try {
      const messages = await firstValueFrom(this.bookingService.getChatMessages(this.selectedBooking.id));

      // Convert all message times to IST
      messages.forEach((message: { sentAt: string }) => {
        message.sentAt = this.convertToIST(message.sentAt);
      });

      // Add new messages to allMessages if they don't exist
      messages.forEach((message: { id: number; messageId?: number }) => {
        if (!this.allMessages.some(m => m.id === message.id || m.messageId === message.messageId)) {
          this.allMessages.push(message);
        }
      });

      // Sort all messages by timestamp
      this.allMessages.sort((a, b) =>
        new Date(a.sentAt).getTime() - new Date(b.sentAt).getTime()
      );

      this.currentPage++;
      this.updateDisplayedMessages();

      // Check if we have more messages to load
      this.hasMoreMessages = this.allMessages.length > this.currentPage * this.MESSAGES_PER_PAGE;

      return true;
    } catch (error) {
      console.error('Error loading more messages:', error);
      return false;
    }
  }

  public async sendMessage(bookingId: number, message: string, messageType?: string, fileUrl?: string, fileName?: string, fileSize?: number) {
    try {
      await this.ensureConnection();

      // Create a temporary message object with current time in IST
      const now = new Date();
      now.setHours(now.getHours() + 5);
      now.setMinutes(now.getMinutes() + 30);

      const tempMessage = {
        id: Date.now(), // Temporary ID
        bookingId,
        message,
        messageType,
        fileUrl,
        fileName,
        fileSize,
        senderId: this.authService.getCurrentUser()?.id,
        sentAt: now.toISOString(),
        isTemporary: true
      };

      // Add the temporary message to allMessages
      this.allMessages.push(tempMessage);
      this.updateDisplayedMessages();

      // Send the message to the server
      await this.hubConnection.invoke('SendMessage', bookingId, message, messageType, fileUrl, fileName, fileSize);
    } catch (err) {
      console.error('Error sending message:', err);
      throw err;
    }
  }

  public getUnreadMessagesCount(): Observable<number> {
    return new Observable<number>(observer => {
      this.hubConnection.on('UnreadMessagesCount', (count) => {
        observer.next(count);
      });
    });
  }

  public resetPagination() {
    this.currentPage = 1;
    this.hasMoreMessages = true;
    this.allMessages = [];
    this.messagesSubject.next([]);
  }
}
