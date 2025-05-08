import { Component, OnInit } from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import { WorkspaceService } from '../../services/workspace.service';
import { UserService, User } from '../../services/user.service';
import { Workspace, Role, WorkspaceUser } from '../../models/WorkspaceFile';
import { MessageService } from 'primeng/api';
import {RadioButton} from "primeng/radiobutton";

@Component({
  selector: 'app-workspace-general-page',
  templateUrl: './workspace-general-page.component.html',
  styleUrls: ['./workspace-general-page.component.scss'],
})
export class WorkspaceGeneralPageComponent implements OnInit {
  isEditing = false;
  searchCollaborator = '';

  workspace: Workspace = {
    id: 0,
    name: '',
    ownerId: 0,
    ownerName: '',
    description: '',
    status: 'Active',
    sizeInGB: 0,
    totalFiles: 0,
    createdOn: '',
    lastUploadOn: '',
    users: []
  };

  collaborators: {
    id: number;
    name: string;
    email: string;
    role: string;           // Display label
    workspaceRole: Role;    // Actual enum value
  }[] = [];

  recentActivity = [
    'Alice uploaded plan.pdf',
    'Bob deleted old_logo.png',
    'Charlie created Assets folder',
  ];

  activeFiles = ['notes.txt', 'readme.md', 'logo.svg'];

  constructor(
    private route: ActivatedRoute,
    private workspaceService: WorkspaceService,
    private userService: UserService,
    private messageService: MessageService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.userService.user$.subscribe(user => {
      if (!user) return;

      this.currentUser = user;

      const workspaceId = +this.route.snapshot.paramMap.get('id')!;
      this.loadWorkspace(workspaceId);
    });
  }

  loadWorkspace(workspaceId: number) {
    this.workspaceService.getWorkspaceById(workspaceId).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.workspace = res.data;

          const users: WorkspaceUser[] = res.data.users ?? [];
          const userIds = users.map(u => u.userId);

          if (userIds.length) {
            this.userService.getUsersBulk(userIds).subscribe(userRes => {
              if (userRes.success && userRes.data) {
                this.collaborators = users.map(wu => {
                  const user = userRes.data!.find(u => u.id === wu.userId);
                  return {
                    id: user?.id ?? 0,
                    name: user?.name ?? '(Unknown)',
                    email: user?.email ?? '',
                    role: this.mapRole(wu.role),
                    workspaceRole: wu.role
                  };
                });
              }
            });
          }
        }
      }
    });
  }

  saveChanges(): void {
    if (!this.isPrivileged) return;

    this.workspaceService.updateWorkspace({
      workspaceId: this.workspace.id,
      name: this.workspace.name,
      description: this.workspace.description ?? ''
    }).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.isEditing = false;
          this.messageService.add({
            severity: 'success',
            summary: 'Saved',
            detail: 'Workspace updated successfully.',
            life: 3000
          });
        } else {
          this.messageService.add({
            severity: 'error',
            summary: 'Update failed',
            detail: res.error?.message || 'Could not update workspace.',
            life: 3000
          });
        }
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'An unexpected error occurred.',
          life: 3000
        });
      }
    });
  }

  get filteredCollaborators() {
    if (!this.searchCollaborator) return this.collaborators;
    return this.collaborators.filter(c =>
      c.name.toLowerCase().includes(this.searchCollaborator.toLowerCase())
    );
  }

  private mapRole(role: Role): string {
    switch (role) {
      case Role.Owner: return 'Owner';
      case Role.Manager: return 'Manager';
      case Role.Guest: return 'Guest';
      default: return 'Unknown';
    }
  }

  goToProfile(userId: number) {
    const currentUser = this.userService.currentUser;
    if (!currentUser) return;

    if (userId === currentUser.id) {
      this.router.navigate(['/profile']);
    } else {
      this.router.navigate(['/profile', userId]);
    }
  }


  currentUser: User | null = null;

  get isPrivileged(): boolean {
    if (!this.currentUser || !this.workspace.users) return false;
    const current = this.workspace.users.find(u => u.userId === this.currentUser!.id);
    return current?.role === Role.Owner || current?.role === Role.Manager;
  }


  removeUser(userId: number) {
    this.workspaceService.removeUserFromWorkspace({ userId, workspaceId: this.workspace.id }).subscribe({
      next: (res) => {
        if (res.success) {
          this.messageService.add({ severity: 'success', summary: 'Removed', detail: 'User was removed.' });
          this.loadWorkspace(this.workspace.id); // Reload data
        } else {
          this.messageService.add({ severity: 'error', summary: 'Error', detail: res.error?.message || 'Failed to remove user.' });
        }
      }
    });
  }

  changeRole(userId: number, currentRole: Role) {
    const newRole = currentRole === Role.Manager ? Role.Guest : Role.Manager;

    this.workspaceService.changeUserRole({
      workspaceId: this.workspace.id,
      userId,
      role: newRole
    }).subscribe({
      next: (res) => {
        if (res.success) {
          this.messageService.add({ severity: 'success', summary: 'Updated', detail: 'Role updated.' });
          this.loadWorkspace(this.workspace.id);
        } else {
          this.messageService.add({ severity: 'error', summary: 'Error', detail: res.error?.message || 'Failed to change role.' });
        }
      }
    });
  }

  getUserActions(userId: number, workspaceRole: Role) {
    if (!this.currentUser || !this.workspace.users) return [];

    const currentUser = this.workspace.users.find(u => u.userId === this.currentUser!.id);
    if (!currentUser) return [];

    const currentRole = currentUser.role;

    if (userId === currentUser.userId) {
      const isLastOwner = currentUser.role === Role.Owner &&
        this.workspace.users.filter(u => u.role === Role.Owner).length === 1;

      if (!isLastOwner) {
        return [{
          label: 'Leave Workspace',
          icon: 'pi pi-sign-out',
          command: () => this.leaveWorkspace()
        }];
      }

      return [];
    }

    const actions = [];

    const isLastOwner =
      workspaceRole === Role.Owner &&
      this.workspace.users.filter(u => u.role === Role.Owner).length === 1;

    if (currentRole === Role.Owner) {
      if (!isLastOwner) {
        actions.push({
          label: 'Remove',
          icon: 'pi pi-times',
          command: () => this.removeUser(userId),
        });
      }

      actions.push({
        label: 'Change Role',
        icon: 'pi pi-user-edit',
        command: () => this.openChangeRoleDialog(userId, this.collaborators.find(c => c.id === userId)?.name || 'User'),
      });
    }

    if (currentRole === Role.Manager && workspaceRole === Role.Guest) {
      actions.push({
        label: 'Remove',
        icon: 'pi pi-times',
        command: () => this.removeUser(userId),
      });
    }

    return actions;
  }


  changeRoleDialogVisible = false;
  selectedUserForRoleChange: { id: number; name: string } | null = null;
  newSelectedRole: number | null = null;

  openChangeRoleDialog(userId: number, userName: string) {
    const collaborator = this.collaborators.find(c => c.id === userId);

    if (!collaborator) return;

    this.selectedUserForRoleChange = { id: userId, name: userName };
    this.newSelectedRole = collaborator.workspaceRole; // 👈 Set current role
    this.changeRoleDialogVisible = true;
  }

  cancelRoleChange() {
    this.changeRoleDialogVisible = false;
    this.selectedUserForRoleChange = null;
    this.newSelectedRole = null;
  }

  confirmRoleChange() {
    if (!this.selectedUserForRoleChange || !this.newSelectedRole) return;

    this.workspaceService.changeUserRole({
      workspaceId: this.workspace.id,
      userId: this.selectedUserForRoleChange.id,
      role: this.newSelectedRole,
    }).subscribe({
      next: (res) => {
        if (res.success) {
          this.messageService.add({ severity: 'success', summary: 'Updated', detail: 'Role updated.' });
          this.loadWorkspace(this.workspace.id);
        } else {
          this.messageService.add({ severity: 'error', summary: 'Error', detail: res.error?.message || 'Failed to change role.' });
        }

        this.cancelRoleChange();
      }
    });
  }


  leaveWorkspace() {
    if (!this.currentUser) return;

    this.workspaceService.removeUserFromWorkspace({
      userId: this.currentUser.id,
      workspaceId: this.workspace.id
    }).subscribe({
      next: (res) => {
        if (res.success) {
          this.messageService.add({ severity: 'success', summary: 'Left Workspace', detail: 'You have left the workspace.' });
          this.router.navigate(['/']); // Or redirect to dashboard
        } else {
          this.messageService.add({ severity: 'error', summary: 'Error', detail: res.error?.message || 'Failed to leave workspace.' });
        }
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'An unexpected error occurred.' });
      }
    });
  }

  canSeeActions(userId: number): boolean {
    if (!this.currentUser) return false;
    return this.isPrivileged || this.currentUser.id === userId;
  }
}
