import { Component } from '@angular/core';

@Component({
  selector: 'app-user-workspaces',
  templateUrl: './user-workspaces.component.html',
  styleUrl: './user-workspaces.component.scss'
})
export class UserWorkspacesComponent {
  workspaces = [
    {
      id: 1,
      initials: 'MC',
      name: 'Marketing Campaign',
      owner: 'Alice Johnson',
      company: 'BriteMank',
      email: 'alice@britemank.com',
      source: 'Website',
      status: 'Active'
    },
    {
      id: 2,
      initials: 'DS',
      name: 'Design System',
      owner: 'Bob Williams',
      company: 'Wavelength',
      email: 'bob@wavelength.com',
      source: 'Cold Call',
      status: 'Inactive'
    },
    {
      id: 3,
      initials: 'PP',
      name: 'Product Pitch',
      owner: 'Carla Evans',
      company: 'ZenTrailMs',
      email: 'carla@zentrailms.com',
      source: 'Linkedin',
      status: 'Prospect'
    },
    {
      id: 4,
      initials: 'RP',
      name: 'Roadmap Planning',
      owner: 'David Brown',
      company: 'TechNova',
      email: 'davidbrown@gmail.com',
      source: 'Email',
      status: 'Active'
    },
    {
      id: 5,
      initials: 'RP',
      name: 'Roadmap Planning',
      owner: 'David Brown',
      company: 'TechNova',
      email: 'davidbrown@gmail.com',
      source: 'Email',
      status: 'Active'
    },
    {
      id: 6,
      initials: 'RP',
      name: 'Roadmap Planning',
      owner: 'David Brown',
      company: 'TechNova',
      email: 'davidbrown@gmail.com',
      source: 'Email',
      status: 'Active'
    },
    {
      id: 7,
      initials: 'RP',
      name: 'Roadmap Planning',
      owner: 'David Brown',
      company: 'TechNova',
      email: 'davidbrown@gmail.com',
      source: 'Email',
      status: 'Active'
    },
    {
      id: 8,
      initials: 'RP',
      name: 'Roadmap Planning',
      owner: 'David Brown',
      company: 'TechNova',
      email: 'davidbrown@gmail.com',
      source: 'Email',
      status: 'Active'
    },
    {
      id: 9,
      initials: 'RP',
      name: 'Roadmap Planning',
      owner: 'David Brown',
      company: 'TechNova',
      email: 'davidbrown@gmail.com',
      source: 'Email',
      status: 'Active'
    },


  ];

  // workspace-overview.component.ts
  searchText = '';
  currentPage = 1;
  itemsPerPage = 10;

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
}
