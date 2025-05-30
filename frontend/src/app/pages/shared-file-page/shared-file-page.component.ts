import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import {GetLinkByTokenResponse, LinkService} from '../../services/link.service';
import { ApiResult } from '../../models/api-result';
import {formatDate} from "@angular/common";
import {DomSanitizer, SafeResourceUrl} from "@angular/platform-browser";
import {FileService} from "../../services/file.service";
import {take} from "rxjs";

@Component({
  selector: 'app-shared-file-page',
  templateUrl: './shared-file-page.component.html',
  styleUrls: ['./shared-file-page.component.scss']
})
export class SharedFilePageComponent implements OnInit {
  token = '';
  linkData: GetLinkByTokenResponse | null = null;
  isLoading = true;
  notFound = false;

  private alreadyFetched = false;

  formatOptions: Intl.DateTimeFormatOptions = {
    dateStyle: 'short',
    timeStyle: 'short'
  };

  constructor(
    private route: ActivatedRoute,
    private linkService: LinkService,
    private fileService: FileService
  ) {}

  private linkLoaded = false;

  ngOnInit(): void {
    this.linkData = this.route.snapshot.data['linkData'];

    if (!this.linkData) {
      this.notFound = true;
    }

    this.isLoading = false;
  }

  get readableSize(): string {
    if (!this.linkData) return '';
    const size = this.linkData.fileSize;
    return size > 1024 * 1024
      ? `${(size / (1024 * 1024)).toFixed(2)} MB`
      : `${(size / 1024).toFixed(2)} KB`;
  }

  get expiresAt(): string {
    return this.linkData
      ? formatDate(this.linkData.expiresAt, 'short', 'en-US')
      : '';
  }

  downloadFile(): void {
    if (!this.linkData) return;

    const filePath = `fast-link-files/${this.linkData.ownerId}/${this.linkData.fileId}`;

    this.fileService.downloadFile(filePath).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = this.linkData!.fileName;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('❌ Failed to download file', err);
      }
    });
  }
}
