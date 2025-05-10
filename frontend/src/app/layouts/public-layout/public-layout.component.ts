import { Component } from '@angular/core';

@Component({
  selector: 'app-public-layout',
  template: `
    <div class="public-container p-4">
      <div class="d-flex justify-content-end mb-3">
        <a routerLink="/login" class="btn pink-outline-btn me-2">Login</a>
        <a routerLink="/register" class="btn pink-btn">Register</a>
      </div>
      <router-outlet></router-outlet>
    </div>
  `,
  styles: [`
    .pink-btn {
      background-color: #ec4899;
      border-color: #ec4899;
      color: white;
    }
    .pink-btn:hover {
      background-color: #db2777;
      border-color: #db2777;
    }
    .pink-outline-btn {
      background-color: transparent;
      border: 2px solid #ec4899;
      color: #ec4899;
    }
    .pink-outline-btn:hover {
      background-color: #ec4899;
      color: white;
    }
  `]
})
export class PublicLayoutComponent {}
