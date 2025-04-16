import {Component, OnInit} from '@angular/core';
import {UserService} from "./services/user.service";
import {firstValueFrom} from "rxjs";
import {Router} from "@angular/router";

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  constructor(private userService: UserService, private router: Router) {}

  async ngOnInit(): Promise<void> {
    const userId = localStorage.getItem('user_id');
    const token = localStorage.getItem('auth_token');

    // Not logged in, redirect
    if (!userId || !token) {
      this.router.navigate(['/login']);
      return;
    }

    // Try to load and cache the user
    try {
      const response = await firstValueFrom(this.userService.getUser(+userId));
      if (response.success && response.data?.user) {
        this.userService.setActualLoggedUser(response.data.user);
      } else {
        this.handleLogout(); // invalid user response
      }
    } catch {
      this.handleLogout(); // error fetching user
    }
  }

  private handleLogout() {
    localStorage.clear();
    this.router.navigate(['/login']);
  }
}
