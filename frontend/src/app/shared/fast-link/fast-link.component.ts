import { Component, Input } from '@angular/core';
import { MessageService } from 'primeng/api';
import { FileUpload } from 'primeng/fileupload';

@Component({
  selector: 'app-fast-link',
  templateUrl: './fast-link.component.html',
  styleUrl: './fast-link.component.scss'
})
export class FastLinkComponent {

  @Input() buttonText: string = 'fast link'; // default text

  constructor(private messageService: MessageService) {

  }

  // Dialog
  showCreateDialog = false;
  newWorkspaceName = '';
  expirationInHours = '';

  selectedFile: File | null = null;
  selectedFileName: string = '';

  onFileSelected(event: any, fileUpload: FileUpload) {
    const file = event.files?.[0];
    if (file) {
      this.selectedFile = file;
      this.selectedFileName = file.name;

      // ✅ Clear the internal state so the same file can be reselected
      fileUpload.clear();
    }
  }

  openCreateWorkspace() {
    this.showCreateDialog = true;
  }

  closeDialog() {
    this.showCreateDialog = false;
    this.resetForm();
  }

  createWorkspace() {
    console.log('Creating workspace:', this.newWorkspaceName, this.expirationInHours);
    this.showCreateDialog = false;

    // Show success message
    this.messageService.add({
      severity: 'info',
      summary: 'FastLink Created',
      detail: 'Your new FastLink ' + this.newWorkspaceName + ' has been successfully created!',
      life: 3000, // optional: auto close duration in ms
    });

    this.resetForm();
  }

  private resetForm() {
    this.newWorkspaceName = '';
    this.expirationInHours = '';
    this.selectedFile = null;
  }
}
