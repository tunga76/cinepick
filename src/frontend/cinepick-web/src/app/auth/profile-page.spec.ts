import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it } from 'vitest';
import { ProfilePage } from './profile-page';

describe('ProfilePage loading', () => {
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
