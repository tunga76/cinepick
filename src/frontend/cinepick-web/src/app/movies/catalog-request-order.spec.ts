import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it } from 'vitest';
import { MovieCatalogPage } from './movie-catalog-page';

describe('Catalog request ordering', () => {
  async function setup() {
    await TestBed.configureTestingModule({
      imports: [MovieCatalogPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    const fixture = TestBed.createComponent(MovieCatalogPage);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    const initial = http.expectOne(request => request.url === '/api/movies/now-playing');
    http.expectOne(request => request.url === '/api/movies/upcoming').flush({ items: [], totalCount: 0, page: 1 });
    http.expectOne('/api/genres').flush([{ id: 'genre', name: 'Komedi' }]);
    return { fixture, http, initial };
  }

  for (const staleFailure of [false, true]) {
    it(`ignores stale initial ${staleFailure ? 'errors' : 'results'} after a tab change`, async () => {
      const { fixture, http, initial } = await setup();
      fixture.nativeElement.querySelectorAll('.catalog-tabs button')[1].click();
      http.expectOne(request => request.url === '/api/movies/upcoming')
        .flush({ items: [], totalCount: 24, page: 2 });
      if (staleFailure) initial.flush(null, { status: 503, statusText: 'Unavailable' });
      else initial.flush({ items: [], totalCount: 99, page: 1 });
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.catalog .result-count').textContent).toContain('24 film');
      expect(fixture.nativeElement.querySelector('.catalog [role="alert"]')).toBeNull();
      expect(fixture.nativeElement.querySelector('#catalog-title').textContent).toContain('Yakında');
      if (!staleFailure) expect(fixture.nativeElement.querySelector('select[formControlName="genreId"]').textContent).toContain('Komedi');
      http.verify();
    });

    it(`ignores older filter ${staleFailure ? 'errors' : 'results'} while the latest request is pending`, async () => {
      const { fixture, http, initial } = await setup();
      initial.flush({ items: [], totalCount: 0, page: 1 });
      fixture.detectChanges();
      const filter = (value: string) => {
        const input: HTMLInputElement = fixture.nativeElement.querySelector('input[type="search"]');
        input.value = value;
        input.dispatchEvent(new Event('input'));
        fixture.nativeElement.querySelector('.catalog-filters').dispatchEvent(new Event('submit', { cancelable: true }));
        return http.expectOne(request => request.url === '/api/movies/now-playing' && request.params.get('search') === value);
      };
      const older = filter('eski');
      const latest = filter('yeni');
      if (staleFailure) older.flush(null, { status: 503, statusText: 'Unavailable' });
      else older.flush({ items: [], totalCount: 99, page: 5 });
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.catalog [aria-busy="true"]')).not.toBeNull();
      expect(fixture.nativeElement.querySelector('.catalog [role="alert"]')).toBeNull();
      latest.flush({ items: [], totalCount: 3, page: 1 });
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.catalog .result-count').textContent).toContain('3 film');
      expect(fixture.nativeElement.querySelector('.catalog [aria-busy="true"]')).toBeNull();
      http.verify();
    });
  }
});
