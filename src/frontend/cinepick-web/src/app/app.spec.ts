import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { vi } from 'vitest';
import { AuthService } from './auth/auth.service';
import { App } from './app';
import { routes } from './app.routes';

describe('App', () => {
  async function createApp() {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes)],
    }).compileComponents();
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    return { fixture, http: TestBed.inject(HttpTestingController) };
  }

  it('provides navigation and a login link for anonymous visitors', async () => {
    const { fixture, http } = await createApp();
    http.expectOne('/api/auth/me').flush(null, { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('router-outlet')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Giriş / Kayıt');
    expect(fixture.nativeElement.textContent).not.toContain('Yönetim');
    http.verify();
  });

  it('shows administration only to an administrator', async () => {
    const { fixture, http } = await createApp();
    http.expectOne('/api/auth/me').flush({
      id: '10000000-0000-0000-0000-000000000001', email: 'admin@example.test',
      displayName: 'Yönetici', roles: ['Admin'],
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Yönetim');
    expect(fixture.nativeElement.textContent).toContain('Yönetici');
    http.verify();
  });

  it('closes the open menu with Escape and restores toggle focus', async () => {
    const { fixture, http } = await createApp();
    http.expectOne('/api/auth/me').flush(null, { status: 401, statusText: 'Unauthorized' });
    const toggle: HTMLButtonElement = fixture.nativeElement.querySelector('.menu-toggle');
    toggle.click();
    fixture.detectChanges();
    expect(toggle.getAttribute('aria-expanded')).toBe('true');
    const focus = vi.spyOn(toggle, 'focus');
    fixture.nativeElement.querySelector('nav a').dispatchEvent(
      new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true }));
    fixture.detectChanges();
    expect(toggle.getAttribute('aria-expanded')).toBe('false');
    expect(focus).toHaveBeenCalledOnce();
    http.verify();
  });

  it('keeps session state on logout failure and allows a successful retry', async () => {
    const { fixture, http } = await createApp();
    const user = { id: 'user', email: 'user@example.test', displayName: 'Test', roles: [] };
    http.expectOne('/api/auth/me').flush(user);
    fixture.detectChanges();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const logout: HTMLButtonElement = fixture.nativeElement.querySelector('.nav-action');
    logout.click();
    http.expectOne('/api/auth/csrf').flush({ token: 'first' });
    http.expectOne('/api/auth/logout').flush(null, { status: 503, statusText: 'Unavailable' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('Çıkış işlemi doğrulanamadı');
    expect(TestBed.inject(AuthService).currentUser()).toEqual(user);
    expect(navigate).not.toHaveBeenCalled();
    expect(logout.disabled).toBe(false);
    logout.click();
    http.expectOne('/api/auth/csrf').flush({ token: 'retry' });
    http.expectOne('/api/auth/logout').flush(null);
    fixture.detectChanges();
    expect(TestBed.inject(AuthService).currentUser()).toBeNull();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
    expect(navigate).toHaveBeenCalledWith('/');
    http.verify();
  });
});
