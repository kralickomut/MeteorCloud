import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { WorkspaceFile } from '../../models/WorkspaceFile';

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

    const files: WorkspaceFile[] = [
      {
        name: 'plan.pdf',
        addedBy: 'Alice',
        date: '2025-03-25',
        createdAt: new Date('2025-03-25T10:00:00'),
        editedAt: new Date('2025-03-25T12:00:00'),
        type: 'pdf',
        size: '2MB',
        resolution: '582 × 530',
        colorSpace: 'RGB',
        status: 'Active'
      },
      {
        name: 'notes.txt',
        addedBy: 'Bob',
        date: '2025-03-26',
        createdAt: new Date('2025-03-26T10:00:00'),
        editedAt: new Date('2025-03-26T12:00:00'),
        type: 'txt',
        size: '500KB',
        resolution: '582 × 530',
        colorSpace: 'RGB',
        status: 'Draft'
      },
      {
        name: 'readme.md',
        addedBy: 'Charlie',
        date: '2025-03-27',
        createdAt: new Date('2025-03-27T10:00:00'),
        editedAt: new Date('2025-03-27T12:00:00'),
        type: 'md',
        size: '800KB',
        resolution: '582 × 530',
        colorSpace: 'RGB',
        status: 'Draft'
      },
      {
        name: 'logo.svg',
        addedBy: 'Alice',
        date: '2025-03-28',
        createdAt: new Date('2025-03-28T10:00:00'),
        editedAt: new Date('2025-03-28T12:00:00'),
        type: 'svg',
        size: '1MB',
        resolution: '582 × 530',
        colorSpace: 'RGB',
        status: 'Active'
      },
      {
        name: 'logo.png',
        addedBy: 'Bob',
        date: '2025-03-29',
        createdAt: new Date('2025-03-29T10:00:00'),
        editedAt: new Date('2025-03-29T12:00:00'),
        type: 'png',
        size: '1MB',
        resolution: '582 × 530',
        colorSpace: 'RGB',
        status: 'Archived'
      }
    ];

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
            files: files
          },
          {
            name: 'Assets',
            folders: [
              {
                name: 'Icons',
                folders: [],
                files: files
              }
            ],
            files: files
          }
        ],
        files: files
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

    const now = new Date();

    const newFile: WorkspaceFile = {
      name: file.name,
      addedBy: 'You',
      date: now.toISOString().split('T')[0],
      createdAt: now,
      editedAt: now,
      type: file.name.split('.').pop()?.toLowerCase() || 'unknown',
      size: `${(file.size / 1024 / 1024).toFixed(1)}MB`,
      resolution: 'Unknown',
      colorSpace: 'Unknown',
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


  isDragOver = false;

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = false;

    if (event.dataTransfer?.files) {
      Array.from(event.dataTransfer.files).forEach(file => {
        const now = new Date();
        const newFile: WorkspaceFile = {
          name: file.name,
          addedBy: 'You',
          date: now.toISOString().split('T')[0],
          createdAt: now,
          editedAt: now,
          type: file.name.split('.').pop()?.toLowerCase() || 'unknown',
          size: `${(file.size / 1024 / 1024).toFixed(1)}MB`,
          resolution: 'Unknown',
          colorSpace: 'Unknown',
          status: 'Active'
        };

        this.currentFolder.files.push(newFile);
      });
    }
  }


  selectedFile: any = null;

  viewFile(file: any): void {
    this.selectedFile = file;
  }

  closeFileDetails(): void {
    this.selectedFile = null;
  }

}
