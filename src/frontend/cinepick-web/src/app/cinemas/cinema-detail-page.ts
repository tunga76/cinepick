import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { CinemaCatalogService, CinemaDetail, ShowtimeListItem } from './cinema-catalog.service';
import { istanbulDateKey, showtimeDateOptions } from './showtime-date';

@Component({ selector: 'app-cinema-detail-page', imports: [RouterLink], templateUrl: './cinema-detail-page.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class CinemaDetailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly catalog = inject(CinemaCatalogService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly cinema = signal<CinemaDetail | null>(null);
  protected readonly showtimes = signal<readonly ShowtimeListItem[]>([]);
  protected readonly selectedDate = signal('');
  protected readonly dateOptions = computed(() => showtimeDateOptions(this.showtimes()));
  protected readonly visibleShowtimes = computed(() => this.showtimes()
    .filter(item => !this.selectedDate() || istanbulDateKey(item.startsAt) === this.selectedDate()));
  protected readonly error = signal(false);
  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.error.set(true); return; }
    forkJoin({ cinema: this.catalog.getCinema(id), showtimes: this.catalog.getShowtimes(id) })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: (result) => {
        this.cinema.set(result.cinema); this.showtimes.set(result.showtimes);
        this.selectedDate.set(showtimeDateOptions(result.showtimes)[0]?.key ?? '');
      }, error: () => this.error.set(true) });
  }
  protected istanbulTime(value: string): string {
    return new Intl.DateTimeFormat('tr-TR', { timeZone: 'Europe/Istanbul', weekday: 'short', day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' }).format(new Date(value));
  }
  protected price(showtime: ShowtimeListItem): string { return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: showtime.currency }).format(showtime.price); }
}
