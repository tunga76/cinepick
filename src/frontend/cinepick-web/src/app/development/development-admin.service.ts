import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, switchMap } from 'rxjs';

export interface SyncResult { providerId: string; receivedCount: number; insertedCount: number; updatedCount: number; }
export interface SyncLogItem {
  id: string; providerId: string; operation: string; status: string; startedAt: string;
  completedAt: string | null; receivedCount: number; insertedCount: number; updatedCount: number;
  errorCode: string | null;
}
export interface DevelopmentShowtimeItem {
  id: string; movieTitle: string; cinemaName: string; auditoriumName: string;
  startsAt: string; isCancelled: boolean; externalSyncKey: string | null;
}

@Injectable({ providedIn: 'root' })
export class DevelopmentAdminService {
  private readonly http = inject(HttpClient);
  getLogs() { return this.http.get<SyncLogItem[]>('/api/admin/sync-logs'); }
  getShowtimes() { return this.http.get<DevelopmentShowtimeItem[]>('/api/admin/showtimes'); }
  syncMovies() { return this.mutate<SyncResult>('/api/admin/movie-catalog-syncs', 'post', null); }
  syncShowtimes() { return this.mutate<SyncResult>('/api/admin/showtime-catalog-syncs', 'post', null); }
  setCancellation(id: string, isCancelled: boolean) {
    return this.mutate<void>(`/api/admin/showtimes/${id}/cancellation`, 'put', { isCancelled });
  }
  private mutate<T>(url: string, method: 'post' | 'put', body: unknown): Observable<T> {
    return this.http.get<{ token: string }>('/api/auth/csrf').pipe(switchMap(response => {
      const options = { headers: new HttpHeaders({ 'X-CSRF-TOKEN': response.token }) };
      return method === 'post' ? this.http.post<T>(url, body, options)
        : this.http.put<T>(url, body, options);
    }));
  }
}
