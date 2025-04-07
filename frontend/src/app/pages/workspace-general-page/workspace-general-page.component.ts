import { Component } from '@angular/core';

@Component({
  selector: 'app-workspace-general-page',
  templateUrl: './workspace-general-page.component.html',
  styleUrl: './workspace-general-page.component.scss'
})
export class WorkspaceGeneralPageComponent {
  workspaceName = 'Marketing Campaign';
  createdOn = '2025-03-01';
  updatedOn = '2025-04-02';
  owner = 'Alice';

  invitedUsers = [
    { name: 'David', invitedBy: 'Alice', status: 'Pending' },
    { name: 'Emma', invitedBy: 'Bob', status: 'Accepted' }
  ];

  recentActivity = [
    'Alice uploaded plan.pdf',
    'Bob deleted old_logo.png',
    'Charlie created Assets folder'
  ];

  activeFiles = ['notes.txt', 'readme.md', 'logo.svg'];

  workspace = {
    name: 'Marketing Campaign',
    id: '1',
    createdOn: '2025-03-01',
    lastUpdated: '2025-04-02',
    owner: 'Alice',
    description: 'A workspace for the marketing campaign project.',
    collaborators: [
      { name: 'Alice', role: 'Owner', invitedBy: 'N/A' },
      { name: 'Bob', role: 'Editor', invitedBy: 'N/A'  },
      { name: 'Charlie', role: 'Viewer', invitedBy: 'N/A'  }
    ]
  };
}
