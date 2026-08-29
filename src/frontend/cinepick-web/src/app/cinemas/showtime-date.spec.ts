import { describe, expect, it } from 'vitest';
import { ShowtimeListItem } from './cinema-catalog.service';
import { istanbulDateKey, showtimeDateOptions } from './showtime-date';

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
});
