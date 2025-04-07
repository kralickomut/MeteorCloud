import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-workspace-history',
  templateUrl: './workspace-history.component.html',
  styleUrl: './workspace-history.component.scss'
})
export class WorkspaceHistoryComponent {
  @Input() buttonLabel = 'workspace history';

  workspaceId: number = 1; // Example workspace ID, replace with actual ID from the route or service
}
