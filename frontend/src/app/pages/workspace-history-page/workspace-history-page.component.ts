import {Component, OnInit} from '@angular/core';
import {WorkspaceInvitationHistoryDto, WorkspaceService} from "../../services/workspace.service";
import {ActivatedRoute} from "@angular/router";
import {MessageService} from "primeng/api";

@Component({
  selector: 'app-workspace-history-page',
  templateUrl: './workspace-history-page.component.html',
  styleUrl: './workspace-history-page.component.scss'
})
export class WorkspaceHistoryPageComponent implements OnInit{
  workspaceName = 'Name fetched from API';
  activeTab: 'files' | 'invitations' = 'files';

  invitations: WorkspaceInvitationHistoryDto[] = [];
  totalInvitationPages = 1;
  isLoadingInvitations = false;

  constructor(
    private route: ActivatedRoute,
    private workspaceService: WorkspaceService,
    private messageService: MessageService,
  ) {}


  ngOnInit() {
    const workspaceId = Number(this.route.snapshot.paramMap.get('id'));
    this.workspaceId = workspaceId;
    this.fetchInvitations(); // initial load
  }

  workspaceId: number = 0;

  fetchInvitations() {
    this.isLoadingInvitations = true;

    this.workspaceService
      .getInvitationHistory(this.workspaceId, this.currentPage, this.itemsPerPage)
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.invitations = res.data.items;
            this.totalInvitationPages = Math.ceil(res.data.totalCount / this.itemsPerPage); // ✅ FIXED
          }
          this.isLoadingInvitations = false;
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load invitation history.' });
          this.isLoadingInvitations = false;
        }
      });
  }

  fileHistory = [
    { file: 'notes.txt', action: 'Created', user: 'Alice', date: '2025-03-28' },
    { file: 'plan.pdf', action: 'Deleted', user: 'Bob', date: '2025-03-29' },
    { file: 'list.txt', action: 'Updated', user: 'Charlie', date: '2025-03-30' },
    { file: 'presentation.pptx', action: 'Created', user: 'David', date: '2025-03-31' },
    { file: 'report.docx', action: 'Deleted', user: 'Emma', date: '2025-04-01' },
    { file: 'summary.txt', action: 'Updated', user: 'Frank', date: '2025-04-02' },
    { file: 'draft.docx', action: 'Created', user: 'Grace', date: '2025-04-03' },
    { file: 'feedback.pdf', action: 'Deleted', user: 'Hannah', date: '2025-04-04' },
    { file: 'report.docx', action: 'Deleted', user: 'Emma', date: '2025-04-01' }
  ];

  invitationHistory = [
    { user: 'David', invitedBy: 'Alice', status: 'Pending', date: '2025-03-30' },
    { user: 'Emma', invitedBy: 'Bob', status: 'Accepted', date: '2025-03-28' },

  ];

  searchText = '';
  currentPage = 1;
  itemsPerPage = 10;

  switchTab(tab: 'files' | 'invitations') {
    this.activeTab = tab;
    this.searchText = '';
    this.currentPage = 1;
    if (tab === 'invitations') {
      this.fetchInvitations();
    }
  }

  get filteredItems() {
    const items = this.activeTab === 'files' ? this.fileHistory : this.invitationHistory;
    return items.filter(item =>
      JSON.stringify(item).toLowerCase().includes(this.searchText.toLowerCase())
    );
  }

  get totalPages() {
    const items =
      this.activeTab === 'files' ? this.fileHistory : this.invitationHistory;
    const filtered = items.filter(item =>
      JSON.stringify(item).toLowerCase().includes(this.searchText.toLowerCase())
    );
    return Math.ceil(filtered.length / this.itemsPerPage);
  }

  get paginatedFileItems() {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.fileHistory
      .filter(item =>
        JSON.stringify(item).toLowerCase().includes(this.searchText.toLowerCase())
      )
      .slice(start, start + this.itemsPerPage);
  }

  get paginatedInvitationItems() {
    const filtered = this.invitations.filter(item =>
      JSON.stringify(item).toLowerCase().includes(this.searchText.toLowerCase())
    );

    const start = (this.currentPage - 1) * this.itemsPerPage;
    return filtered.slice(start, start + this.itemsPerPage);
  }

  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      if (this.activeTab === 'invitations') this.fetchInvitations();
    }
  }

  prevPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
      if (this.activeTab === 'invitations') this.fetchInvitations();
    }
  }


}
