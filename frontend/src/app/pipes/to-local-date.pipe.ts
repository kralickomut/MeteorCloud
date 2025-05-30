import { Pipe, PipeTransform } from '@angular/core';
import { DateTimeService } from '../services/date-time.service'; // adjust if needed

@Pipe({
  name: 'toLocalDate'
})
export class ToLocalDatePipe implements PipeTransform {
  transform(value: string | Date | null | undefined, options?: Intl.DateTimeFormatOptions): string {
    if (!value) return '—';

    let date: Date;
    if (typeof value === 'string') {
      // Force UTC interpretation by adding 'Z' if no timezone is present
      const hasTimezone = /Z|[+-]\d{2}:\d{2}/.test(value);
      date = new Date(hasTimezone ? value : value + 'Z');
    } else {
      date = new Date(value);
    }

    return new Intl.DateTimeFormat(undefined, options).format(date);
  }
}
