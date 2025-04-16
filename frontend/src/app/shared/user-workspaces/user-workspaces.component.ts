import { Component, OnInit } from '@angular/core';
import { WorkspaceService } from '../../services/workspace.service';
import {Workspace} from "../../models/WorkspaceFile";
import {ConfirmationService, MessageService} from "primeng/api";

@Component({
  selector: 'app-user-workspaces',
  templateUrl: './user-workspaces.component.html',
  styleUrl: './user-workspaces.component.scss'
})
export class UserWorkspacesComponent implements OnInit {
  workspaces: Workspace[] = [];

  searchText = '';
  currentPage = 1;
  itemsPerPage = 10;

  constructor(
              private workspaceService: WorkspaceService,
              private confirmationService: ConfirmationService,
              private messageService: MessageService) {}

  ngOnInit(): void {
    const userId = Number(localStorage.getItem('user_id'));

    this.workspaceService.workspaceCreated$.subscribe(newWorkspace => {
      this.workspaces.unshift(newWorkspace);
    });

    this.workspaceService.getUserWorkspaces(userId, this.currentPage, this.itemsPerPage).subscribe(res => {
      if (res.success) {
        this.workspaces = res.data ?? [];
      }
    });
  }

  get filteredWorkspaces() {
    return this.workspaces.filter(w =>
      w.name.toLowerCase().includes(this.searchText.toLowerCase())
    );
  }

  get totalPages() {
    return Math.ceil(this.filteredWorkspaces.length / this.itemsPerPage);
  }

  get paginatedWorkspaces() {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredWorkspaces.slice(start, start + this.itemsPerPage);
  }

  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
    }
  }

  prevPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
    }
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
              this.workspaces = this.workspaces.filter(w => w.id !== workspace.id);
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
