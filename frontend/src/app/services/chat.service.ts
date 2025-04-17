import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { BehaviorSubject, Observable } from 'rxjs';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private hubConnection: HubConnection;
  private unreadMessagesCount = new BehaviorSubject<number>(0);
  private messages = new BehaviorSubject<any[]>([]);
  private isConnecting = false;
  public selectedBooking: any = null;

  constructor(private authService: AuthService) {
    const apiUrl = environment.apiUrl.replace('http://', '').replace('https://', '');
    const protocol = environment.apiUrl.startsWith('https') ? 'wss' : 'ws';
    const chatUrl = `${protocol}://${apiUrl}/chat`;

    console.log('ChatService: Initializing with URL:', chatUrl);

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(chatUrl, {
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
            console.log('ChatService: Attempting immediate retry');
            return 0;
          }
          const delay = Math.min(1000 * Math.pow(2, retryContext.previousRetryCount - 1), 30000);
          console.log(`ChatService: Next retry in ${delay}ms`);
          return delay;
        }
      })
      .build();

    this.setupConnectionHandlers();
    this.startConnection();
  }

  private setupConnectionHandlers() {
    this.hubConnection.onreconnecting((error) => {
      console.log('ChatService: SignalR reconnecting...', error);
      this.isConnecting = true;
    });

    this.hubConnection.onreconnected((connectionId) => {
      console.log('ChatService: SignalR reconnected with ID:', connectionId);
      this.isConnecting = false;
    });

    this.hubConnection.onclose((error) => {
      console.log('ChatService: SignalR connection closed', error);
      this.isConnecting = false;
      if (error) {
        console.error('ChatService: Connection closed due to error:', error);
        setTimeout(() => this.startConnection(), 5000);
      }
    });

    this.hubConnection.on('ReceiveMessage', (message: any) => {
      console.log('ChatService: Received message:', message);
      const currentMessages = this.messages.value;
      this.messages.next([...currentMessages, message]);
    });

    this.hubConnection.on('UnreadMessagesCount', (count: number) => {
      console.log('ChatService: Updated unread count:', count);
      this.unreadMessagesCount.next(count);
    });
  }

  private async startConnection() {
    if (this.isConnecting || this.hubConnection.state === HubConnectionState.Connected) {
      console.log('ChatService: Connection already in progress or connected');
      return;
    }

    this.isConnecting = true;
    try {
      console.log('ChatService: Starting SignalR connection...');
      await this.hubConnection.start();
      console.log('ChatService: SignalR Connected, State:', this.hubConnection.state);
      this.isConnecting = false;
    } catch (err) {
      console.error('ChatService: Error while starting connection:', err);
      this.isConnecting = false;
      // Rely on automatic reconnection
    }
  }

  private async ensureConnection(): Promise<void> {
    const currentState = this.hubConnection.state;
    console.log('ChatService: Current connection state:', currentState);

    if (currentState === HubConnectionState.Connected) {
      return;
    }

    if (currentState === HubConnectionState.Disconnected || currentState === HubConnectionState.Disconnecting) {
      console.log('ChatService: Connection is disconnected, attempting to reconnect...');
      await this.startConnection();

      let attempts = 0;
      const maxAttempts = 5;
      while (attempts < maxAttempts) {
        console.log(`ChatService: Connection attempt ${attempts + 1}/${maxAttempts}`);
        const state = this.hubConnection.state;
        if (state === HubConnectionState.Connected) {
          console.log('ChatService: Connection established');
          return;
        }
        await new Promise(resolve => setTimeout(resolve, 1000));
        attempts++;
        if (attempts < maxAttempts && ![HubConnectionState.Connected, HubConnectionState.Connecting].includes(state)) {
          console.log('ChatService: Retrying connection...');
          await this.startConnection();
        }
      }

      throw new Error('Failed to establish connection after multiple attempts');
    }
  }

  getUnreadMessagesCount(): Observable<number> {
    return this.unreadMessagesCount.asObservable();
  }

  getMessages(): Observable<any[]> {
    return this.messages.asObservable();
  }

  async sendMessage(bookingId: number, message: string, messageType: string = 'text', fileUrl?: string, fileName?: string, fileSize?: number) {
    console.log('ChatService: Attempting to send message:', {
      bookingId,
      message,
      messageType,
      fileUrl,
      fileName,
      fileSize,
      connectionState: this.hubConnection.state
    });

    try {
      await this.ensureConnection();
      await this.hubConnection.invoke('SendMessage', bookingId, message, messageType, fileUrl, fileName, fileSize);
      console.log('ChatService: Message sent successfully');
    } catch (err) {
      console.error('ChatService: Error while sending message:', err);
      throw err;
    }
  }

  async joinChat(bookingId: number) {
    try {
      await this.ensureConnection();
      console.log('ChatService: Joining chat for booking:', bookingId);
      await this.hubConnection.invoke('JoinBookingChat', bookingId);
      this.messages.next([]);
      console.log('ChatService: Successfully joined chat');
    } catch (err) {
      console.error('ChatService: Error while joining chat:', err);
      throw err;
    }
  }

  async leaveChat(bookingId: number) {
    try {
      await this.ensureConnection();
      console.log('ChatService: Leaving chat for booking:', bookingId);
      await this.hubConnection.invoke('LeaveBookingChat', bookingId);
      this.messages.next([]);
      console.log('ChatService: Successfully left chat');
    } catch (err) {
      console.error('ChatService: Error while leaving chat:', err);
      throw err;
    }
  }

  async markMessageAsRead(messageId: number) {
    try {
      await this.ensureConnection();
      console.log('ChatService: Marking message as read:', messageId);
      await this.hubConnection.invoke('MarkMessageAsRead', messageId);
      console.log('ChatService: Message marked as read');
    } catch (err) {
      console.error('ChatService: Error while marking message as read:', err);
      throw err;
    }
  }
}
