import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import {ConfirmationService, MessageService} from 'primeng/api';
import { WorkspaceFile } from '../../models/WorkspaceFile';
import {MetadataService} from "../../services/metadata.service";
import {UserService} from "../../services/user.service";
import {FileService} from "../../services/file.service";
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

@Component({
  selector: 'app-workspace-table',
  templateUrl: './workspace-table.component.html',
  styleUrl: './workspace-table.component.scss'
})
export class WorkspaceTableComponent implements OnInit {

  workspaceId: string = '';
  workspace: any = null;
  currentFolder: any = null;
  currentPath: string[] = [];
  userId: number = 0;
  uploadProgress: number = 0;
  uploading: boolean = false;

  viewUrl: SafeResourceUrl | null = null;
  viewDialogVisible = false;
  viewDialogUrl: SafeResourceUrl | null = null;
  viewDialogTitle = '';

  constructor(
    private route: ActivatedRoute,
    private confirmationService: ConfirmationService,
    private workspaceApi: MetadataService,
    private userService: UserService,
    private fileService: FileService,
    private sanitizer: DomSanitizer,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    this.workspaceId = this.route.snapshot.paramMap.get('id') ?? '';

    this.userService.user$.subscribe((user) => {
      if (user) {
        this.userId = user.id;
      }
    });

    this.fetchWorkspaceTree();
  }

  fetchWorkspaceTree(): void {
    this.workspaceApi.getTreeByWorkspaceId(Number(this.workspaceId)).subscribe({
      next: (res) => {
        this.workspace = {
          id: this.workspaceId,
          structure: res.data
        };
        this.currentPath = [];
        this.updateView();
      },
      error: (err) => {
        console.error('Failed to fetch workspace tree:', err);
      }
    });
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
        let parts: string[] = [];

        parts.push(this.workspaceId);

        if (this.currentPath.length > 0) {
          parts = parts.concat(this.currentPath);
        }

        parts.push(file.id); // <-- Use file.id (not file.name!)

        const fullPath = parts.join('/');

        this.fileService.deleteFile(fullPath).subscribe({
          next: (res) => {
            if (res.success) {
              this.currentFolder.files = this.currentFolder.files.filter((f: any) => f.id !== file.id);
              console.log('✅ File deleted');
            } else {
              console.error('❌ Failed to delete file', res.error?.message);
            }
          },
          error: (err) => {
            console.error('❌ Failed to delete file', err);
          }
        });
      }
    });
  }

  onFileSelected(event: any): void {
    const file: File = event.target.files[0];
    if (!file) return;

    const fullPath = this.buildFullPath('');
    this.uploading = true;
    this.uploadProgress = 0;

    this.fileService.uploadFile(file, Number(this.workspaceId), fullPath).subscribe({
      next: (event) => {
        if (event.type === 1 && event.total) {
          // Progress event
          this.uploadProgress = Math.round((100 * event.loaded) / event.total);
        } else if (event.type === 4) {
          // Upload completed
          this.uploadProgress = 100;
          this.messageService.add({
            severity: 'success',
            summary: 'Upload Completed',
            detail: `${file.name} was uploaded successfully!`,
            life: 3000
          });

          setTimeout(() => {
            this.uploading = false;
            this.uploadProgress = 0;
            this.fetchWorkspaceTree();
          }, 500); // Small delay so user sees 100%
        }
      },
      error: (err) => {
        console.error('❌ Upload failed', err);
        this.messageService.add({
          severity: 'error',
          summary: 'Upload Failed',
          detail: `Failed to upload ${file.name}.`,
          life: 4000
        });
        this.uploading = false;
        this.uploadProgress = 0;
      }
    });
  }

  private buildFullPath(fileOrFolderName: string = ''): string {
    let parts: string[] = [];

    // Always start with workspaceId
    parts.push(this.workspaceId);

    // Add currentPath if user is inside some folder
    if (this.currentPath.length > 0) {
      parts = parts.concat(this.currentPath);
    }

    // If fileOrFolderName is provided (file or folder name), add it
    if (fileOrFolderName) {
      parts.push(fileOrFolderName);
    }

    // Join everything with '/'
    return parts.join('/');
  }


  showNewFolderDialog = false;
  newFolderName = '';

  createFolder(): void {
    if (!this.newFolderName.trim()) return;

    let fullPath = this.workspaceId; // Always start with workspaceId

    if (this.currentPath.length > 0) {
      fullPath += '/' + this.currentPath.join('/');
    }

    const payload = {
      workspaceId: Number(this.workspaceId),
      name: this.newFolderName.trim(),
      path: fullPath,
      uploadedBy: this.userId
    };

    this.workspaceApi.createFolder(payload).subscribe({
      next: () => {
        this.newFolderName = '';
        this.showNewFolderDialog = false;
        this.fetchWorkspaceTree(); // Reload whole tree
      },
      error: (err) => {
        console.error('Failed to create folder:', err);
      }
    });
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
        const fullPath = this.buildFullPath(folder.name); // Build with workspaceId
        this.fileService.deleteFolder(fullPath).subscribe({
          next: (res) => {
            if (res.success) {
              this.currentFolder.folders = this.currentFolder.folders.filter((f: any) => f.name !== folder.name);
              console.log('✅ Folder deleted');
            } else {
              console.error('❌ Failed to delete folder', res.error?.message);
            }
          },
          error: (err) => {
            console.error('❌ Failed to delete folder', err);
          }
        });
      }
    });
  }

  downloadFile(file: any): void {
    const fullPath = this.buildFullPath(file.id);

    this.fileService.downloadFile(fullPath).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = file.name;
        a.click();
        window.URL.revokeObjectURL(url);
        console.log('✅ File download started');
      },
      error: (err) => {
        console.error('❌ Failed to download file', err);
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

  formatSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    const size = parseFloat((bytes / Math.pow(k, i)).toFixed(2));
    return `${size} ${sizes[i]}`;
  }

  isFileLoading = false; // 🔥 NEW: spinner control

  openFileDialog(file: any): void {
    const fullPath = this.buildFullPath(file.id);
    console.log('Full path for view:', fullPath);
    const fileUrl = `${this.fileService.apiUrl}/view/${encodeURIComponent(fullPath)}`;

    this.viewDialogTitle = file.name;
    this.isFileLoading = true;

    if (file.contentType.includes('pdf') || file.contentType.startsWith('image/')) {
      this.viewDialogUrl = this.sanitizer.bypassSecurityTrustResourceUrl(fileUrl);

      // 🚀 Important: open dialog in next microtask!
      setTimeout(() => {
        this.viewDialogVisible = true;
      }, 0);

    } else if (
      file.contentType.includes('word') ||
      file.contentType.includes('excel') ||
      file.contentType.includes('presentation')
    ) {
      const officeViewerUrl = `https://view.officeapps.live.com/op/embed.aspx?src=${encodeURIComponent(fileUrl)}`;
      this.viewDialogUrl = this.sanitizer.bypassSecurityTrustResourceUrl(officeViewerUrl);

      setTimeout(() => {
        this.viewDialogVisible = true;
      }, 0);
    } else {
      this.downloadFile(file);
    }
  }

  onIframeLoad(): void {
    console.log('✅ File loaded successfully in iframe');
    this.isFileLoading = false;
  }

  onIframeError(): void {
    console.error('❌ Iframe failed to load the file, fallback to download');
    this.isFileLoading = false;
    if (this.selectedFile) {
      this.downloadFile(this.selectedFile);
    }
  }

  getDisplayTypeColorAndIcon(contentType: string): { label: string, color: string, icon: string } {
    if (!contentType) return { label: 'Unknown', color: 'gray', icon: 'pi pi-file' };

    if (contentType.includes('pdf')) return { label: 'PDF', color: 'red', icon: 'pi pi-file-pdf' };
    if (contentType.includes('presentation')) return { label: 'PPTX', color: 'orange', icon: 'pi pi-file-ppt' };
    if (contentType.includes('spreadsheet') || contentType.includes('excel')) return { label: 'XLSX', color: 'green', icon: 'pi pi-file-excel' };
    if (contentType.includes('word')) return { label: 'DOCX', color: 'blue', icon: 'pi pi-file-word' };
    if (contentType.startsWith('image/')) return { label: 'Image', color: 'purple', icon: 'pi pi-image' };
    if (contentType.startsWith('video/')) return { label: 'Video', color: 'teal', icon: 'pi pi-video' };
    if (contentType.startsWith('audio/')) return { label: 'Audio', color: 'pink', icon: 'pi pi-volume-up' };

    return { label: 'File', color: 'gray', icon: 'pi pi-file' };
  }

}
