import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-workspace-detail',
  templateUrl: './workspace-detail.component.html',
  styleUrl: './workspace-detail.component.scss'
})
export class WorkspaceDetailComponent {
  workspaceName: string = 'Name fetched from API';

  recentActivity = [
    'Alice uploaded plan.pdf',
    'Bob deleted old_logo.png',
    'Charlie created Assets folder'
  ];

  activeFiles = [
    'notes.txt',
    'readme.md',
    'logo.svg'
  ];

  invitedUsers = [
    { name: 'David', invitedBy: 'Alice', status: 'Pending' },
    { name: 'Emma', invitedBy: 'Bob', status: 'Accepted' }
  ];
}
