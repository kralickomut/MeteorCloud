import { Component } from '@angular/core';
import {UserService} from "../../services/user.service";

@Component({
  selector: 'app-shared-layout-router',
  template: `
    <ng-container *ngIf="(userService.user$ | async) as user; else guest">
      <app-main-layout>
        <router-outlet></router-outlet>
      </app-main-layout>
    </ng-container>

    <ng-template #guest>
      <app-public-layout>
        <router-outlet></router-outlet>
      </app-public-layout>
    </ng-template>
  `
})
export class SharedLayoutRouterComponent {
  constructor(public userService: UserService) {}
}
