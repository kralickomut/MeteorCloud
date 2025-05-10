import {Component, OnInit} from '@angular/core';
import {AuthService} from "../../services/auth.service";
import {Router} from "@angular/router";
import {User, UserService} from "../../services/user.service";

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent implements OnInit {

  user: User | null = null;

  constructor(private authService: AuthService, private router: Router, private userService: UserService) { }

  ngOnInit(): void {
    // Subscribe to user
    this.userService.user$.subscribe(user => {
      if (user) {
        this.user = user;
      }
    });
  }

  get profileImageUrl(): string {
    if (!this.user) {
      return '/assets/img/default-profile.jpg';
    }
    return this.userService.getProfileImageUrl(this.user?.id) ?? '/assets/img/default-profile.jpg';
  }

  onLogout() {
    this.authService.logout();
    this.router.navigate(['/login']).then(() => {
      window.location.reload(); // Hard reload to force the app to refresh
    });
  }

  onImageError(event: Event): void {
    const imgElement = event.target as HTMLImageElement;
    imgElement.src = '/assets/img/default-profile.jpg';
  }
}
