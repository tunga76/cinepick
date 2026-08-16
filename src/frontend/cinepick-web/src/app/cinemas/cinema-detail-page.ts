import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { CinemaCatalogService, CinemaDetail, ShowtimeListItem } from './cinema-catalog.service';

@Component({ selector: 'app-cinema-detail-page', imports: [RouterLink], templateUrl: './cinema-detail-page.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class CinemaDetailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly catalog = inject(CinemaCatalogService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly cinema = signal<CinemaDetail | null>(null);
  protected readonly showtimes = signal<readonly ShowtimeListItem[]>([]);
  protected readonly error = signal(false);
  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.error.set(true); return; }
    forkJoin({ cinema: this.catalog.getCinema(id), showtimes: this.catalog.getShowtimes(id) })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: (result) => { this.cinema.set(result.cinema); this.showtimes.set(result.showtimes); }, error: () => this.error.set(true) });
  }
  protected istanbulTime(value: string): string {
    return new Intl.DateTimeFormat('tr-TR', { timeZone: 'Europe/Istanbul', weekday: 'short', day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' }).format(new Date(value));
  }
  protected price(showtime: ShowtimeListItem): string { return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: showtime.currency }).format(showtime.price); }
}
