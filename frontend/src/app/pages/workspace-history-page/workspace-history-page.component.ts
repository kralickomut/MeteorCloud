import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MessageService } from 'primeng/api';
import { WorkspaceInvitationHistoryDto, WorkspaceService } from '../../services/workspace.service';
import { AuditEventModel, AuditService } from '../../services/audit.service';

@Component({
  selector: 'app-workspace-history-page',
  templateUrl: './workspace-history-page.component.html',
  styleUrl: './workspace-history-page.component.scss'
})
export class WorkspaceHistoryPageComponent implements OnInit {
  workspaceId = 0;
  workspaceName = '';
  activeTab: 'files' | 'invitations' = 'files';

  // File history
  fileHistory: AuditEventModel[] = [];
  totalFilePages = 1;
  currentFilePage = 1;
  isLoadingFiles = false;

  // Invitations
  invitations: WorkspaceInvitationHistoryDto[] = [];
  totalInvitationPages = 1;
  currentInvitationPage = 1;
  isLoadingInvitations = false;

  // Common
  itemsPerPage = 10;
  searchText = '';

  constructor(
    private route: ActivatedRoute,
    private workspaceService: WorkspaceService,
    private auditService: AuditService,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    this.workspaceId = Number(this.route.snapshot.paramMap.get('id'));

    this.workspaceService.getWorkspaceById(this.workspaceId).subscribe(res => {
      if (res.success && res.data) {
        this.workspaceName = res.data.name;
      } else {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load workspace name.'
        });
      }
    })

    this.fetchFileHistory(); // default tab
  }

  switchTab(tab: 'files' | 'invitations'): void {
    this.activeTab = tab;
    this.searchText = '';

    if (tab === 'files') {
      this.fetchFileHistory();
    } else {
      this.fetchInvitations();
    }
  }

  fetchFileHistory(): void {
    this.isLoadingFiles = true;

    this.auditService
      .getFileHistory(this.workspaceId, this.currentFilePage, this.itemsPerPage)
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.fileHistory = res.data.items;
            this.totalFilePages = Math.ceil(res.data.totalCount / this.itemsPerPage);
          }
          this.isLoadingFiles = false;
        },
        error: () => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to load file history.'
          });
          this.isLoadingFiles = false;
        }
      });
  }

  fetchInvitations(): void {
    this.isLoadingInvitations = true;

    this.workspaceService
      .getInvitationHistory(this.workspaceId, this.currentInvitationPage, this.itemsPerPage)
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.invitations = res.data.items;
            this.totalInvitationPages = Math.ceil(res.data.totalCount / this.itemsPerPage);
          }
          this.isLoadingInvitations = false;
        },
        error: () => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to load invitation history.'
          });
          this.isLoadingInvitations = false;
        }
      });
  }

  // Pagination logic
  nextPage(): void {
    if (this.activeTab === 'files' && this.currentFilePage < this.totalFilePages) {
      this.currentFilePage++;
      this.fetchFileHistory();
    }

    if (this.activeTab === 'invitations' && this.currentInvitationPage < this.totalInvitationPages) {
      this.currentInvitationPage++;
      this.fetchInvitations();
    }
  }

  prevPage(): void {
    if (this.activeTab === 'files' && this.currentFilePage > 1) {
      this.currentFilePage--;
      this.fetchFileHistory();
    }

    if (this.activeTab === 'invitations' && this.currentInvitationPage > 1) {
      this.currentInvitationPage--;
      this.fetchInvitations();
    }
  }

  // UI helpers
  get paginatedFileItems(): AuditEventModel[] {
    return this.fileHistory.filter(item =>
      JSON.stringify(item).toLowerCase().includes(this.searchText.toLowerCase())
    );
  }

  get paginatedInvitationItems(): WorkspaceInvitationHistoryDto[] {
    return this.invitations.filter(item =>
      JSON.stringify(item).toLowerCase().includes(this.searchText.toLowerCase())
    );
  }

  get currentPage(): number {
    return this.activeTab === 'files' ? this.currentFilePage : this.currentInvitationPage;
  }

  get totalPages(): number {
    return this.activeTab === 'files' ? this.totalFilePages : this.totalInvitationPages;
  }
}
