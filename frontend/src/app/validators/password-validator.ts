import { AbstractControl, ValidationErrors } from '@angular/forms';

export function complexPasswordValidator(control: AbstractControl): ValidationErrors | null {
  const value = control.value as string;

  if (!value) return null;

  const isAllLetters = /^[A-Za-z]+$/.test(value);
  const isAllDigits = /^[0-9]+$/.test(value);
  const hasUppercase = /[A-Z]/.test(value);

  const errors: ValidationErrors = {};

  if (value.length < 6) {
    errors['minlength'] = true;
  }

  if (isAllLetters || isAllDigits) {
    errors['lettersAndDigits'] = true;
  }

  if (!hasUppercase) {
    errors['uppercase'] = true;
  }

  return Object.keys(errors).length ? errors : null;
}
