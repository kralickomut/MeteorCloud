import { Component, Input } from '@angular/core';
import { MessageService } from 'primeng/api';
import { WorkspaceService } from '../../services/workspace.service';
import {InviteToWorkspace} from "../../models/WorkspaceFile";

@Component({
  selector: 'app-invite-workspace',
  templateUrl: './invite-workspace.component.html',
  styleUrl: './invite-workspace.component.scss'
})
export class InviteWorkspaceComponent {
  @Input() buttonText: string = 'Invite';
  @Input() workspaceId?: number;

  showInviteDialog = false;
  inviteEmail = '';

  constructor(
    private messageService: MessageService,
    private workspaceService: WorkspaceService
  ) {}

  openInviteDialog() {
    this.showInviteDialog = true;
  }

  closeInviteDialog() {
    this.showInviteDialog = false;
    this.resetForm();
  }

  sendInvite() {
    const userIdString = localStorage.getItem('user_id');
    const userId = userIdString ? Number(userIdString) : null;

    if (!userId) {
      this.messageService.add({
        severity: 'error',
        summary: 'Missing Info',
        detail: 'You must be logged in and have a workspace selected.',
      });
      return;
    }

    if (!this.workspaceId) {
      this.messageService.add({
        severity: 'error',
        summary: 'Missing Info',
        detail: 'Workspace ID is required.',
      });
      return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(this.inviteEmail.trim())) {
      this.messageService.add({
        severity: 'error',
        summary: 'Invalid Email',
        detail: 'Please enter a valid email address.',
      });
      return;
    }

    const inviteData: InviteToWorkspace = {
      email: this.inviteEmail.trim(),
      workspaceId: this.workspaceId,
      invitedByUserId: userId,
    };

    this.workspaceService.inviteToWorkspace(inviteData).subscribe({
      next: (res) => {
        if (res.success) {
          this.messageService.add({
            severity: 'success',
            summary: 'Invitation Sent',
            detail: `Invitation to ${this.inviteEmail} has been sent successfully.`,
            life: 3000,
          });
          this.closeInviteDialog();
        } else {
          this.messageService.add({
            severity: 'error',
            summary: 'Failed to Invite',
            detail: res.error?.message || 'Unknown error',
          });
        }
      },
      error: (err) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: err?.error?.message || 'Something went wrong.',
        });
      },
    });
  }

  private resetForm() {
    this.inviteEmail = '';
  }

  isValidEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }
}
