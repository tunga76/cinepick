import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
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
});
