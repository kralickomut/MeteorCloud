import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ConfirmationService } from 'primeng/api';

@Component({
  selector: 'app-workspace-table',
  templateUrl: './workspace-table.component.html',
  styleUrl: './workspace-table.component.scss'
})
export class WorkspaceTableComponent implements OnInit {
  workspaceId: string = '';
  workspace: any;
  currentPath: string[] = [];
  currentFolder: any;

  constructor(private route: ActivatedRoute, private confirmationService: ConfirmationService) {}

  ngOnInit(): void {
    this.workspaceId = this.route.snapshot.paramMap.get('id') ?? '';

    this.workspace = {
      id: this.workspaceId,
      name: 'Marketing Campaign',
      collaborators: [
        { name: 'Alice', role: 'Owner' },
        { name: 'Bob', role: 'Editor' },
        { name: 'Charlie', role: 'Viewer' }
      ],
      structure: {
        name: 'root',
        folders: [
          {
            name: 'Design System',
            folders: [],
            files: [
              {
                name: 'readme.md',
                date: '2025-03-01',
                addedBy: 'Alice',
                status: 'Draft'
              }
            ]
          },
          {
            name: 'Assets',
            folders: [
              {
                name: 'Icons',
                folders: [],
                files: [
                  {
                    name: 'logo.svg',
                    date: '2025-03-02',
                    addedBy: 'Charlie',
                    status: 'Active'
                  }
                ]
              }
            ],
            files: [
              {
                name: 'logo.png',
                date: '2025-03-01',
                addedBy: 'Bob',
                status: 'Archived'
              }
            ]
          }
        ],
        files: [
          {
            name: 'plan.pdf',
            date: '2025-03-25',
            addedBy: 'Alice',
            status: 'Active'
          },
          {
            name: 'notes.txt',
            date: '2025-03-26',
            addedBy: 'Bob',
            status: 'Draft'
          },
          {
            name: 'readme.md',
            date: '2025-03-27',
            addedBy: 'Charlie',
            status: 'Draft'
          },
          {
            name: 'logo.svg',
            date: '2025-03-28',
            addedBy: 'Alice',
            status: 'Active'
          },
          {
            name: 'logo.png',
            date: '2025-03-29',
            addedBy: 'Bob',
            status: 'Archived'
          },
          {
            name: 'notes.txt',
            date: '2025-03-30',
            addedBy: 'Charlie',
            status: 'Draft'
          },
          {
            name: 'readme.md',
            date: '2025-03-31',
            addedBy: 'Alice',
            status: 'Draft'
          },
          {
            name: 'logo.svg',
            date: '2025-04-01',
            addedBy: 'Bob',
            status: 'Active'
          },
          {
            name: 'logo.png',
            date: '2025-04-02',
            addedBy: 'Charlie',
            status: 'Archived'
          },
          {
            name: 'notes.txt',
            date: '2025-04-03',
            addedBy: 'Alice',
            status: 'Draft'
          }
        ]
      }
    };

    this.updateView();
  }

  updateView(): void {
    let folder = this.workspace.structure;
    for (const segment of this.currentPath) {
      const found = folder.folders.find((f: any) => f.name === segment);
      if (!found) break;
      folder = found;
    }
    this.currentFolder = folder;
  }

  navigateToFolder(folderName: string): void {
    this.currentPath.push(folderName);
    this.updateView();
  }

  navigateToBreadcrumb(index: number): void {
    if (index < 0) {
      this.currentPath = [];
    } else {
      this.currentPath = this.currentPath.slice(0, index + 1);
    }
    this.updateView();
  }

  get breadcrumbPath(): string[] {
    return ['Root', ...this.currentPath];
  }

  viewFile(file: any): void {
    console.log('Viewing file:', file.name);
    // You can later open a modal or preview
  }

  deleteFile(file: any): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete "${file.name}"?`,
      header: 'Confirm Deletion',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Delete',
      rejectLabel: 'Cancel',
      acceptButtonStyleClass: 'p-button-danger',
      rejectButtonStyleClass: 'p-button-text',
      accept: () => {
        this.currentFolder.files = this.currentFolder.files.filter((f: any) => f.name !== file.name);
      }
    });
  }

  onFileSelected(event: any): void {
    const file: File = event.target.files[0];
    if (!file) return;

    const newFile = {
      name: file.name,
      date: new Date().toISOString().split('T')[0],
      addedBy: 'You',
      status: 'Active'
    };

    this.currentFolder.files.push(newFile);
  }


  showNewFolderDialog = false;
  newFolderName = '';

  createFolder(): void {
    if (!this.newFolderName.trim()) return;

    const newFolder = {
      name: this.newFolderName.trim(),
      folders: [],
      files: []
    };

    this.currentFolder.folders.push(newFolder);
    this.newFolderName = '';
    this.showNewFolderDialog = false;
  }

  cancelFolderCreation(): void {
    this.newFolderName = '';
    this.showNewFolderDialog = false;
  }

  deleteFolder(folder: any): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete the folder "${folder.name}" and all its contents?`,
      header: 'Delete Folder',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Delete',
      rejectLabel: 'Cancel',
      acceptButtonStyleClass: 'p-button-danger',
      rejectButtonStyleClass: 'p-button-text',
      accept: () => {
        this.currentFolder.folders = this.currentFolder.folders.filter((f: any) => f !== folder);
      }
    });
  }

}
