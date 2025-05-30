import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import { AuthService } from "../../services/auth.service";

@Component({
  selector: 'app-reset-password',
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss'
})
export class ResetPasswordComponent {
  form!: FormGroup;
  errorMessage: string | null = null;
  successMessage: string | null = null;
  token: string = '';
  welcomeText: string = '';
  resetSuccess: boolean = false;

  welcomeTexts = [
    'Let’s patch that leaky brain.',
    'Pick a new password and get back in.',
    'New password, fresh start!',
  ];

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit() {
    this.token = this.route.snapshot.paramMap.get('token') ?? '';
    console.log('Token:', this.token);

    this.form = this.fb.group({
      newPassword: ['', [Validators.required, Validators.minLength(6)]]
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
    if (this.form.invalid || !this.token) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage = null;
    this.successMessage = null;

    this.authService.resetPassword(this.token, this.form.value.newPassword).subscribe({
      next: (res) => {
        if (res.success) {
          this.resetSuccess = true;
          setTimeout(() => this.router.navigate(['/login']), 2000);
        } else {
          this.errorMessage = res.error?.message || 'Failed to reset password.';
        }
      },
      error: (err) => {
        this.errorMessage = err?.error?.message || 'Something went wrong.';
      }
    });
  }
}
