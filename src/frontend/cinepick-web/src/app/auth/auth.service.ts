import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { catchError, map, Observable, of, switchMap, tap, throwError } from 'rxjs';

export interface AuthenticatedUser {
  id: string;
  email: string;
  displayName: string;
  roles: readonly string[];
}

interface CsrfResponse { token: string; }
export interface RegisterRequest { email: string; password: string; displayName: string; }
export interface LoginRequest { email: string; password: string; }

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly userState = signal<AuthenticatedUser | null>(null);
  readonly currentUser = this.userState.asReadonly();

  refresh(): Observable<AuthenticatedUser | null> {
    return this.http.get<AuthenticatedUser>('/api/auth/me').pipe(
      tap(user => this.userState.set(user)),
      map(user => user as AuthenticatedUser | null),
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          this.userState.set(null);
          return of(null);
        }
        return throwError(() => error);
      }),
    );
  }

  register(request: RegisterRequest): Observable<AuthenticatedUser> {
    return this.withCsrf(headers => this.http.post<AuthenticatedUser>(
      '/api/auth/register', request, { headers })).pipe(
        tap(user => this.userState.set(user)),
      );
  }

  login(request: LoginRequest): Observable<AuthenticatedUser> {
    return this.withCsrf(headers => this.http.post<AuthenticatedUser>(
      '/api/auth/login', request, { headers })).pipe(
        tap(user => this.userState.set(user)),
      );
  }

  logout(): Observable<void> {
    return this.withCsrf(headers => this.http.post<void>(
      '/api/auth/logout', null, { headers })).pipe(
        tap(() => this.userState.set(null)),
      );
  }

  private withCsrf<T>(operation: (headers: HttpHeaders) => Observable<T>): Observable<T> {
    return this.http.get<CsrfResponse>('/api/auth/csrf').pipe(
      switchMap(response => operation(new HttpHeaders({ 'X-CSRF-TOKEN': response.token }))),
    );
  }
}
