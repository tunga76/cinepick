import { describe, expect, it } from 'vitest';
import { ShowtimeListItem } from './cinema-catalog.service';
import { istanbulDateKey, matchesShowtimePeriod, showtimeDateOptions, showtimeFacetOptions, showtimePriceOptions, sortShowtimes } from './showtime-date';

const showtime = (id: string, startsAt: string): ShowtimeListItem => ({
  id, startsAt, endsAt: startsAt, movieId: 'movie', movieTitle: 'Film', cinemaId: 'cinema',
  cinemaName: 'Sinema', auditoriumId: 'auditorium', auditoriumName: 'Salon', price: 100,
  currency: 'TRY', language: 'tr', format: '2D', ticketUrl: 'https://tickets.example.invalid',
});

describe('showtime date helpers', () => {
  it('uses the Europe/Istanbul calendar date', () => {
    expect(istanbulDateKey('2026-08-29T21:30:00Z')).toBe('2026-08-30');
  });

  it('returns distinct date options in chronological order', () => {
    const options = showtimeDateOptions([
      showtime('2', '2026-08-30T15:00:00Z'),
      showtime('1', '2026-08-29T15:00:00Z'),
      showtime('3', '2026-08-29T18:00:00Z'),
    ]);
    expect(options.map(option => option.key)).toEqual(['2026-08-29', '2026-08-30']);
  });

  it('returns distinct facets only for the selected Istanbul date', () => {
    const items = [
      { ...showtime('1', '2026-08-29T18:00:00Z'), language: 'tr', format: '2D', cinemaName: 'Moda Sineması' },
      { ...showtime('2', '2026-08-29T19:00:00Z'), language: 'en', format: 'IMAX', cinemaName: 'Atlas Sineması' },
      { ...showtime('3', '2026-08-29T22:00:00Z'), language: 'de', format: '3D', cinemaName: 'Başka Sinema' },
    ];
    expect(showtimeFacetOptions(items, '2026-08-29', 'language')).toEqual(['en', 'tr']);
    expect(showtimeFacetOptions(items, '2026-08-30', 'format')).toEqual(['3D']);
    expect(showtimeFacetOptions(items, '2026-08-29', 'cinemaName'))
      .toEqual(['Atlas Sineması', 'Moda Sineması']);
  });

  it('sorts by price and then by start time', () => {
    const items = [
      { ...showtime('late-cheap', '2026-08-29T19:00:00Z'), price: 100 },
      { ...showtime('expensive', '2026-08-29T17:00:00Z'), price: 150 },
      { ...showtime('early-cheap', '2026-08-29T18:00:00Z'), price: 100 },
    ];
    expect(sortShowtimes(items, 'price').map(item => item.id))
      .toEqual(['early-cheap', 'late-cheap', 'expensive']);
    expect(sortShowtimes(items, 'time').map(item => item.id))
      .toEqual(['expensive', 'early-cheap', 'late-cheap']);
  });

  it('sorts actual instants across offsets and calendar boundaries without mutating input', () => {
    const items = Object.freeze([
      showtime('later', '2026-08-29T22:00:00Z'),
      showtime('earlier', '2026-08-30T00:30:00+03:00'),
    ]);
    expect(sortShowtimes(items, 'time').map(item => item.id)).toEqual(['earlier', 'later']);
    expect(items.map(item => item.id)).toEqual(['later', 'earlier']);
  });

  it('breaks equal-price ties using actual instants across offsets', () => {
    const items = [
      showtime('later', '2026-08-29T16:00:00Z'),
      showtime('earlier', '2026-08-29T18:00:00+03:00'),
    ];
    expect(sortShowtimes(items, 'price').map(item => item.id)).toEqual(['earlier', 'later']);
  });

  it('uses identifiers to order equivalent instants regardless of timestamp representation', () => {
    const items = [
      showtime('b', '2026-08-29T15:00:00Z'),
      showtime('a', '2026-08-29T18:00:00+03:00'),
    ];
    for (const mode of ['time', 'price'] as const) {
      expect(sortShowtimes(items, mode).map(item => item.id)).toEqual(['a', 'b']);
      expect(sortShowtimes([...items].reverse(), mode).map(item => item.id)).toEqual(['a', 'b']);
    }
  });

  it('returns distinct prices only for the selected Istanbul date', () => {
    const items = [
      { ...showtime('1', '2026-08-29T18:00:00Z'), price: 180 },
      { ...showtime('2', '2026-08-29T19:00:00Z'), price: 120 },
      { ...showtime('3', '2026-08-29T22:00:00Z'), price: 90 },
      { ...showtime('4', '2026-08-29T17:00:00Z'), price: 120 },
    ];
    expect(showtimePriceOptions(items, '2026-08-29')).toEqual([120, 180]);
    expect(showtimePriceOptions(items, '2026-08-30')).toEqual([90]);
  });

  it('classifies periods using Europe/Istanbul time', () => {
    expect(matchesShowtimePeriod('2026-08-29T08:59:00Z', 'morning')).toBe(true);
    expect(matchesShowtimePeriod('2026-08-29T09:00:00Z', 'afternoon')).toBe(true);
    expect(matchesShowtimePeriod('2026-08-29T14:59:00Z', 'afternoon')).toBe(true);
    expect(matchesShowtimePeriod('2026-08-29T15:00:00Z', 'evening')).toBe(true);
    expect(matchesShowtimePeriod('2026-08-29T15:00:00Z', 'all')).toBe(true);
  });
});
