import { Component } from '@angular/core';
import { MessageService } from 'primeng/api';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-change-password',
  templateUrl: './change-password.component.html',
  styleUrls: ['./change-password.component.scss']
})
export class ChangePasswordComponent {
  showDialog = false;
  oldPassword = '';
  newPassword = '';
  loading = false;

  constructor(
    private authService: AuthService,
    private messageService: MessageService
  ) {}

  openDialog() {
    this.resetForm();
    this.showDialog = true;
  }

  closeDialog() {
    this.resetForm();
    this.showDialog = false;
    this.oldPassword = '';
    this.newPassword = '';
  }

  isValidPassword(password: string): boolean {
    return password.length >= 6 && /[A-Z]/.test(password) && /\d/.test(password);
  }

  changePassword() {
    if (!this.isValidPassword(this.newPassword)) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Invalid Password',
        detail: 'New password must be at least 6 characters long, include one uppercase letter and one digit.'
      });
      return;
    }

    this.loading = true;

    this.authService.changePassword(this.oldPassword, this.newPassword).subscribe({
      next: (res) => {
        if (res.success) {
          this.messageService.add({
            severity: 'success',
            summary: 'Password Changed',
            detail: 'Your password was updated successfully.'
          });
          this.closeDialog();
        } else {
          this.messageService.add({
            severity: 'error',
            summary: 'Change Failed',
            detail: res.error?.message || 'Password update failed.'
          });
        }
      },
      error: (err) => {
        const serverMessage =
          err?.error?.message || err?.error?.error?.message || 'Something went wrong.';

        this.messageService.add({
          severity: 'error',
          summary: 'Request Failed',
          detail: serverMessage
        });
        this.loading = false;
      },
      complete: () => {
        this.loading = false;
        this.resetForm();
      }
    });
  }

  get canSubmit(): boolean {
    return (
      !this.loading &&
      this.oldPassword.trim().length > 0 &&
      this.newPassword.trim().length > 0 &&
      this.isValidPassword(this.newPassword)
    );
  }

  resetForm() {
    this.oldPassword = '';
    this.newPassword = '';
    this.loading = false;
  }
}
