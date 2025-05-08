import { Component } from '@angular/core';

@Component({
  selector: 'app-public-layout',
  template: `
    <div class="public-container p-4">
      <div class="d-flex justify-content-end mb-3">
        <a routerLink="/login" class="btn btn-outline-primary me-2">Login</a>
        <a routerLink="/register" class="btn btn-primary">Register</a>
      </div>
      <router-outlet></router-outlet>
    </div>
  `
})
export class PublicLayoutComponent {}
