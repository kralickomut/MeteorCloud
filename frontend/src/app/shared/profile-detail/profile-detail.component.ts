import { Component } from '@angular/core';

@Component({
  selector: 'app-profile-detail',
  templateUrl: './profile-detail.component.html',
  styleUrl: './profile-detail.component.scss'
})
export class ProfileDetailComponent {
  user = {
    name: 'František Hromek',
    email: 'frantisek@meteorcloud.com',
    bio: 'I build modern, user-friendly cloud systems and I love clean UI.',
    role: 'Software Engineer'
  };

  editMode = false;

  saveChanges() {
    console.log('Saved:', this.user);
    this.editMode = false;
  }

  cancelChanges() {
    // Optionally reset to original values if needed
    this.editMode = false;
  }
}
