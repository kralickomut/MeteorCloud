import { Component, OnInit } from '@angular/core';
import { WorkspaceService } from '../../services/workspace.service';
import { Workspace } from '../../models/WorkspaceFile';
import { ConfirmationService, MessageService } from 'primeng/api';
import { UserService, User } from '../../services/user.service';
import { filter, take } from 'rxjs/operators';

@Component({
  selector: 'app-user-workspaces',
  templateUrl: './user-workspaces.component.html',
  styleUrls: ['./user-workspaces.component.scss']
})
export class UserWorkspacesComponent implements OnInit {
  workspaces: Workspace[] = [];
  totalWorkspaces = 0;
  currentPage = 1;
  itemsPerPage = 10;
  searchText = '';

  private currentUserId: number | null = null;

  constructor(
    private workspaceService: WorkspaceService,
    private userService: UserService,
    private confirmationService: ConfirmationService,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    // Wait until user is available
    this.userService.user$
      .pipe(
        filter((u): u is User => !!u),
        take(1)
      )
      .subscribe(user => {
        this.currentUserId = user.id;
        this.totalWorkspaces = user.inTotalWorkspaces;
        this.fetchWorkspaces();
      });

    // Handle real-time workspace events
    this.workspaceService.workspaceCreated$.subscribe(() => this.refreshWorkspaces());
    this.workspaceService.workspaceJoined$.subscribe(() => this.refreshWorkspaces());
  }

  fetchWorkspaces(): void {
    if (!this.currentUserId) return;

    this.workspaceService.getUserWorkspaces(this.currentUserId, this.currentPage, this.itemsPerPage)
      .subscribe(res => {
        if (res.success) {
          this.workspaces = res.data ?? [];
        }
      });
  }

  refreshWorkspaces(): void {
    this.fetchWorkspaces();
    this.messageService.add({
      severity: 'info',
      summary: 'Refreshed',
      detail: 'Workspaces list updated.',
      life: 2000
    });
  }

  get filteredWorkspaces(): Workspace[] {
    return this.workspaces.filter(w =>
      w.name.toLowerCase().includes(this.searchText.toLowerCase())
    );
  }

  get totalPages(): number {
    return Math.ceil(this.totalWorkspaces / this.itemsPerPage);
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.fetchWorkspaces();
    }
  }

  prevPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.fetchWorkspaces();
    }
  }

  get paginatedWorkspaces(): Workspace[] {
    return this.filteredWorkspaces;
  }

  deleteWorkspace(workspace: Workspace): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete the workspace "${workspace.name}"?`,
      header: 'Delete Workspace',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Delete',
      rejectLabel: 'Cancel',
      acceptButtonStyleClass: 'p-button-danger',
      rejectButtonStyleClass: 'p-button-text',
      accept: () => {
        this.workspaceService.deleteWorkspace(workspace.id).subscribe({
          next: (res) => {
            if (res.success) {
              this.fetchWorkspaces();
              this.messageService.add({
                severity: 'success',
                summary: 'Deleted',
                detail: `Workspace "${workspace.name}" has been deleted.`,
                life: 3000
              });
            } else {
              this.messageService.add({
                severity: 'error',
                summary: 'Failed',
                detail: res.error?.message || 'Workspace could not be deleted.'
              });
            }
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'An unexpected error occurred.'
            });
          }
        });
      }
    });
  }
}
