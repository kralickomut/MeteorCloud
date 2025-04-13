import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {User, UserService} from '../../services/user.service';
import { Router } from '@angular/router';
import {NotificationService} from "../../services/notification.service";

@Component({
  selector: 'app-main-layout',
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss'
})
export class MainLayoutComponent implements OnInit {
  user: User | null = null;
  isMobileMenuOpen = false;
  user$ = this.userService.user$;

  constructor(
    private userService: UserService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private notificationService: NotificationService
  ) {}



  ngOnInit(): void {
    const userId = localStorage.getItem('user_id');
    const token = localStorage.getItem('auth_token');

    if (!userId || !token) {
      this.router.navigate(['/login']);
      return;
    }

    // Start SignalR connection
    this.notificationService.startConnection(token);

    const cachedUser = this.userService.currentUser;
    if (cachedUser) {
      this.user = cachedUser;
      return;
    }

    this.userService.getUser(+userId).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.user = res.data.user;
          this.userService.setActualLoggedUser(this.user);
          console.log('User data:', this.user);
          this.cdr.detectChanges();
        } else {
          this.logout();
        }
      },
      error: () => this.logout()
    });
  }

  logout() {
    localStorage.clear();
    this.router.navigate(['/login']);
  }

  toggleMobileMenu() {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
  }
}
