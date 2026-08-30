import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { describe, expect, it } from 'vitest';
import { MovieCatalogPage } from './movie-catalog-page';
import { MovieCatalogService } from './movie-catalog.service';

describe('Recommendation recovery', () => {
  for (const failureStage of ['csrf', 'recommendation'] as const) {
    it(`preserves the request and retries after ${failureStage} failure without duplicates`, async () => {
      const emptyPage = { items: [], totalCount: 0, page: 1 };
      await TestBed.configureTestingModule({
        imports: [MovieCatalogPage],
        providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
          { provide: MovieCatalogService, useValue: {
            getNowPlaying: () => of(emptyPage), getUpcoming: () => of(emptyPage), getGenres: () => of([]),
          } }],
      }).compileComponents();
      const fixture = TestBed.createComponent(MovieCatalogPage);
      const http = TestBed.inject(HttpTestingController);
      fixture.detectChanges();
      const input: HTMLTextAreaElement = fixture.nativeElement.querySelector('textarea');
      input.value = 'Yarın İstanbul’da komedi';
      input.dispatchEvent(new Event('input'));
      const submit = () => fixture.nativeElement.querySelector('form.prompt')
        .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
      submit();
      submit();
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.recommendation-state[role="status"]').textContent).toContain('değerlendiriliyor');
      const csrf = http.expectOne('/api/auth/csrf');
      if (failureStage === 'csrf') {
        csrf.flush(null, { status: 503, statusText: 'Unavailable' });
        http.expectNone('/api/recommendations');
      } else {
        csrf.flush({ token: 'first' });
        submit();
        http.expectNone('/api/auth/csrf');
        http.expectOne('/api/recommendations').flush(null, { status: 503, statusText: 'Unavailable' });
      }
      fixture.detectChanges();
      expect(input.value).toBe('Yarın İstanbul’da komedi');
      expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
      expect(fixture.nativeElement.textContent).not.toContain('gerçek bir seans bulunamadı');
      const retry: HTMLButtonElement = fixture.nativeElement.querySelector('.recommendation-state button');
      retry.click();
      retry.click();
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
      http.expectOne('/api/auth/csrf').flush({ token: 'retry' });
      const request = http.expectOne('/api/recommendations');
      expect(request.request.body).toEqual({ text: input.value });
      expect(request.request.headers.get('X-CSRF-TOKEN')).toBe('retry');
      request.flush({ sessionId: 'session', filter: {}, method: 'Fallback', candidateCount: 0, items: [] });
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.recommendation-state[role="status"]').textContent).toContain('gerçek bir seans bulunamadı');
      expect(fixture.nativeElement.querySelector('form.prompt button').disabled).toBe(false);
      http.verify();
    });
  }
});
