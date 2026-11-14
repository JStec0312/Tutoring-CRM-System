import type { CalendarLesson } from '../../calendar/api/get-calendar';
import type { OverdueAgreement } from '../../payments/api/get-overdue-agreements';

export function startOfDay(date: Date) {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

export function addDays(date: Date, days: number) {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}

export function startOfWeek(date: Date) {
  return addDays(date, -((date.getDay() + 6) % 7));
}

export function dateKey(date: Date) {
  return [date.getFullYear(), date.getMonth() + 1, date.getDate()].map(value => String(value).padStart(2, '0')).join('-');
}

export function parseLocalDateTime(date: string, time: string) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(date) || !/^([01]\d|2[0-3]):[0-5]\d$/.test(time)) return null;
  const value = new Date(`${date}T${time}:00`);
  if (Number.isNaN(value.getTime()) || dateKey(value) !== date || timeLabel(value) !== time) return null;
  return value;
}

export function timeLabel(date: string | Date) {
  return new Intl.DateTimeFormat('en-GB', { hour: '2-digit', minute: '2-digit', hourCycle: 'h23' }).format(new Date(date));
}

export function shortDate(date: string | Date) {
  return new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short' }).format(new Date(date));
}

export function lessonTime(lesson: CalendarLesson) {
  return `${timeLabel(lesson.startsAtUtc)}–${timeLabel(lesson.endsAtUtc)}`;
}

export function lessonMinutes({ startsAtUtc, endsAtUtc }: { startsAtUtc: string; endsAtUtc: string }) {
  return Math.round((Date.parse(endsAtUtc) - Date.parse(startsAtUtc)) / 60_000);
}

export function money(amount: number, currency: string) {
  const number = new Intl.NumberFormat('en-GB', { maximumFractionDigits: 2 }).format(amount);
  return `${number} ${currency}`;
}

export function unpaidLabel(agreements: OverdueAgreement[]) {
  const totals = new Map<string, number>();
  for (const agreement of agreements) {
    for (const { currency, amount } of agreement.amountsByCurrency ?? []) {
      totals.set(currency, (totals.get(currency) ?? 0) + amount);
    }
  }
  return Array.from(totals).sort(([a], [b]) => a.localeCompare(b))
    .map(([currency, amount]) => money(amount, currency)).join(' + ');
}

export function estimateLabel(lesson: CalendarLesson) {
  return lesson.estimatedAmount != null && lesson.currency ? money(lesson.estimatedAmount, lesson.currency) : null;
}

export function lessonSummary(lesson: CalendarLesson) {
  return [lesson.subject, lesson.status, `${lessonMinutes(lesson)} min`, estimateLabel(lesson)].filter(Boolean).join(' · ');
}

export function quantity(count: number, label: string) {
  return `${count} ${label}${count === 1 ? '' : 's'}`;
}

export function parseRate(value: string) {
  const text = value.trim().replace(',', '.');
  if (!text) return null;
  return /^\d{1,12}(\.\d{1,2})?$/.test(text) ? Number(text) : Number.NaN;
}
