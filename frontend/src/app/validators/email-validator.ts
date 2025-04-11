import { AbstractControl, ValidationErrors } from '@angular/forms';

export function specificEmailValidator(control: AbstractControl): ValidationErrors | null {
  const value = control.value as string;

  if (!value) return null;

  // Regex breakdown:
  // ^         - start of line
  // [^@]+     - one or more non-@ characters (local part)
  // @         - literal @ symbol
  // [a-zA-Z0-9]{2,} - at least 2 letters or numbers for domain
  // \.        - literal dot
  // [a-zA-Z]{2,} - at least 2 letters for top-level domain
  // $         - end of line
  const emailRegex = /^[^@]+@[a-zA-Z0-9]{2,}\.[a-zA-Z]{2,}$/;

  return emailRegex.test(value) ? null : { specificEmail: true };
}
