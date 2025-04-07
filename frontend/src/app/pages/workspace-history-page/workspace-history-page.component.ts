import { Component } from '@angular/core';

@Component({
  selector: 'app-workspace-history-page',
  templateUrl: './workspace-history-page.component.html',
  styleUrl: './workspace-history-page.component.scss'
})
export class WorkspaceHistoryPageComponent {
  workspaceName = 'Name fetched from API';
  activeTab: 'files' | 'invitations' = 'files';

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
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.invitationHistory
      .filter(item =>
        JSON.stringify(item).toLowerCase().includes(this.searchText.toLowerCase())
      )
      .slice(start, start + this.itemsPerPage);
  }

  nextPage() {
    if (this.currentPage < this.totalPages) this.currentPage++;
  }

  prevPage() {
    if (this.currentPage > 1) this.currentPage--;
  }
}
