import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { complexPasswordValidator } from '../../validators/password-validator';
import { specificEmailValidator } from '../../validators/email-validator';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent implements OnInit {
  form!: FormGroup;
  errorMessage: string | null = null;

  constructor(private fb: FormBuilder, private authService: AuthService, private router: Router) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
      email: ['', [Validators.required, specificEmailValidator, Validators.maxLength(50)]],
      password: ['', [Validators.required, complexPasswordValidator]]
    });
  }

  onRegister() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.authService.register(this.form.value).subscribe({
      next: res => {
        if (res.success) {
          this.router.navigate(['/verify'], {
            queryParams: { email: this.form.value.email }
          });
        } else {
          this.errorMessage = res.error?.message ?? 'Invalid code.';
        }
      },
      error: err => {
        this.errorMessage = err.error?.message || 'Something went wrong.';
      }
    });
  }

  get f() {
    return this.form.controls;
  }



}
