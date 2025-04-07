import { Component } from '@angular/core';

@Component({
  selector: 'app-notification-bar',
  templateUrl: './notification-bar.component.html',
  styleUrl: './notification-bar.component.scss'
})
export class NotificationBarComponent {
  showNotifications = false;

  notifications = [
    {
      icon: 'pi-bell',
      message: 'New file uploaded',
      time: 'Just now'
    },
    {
      icon: 'pi-share-alt',
      message: 'Workspace shared with you',
      time: '5 minutes ago'
    },
    {
      icon: 'pi-eye',
      message: 'Preview generated successfully',
      time: '10 minutes ago'
    },
    {
      icon: 'pi-clock',
      message: 'Your link is about to expire',
      time: '30 minutes ago'
    }
  ];

  toggleNotifications() {
    this.showNotifications = !this.showNotifications;
  }
}
