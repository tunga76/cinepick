import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

const tmdbImagePathPattern = /^\/[a-zA-Z0-9._/-]+$/;

export function tmdbPosterUrl(path: string | null, size: 'w342' | 'w500' = 'w500'): string | null {
  if (!path || !tmdbImagePathPattern.test(path) || path.includes('//')) return null;
  return `https://image.tmdb.org/t/p/${size}${path}`;
}

export interface MovieListItem {
  id: string;
  title: string;
  overview: string;
  releaseDate: string;
  runtimeMinutes: number;
  originalLanguage: string;
  ageRating: number;
  posterPath: string | null;
  voteAverage: number;
  popularity: number;
  genres: string[];
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface GenreListItem { id: string; name: string; }
export interface MovieDetail extends MovieListItem {
  originalTitle: string;
  backdropPath: string | null;
  voteCount: number;
  isNowPlaying: boolean;
  isUpcoming: boolean;
}
export interface MovieCatalogFilters {
  search?: string;
  genreId?: string;
  maximumRuntimeMinutes?: number;
}

@Injectable({ providedIn: 'root' })
export class MovieCatalogService {
  private readonly http = inject(HttpClient);

  getNowPlaying(filters: MovieCatalogFilters = {}, pageSize = 12): Observable<PagedResponse<MovieListItem>> {
    let params = new HttpParams().set('page', 1).set('pageSize', pageSize);
    if (filters.search) params = params.set('search', filters.search);
    if (filters.genreId) params = params.set('genreId', filters.genreId);
    if (filters.maximumRuntimeMinutes) params = params.set('maximumRuntimeMinutes', filters.maximumRuntimeMinutes);
    return this.http.get<PagedResponse<MovieListItem>>('/api/movies/now-playing', { params });
  }

  getGenres(): Observable<GenreListItem[]> { return this.http.get<GenreListItem[]>('/api/genres'); }
  getById(id: string): Observable<MovieDetail> { return this.http.get<MovieDetail>(`/api/movies/${id}`); }
}
