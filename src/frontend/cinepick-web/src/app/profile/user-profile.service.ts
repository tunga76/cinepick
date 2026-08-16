import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, switchMap } from 'rxjs';

export interface UserPreferences {
  preferredGenreSlug: string | null;
  preferredLanguage: string | null;
  maximumRuntimeMinutes: number | null;
  maximumDistanceKilometers: number | null;
}
export interface UserMovieState {
  movieId: string;
  movieTitle: string;
  isFavorite: boolean;
  isWatched: boolean;
  rating: number | null;
  updatedAt: string;
}
export interface RecommendationHistoryItem {
  sessionId: string; createdAt: string; method: string; resultCount: number;
  results: readonly { rank: number; movieId: string; movieTitle: string; score: number; reason: string }[];
}
interface CsrfResponse { token: string; }

@Injectable({ providedIn: 'root' })
export class UserProfileService {
  private readonly http = inject(HttpClient);

  getPreferences(): Observable<UserPreferences> {
    return this.http.get<UserPreferences>('/api/users/me/preferences');
  }
  updatePreferences(value: UserPreferences): Observable<UserPreferences> {
    return this.putWithCsrf('/api/users/me/preferences', value);
  }
  getMovieStates(): Observable<readonly UserMovieState[]> {
    return this.http.get<readonly UserMovieState[]>('/api/users/me/movie-states');
  }
  getRecommendationHistory(): Observable<readonly RecommendationHistoryItem[]> {
    return this.http.get<readonly RecommendationHistoryItem[]>('/api/users/me/recommendation-history');
  }
  getMovieState(movieId: string): Observable<UserMovieState> {
    return this.http.get<UserMovieState>(`/api/users/me/movie-states/${movieId}`);
  }
  updateMovieState(movieId: string, value: Pick<UserMovieState, 'isFavorite' | 'isWatched' | 'rating'>): Observable<UserMovieState> {
    return this.putWithCsrf(`/api/users/me/movie-states/${movieId}`, value);
  }

  private putWithCsrf<T>(url: string, body: unknown): Observable<T> {
    return this.http.get<CsrfResponse>('/api/auth/csrf').pipe(switchMap(response =>
      this.http.put<T>(url, body, { headers: new HttpHeaders({ 'X-CSRF-TOKEN': response.token }) })));
  }
}
