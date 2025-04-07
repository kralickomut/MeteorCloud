import { Component, Input } from '@angular/core';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-workspace-create',
  templateUrl: './workspace-create.component.html',
  styleUrl: './workspace-create.component.scss'
})
export class WorkspaceCreateComponent {
  @Input() buttonLabel = 'create workspace';

  constructor(private messageService: MessageService) {

  }


  // Dialog
  showCreateDialog = false;
  newWorkspaceName = '';
  newWorkspaceDescription = '';



  openCreateWorkspace() {
    this.showCreateDialog = true;
  }

  closeDialog() {
    this.showCreateDialog = false;
    this.resetForm();
  }

  createWorkspace() {
    console.log('Creating workspace:', this.newWorkspaceName, this.newWorkspaceDescription);
    this.showCreateDialog = false;

    // Show success message
    this.messageService.add({
      severity: 'success',
      summary: 'Workspace Created',
      detail: 'Your new workspace ' + this.newWorkspaceName + ' has been successfully created!',
      life: 3000, // optional: auto close duration in ms
    });

    this.resetForm();
  }

  private resetForm() {
    this.newWorkspaceName = '';
    this.newWorkspaceDescription = '';
  }
}
