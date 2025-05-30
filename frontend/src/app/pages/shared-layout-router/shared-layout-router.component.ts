import { Component, OnInit } from '@angular/core';
import {User, UserService} from '../../services/user.service';
import { MainLayoutComponent } from "../../layouts/main-layout/main-layout.component";
import { PublicLayoutComponent } from "../../layouts/public-layout/public-layout.component";
import {take} from "rxjs";

@Component({
  selector: 'app-shared-layout-router',
  template: `
    <ng-container *ngIf="userLoaded">
      <ng-container *ngIf="user; else guest">
        <app-main-layout>
          <router-outlet></router-outlet>
        </app-main-layout>
      </ng-container>

      <ng-template #guest>
        <app-public-layout>
          <router-outlet></router-outlet>
        </app-public-layout>
      </ng-template>
    </ng-container>
  `
})
export class SharedLayoutRouterComponent implements OnInit {
  user: User | null = null;
  userLoaded = false;

  constructor(private userService: UserService) {}

  ngOnInit(): void {
    this.userService.user$.subscribe((u) => {
      this.user = u;
      this.userLoaded = true;
    });
  }
}

@Component({
  selector: 'main-layout-wrapper',
  template: `<app-main-layout><router-outlet></router-outlet></app-main-layout>`
})
export class MainLayoutWrapperComponent {}

@Component({
  selector: 'public-layout-wrapper',
  template: `<app-public-layout><router-outlet></router-outlet></app-public-layout>`
})
export class PublicLayoutWrapperComponent {}
