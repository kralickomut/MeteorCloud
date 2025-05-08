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
  selectedFilePath: string[] = [];
  selectedFile: any = null;
  plainTextContent: string | null = null;
  internalDrag: boolean = false;
  hoveredDropTarget: string | null = null;

  viewDialogVisible = false;
  viewDialogUrl: SafeResourceUrl | null = null;
  viewDialogTitle = '';
  isFileLoading = false;

  constructor(
    private route: ActivatedRoute,
    private confirmationService: ConfirmationService,
    private messageService: MessageService,
    private workspaceApi: MetadataService,
    private userService: UserService,
    private fileService: FileService,
    private sanitizer: DomSanitizer
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
    const previousPath = [...this.currentPath];
    this.workspaceApi.getTreeByWorkspaceId(Number(this.workspaceId)).subscribe({
      next: (res) => {
        this.workspace = {
          id: this.workspaceId,
          structure: res.data
        };
        this.attachPaths(this.workspace.structure, []);
        this.currentPath = previousPath;
        this.updateView();
      },
      error: (err) => {
        console.error('Failed to fetch workspace tree:', err);
      }
    });
  }

  private attachPaths(folder: any, path: string[]): void {
    folder.files = folder.files || [];    // ✅ ensure arrays
    folder.folders = folder.folders || [];

    folder.files.forEach((file: any) => {
      file._path = [...path];
    });
    folder.folders.forEach((sub: any) => {
      this.attachPaths(sub, [...path, sub.name]);
    });
  }

  updateView(): void {
    let folder = this.workspace.structure;

    for (const segment of this.currentPath) {
      const found = folder.folders?.find((f: any) => f.name === segment);
      if (!found) break;
      folder = found;
    }

    // ✅ Defensive fix for potential undefined
    folder.files = folder.files || [];
    folder.folders = folder.folders || [];

    this.currentFolder = folder;
    console.log('Current folder view:', this.currentFolder);
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

  sanitizeFileName(name: string): string {
    return name.normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')  // remove accents
      .replace(/[^\x00-\x7F]/g, '')     // remove non-ASCII
      .replace(/\s+/g, '_');            // replace spaces with underscores
  }

  onFileSelected(event: any): void {
    const originalFile: File = event.target.files[0];
    if (!originalFile) return;

    if (!this.checkFileSize(originalFile)) return;

    const sanitizedFileName = this.sanitizeFileName(originalFile.name);

    const file = new File([originalFile], sanitizedFileName, {
      type: originalFile.type,
      lastModified: originalFile.lastModified,
    });

    const fullPath = this.buildFullPath('');
    this.uploading = true;
    this.uploadProgress = 0;

    this.fileService.uploadFile(file, Number(this.workspaceId), fullPath).subscribe({
      next: (event) => {
        if (event.type === 1 && event.total) {
          this.uploadProgress = Math.round((100 * event.loaded) / event.total);
        } else if (event.type === 4) {
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
          }, 500);
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

  buildFullPath(fileOrFolderName: string = '', pathOverride?: string[]): string {
    const parts: string[] = [this.workspaceId];
    const path = pathOverride !== undefined ? pathOverride : this.currentPath;
    parts.push(...path);
    if (fileOrFolderName) parts.push(fileOrFolderName);
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

  downloadFile(file: any, pathOverride: string[] = []): void {
    const fullPath = this.buildFullPath(file.id, pathOverride);
    this.fileService.downloadFile(fullPath).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = file.name;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('❌ Failed to download file', err);
      }
    });
  }

  onFilesSelected(event: any): void {
    const files: FileList = event.target.files;
    if (!files || files.length === 0) return;

    const fullPath = this.buildFullPath('');
    const uploadQueue: File[] = [];

    for (let i = 0; i < files.length; i++) {
      const original = files[i];
      if (!this.checkFileSize(original)) continue;

      const sanitizedName = this.sanitizeFileName(original.name);
      const sanitized = new File([original], sanitizedName, {
        type: original.type,
        lastModified: original.lastModified
      });

      uploadQueue.push(sanitized);
    }

    uploadQueue.forEach(file => this.uploadFile(file, fullPath));
  }


  isDragOver = false;

  onDragOver(event: DragEvent): void {
    event.preventDefault();

    if (this.internalDrag) return;

    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = false;
  }

  uploadFile(file: File, fullPath: string): void {
    this.uploading = true;
    this.uploadProgress = 0;

    this.fileService.uploadFile(file, Number(this.workspaceId), fullPath).subscribe({
      next: (event) => {
        if (event.type === 1 && event.total) {
          this.uploadProgress = Math.round((100 * event.loaded) / event.total);
        } else if (event.type === 4) {
          this.messageService.add({
            severity: 'success',
            summary: 'Uploaded',
            detail: `${file.name} uploaded successfully!`
          });
          setTimeout(() => this.fetchWorkspaceTree(), 300);
        }
      },
      error: (err) => {
        console.error('❌ Upload failed', err);
        this.messageService.add({
          severity: 'error',
          summary: 'Upload Failed',
          detail: `Failed to upload ${file.name}`
        });
      },
      complete: () => {
        this.uploading = false;
        this.uploadProgress = 0;
      }
    });
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();

    if (this.internalDrag) {
      this.internalDrag = false;
      return;
    }

    this.isDragOver = false;

    if (this.uploading) return;

    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      const file = event.dataTransfer.files[0];
      if (!this.checkFileSize(file)) return;

      const fullPath = this.buildFullPath('');
      this.uploading = true;
      this.uploadProgress = 0;

      this.fileService.uploadFile(file, Number(this.workspaceId), fullPath).subscribe({
        next: (event) => {
          if (event.type === 1 && event.total) {
            this.uploadProgress = Math.round((100 * event.loaded) / event.total);
          } else if (event.type === 4) {
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
            }, 500);
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
  }



  viewFile(file: any): void {
    this.selectedFile = file;
    this.selectedFilePath = [...(file._path ?? [])];
  }

  closeFileDetails(): void {
    this.selectedFile = null;
    this.plainTextContent = null;
  }

  formatSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    const size = parseFloat((bytes / Math.pow(k, i)).toFixed(2));
    return `${size} ${sizes[i]}`;
  }


  openFileDialog(file: any = this.selectedFile, path: string[] = this.selectedFilePath): void {
    if (!file) return;

    const fullPath = this.buildFullPath(file.id, path);
    const fileUrl = `${this.fileService.apiUrl}/view/${fullPath}`;
    this.viewDialogTitle = file.name;
    this.isFileLoading = true;

    if (file.contentType.startsWith('video/')) {
      this.viewDialogUrl = this.sanitizer.bypassSecurityTrustResourceUrl(fileUrl);
      this.viewDialogVisible = true;
      this.isFileLoading = false;
    } else if (file.contentType.startsWith('audio/')) {
      this.viewDialogUrl = this.sanitizer.bypassSecurityTrustResourceUrl(fileUrl);
      this.viewDialogVisible = true;
      this.isFileLoading = false;
    } else if (file.contentType.startsWith('text/') || file.contentType.includes('json') || file.contentType.includes('xml')) {
      this.fileService.downloadFile(fullPath).subscribe({
        next: (blob) => {
          blob.text().then((text) => {
            this.plainTextContent = text;
            this.viewDialogVisible = true;
            this.isFileLoading = false;
          });
        },
        error: (err) => {
          console.error('❌ Failed to load text file', err);
          this.isFileLoading = false;
        }
      });
    } else if (file.contentType.includes('pdf') || file.contentType.startsWith('image/')) {
      this.viewDialogUrl = this.sanitizer.bypassSecurityTrustResourceUrl(fileUrl);
      this.viewDialogVisible = true;
      this.isFileLoading = false;
    } else if (
      file.contentType.includes('word') ||
      file.contentType.includes('excel') ||
      file.contentType.includes('presentation')
    ) {
      const officeViewerUrl = `https://view.officeapps.live.com/op/embed.aspx?src=${encodeURIComponent(fileUrl)}`;
      this.viewDialogUrl = this.sanitizer.bypassSecurityTrustResourceUrl(officeViewerUrl);
      this.viewDialogVisible = true;
      this.isFileLoading = false;
    } else {
      this.downloadFile(file, path);
    }
  }



  onIframeLoad(): void {
    this.isFileLoading = false;
  }

  onIframeError(): void {
    this.isFileLoading = false;
    if (this.selectedFile) {
      this.downloadFile(this.selectedFile, this.selectedFilePath);
    }
  }

  getDisplayTypeColorAndIcon(contentType: string): { label: string, color: string, icon: string } {
    if (!contentType) return { label: 'Unknown', color: 'gray', icon: 'pi pi-file' };

    // Office
    if (contentType.includes('pdf')) return { label: 'PDF', color: 'red', icon: 'pi pi-file-pdf' };
    if (contentType.includes('presentation') || contentType.includes('powerpoint')) return { label: 'PPTX', color: 'orange', icon: 'pi pi-table' };
    if (contentType.includes('spreadsheet') || contentType.includes('excel') || contentType.includes('csv')) return { label: 'XLSX', color: 'green', icon: 'pi pi-file-excel' };
    if (contentType.includes('word')) return { label: 'DOCX', color: 'blue', icon: 'pi pi-file-word' };
    if (contentType.includes('rtf') || contentType.includes('plain') || contentType.includes('text')) return { label: 'Text', color: 'gray', icon: 'pi pi-align-left' };

    // Code & scripts
    if (contentType.includes('javascript')) return { label: 'JS', color: 'yellow', icon: 'pi pi-code' };
    if (contentType.includes('json')) return { label: 'JSON', color: 'green', icon: 'pi pi-code' };
    if (contentType.includes('html')) return { label: 'HTML', color: 'orange', icon: 'pi pi-code' };
    if (contentType.includes('css')) return { label: 'CSS', color: 'bluegray', icon: 'pi pi-code' };
    if (contentType.includes('python')) return { label: 'Python', color: 'blue', icon: 'pi pi-code' };
    if (contentType.includes('java')) return { label: 'Java', color: 'orange', icon: 'pi pi-code' };
    if (contentType.includes('xml')) return { label: 'XML', color: 'bluegray', icon: 'pi pi-code' };

    // Archives
    if (contentType.includes('zip') || contentType.includes('rar') || contentType.includes('7z') || contentType.includes('compressed')) {
      return { label: 'Archive', color: 'indigo', icon: 'pi pi-folder' };
    }

    // Media
    if (contentType.startsWith('image/')) return { label: 'Image', color: 'purple', icon: 'pi pi-image' };
    if (contentType.startsWith('video/')) return { label: 'Video', color: 'teal', icon: 'pi pi-video' };
    if (contentType.startsWith('audio/')) return { label: 'Audio', color: 'pink', icon: 'pi pi-volume-up' };

    // Default
    return { label: 'File', color: 'gray', icon: 'pi pi-file' };
  }

  checkFileSize(file: File): boolean {
    const maxFileSize = 500 * 1024 * 1024; // 500 MB

    if (file.size > maxFileSize) {
      this.messageService.add({
        severity: 'warn',
        summary: 'File Too Large',
        detail: `Maximum allowed size is 500MB. Selected file is ${(file.size / (1024 * 1024)).toFixed(2)}MB.`,
        life: 5000
      });
      return false;
    }

    return true;
  }


  draggedFile: any = null;

  onFileDragStart(event: DragEvent, file: any): void {
    this.draggedFile = file;
    this.internalDrag = true; // Set internal drag flag
    event.dataTransfer?.setData('text/plain', JSON.stringify(file));
  }

  allowDrop(event: DragEvent, folderName: string): void {
    event.preventDefault();
    this.hoveredDropTarget = folderName;
  }

  onFileDrop(event: DragEvent, targetFolderName: string): void {
    event.preventDefault();

    if (!this.draggedFile) return;

    const sourcePath = this.buildFullPath(this.draggedFile.id, this.draggedFile._path);
    const targetFolder = this.buildFullPath('', [...this.currentPath, targetFolderName]);

    this.fileService.moveFile(sourcePath, targetFolder, Number(this.workspaceId), this.userId).subscribe({
      next: (res) => {
        if (res.success) {
          this.fetchWorkspaceTree();

          setTimeout(() => {
            this.messageService.add({
              severity: 'success',
              summary: 'File Moved',
              detail: `"${this.draggedFile.name}" moved to "${targetFolderName}" successfully.`,
              life: 3000
            });
          }, 300);
        } else {
          this.messageService.add({
            severity: 'error',
            summary: 'Move Failed',
            detail: res.error?.message || 'Unknown error.'
          });
        }

        this.hoveredDropTarget = null; //
      },
      error: (err) => {
        console.error(' Failed to move file', err);
        this.messageService.add({
          severity: 'error',
          summary: 'Move Failed',
          detail: 'An error occurred while moving the file.'
        });
        this.hoveredDropTarget = null; //
      }
    });

    this.draggedFile = null;
  }

}
