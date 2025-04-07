import {Component, Input} from '@angular/core';

@Component({
  selector: 'app-workspace-general',
  templateUrl: './workspace-general.component.html',
  styleUrl: './workspace-general.component.scss'
})
export class WorkspaceGeneralComponent {
  @Input() buttonLabel = 'General';

  workspaceId: number = 1; // Example workspace ID, replace with actual ID from the route or service
}
