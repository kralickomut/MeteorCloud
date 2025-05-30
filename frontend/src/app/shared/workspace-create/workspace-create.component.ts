import { Component, Input } from '@angular/core';
import { MessageService } from 'primeng/api';
import {WorkspaceService} from "../../services/workspace.service";
import {UserService} from "../../services/user.service";

@Component({
  selector: 'app-workspace-create',
  templateUrl: './workspace-create.component.html',
  styleUrl: './workspace-create.component.scss'
})
export class WorkspaceCreateComponent {
  @Input() buttonLabel = 'create workspace';

  showCreateDialog = false;
  newWorkspaceName = '';
  newWorkspaceDescription = '';

  constructor(
    private messageService: MessageService,
    private workspaceService: WorkspaceService,
    private userService: UserService
  ) {}

  openCreateWorkspace() {
    this.showCreateDialog = true;
  }

  closeDialog() {
    this.showCreateDialog = false;
    this.resetForm();
  }

  createWorkspace() {
    const userId = localStorage.getItem('user_id');

    if (!userId) {
      this.messageService.add({
        severity: 'error',
        summary: 'User Not Logged In',
        detail: 'You must be logged in to create a workspace.',
      });
      return;
    }

    const payload = {
      name: this.newWorkspaceName,
      description: this.newWorkspaceDescription,
      ownerId: userId,
      ownerName: this.userService.currentUser?.name || '',
    };

    this.workspaceService.createWorkspace(payload).subscribe({
      next: (result) => {
        if (result.success && result.data) {
          this.workspaceService.suppressNextRefreshToastCount = 2;
          this.workspaceService.emitWorkspaceCreated(result.data);

          this.messageService.add({
            severity: 'success',
            summary: 'Workspace Created',
            detail: `Workspace "${this.newWorkspaceName}" created successfully!`,
            life: 3000,
          });

          this.closeDialog();
        } else {
          this.messageService.add({
            severity: 'error',
            summary: 'Creation Failed',
            detail: result.error?.message || 'Unknown error',
          });
        }
      },
      error: (err) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Server Error',
          detail: err?.error?.message || 'Something went wrong.',
        });
      }
    });
  }

  private resetForm() {
    this.newWorkspaceName = '';
    this.newWorkspaceDescription = '';
  }
}
