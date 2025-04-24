import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private hubConnection: HubConnection;
  private messagesSubject = new BehaviorSubject<any[]>([]);
  public messages$ = this.messagesSubject.asObservable();
  public selectedBooking: any = null;
  private isConnecting = false;

  constructor(private authService: AuthService) {
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
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.previousRetryCount === 0) {
            return 0; // Immediate retry on first attempt
          }
          return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount - 1), 30000);
        }
      })
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
      const currentMessages = this.messagesSubject.value;
      this.messagesSubject.next([...currentMessages, message]);
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
      this.messagesSubject.next([]);
    } catch (err) {
      console.error('Error joining chat:', err);
      throw err;
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

  public async sendMessage(bookingId: number, message: string, messageType?: string, fileUrl?: string, fileName?: string, fileSize?: number) {
    try {
      await this.ensureConnection();

      // Create a temporary message object
      const tempMessage = {
        id: Date.now(), // Temporary ID
        bookingId,
        message,
        messageType,
        fileUrl,
        fileName,
        fileSize,
        senderId: this.authService.getCurrentUser()?.id,
        sentAt: new Date().toISOString(),
        isTemporary: true
      };

      // Add the message to the local messages array immediately
      const currentMessages = this.messagesSubject.value;
      this.messagesSubject.next([...currentMessages, tempMessage]);

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
}
