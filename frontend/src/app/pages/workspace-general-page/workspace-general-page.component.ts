import { Component } from '@angular/core';

@Component({
  selector: 'app-workspace-general-page',
  templateUrl: './workspace-general-page.component.html',
  styleUrls: ['./workspace-general-page.component.scss'],
})
export class WorkspaceGeneralPageComponent {
  isEditing = false;
  searchCollaborator = '';

  workspace = {
    name: 'Marketing Campaign',
    description: 'A workspace for the marketing campaign project.',
    createdOn: '2025-03-01',
    lastUpdated: '2025-04-02',
    owner: 'Alice',
    collaborators: [
      { name: 'Alice', role: 'Owner', email: 'alice@example.com' },
      { name: 'Bob', role: 'Manager', email: 'bob@example.com' },
      { name: 'Charlie', role: 'Guest', email: 'charlie@example.com' },
      { name: 'David', role: 'Guest', email: 'david@example.com' },
      { name: 'Eva', role: 'Guest', email: 'eva@example.com' },
      { name: 'Peter', role: 'Guest', email: 'peter@example.com' },
      { name: 'Hana', role: 'Guest', email: 'hana@example.com' },
      { name: 'Jan', role: 'Manager', email: 'jan@example.com' }
    ]
  };

  recentActivity = [
    'Alice uploaded plan.pdf',
    'Bob deleted old_logo.png',
    'Charlie created Assets folder'
  ];

  activeFiles = ['notes.txt', 'readme.md', 'logo.svg'];

  get filteredCollaborators() {
    if (!this.searchCollaborator) return this.workspace.collaborators;
    return this.workspace.collaborators.filter(c =>
      c.name.toLowerCase().includes(this.searchCollaborator.toLowerCase())
    );
  }
}
