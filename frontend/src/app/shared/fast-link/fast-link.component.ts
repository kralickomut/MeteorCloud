import { Component, Input } from '@angular/core';
import { MessageService } from 'primeng/api';
import { FileUpload } from 'primeng/fileupload';
import {HttpClient, HttpEventType} from "@angular/common/http";
import {AuthService} from "../../services/auth.service";
import {FileService} from "../../services/file.service";
import {LinkService} from "../../services/link.service";

@Component({
  selector: 'app-fast-link',
  templateUrl: './fast-link.component.html',
  styleUrl: './fast-link.component.scss'
})
export class FastLinkComponent {
  @Input() buttonText: string = 'create fast link'; // default button text

  constructor(
    private messageService: MessageService,
    private http: HttpClient,
    private linkService: LinkService
  ) {}

  // Dialog state
  showCreateDialog = false;
  linkName = '';
  expirationHours = '';
  uploading = false;

  selectedFile: File | null = null;
  selectedFileName: string = '';

  onFileSelected(event: any, fileUpload: FileUpload) {
    const file = event.files?.[0];
    if (file) {
      this.selectedFile = file;
      this.selectedFileName = file.name;

      // Clear internal state so the same file can be reselected
      fileUpload.clear();
    }
  }

  openCreateDialog() {
    this.showCreateDialog = true;
  }

  closeDialog() {
    this.showCreateDialog = false;
    this.resetForm();
  }

  createFastLink() {
    const hours = Number(this.expirationHours);
    const name = this.linkName; // store name before reset

    if (!this.selectedFile || !name || !hours || hours < 1 || hours > 24) {
      this.messageService.add({
        severity: 'error',
        summary: 'Validation failed',
        detail: 'All fields are required and expiration must be between 1 and 24 hours.'
      });
      return;
    }

    this.uploading = true;

    this.linkService.uploadFastLink(this.selectedFile, name, hours).subscribe({
      next: (event) => {
        if (event.type === HttpEventType.Response) {
          this.messageService.add({
            severity: 'success',
            summary: 'Fast Link Created',
            detail: `Fast Link "${name}" was created successfully!`
          });
          this.linkService.notifyFastLinkCreated();
          this.closeDialog();
        }
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Upload failed',
          detail: 'There was a problem uploading the file.'
        });
      },
      complete: () => {
        this.uploading = false;
      }
    });
  }

  private resetForm() {
    this.linkName = '';
    this.expirationHours = '';
    this.selectedFile = null;
    this.selectedFileName = '';
  }
}
