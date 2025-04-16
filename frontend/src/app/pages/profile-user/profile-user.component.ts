import { Component } from '@angular/core';
import {ActivatedRoute} from "@angular/router";

@Component({
  selector: 'app-profile-user',
  templateUrl: './profile-user.component.html',
  styleUrl: './profile-user.component.scss'
})
export class ProfileUserComponent {
  userId: number;

  constructor(private route: ActivatedRoute) {
    this.userId = Number(this.route.snapshot.paramMap.get('id'));
  }
}
