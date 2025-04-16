// src/app/components/notification-bar/notification-bar.component.ts
import { Component, OnInit } from '@angular/core';
import { NotificationService, Notification } from '../../services/notification.service';

interface DisplayNotification extends Notification {
  icon: string;
  time: string;
  localAction?: 'accepted' | 'declined';
}


@Component({
  selector: 'app-notification-bar',
  templateUrl: './notification-bar.component.html',
  styleUrl: './notification-bar.component.scss'
})
export class NotificationBarComponent implements OnInit {
  showNotifications = false;
  unreadCount = 0;

  notifications: DisplayNotification[] = [];


  constructor(private notificationService: NotificationService) {}

  ngOnInit(): void {
    const token = localStorage.getItem('auth_token');
    if (token) {
      this.notificationService.startConnection(token);
    }

    this.notificationService.getRecent().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.notifications = res.data.map(n => ({
            ...n,
            icon: 'pi-bell',
            time: this.timeSince(new Date(n.createdAt)),
          }));
          this.unreadCount = res.data.filter(n => !n.isRead).length;
        }
      }
    });

    // Real-time notifications
    this.notificationService.notification$.subscribe((incoming) => {
      const exists = this.notifications.some(n => n.id === incoming.id);
      if (exists) return;

      this.notifications.unshift({
        ...incoming,
        icon: 'pi-bell',
        time: this.timeSince(new Date(incoming.createdAt)),
      });
      this.unreadCount++;
    });
  }

  toggleNotifications(): void {
    this.showNotifications = !this.showNotifications;
  }

  private timeSince(date: Date): string {
    const seconds = Math.floor((+new Date() - +date) / 1000);
    const minutes = Math.floor(seconds / 60);
    if (minutes < 1) return 'Just now';
    if (minutes < 60) return `${minutes} min ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours} h ago`;
    const days = Math.floor(hours / 24);
    return `${days} day(s) ago`;
  }

  markNotificationAsRead(notification: any) {
    if (notification.isRead) return;

    this.notificationService.markAsRead(notification.id).subscribe(() => {
      notification.isRead = true;
      this.unreadCount = this.notifications.filter(n => !n.isRead).length;
    });
  }

  acceptInvitation(notification: DisplayNotification) {
    console.log('✅ Accept invitation for:', notification);
    notification.localAction = 'accepted';

    // Optional: call backend API
    // this.notificationService.acceptInvitation(notification.id).subscribe(() => {
    //  this.markNotificationAsRead(notification);
    //});
  }

  declineInvitation(notification: DisplayNotification) {
    console.log('❌ Decline invitation for:', notification);
    notification.localAction = 'declined';

    // Optional: call backend API
    // this.notificationService.declineInvitation(notification.id).subscribe(() => {
    //  this.markNotificationAsRead(notification);
    //});
  }

}
