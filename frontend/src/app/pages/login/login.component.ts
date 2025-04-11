import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import {Router} from "@angular/router";

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
  form!: FormGroup;
  errorMessage: string | null = null;
  welcomeText: string = '';

  welcomeTexts = [
    'Ready to dive back in and get things done?',
    'Pick up right where you left off — your workspace is waiting!',
    'Good to see you again!',
    'Your files, your team, your flow — all in one place.'
  ];

  constructor(private fb: FormBuilder, private authService: AuthService, private router: Router) {}

  ngOnInit() {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
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

  onLogin() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage = null;

    this.authService.login(this.form.value).subscribe({
      next: (res) => {
        if (res.success) {
          console.log('Login success:', res);
          // Store the token in local storage
          localStorage.setItem('auth_token', res.data?.token || '');
          this.router.navigate(['/home']);
        } else {
          this.errorMessage = res.error?.message ?? 'Login failed.';
        }
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Something went wrong.';
      }
    });
  }
}
