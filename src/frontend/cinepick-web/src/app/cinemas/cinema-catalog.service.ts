import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

export interface CityListItem { id: string; name: string; }
export interface CinemaListItem {
  id: string; name: string; city: string; district: string; address: string;
  latitude: number; longitude: number; distanceKilometers: number | null;
}
export interface AuditoriumListItem { id: string; name: string; capacity: number; }
export interface CinemaDetail extends Omit<CinemaListItem, 'distanceKilometers'> {
  auditoriums: AuditoriumListItem[];
}
export interface ShowtimeListItem {
  id: string; movieId: string; movieTitle: string; cinemaId: string; cinemaName: string;
  auditoriumId: string; auditoriumName: string; startsAt: string; endsAt: string;
  price: number; currency: string; language: string; format: string; ticketUrl: string;
}

@Injectable({ providedIn: 'root' })
export class CinemaCatalogService {
  private readonly http = inject(HttpClient);
  getCities() { return this.http.get<CityListItem[]>('/api/cities'); }
  getCinemas(filters: { cityId?: string; latitude?: number; longitude?: number } = {}) {
    let params = new HttpParams();
    if (filters.cityId) params = params.set('cityId', filters.cityId);
    if (filters.latitude !== undefined && filters.longitude !== undefined) {
      params = params.set('latitude', filters.latitude).set('longitude', filters.longitude)
        .set('radiusKilometers', 25);
    }
    return this.http.get<CinemaListItem[]>('/api/cinemas', { params });
  }
  getCinema(id: string) { return this.http.get<CinemaDetail>(`/api/cinemas/${id}`); }
  getShowtimes(cinemaId: string) {
    return this.http.get<ShowtimeListItem[]>('/api/showtimes', {
      params: new HttpParams().set('cinemaId', cinemaId),
    });
  }
}
