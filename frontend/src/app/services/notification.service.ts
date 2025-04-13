// src/app/services/notification.service.ts
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { enviroment } from '../../../enviroment';
import { Observable, Subject } from 'rxjs';
import { ApiResult } from '../models/api-result';
import * as signalR from '@microsoft/signalr';

export interface Notification {
  id: number;
  userId: number;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private apiUrl = enviroment.notificationUrl + '/api/notifications';
  private hubUrl = enviroment.notificationUrl + '/hub/notifications';

  private hubConnection!: signalR.HubConnection;

  private notificationSubject = new Subject<Notification>();
  public notification$ = this.notificationSubject.asObservable();

  constructor(private http: HttpClient) {}

  // REST API methods
  getRecent(skip: number = 0, take: number = 10): Observable<ApiResult<Notification[]>> {
    return this.http.get<ApiResult<Notification[]>>(`${this.apiUrl}/recent?skip=${skip}&take=${take}`);
  }

  markAsRead(id: number): Observable<ApiResult<boolean>> {
    return this.http.post<ApiResult<boolean>>(`${this.apiUrl}/${id}/read`, {});
  }

  // SignalR real-time connection
  startConnection(token: string) {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('✅ SignalR connection started'))
      .catch(err => console.error('❌ Error starting SignalR connection', err));

    this.hubConnection.on('ReceiveNotification', (notification: Notification) => {
      this.notificationSubject.next(notification);
    });
  }

  stopConnection() {
    this.hubConnection?.stop().then(() => console.log('🛑 SignalR connection stopped'));
  }
}
