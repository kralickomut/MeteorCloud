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

        this.linkService.fastLinkCreated$.subscribe(() => {
          this.loadLinks(user.id);
        });
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

  calculateHoursLeft(date: string): number {
    const now = new Date().getTime();
    const target = new Date(date + 'Z').getTime(); // Force parse as UTC
    return Math.max(0, Math.floor((target - now) / (1000 * 60 * 60)));
  }

  isExpired(date: string): boolean {
    return new Date(date).getTime() <= new Date().getTime();
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

  protected readonly window = window;
}
