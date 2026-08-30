import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { describe, expect, it } from 'vitest';
import { MovieDetailPage } from './movie-detail-page';

describe('Movie showtime retry', () => {
  for (const hasShowtimes of [true, false]) {
    it(`recovers to ${hasShowtimes ? 'showtimes' : 'an empty result'} without reloading movie data`, async () => {
      await TestBed.configureTestingModule({
        imports: [MovieDetailPage],
        providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
          { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'movie' }) } } }],
      }).compileComponents();
      const fixture = TestBed.createComponent(MovieDetailPage);
      const http = TestBed.inject(HttpTestingController);
      fixture.detectChanges();
      http.expectOne('/api/movies/movie').flush({ id: 'movie', title: 'Test Film', originalTitle: 'Test Film',
        overview: '', genres: [], ageRating: 0, runtimeMinutes: 100, voteAverage: 7, voteCount: 10,
        posterPath: null, isNowPlaying: true });
      http.expectOne('/api/users/me/movie-states/movie').flush(null, { status: 401, statusText: 'Unauthorized' });
      http.expectOne(request => request.url === '/api/showtimes' && request.params.get('movieId') === 'movie')
        .flush(null, { status: 503, statusText: 'Unavailable' });
      fixture.detectChanges();
      const section: HTMLElement = fixture.nativeElement.querySelector('.movie-showtimes');
      expect(section.querySelector('[role="alert"]')).not.toBeNull();
      expect(section.textContent).not.toContain('yaklaşan seans bulunamadı');
      const retry = section.querySelector('button')!;
      retry.click();
      retry.click();
      fixture.detectChanges();
      expect(section.querySelector('[role="alert"]')).toBeNull();
      expect(section.querySelector('[role="status"]')?.textContent).toContain('yükleniyor');
      http.expectNone('/api/movies/movie');
      http.expectNone('/api/users/me/movie-states/movie');
      const request = http.expectOne(request => request.url === '/api/showtimes' && request.params.get('movieId') === 'movie');
      request.flush(hasShowtimes ? [{ id: 'showtime', movieId: 'movie', cinemaId: 'cinema',
        cinemaName: 'Test Sinema', auditoriumName: 'Salon 1', language: 'tr', format: '2D',
        startsAt: '2026-08-30T18:00:00Z', endsAt: '2026-08-30T20:00:00Z', price: 100, currency: 'TRY',
        ticketUrl: 'https://tickets.example.invalid' }] : []);
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('h1').textContent).toBe('Test Film');
      expect(section.querySelector('[role="alert"]')).toBeNull();
      expect(section.querySelector('[aria-busy="true"]')).toBeNull();
      if (hasShowtimes) {
        expect(section.querySelector('.showtime-card')?.textContent).toContain('Test Sinema');
        expect(section.querySelector('[role="tab"][aria-selected="true"]')).not.toBeNull();
      } else {
        expect(section.querySelector('[role="status"]')?.textContent).toContain('yaklaşan seans bulunamadı');
      }
      http.verify();
    });
  }
});
