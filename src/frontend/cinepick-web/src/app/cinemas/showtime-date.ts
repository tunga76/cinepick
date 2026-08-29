import { ShowtimeListItem } from './cinema-catalog.service';

export interface ShowtimeDateOption { key: string; label: string; }

export function istanbulDateKey(value: string): string {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Europe/Istanbul', year: 'numeric', month: '2-digit', day: '2-digit',
  }).formatToParts(new Date(value));
  const part = (type: Intl.DateTimeFormatPartTypes) => parts.find(item => item.type === type)?.value ?? '';
  return `${part('year')}-${part('month')}-${part('day')}`;
}

export function showtimeDateOptions(showtimes: readonly ShowtimeListItem[]): ShowtimeDateOption[] {
  const formatter = new Intl.DateTimeFormat('tr-TR', {
    timeZone: 'Europe/Istanbul', weekday: 'short', day: 'numeric', month: 'short',
  });
  const unique = new Map<string, string>();
  for (const showtime of showtimes) {
    const key = istanbulDateKey(showtime.startsAt);
    if (!unique.has(key)) unique.set(key, formatter.format(new Date(showtime.startsAt)));
  }
  return [...unique].sort(([left], [right]) => left.localeCompare(right))
    .map(([key, label]) => ({ key, label }));
}
