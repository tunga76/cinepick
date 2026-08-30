import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { MovieCatalogService, MovieDetail, tmdbPosterUrl } from './movie-catalog.service';
import { UserMovieState, UserProfileService } from '../profile/user-profile.service';
import { CinemaCatalogService, ShowtimeListItem } from '../cinemas/cinema-catalog.service';
import { istanbulDateKey, matchesShowtimePeriod, showtimeDateOptions, showtimeFacetOptions, showtimePriceOptions, ShowtimePeriod, ShowtimeSort, sortShowtimes } from '../cinemas/showtime-date';

@Component({
  selector: 'app-movie-detail-page',
  imports: [RouterLink, FormsModule],
  templateUrl: './movie-detail-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MovieDetailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly catalog = inject(MovieCatalogService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly profile = inject(UserProfileService);
  private readonly cinemas = inject(CinemaCatalogService);
  protected readonly movie = signal<MovieDetail | null>(null);
  protected readonly hasError = signal(false);
  protected readonly movieState = signal<UserMovieState | null>(null);
  protected readonly isAuthenticated = signal(false);
  protected readonly stateBusy = signal(false);
  protected readonly stateMessage = signal('');
  protected readonly posterFailed = signal(false);
  protected readonly showtimes = signal<readonly ShowtimeListItem[]>([]);
  protected readonly selectedDate = signal('');
  protected readonly selectedLanguage = signal('');
  protected readonly selectedFormat = signal('');
  protected readonly selectedCinema = signal('');
  protected readonly selectedSort = signal<ShowtimeSort>('time');
  protected readonly maximumPrice = signal<number | null>(null);
  protected readonly selectedPeriod = signal<ShowtimePeriod>('all');
  protected readonly dateOptions = computed(() => showtimeDateOptions(this.showtimes()));
  protected readonly languageOptions = computed(() => showtimeFacetOptions(this.showtimes(), this.selectedDate(), 'language'));
  protected readonly formatOptions = computed(() => showtimeFacetOptions(this.showtimes(), this.selectedDate(), 'format'));
  protected readonly cinemaOptions = computed(() => showtimeFacetOptions(this.showtimes(), this.selectedDate(), 'cinemaName'));
  protected readonly priceOptions = computed(() => showtimePriceOptions(this.showtimes(), this.selectedDate()));
  protected readonly visibleShowtimes = computed(() => sortShowtimes(this.showtimes()
    .filter(item => (!this.selectedDate() || istanbulDateKey(item.startsAt) === this.selectedDate())
      && (!this.selectedLanguage() || item.language === this.selectedLanguage())
      && (!this.selectedFormat() || item.format === this.selectedFormat())
      && (!this.selectedCinema() || item.cinemaName === this.selectedCinema())
      && (this.maximumPrice() === null || item.price <= this.maximumPrice()!)
      && matchesShowtimePeriod(item.startsAt, this.selectedPeriod())), this.selectedSort()));
  protected readonly showtimesLoading = signal(true);
  protected readonly showtimesError = signal(false);
  protected rating: number | null = null;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.hasError.set(true); return; }
    this.catalog.getById(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (movie) => this.movie.set(movie),
      error: () => this.hasError.set(true),
    });
    this.cinemas.getMovieShowtimes(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: showtimes => {
        this.showtimes.set(showtimes);
        this.selectedDate.set(showtimeDateOptions(showtimes)[0]?.key ?? '');
        this.showtimesLoading.set(false);
      },
      error: () => { this.showtimesError.set(true); this.showtimesLoading.set(false); },
    });
    this.profile.getMovieState(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: state => { this.isAuthenticated.set(true); this.setState(state); },
      error: (error: HttpErrorResponse) => {
        if (error.status === 404) this.isAuthenticated.set(true);
        else if (error.status !== 401) this.stateMessage.set('Film durumun yüklenemedi.');
      },
    });
  }

  protected ageLabel(ageRating: number): string { return ageRating === 0 ? 'Genel İzleyici' : `${ageRating}+`; }
  protected posterUrl(movie: MovieDetail): string | null {
    return this.posterFailed() ? null : tmdbPosterUrl(movie.posterPath);
  }
  protected istanbulTime(value: string): string {
    return new Intl.DateTimeFormat('tr-TR', {
      timeZone: 'Europe/Istanbul', weekday: 'short', day: 'numeric', month: 'short',
      hour: '2-digit', minute: '2-digit',
    }).format(new Date(value));
  }
  protected price(showtime: ShowtimeListItem): string {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency', currency: showtime.currency,
    }).format(showtime.price);
  }
  protected priceLimit(value: number): string {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency', currency: 'TRY', maximumFractionDigits: 0,
    }).format(value);
  }
  protected selectDate(date: string): void {
    this.selectedDate.set(date); this.selectedLanguage.set(''); this.selectedFormat.set('');
    this.selectedCinema.set(''); this.maximumPrice.set(null); this.selectedPeriod.set('all');
  }
  protected resetShowtimeFilters(): void {
    this.selectedCinema.set('');
    this.selectedLanguage.set('');
    this.selectedFormat.set('');
    this.maximumPrice.set(null);
    this.selectedPeriod.set('all');
    this.selectedSort.set('time');
  }

  protected saveState(change: Partial<Pick<UserMovieState, 'isFavorite' | 'isWatched'>> = {}): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    const current = this.movieState();
    this.stateBusy.set(true); this.stateMessage.set('');
    this.profile.updateMovieState(id, {
      isFavorite: change.isFavorite ?? current?.isFavorite ?? false,
      isWatched: change.isWatched ?? current?.isWatched ?? false,
      rating: this.rating,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: state => { this.setState(state); this.stateBusy.set(false); this.stateMessage.set('Tercihin kaydedildi.'); },
      error: () => { this.stateBusy.set(false); this.stateMessage.set('Tercihin kaydedilemedi.'); },
    });
  }

  private setState(state: UserMovieState): void {
    this.movieState.set(state);
    this.rating = state.rating;
  }
}
