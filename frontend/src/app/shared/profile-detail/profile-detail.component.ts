import { Component, Input, OnInit } from '@angular/core';
import { UserService, User, UserUpdateRequest } from '../../services/user.service';
import { MessageService } from 'primeng/api';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {FileService} from "../../services/file.service";

@Component({
  selector: 'app-profile-detail',
  templateUrl: './profile-detail.component.html',
  styleUrl: './profile-detail.component.scss'
})
export class ProfileDetailComponent implements OnInit {
  @Input() viewOnly = false; // if true, edit buttons are hidden
  @Input() userId?: number;  // if provided, we fetch a specific user

  user: User | null = null;
  editMode = false;

  constructor(
    private userService: UserService,
    private fileService: FileService,
    private messageService: MessageService,
    private route: ActivatedRoute
  ) {}

  async ngOnInit(): Promise<void> {
    const id = this.userId ?? localStorage.getItem('user_id');

    if (!id || isNaN(Number(id))) {
      console.error('❌ Invalid userId provided');
      return;
    }

    try {
      const result = await firstValueFrom(this.userService.getUser(Number(id)));
      if (result.success && result.data?.user) {
        this.user = result.data.user;

        // if it's the current user's profile, cache it
        if (!this.userId && !this.viewOnly) {
          this.userService.setActualLoggedUser(result.data.user);
        }
      } else {
        console.warn('⚠️ User not found');
      }
    } catch (err) {
      console.error('❌ Failed to load user:', err);
    }
  }

  saveChanges(): void {
    if (!this.user) return;

    const updateRequest: UserUpdateRequest = {
      name: this.user.name,
      description: this.user.description
    };

    this.userService.updateUser(updateRequest).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          const updatedUser: User = { ...this.user!, ...res.data };
          this.user = updatedUser;
          this.userService.setActualLoggedUser(updatedUser);

          this.messageService.add({
            severity: 'success',
            summary: 'Profile updated',
            detail: 'Your profile information has been saved.',
            life: 3000
          });

          this.editMode = false;
        } else {
          this.messageService.add({
            severity: 'error',
            summary: 'Update failed',
            detail: res.error?.message ?? 'Something went wrong.'
          });
        }
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to update your profile. Please try again later.'
        });
      }
    });
  }

  get profileImageUrl(): string {
    return this.userService.getProfileImageUrl(this.user?.id ?? 0);
  }

  onProfileImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input?.files?.[0];

    if (!file) return;

    if (!file.type.startsWith('image/')) {
      this.messageService.add({
        severity: 'error',
        summary: 'Invalid file',
        detail: 'Please upload an image file.'
      });
      return;
    }

    this.fileService.uploadProfileImage(file).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          if (this.user) {
            this.user.profileImageUrl = res.data;
          }
          this.messageService.add({
            severity: 'success',
            summary: 'Profile Image Updated',
            detail: 'Your profile image was updated successfully.'
          });

          // Refresh page
          window.location.reload();
        } else {
          this.messageService.add({
            severity: 'error',
            summary: 'Upload failed',
            detail: res.error?.message ?? 'Something went wrong.'
          });
        }
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Upload error',
          detail: 'Failed to upload profile image.'
        });
      }
    });
  }

  cancelChanges(): void {
    if (!this.user?.id) return;

    this.userService.getUser(this.user.id).subscribe({
      next: (res) => {
        if (res.success && res.data?.user) {
          this.user = res.data.user;
          this.editMode = false;
        }
      }
    });
  }

  onImageError(event: Event): void {
    const imgElement = event.target as HTMLImageElement;
    imgElement.src = '/assets/img/default-profile.jpg';
  }
}
