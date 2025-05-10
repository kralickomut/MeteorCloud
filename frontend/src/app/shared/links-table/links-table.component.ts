import {Component, OnInit} from '@angular/core';
import {PagedResult} from "../../services/audit.service";
import {FastLink, LinkService} from "../../services/link.service";
import {ApiResult} from "../../models/api-result";
import {UserService} from "../../services/user.service";
import {ConfirmationService, MessageService} from "primeng/api";
import {FileService} from "../../services/file.service";

@Component({
  selector: 'app-links-table',
  templateUrl: './links-table.component.html',
  styleUrl: './links-table.component.scss'
})
export class LinksTableComponent implements OnInit {
  searchText = '';
  currentPage = 1;
  itemsPerPage = 10;
  userId: number | null = null;
  baseUrl = window.location.origin;

  links: FastLink[] = [];
  totalCount = 0;

  refreshDialogVisible = false;
  refreshTargetToken: string = '';
  refreshHours: number = 12; // default value

  formatOptions: Intl.DateTimeFormatOptions = {
    dateStyle: 'short',
    timeStyle: 'short'
  };

  constructor(
    private linkService: LinkService,
    private userService: UserService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService,
    private fileService: FileService
  ) {}

  ngOnInit(): void {
    this.userService.user$.subscribe(user => {
      if (user) {
        this.userId = user.id;
        this.loadLinks(user.id);
      }
    });

    // listen for new links
    this.linkService.fastLinkCreated$.subscribe(() => {
      if (this.userId) {
        this.loadLinks(this.userId);
      }
    });
  }

  loadLinks(userId: number): void {
    this.linkService.getUserLinks(userId, this.currentPage, this.itemsPerPage).subscribe({
      next: (res: ApiResult<PagedResult<FastLink>>) => {
        this.links = res.data?.items ?? [];
        this.totalCount = res.data?.totalCount ?? 0;
      },
      error: () => {
        this.links = [];
        this.totalCount = 0;
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load links.'
        });
      }
    });
  }

  get totalPages(): number {
    return Math.ceil((this.totalCount || 0) / this.itemsPerPage);
  }

  get paginatedLinks(): FastLink[] {
    return (this.links ?? []).filter(link =>
      link.name?.toLowerCase().includes(this.searchText.toLowerCase())
    );
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadLinks(this.userId!);
    }
  }

  prevPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadLinks(this.userId!);
    }
  }

  calculateTimeLeft(date: string): string {
    const now = new Date().getTime();
    const target = new Date(date + 'Z').getTime(); // 👈 force UTC
    const diff = target - now;

    if (diff <= 0) return '0 mins';

    const totalMinutes = Math.floor(diff / (1000 * 60));
    const hours = Math.floor(totalMinutes / 60);
    const minutes = totalMinutes % 60;

    if (hours > 0 && minutes > 0) {
      return `${hours} hr${hours > 1 ? 's' : ''} ${minutes} min${minutes > 1 ? 's' : ''}`;
    }
    if (hours > 0) return `${hours} hr${hours > 1 ? 's' : ''}`;
    return `${minutes} min${minutes > 1 ? 's' : ''}`;
  }

  isExpired(date: string): boolean {
    const now = new Date().getTime();
    const target = new Date(date + 'Z').getTime(); // 👈 force UTC
    return target <= now;
  }

  copyToClipboard(token: string): void {
    const baseUrl = window.location.origin; // gets http://localhost:4200 or your deployed domain
    const fullUrl = `${baseUrl}/shared/${token}`;
    navigator.clipboard.writeText(fullUrl);
    this.messageService.add({
      severity: 'info',
      summary: 'Copied',
      detail: 'Link copied to clipboard!'
    });
  }

  deleteLink(linkToDelete: FastLink): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete the fast link "${linkToDelete.name}"?`,
      header: 'Delete Fast Link',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Delete',
      rejectLabel: 'Cancel',
      acceptButtonStyleClass: 'p-button-danger',
      rejectButtonStyleClass: 'p-button-text',
      accept: () => this.executeDelete(linkToDelete)
    });
  }

  submitRefresh(): void {
    this.linkService.refreshLink(this.refreshTargetToken, this.refreshHours).subscribe({
      next: (res) => {
        if (res.success) {
          this.messageService.add({
            severity: 'success',
            summary: 'Link Refreshed',
            detail: `Link was extended by ${this.refreshHours} hours.`
          });
          this.refreshDialogVisible = false;
          this.refreshLinks(); // reload list
        } else {
          this.messageService.add({
            severity: 'error',
            summary: 'Failed',
            detail: res.error?.message || 'Could not refresh link.'
          });
        }
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Something went wrong.'
        });
      }
    });
  }

  openRefreshDialog(token: string): void {
    this.refreshTargetToken = token;
    this.refreshHours = 12;
    this.refreshDialogVisible = true;
  }

  private executeDelete(linkToDelete: FastLink): void {
    if (!this.userId) return;

    const path = `fast-link-files/${this.userId}/${linkToDelete.fileId}`;

    this.linkService.deleteFastLinkFile(path).subscribe({
      next: (res) => {
        if (res.success) {
          this.messageService.add({
            severity: 'success',
            summary: 'Deleted',
            detail: `Fast link "${linkToDelete.name}" deleted successfully`
          });
          this.loadLinks(this.userId!);
        } else {
          this.messageService.add({
            severity: 'error',
            summary: 'Delete Failed',
            detail: res.error?.message || 'Could not delete the link'
          });
        }
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'There was an error while deleting the link'
        });
      }
    });
  }

  refreshLinks(): void {
    if (this.userId) {
      this.loadLinks(this.userId);
      this.messageService.add({
        severity: 'info',
        summary: 'Refreshed',
        detail: 'Link list updated.',
        life: 2000
      });
    }
  }

  refreshLink(link: FastLink): void {
    const hours = 12; // or prompt user for value

    this.linkService.refreshLink(link.token, hours).subscribe({
      next: (res) => {
        if (res.success) {
          this.messageService.add({
            severity: 'success',
            summary: 'Link Refreshed',
            detail: `Link "${link.name}" was extended by ${hours} hours.`
          });
          this.refreshLinks(); // reload the list
        } else {
          this.messageService.add({
            severity: 'error',
            summary: 'Refresh Failed',
            detail: res.error?.message || 'Link could not be refreshed.'
          });
        }
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Something went wrong while refreshing the link.'
        });
      }
    });
  }

  protected readonly window = window;
}
