import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {User, UserService} from '../../services/user.service';
import { Router } from '@angular/router';
import {NotificationService} from "../../services/notification.service";
import {WorkspaceService} from "../../services/workspace.service";
import {filter, take} from "rxjs";

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
    private notificationService: NotificationService,
    private workspaceService: WorkspaceService
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
    this.workspaceService.startConnection(token);

    this.userService.user$
      .pipe(filter((u): u is User => !!u), take(1))
      .subscribe(user => {
        this.user = user;
      });
  }

  toggleMobileMenu() {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
  }
}
