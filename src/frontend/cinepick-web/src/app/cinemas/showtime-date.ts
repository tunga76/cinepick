import { ShowtimeListItem } from './cinema-catalog.service';

export interface ShowtimeDateOption { key: string; label: string; }
export type ShowtimeSort = 'time' | 'price';
export type ShowtimePeriod = 'all' | 'morning' | 'afternoon' | 'evening';

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

export function showtimeFacetOptions(
  showtimes: readonly ShowtimeListItem[], dateKey: string, field: 'language' | 'format' | 'cinemaName',
): string[] {
  return [...new Set(showtimes
    .filter(item => !dateKey || istanbulDateKey(item.startsAt) === dateKey)
    .map(item => item[field]))]
    .sort((left, right) => left.localeCompare(right, 'tr-TR'));
}

export function sortShowtimes(
  showtimes: readonly ShowtimeListItem[], sort: ShowtimeSort,
): ShowtimeListItem[] {
  return [...showtimes].sort((left, right) => {
    if (sort === 'price' && left.price !== right.price) return left.price - right.price;
    const timeComparison = left.startsAt.localeCompare(right.startsAt);
    return timeComparison || left.id.localeCompare(right.id);
  });
}

export function showtimePriceOptions(
  showtimes: readonly ShowtimeListItem[], dateKey: string,
): number[] {
  return [...new Set(showtimes
    .filter(item => !dateKey || istanbulDateKey(item.startsAt) === dateKey)
    .map(item => item.price))]
    .sort((left, right) => left - right);
}

export function matchesShowtimePeriod(value: string, period: ShowtimePeriod): boolean {
  if (period === 'all') return true;
  const hour = Number(new Intl.DateTimeFormat('en-GB', {
    timeZone: 'Europe/Istanbul', hour: '2-digit', hourCycle: 'h23',
  }).format(new Date(value)));
  if (period === 'morning') return hour < 12;
  if (period === 'afternoon') return hour >= 12 && hour < 18;
  return hour >= 18;
}
