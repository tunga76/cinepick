import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService, AuthenticatedUser } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('uses a fresh CSRF token for login and logout while updating session state', () => {
    const user: AuthenticatedUser = {
      id: '10000000-0000-0000-0000-000000000001',
      email: 'user@example.test', displayName: 'Test User', roles: [],
    };

    service.login({ email: user.email, password: 'Strong!Pass1' }).subscribe();
    http.expectOne('/api/auth/csrf').flush({ token: 'anonymous-token' });
    const login = http.expectOne('/api/auth/login');
    expect(login.request.headers.get('X-CSRF-TOKEN')).toBe('anonymous-token');
    login.flush(user);
    expect(service.currentUser()).toEqual(user);

    service.logout().subscribe();
    http.expectOne('/api/auth/csrf').flush({ token: 'authenticated-token' });
    const logout = http.expectOne('/api/auth/logout');
    expect(logout.request.headers.get('X-CSRF-TOKEN')).toBe('authenticated-token');
    logout.flush(null);
    expect(service.currentUser()).toBeNull();
  });
});
