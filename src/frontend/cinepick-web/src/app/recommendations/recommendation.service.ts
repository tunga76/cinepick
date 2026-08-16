import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { HttpHeaders } from '@angular/common/http';
import { switchMap } from 'rxjs';

export interface RecommendationFilter {
  startsFrom: string; startsBefore: string; maximumRuntimeMinutes: number | null;
  genreSlug: string | null; citySlug: string | null; districtSlug: string | null;
  maximumPrice: number | null; language: string | null; format: string | null;
}
export interface RecommendationItem {
  movieId: string; showtimeId: string; movieTitle: string; cinemaName: string;
  districtName: string; startsAt: string; endsAt: string; price: number; currency: string;
  language: string; format: string; score: number; reason: string; ticketUrl: string;
}
export interface RecommendationResponse {
  sessionId: string; filter: RecommendationFilter; method: string; candidateCount: number; items: RecommendationItem[];
}

@Injectable({ providedIn: 'root' })
export class RecommendationService {
  private readonly http = inject(HttpClient);
  recommend(text: string) {
    return this.http.get<{ token: string }>('/api/auth/csrf').pipe(switchMap(response =>
      this.http.post<RecommendationResponse>('/api/recommendations', { text }, {
        headers: new HttpHeaders({ 'X-CSRF-TOKEN': response.token }),
      })));
  }
}
