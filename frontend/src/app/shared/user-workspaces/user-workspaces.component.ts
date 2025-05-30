import { Component, OnInit } from '@angular/core';
import { WorkspaceService } from '../../services/workspace.service';
import { Workspace } from '../../models/WorkspaceFile';
import { ConfirmationService, MessageService } from 'primeng/api';
import { UserService, User } from '../../services/user.service';
import { filter, take } from 'rxjs/operators';
import {OverlayPanel} from "primeng/overlaypanel";

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
  formatOptions: Intl.DateTimeFormatOptions = {
    dateStyle: 'short',
    timeStyle: 'short'
  };
  protected currentUserId: number | null = null;

  constructor(
    private workspaceService: WorkspaceService,
    private userService: UserService,
    private confirmationService: ConfirmationService,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
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

    this.workspaceService.workspaceCreated$.subscribe(() => {
      this.fetchWorkspaces();
      this.totalWorkspaces += 1;

      if (this.workspaceService.suppressNextRefreshToastCount > 0) {
        this.workspaceService.suppressNextRefreshToastCount--;
      } else {
        this.messageService.add({
          severity: 'info',
          summary: 'Refreshed',
          detail: 'Workspaces list updated.',
          life: 2000
        });
      }
    });

    this.workspaceService.workspaceJoined$.subscribe(() => {
      this.fetchWorkspaces();
      this.totalWorkspaces += 1;

      if (this.workspaceService.suppressNextRefreshToastCount > 0) {
        this.workspaceService.suppressNextRefreshToastCount--;
      } else {
        this.messageService.add({
          severity: 'info',
          summary: 'Refreshed',
          detail: 'Workspaces list updated.',
          life: 2000
        });
      }
    });
  }

  fetchWorkspaces(): void {
    if (!this.currentUserId) return;

    this.workspaceService.getUserWorkspaces(this.currentUserId, this.currentPage, this.itemsPerPage, {
      sizeFrom: this.sizeRange[0] > 0 ? this.sizeRange[0] : undefined,
      sizeTo: this.sizeRange[1] < 5 ? this.sizeRange[1] : undefined,
      sortByDate: this.sortOrder.dateCreated ?? undefined,
      sortByFiles: this.sortOrder.totalFiles ?? undefined
    }).subscribe(res => {
      if (res.success) {
        this.workspaces = res.data ?? [];
      }
    });
  }

  refreshWorkspaces(): void {
    this.fetchWorkspaces();

    if (this.workspaceService.suppressNextRefreshToastCount > 0) {
      this.workspaceService.suppressNextRefreshToastCount--;
      return;
    }

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
  /*
  get paginatedWorkspaces(): Workspace[] {
    let result = this.filteredWorkspaces;

    // Sort by dateCreated
    if (this.sortOrder.dateCreated) {
      result = [...result].sort((a, b) => {
        const dateA = new Date(a.createdOn || 0).getTime();
        const dateB = new Date(b.createdOn || 0).getTime();
        return this.sortOrder.dateCreated === 'asc' ? dateA - dateB : dateB - dateA;
      });
    }

    // Then sort by totalFiles
    if (this.sortOrder.totalFiles) {
      result = [...result].sort((a, b) => {
        return this.sortOrder.totalFiles === 'asc'
          ? a.totalFiles - b.totalFiles
          : b.totalFiles - a.totalFiles;
      });
    }

    return result;
  }
  */

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
              this.totalWorkspaces -= 1;
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

  sizeRange: [number, number] = [0, 5];
  sortOrder = {
    dateCreated: null as 'asc' | 'desc' | null,
    totalFiles: null as 'asc' | 'desc' | null
  };

  showFilters(event: Event, overlayPanel: OverlayPanel) {
    overlayPanel.toggle(event);
  }

  sortByDate(order: 'asc' | 'desc') {
    this.sortOrder.dateCreated = order;
    this.applyFiltersAndSorting();
  }

  sortByFiles(order: 'asc' | 'desc') {
    this.sortOrder.totalFiles = order;
    this.applyFiltersAndSorting();
  }

  clearFilters() {
    this.sizeRange = [0, 5];
    this.sortOrder = { dateCreated: null, totalFiles: null };
    this.applyFiltersAndSorting();
  }

  applyFiltersAndSorting() {
    this.fetchWorkspaces();
  }

  clearSizeFilter(): void {
    this.sizeRange = [0, 5];
    this.applyFiltersAndSorting();
  }

  clearDateSort(): void {
    this.sortOrder.dateCreated = null;
    this.applyFiltersAndSorting();
  }

  clearFileSort(): void {
    this.sortOrder.totalFiles = null;
    this.applyFiltersAndSorting();
  }

  get anyFiltersActive(): boolean {
    return (this.sizeRange[0] > 0 || this.sizeRange[1] < 5) ||
      !!this.sortOrder.dateCreated ||
      !!this.sortOrder.totalFiles;
  }

  getDisplayStatus(lastUploadOn: string | undefined): { label: string; class: string } {
    if (!lastUploadOn) {
      return { label: 'Never Uploaded', class: 'badge-info' };
    }

    const lastUpload = new Date(lastUploadOn);
    const now = new Date();
    const diffMs = now.getTime() - lastUpload.getTime();
    const diffDays = diffMs / (1000 * 60 * 60 * 24);

    if (diffDays < 1) {
      return { label: 'Active Today', class: 'badge-success' };
    } else if (diffDays < 3) {
      return { label: 'Recently Active', class: 'badge-info' };
    } else if (diffDays < 7) {
      return { label: 'This Week', class: 'badge-warning' };
    } else if (diffDays < 31) {
      return { label: 'Dusty Folder', class: 'badge-secondary' };
    } else {
      return { label: 'Forgotten Realm', class: 'badge-danger' };
    }
  }
}
