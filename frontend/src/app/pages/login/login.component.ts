import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import {Router} from "@angular/router";
import {UserService} from "../../services/user.service";

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

  constructor(private fb: FormBuilder, private authService: AuthService, private router: Router, private userService: UserService) {}

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


    this.authService.loginAndStore(this.form.value).subscribe({
      next: (success) => {
        if (success) {
          const userId = localStorage.getItem('user_id');
          if (userId) {
            this.userService.getUser(Number(userId)).subscribe({
              next: (res) => {
                if (res.success && res.data?.user) {
                  this.userService.setActualLoggedUser(res.data.user);
                  this.router.navigate(['/']);
                } else {
                  this.errorMessage = 'Failed to fetch user info.';
                }
              },
              error: (err) => {
                this.errorMessage = 'Something went wrong while fetching user data.';
                console.error('Error fetching user data:', err);
              }
            });
          } else {
            this.errorMessage = 'No user ID found after login.';
          }
        } else {
          this.errorMessage = 'Invalid email or password.';
        }
      },
      error: (err) => {
        this.errorMessage = err?.message || 'Something went wrong.';
      }
    });
  }
}
