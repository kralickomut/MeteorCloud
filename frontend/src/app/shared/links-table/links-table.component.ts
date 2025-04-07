import { Component } from '@angular/core';

@Component({
  selector: 'app-links-table',
  templateUrl: './links-table.component.html',
  styleUrl: './links-table.component.scss'
})
export class LinksTableComponent {
  searchText = '';
  currentPage = 1;
  itemsPerPage = 10;

  links = [
    {
      name: 'Marketing PDF',
      url: 'https://meteorcloud.com/share/abc123',
      expiresAt: new Date(new Date().getTime() + 2 * 60 * 60 * 1000)
    },
    {
      name: 'Design Mockup',
      url: 'https://meteorcloud.com/share/xyz456',
      expiresAt: new Date(new Date().getTime() + 34 * 60 * 60 * 1000)
    },
    {
      name: 'Invoice File',
      url: 'https://meteorcloud.com/share/inv789',
      expiresAt: new Date() // expired
    },
    {
      name: 'Project Roadmap',
      url: 'https://meteorcloud.com/share/roadmap101',
      expiresAt: new Date(new Date().getTime() + 5 * 60 * 60 * 1000)
    },
    {
      name: 'Design System',
      url: 'https://meteorcloud.com/share/design202',
      expiresAt: new Date(new Date().getTime() + 1 * 60 * 60 * 1000)
    },
    {
      name: 'Marketing Brief',
      url: 'https://meteorcloud.com/share/brief303',
      expiresAt: new Date(new Date().getTime() + 3 * 60 * 60 * 1000)
    },
    {
      name: 'Sales Report',
      url: 'https://meteorcloud.com/share/report404',
      expiresAt: new Date(new Date().getTime() + 10 * 60 * 60 * 1000)
    },
    {
      name: 'Team Folders',
      url: 'https://meteorcloud.com/share/folders505',
      expiresAt: new Date(new Date().getTime() + 12 * 60 * 60 * 1000)
    },
    {
      name: 'User Guide',
      url: 'https://meteorcloud.com/share/guide606',
      expiresAt: new Date(new Date().getTime() + 8 * 60 * 60 * 1000)
    },
    {
      name: 'API Documentation',
      url: 'https://meteorcloud.com/share/api707',
      expiresAt: new Date(new Date().getTime() + 4 * 60 * 60 * 1000)
    }
  ];

  get filteredLinks() {
    return this.links.filter(link =>
      link.name.toLowerCase().includes(this.searchText.toLowerCase())
    );
  }

  get totalPages() {
    return Math.ceil(this.filteredLinks.length / this.itemsPerPage);
  }

  get paginatedLinks() {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredLinks.slice(start, start + this.itemsPerPage);
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

  calculateHoursLeft(date: Date): number {
    const now = new Date().getTime();
    const target = new Date(date).getTime();
    const diff = target - now;
    return Math.max(0, Math.floor(diff / (1000 * 60 * 60)));
  }

  isExpired(date: Date): boolean {
    return new Date(date).getTime() <= new Date().getTime();
  }

  copyToClipboard(url: string): void {
    navigator.clipboard.writeText(url);
    // You can trigger a toast here
  }

  deleteLink(linkToDelete: any) {
    this.links = this.links.filter(link => link !== linkToDelete);
  }
}
