import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { describe, expect, it } from 'vitest';
import { MovieCatalogPage } from './movie-catalog-page';
import { MovieDetailPage } from './movie-detail-page';
import { MovieCatalogService, MovieDetail } from './movie-catalog.service';
import { CinemaCatalogService } from '../cinemas/cinema-catalog.service';
import { UserProfileService } from '../profile/user-profile.service';

const movie: MovieDetail = {
  id: 'movie', title: 'Test Film', originalTitle: 'Test Film', overview: '',
  releaseDate: '2026-08-30', runtimeMinutes: 100, originalLanguage: 'tr', ageRating: 0,
  posterPath: '/poster.jpg', backdropPath: null, voteAverage: 7, voteCount: 10,
  popularity: 10, genres: [], isNowPlaying: true, isUpcoming: false,
};

describe('Poster retry', () => {
  for (const page of ['catalog', 'detail'] as const) {
    for (const hasPoster of [true, false]) {
      it(`${page}: ${hasPoster ? 'retries failed images only on user action' : 'keeps a placeholder without retry for missing metadata'}`, async () => {
        const item = { ...movie, posterPath: hasPoster ? movie.posterPath : null };
        await TestBed.configureTestingModule({
          imports: [MovieCatalogPage, MovieDetailPage],
          providers: [provideHttpClient(), provideRouter([]),
            { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: movie.id }) } } },
            { provide: MovieCatalogService, useValue: {
              getNowPlaying: () => of({ items: [item], totalCount: 1, page: 1 }),
              getUpcoming: () => of({ items: [], totalCount: 0, page: 1 }),
              getGenres: () => of([]), getById: () => of(item),
            } },
            { provide: CinemaCatalogService, useValue: { getMovieShowtimes: () => of([]) } },
            { provide: UserProfileService, useValue: { getMovieState: () => throwError(() => ({ status: 401 })) } },
          ],
        }).compileComponents();
        const fixture = page === 'catalog'
          ? TestBed.createComponent(MovieCatalogPage) : TestBed.createComponent(MovieDetailPage);
        fixture.detectChanges();
        const element: HTMLElement = fixture.nativeElement;
        const retrySelector = page === 'catalog' ? '.poster-retry button' : '.detail-poster button';
        const image = element.querySelector<HTMLImageElement>('.poster img');
        expect(element.querySelector(retrySelector)).toBeNull();
        if (!hasPoster) {
          expect(image).toBeNull();
          expect(element.querySelector('.poster')?.textContent).toContain('CinePick');
          return;
        }
        const source = image!.getAttribute('src');
        image!.dispatchEvent(new Event('error'));
        fixture.detectChanges();
        expect(element.querySelector('.poster img')).toBeNull();
        fixture.detectChanges();
        expect(element.querySelector('.poster img')).toBeNull();
        const retry = element.querySelector<HTMLButtonElement>(retrySelector)!;
        expect(retry.closest('a')).toBeNull();
        retry.click();
        fixture.detectChanges();
        const retried = element.querySelector<HTMLImageElement>('.poster img')!;
        expect(retried).not.toBe(image);
        expect(retried.getAttribute('src')).toBe(source);
        expect(element.querySelector(retrySelector)).toBeNull();
        retried.dispatchEvent(new Event('error'));
        fixture.detectChanges();
        expect(element.querySelector(retrySelector)).not.toBeNull();
      });
    }
  }
});
