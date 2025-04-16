// src/app/components/notification-bar/notification-bar.component.ts
import { Component, OnInit } from '@angular/core';
import { NotificationService, Notification } from '../../services/notification.service';
import {WorkspaceService} from "../../services/workspace.service";
import {MessageService} from "primeng/api";

interface DisplayNotification extends Notification {
  icon: string;
  time: string;
  localAction?: 'accepted' | 'declined';
  token?: string;
  invitationStatus?: 'Accepted' | 'Declined' | 'Pending';
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


  constructor(private notificationService: NotificationService, private workspaceService: WorkspaceService, private messageService: MessageService) {}

  ngOnInit(): void {
    const token = localStorage.getItem('auth_token');

    this.notificationService.getRecent().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.notifications = res.data.map(n => {
            const token = this.extractToken(n.message);
            const cleanMessage = n.message.replace(/^([0-9a-fA-F-]+)-/, ''); // remove token from message
            return {
              ...n,
              message: cleanMessage,
              icon: 'pi-bell',
              time: this.timeSince(new Date(n.createdAt)),
              token: token
            };
          });

          this.unreadCount = res.data.filter(n => !n.isRead).length;
        }
      }
    });

    // Real-time notifications
    this.notificationService.notification$.subscribe((incoming) => {
      const exists = this.notifications.some(n => n.id === incoming.id);
      if (exists) return;

      const token = this.extractToken(incoming.message);
      const cleanMessage = incoming.message.replace(/^([0-9a-fA-F-]+)-/, '');

      this.notifications.unshift({
        ...incoming,
        message: cleanMessage,
        icon: 'pi-bell',
        time: this.timeSince(new Date(incoming.createdAt)),
        token: token
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
    console.log('✅ Accepting invitation for:', notification);

    if (!notification.token) {
      console.warn('No token found for this invitation.');
      return;
    }

    this.workspaceService.respondToInvitation(notification.token, true).subscribe({
      next: (res) => {
        if (res.success) {
          notification.localAction = 'accepted'; // ✅ only after success
          this.markNotificationAsRead(notification);
          this.messageService.add({
            severity: 'success',
            summary: 'Invitation Accepted',
            detail: 'You have accepted the workspace invitation.',
            life: 3000
          });
        } else {
          this.showErrorToast(res.error?.message || 'Could not accept invite.');
        }
      },
      error: () => {
        this.showErrorToast('An error occurred while accepting the invitation.');
      }
    });
  }

  declineInvitation(notification: DisplayNotification) {
    console.log('❌ Declining invitation for:', notification);

    if (!notification.token) {
      console.warn('No token found for this invitation.');
      return;
    }

    this.workspaceService.respondToInvitation(notification.token, false).subscribe({
      next: (res) => {
        if (res.success) {
          notification.localAction = 'declined'; // ✅ only after success
          this.markNotificationAsRead(notification);
          this.messageService.add({
            severity: 'success',
            summary: 'Invitation Declined',
            detail: 'You have declined the workspace invitation.',
            life: 3000
          });
        } else {
          this.showErrorToast(res.error?.message || 'Could not decline invite.');
        }
      },
      error: () => {
        this.showErrorToast('An error occurred while declining the invitation.');
      }
    });
  }

  private extractToken(message: string): string | undefined {
    const match = message.match(/^([0-9a-fA-F-]+)-/);
    return match?.[1];
  }

  private showErrorToast(message: string) {
    this.messageService.add({
      severity: 'error',
      summary: 'Action Failed',
      detail: message,
      life: 3000
    });
  }

}
