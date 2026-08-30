import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it } from 'vitest';
import { ProfilePage } from './profile-page';

describe('ProfilePage loading', () => {
  it('rejects invalid numeric preferences without requests and accepts fractions and empty values', async () => {
    await TestBed.configureTestingModule({
      imports: [ProfilePage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    const fixture = TestBed.createComponent(ProfilePage);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/users/me/movie-states').flush([]);
    http.expectOne('/api/users/me/recommendation-history').flush([]);
    http.expectOne('/api/users/me/preferences').flush({ preferredGenreSlug: null,
      preferredLanguage: null, maximumRuntimeMinutes: null, maximumDistanceKilometers: null });
    fixture.detectChanges();
    const set = (field: string, value: string) => {
      const input: HTMLInputElement = fixture.nativeElement.querySelector(`[formControlName="${field}"]`);
      input.value = value;
      input.dispatchEvent(new Event('input'));
      return input;
    };
    const submit = () => {
      fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit', { cancelable: true }));
      fixture.detectChanges();
    };
    for (const [field, values] of [
      ['maximumRuntimeMinutes', ['0', '-1', '601', '90.5']],
      ['maximumDistanceKilometers', ['0', '-1', '101']],
    ] as const) {
      for (const value of values) {
        const input = set(field, value);
        submit();
        expect(input.getAttribute('aria-invalid')).toBe('true');
        expect(fixture.nativeElement.textContent).toContain('işaretli alanları düzelt');
        http.expectNone('/api/auth/csrf');
        http.expectNone('/api/users/me/preferences');
      }
      set(field, '');
    }
    for (const [runtime, distance] of [['1', '0.5'], ['600', '100'], ['', '']]) {
      set('maximumRuntimeMinutes', runtime);
      set('maximumDistanceKilometers', distance);
      submit();
      http.expectOne('/api/auth/csrf').flush({ token: 'test' });
      const request = http.expectOne('/api/users/me/preferences');
      expect(request.request.body.maximumRuntimeMinutes).toBe(runtime ? Number(runtime) : null);
      expect(request.request.body.maximumDistanceKilometers).toBe(distance ? Number(distance) : null);
      request.flush(request.request.body);
      fixture.detectChanges();
    }
    http.verify();
  });

  for (const failureStage of ['csrf', 'save'] as const) {
    it(`locks preference saves and allows retry after ${failureStage} failure`, async () => {
      await TestBed.configureTestingModule({
        imports: [ProfilePage],
        providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
      }).compileComponents();
      const fixture = TestBed.createComponent(ProfilePage);
      const http = TestBed.inject(HttpTestingController);
      fixture.detectChanges();
      http.expectOne('/api/users/me/movie-states').flush([]);
      http.expectOne('/api/users/me/recommendation-history').flush([]);
      const preferences = { preferredGenreSlug: 'dram', preferredLanguage: 'tr',
        maximumRuntimeMinutes: 120, maximumDistanceKilometers: 20 };
      http.expectOne('/api/users/me/preferences').flush(preferences);
      fixture.detectChanges();
      const input: HTMLInputElement = fixture.nativeElement.querySelector('input[formControlName="preferredGenreSlug"]');
      input.value = 'komedi';
      input.dispatchEvent(new Event('input'));
      const submit = () => fixture.nativeElement.querySelector('form')
        .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
      submit();
      submit();
      fixture.detectChanges();
      expect(input.disabled).toBe(true);
      expect(fixture.nativeElement.querySelector('button[type="submit"]').disabled).toBe(true);
      const csrf = http.expectOne('/api/auth/csrf');
      if (failureStage === 'csrf') {
        csrf.flush(null, { status: 503, statusText: 'Unavailable' });
        http.expectNone('/api/users/me/preferences');
      } else {
        csrf.flush({ token: 'first' });
        submit();
        http.expectNone('/api/auth/csrf');
        const save = http.expectOne('/api/users/me/preferences');
        expect(save.request.method).toBe('PUT');
        expect(save.request.body.preferredGenreSlug).toBe('komedi');
        save.flush(null, { status: 503, statusText: 'Unavailable' });
      }
      fixture.detectChanges();
      expect(input.disabled).toBe(false);
      expect(input.value).toBe('komedi');
      expect(fixture.nativeElement.textContent).toContain('Tekrar deneyebilirsin');
      submit();
      fixture.detectChanges();
      expect(fixture.nativeElement.textContent).not.toContain('Tekrar deneyebilirsin');
      http.expectOne('/api/auth/csrf').flush({ token: 'retry' });
      const retry = http.expectOne('/api/users/me/preferences');
      expect(retry.request.body).toEqual({ ...preferences, preferredGenreSlug: 'komedi' });
      retry.flush(retry.request.body);
      fixture.detectChanges();
      expect(input.disabled).toBe(false);
      expect(fixture.nativeElement.querySelector('button[type="submit"]').disabled).toBe(false);
      expect(fixture.nativeElement.textContent).toContain('Tercihlerin kaydedildi.');
      http.verify();
    });
  }

  it('hides empty data while loading and recovers from a failed fetch', async () => {
    await TestBed.configureTestingModule({
      imports: [ProfilePage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    const fixture = TestBed.createComponent(ProfilePage);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="status"]').textContent).toContain('yükleniyor');
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('Henüz favori');
    http.expectOne('/api/users/me/movie-states').flush([]);
    http.expectOne('/api/users/me/recommendation-history').flush([]);
    http.expectOne('/api/users/me/preferences').flush(null, { status: 503, statusText: 'Unavailable' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('yüklenemedi');
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('Henüz favori');
    const retry = [...fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>]
      .find(button => button.textContent?.includes('Tekrar dene'))!;
    retry.click();
    retry.click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
    http.expectOne('/api/users/me/movie-states').flush([]);
    http.expectOne('/api/users/me/recommendation-history').flush([]);
    http.expectOne('/api/users/me/preferences').flush({
      preferredGenreSlug: 'dram', preferredLanguage: 'tr',
      maximumRuntimeMinutes: 120, maximumDistanceKilometers: 20,
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('input[formControlName="preferredGenreSlug"]').value).toBe('dram');
    expect(fixture.nativeElement.textContent).toContain('Henüz favori');
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
    http.verify();
  });
});
