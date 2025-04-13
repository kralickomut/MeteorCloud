import { Component } from '@angular/core';
import {AuthService} from "../../services/auth.service";
import {Router} from "@angular/router";

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent {

  constructor(private authService: AuthService, private router: Router) { }


  onLogout() {
    this.authService.logout();
    this.router.navigate(['/login']).then(() => {
      window.location.reload(); // Hard reload to force the app to refresh
    });
  }
}
