import {
  AfterViewInit, ChangeDetectorRef, Component, ElementRef,
  QueryList, ViewChildren
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-verify',
  templateUrl: './verify.component.html',
  styleUrls: ['./verify.component.scss']
})
export class VerifyComponent implements AfterViewInit {
  @ViewChildren('codeBox') codeBoxes!: QueryList<ElementRef<HTMLInputElement>>;

  codeInputs = new Array(6);

  code: string[] = ['', '', '', '', '', ''];
  email!: string;
  errorMessage: string | null = null;

  constructor(private route: ActivatedRoute, private authService: AuthService, private cdr: ChangeDetectorRef, private router: Router) {
    this.route.queryParams.subscribe(params => {
      this.email = params['email'] || '';
    });
  }

  ngAfterViewInit(): void {
    setTimeout(() => {
      this.codeBoxes.first?.nativeElement.focus();
    });
  }

  onInput(event: Event, index: number): void {
    const value = (event.target as HTMLInputElement).value;
    if (!value) return;

    this.code[index] = value[0];
    (event.target as HTMLInputElement).value = value[0];

    if (index < this.codeBoxes.length - 1) {
      this.codeBoxes.toArray()[index + 1].nativeElement.focus();
    }

    this.tryAutoSubmit();
  }

  onKeyDown(event: KeyboardEvent, index: number): void {
    const input = this.codeBoxes.toArray()[index].nativeElement;

    if (event.key === 'Backspace') {
      event.preventDefault();
      if (input.value) {
        input.value = '';
        this.code[index] = '';
      } else if (index > 0) {
        const prevInput = this.codeBoxes.toArray()[index - 1].nativeElement;
        prevInput.focus();
        prevInput.value = '';
        this.code[index - 1] = '';
      }
    }
  }

  onPaste(event: ClipboardEvent): void {
    const pasted = event.clipboardData?.getData('text')?.trim();
    if (pasted && /^[0-9]{6}$/.test(pasted)) {
      event.preventDefault();
      this.code = pasted.split('');
      this.codeBoxes.forEach((box, i) => {
        box.nativeElement.value = this.code[i];
      });
      this.codeBoxes.last.nativeElement.focus();
      this.tryAutoSubmit();
    }
  }

  tryAutoSubmit() {
    const full = this.code.join('');
    if (full.length === 6 && /^[0-9]{6}$/.test(full)) {
      this.authService.verifyCode({ code: full }).subscribe({
        next: (res) => {
          if (res.success) {
            this.router.navigate(['/login']);
          } else {
            this.errorMessage = res.error?.message ?? 'Invalid code.';
          }
        },
        error: () => {
          this.errorMessage = 'Verification failed. Please try again.';
        }
      });
    }
  }

  onResend() {
    console.log('Resend verification email');
  }
}
