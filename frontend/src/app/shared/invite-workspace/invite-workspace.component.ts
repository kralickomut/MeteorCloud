import { Component, Input } from '@angular/core';
import { MessageService } from 'primeng/api';
import { FileUpload } from 'primeng/fileupload';

@Component({
  selector: 'app-invite-workspace',
  templateUrl: './invite-workspace.component.html',
  styleUrl: './invite-workspace.component.scss'
})
export class InviteWorkspaceComponent {
  @Input() buttonText: string = 'fast link'; // default text

  constructor(private messageService: MessageService) {

  }

  // Dialog
  showCreateDialog = false;
  newWorkspaceName = '';

  openCreateWorkspace() {
    this.showCreateDialog = true;
  }

  closeDialog() {
    this.showCreateDialog = false;
    this.resetForm();
  }

  createWorkspace() {
    console.log('Creating invite to someone:', this.newWorkspaceName);
    this.showCreateDialog = false;

    // Show success message
    this.messageService.add({
      severity: 'info',
      summary: 'Collaboration Invite Sent',
      detail: 'Your invitation to ' + this.newWorkspaceName + ' has been successfully sent!',
      life: 3000, // optional: auto close duration in ms
    });

    this.resetForm();
  }

  private resetForm() {
    this.newWorkspaceName = '';
  }
}
