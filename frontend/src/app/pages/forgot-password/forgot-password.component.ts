import { Component } from '@angular/core';
import {FormBuilder, FormGroup, Validators} from "@angular/forms";
import {AuthService} from "../../services/auth.service";
import {Router} from "@angular/router";
import {UserService} from "../../services/user.service";

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent {
  form!: FormGroup;
  errorMessage: string | null = null;
  successMessage: string | null = null;
  welcomeText: string = '';

  welcomeTexts = [
    'Ready to dive back in and get things done?',
    'We all forget sometimes – let’s get you back in.',
    'Memory issues? Let’s reset that password.',
  ];

  constructor(private fb: FormBuilder, private authService: AuthService) {}

  ngOnInit() {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });

    this.setWelcomeText();
  }

  get f() {
    return this.form.controls;
  }

  setWelcomeText() {
    const index = Math.floor(Math.random() * this.welcomeTexts.length);
    this.welcomeText = this.welcomeTexts[index];
  }

  onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage = null;
    this.successMessage = null;

    this.authService.requirePasswordReset(this.form.value.email).subscribe({
      next: (res) => {
        if (res.success) {
          this.successMessage = 'Password reset email sent. Please check your inbox.';
          this.form.reset();
        } else {
          this.errorMessage = res.error?.message || 'Failed to send reset email.';
        }
      },
      error: (err) => {
        this.errorMessage = err?.error?.message || 'Something went wrong.';
      }
    });
  }
}
