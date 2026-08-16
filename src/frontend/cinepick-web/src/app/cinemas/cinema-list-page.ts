import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { CinemaCatalogService, CinemaListItem, CityListItem } from './cinema-catalog.service';

@Component({
  selector: 'app-cinema-list-page', imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './cinema-list-page.html', changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CinemaListPage implements OnInit {
  private readonly catalog = inject(CinemaCatalogService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly cities = signal<readonly CityListItem[]>([]);
  protected readonly cinemas = signal<readonly CinemaListItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly locationMessage = signal('');
  protected readonly cityId = new FormControl('', { nonNullable: true });

  ngOnInit(): void {
    forkJoin({ cities: this.catalog.getCities(), cinemas: this.catalog.getCinemas() })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: ({ cities, cinemas }) => { this.cities.set(cities); this.cinemas.set(cinemas); this.loading.set(false); },
        error: () => { this.error.set(true); this.loading.set(false); },
      });
  }

  protected filterByCity(): void { this.load({ cityId: this.cityId.value || undefined }); }
  protected useLocation(): void {
    if (!navigator.geolocation) { this.locationMessage.set('Tarayıcınız konum özelliğini desteklemiyor. Şehir seçebilirsiniz.'); return; }
    this.locationMessage.set('Konumunuz alınıyor…');
    navigator.geolocation.getCurrentPosition(
      ({ coords }) => { this.locationMessage.set('25 km içindeki sinemalar gösteriliyor.'); this.load({ latitude: coords.latitude, longitude: coords.longitude }); },
      () => this.locationMessage.set('Konum izni verilmedi. Şehir seçerek devam edebilirsiniz.'),
      { enableHighAccuracy: false, timeout: 8000, maximumAge: 300000 },
    );
  }

  private load(filters: { cityId?: string; latitude?: number; longitude?: number }): void {
    this.loading.set(true); this.error.set(false);
    this.catalog.getCinemas(filters).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (cinemas) => { this.cinemas.set(cinemas); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
  }
}
