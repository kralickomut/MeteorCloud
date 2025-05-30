import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class DateTimeService {
  // Converts a UTC ISO string or Date object to local Date object
  toLocalDate(utcInput: string | Date): Date {
    const utcDate = typeof utcInput === 'string' ? new Date(utcInput) : utcInput;
    return new Date(utcDate.getTime() + utcDate.getTimezoneOffset() * 60000);
  }

  // Converts and formats a UTC string as a local date string (e.g. "May 9, 2025, 4:30 PM")
  formatLocalDateTime(utcInput: string | Date, options?: Intl.DateTimeFormatOptions): string {
    const localDate = this.toLocalDate(utcInput);
    return new Intl.DateTimeFormat(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true,
      ...options
    }).format(localDate);
  }
}
