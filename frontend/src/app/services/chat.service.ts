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

  constructor(private authService: AuthService) {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/chat`, {
        accessTokenFactory: () => this.authService.getToken() || '',
        skipNegotiation: true,
        transport: 1 // WebSockets only
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 20000]) // Custom retry intervals
      .build();

    this.setupConnectionHandlers();
    this.startConnection();
  }

  private setupConnectionHandlers() {
    this.hubConnection.onreconnecting((error) => {
      console.log('SignalR reconnecting...', error);
    });

    this.hubConnection.onreconnected((connectionId) => {
      console.log('SignalR reconnected with ID:', connectionId);
    });

    this.hubConnection.onclose((error) => {
      console.log('SignalR connection closed', error);
      if (error) {
        console.error('Connection closed due to error:', error);
      }
    });

    this.hubConnection.on('NewMessage', (message: any) => {
      this.messages.next([...this.messages.value, message]);
    });

    this.hubConnection.on('UnreadMessagesCount', (count: number) => {
      this.unreadMessagesCount.next(count);
    });
  }

  private async startConnection() {
    if (this.isConnecting || this.hubConnection.state === HubConnectionState.Connected) {
      return;
    }

    this.isConnecting = true;
    try {
      await this.hubConnection.start();
      console.log('SignalR Connected');
      this.isConnecting = false;
    } catch (err) {
      console.error('Error while starting connection: ' + err);
      this.isConnecting = false;
      // Don't automatically retry here, let the automatic reconnection handle it
    }
  }

  getUnreadMessagesCount(): Observable<number> {
    return this.unreadMessagesCount.asObservable();
  }

  getMessages(): Observable<any[]> {
    return this.messages.asObservable();
  }

  async sendMessage(bookingId: number, message: string) {
    if (this.hubConnection.state !== HubConnectionState.Connected) {
      throw new Error('Not connected to chat server');
    }
    try {
      await this.hubConnection.invoke('SendMessage', bookingId, message);
    } catch (err) {
      console.error('Error while sending message: ' + err);
      throw err;
    }
  }

  async joinChat(bookingId: number) {
    if (this.hubConnection.state !== HubConnectionState.Connected) {
      throw new Error('Not connected to chat server');
    }
    try {
      await this.hubConnection.invoke('JoinChat', bookingId);
    } catch (err) {
      console.error('Error while joining chat: ' + err);
      throw err;
    }
  }

  async leaveChat(bookingId: number) {
    if (this.hubConnection.state !== HubConnectionState.Connected) {
      throw new Error('Not connected to chat server');
    }
    try {
      await this.hubConnection.invoke('LeaveChat', bookingId);
    } catch (err) {
      console.error('Error while leaving chat: ' + err);
      throw err;
    }
  }

  async markMessageAsRead(messageId: number) {
    if (this.hubConnection.state !== HubConnectionState.Connected) {
      throw new Error('Not connected to chat server');
    }
    try {
      await this.hubConnection.invoke('MarkMessageAsRead', messageId);
    } catch (err) {
      console.error('Error while marking message as read: ' + err);
      throw err;
    }
  }
}
