import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { GenreListItem, MovieCatalogService, MovieListItem, tmdbPosterUrl } from './movie-catalog.service';
import { RecommendationItem, RecommendationService } from '../recommendations/recommendation.service';

@Component({
  selector: 'app-movie-catalog-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './movie-catalog-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MovieCatalogPage implements OnInit {
  private readonly movieCatalog = inject(MovieCatalogService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly recommendationService = inject(RecommendationService);
  protected readonly movies = signal<readonly MovieListItem[]>([]);
  protected readonly upcomingPreview = signal<readonly MovieListItem[]>([]);
  protected readonly genres = signal<readonly GenreListItem[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = 12;
  protected readonly catalogMode = signal<'now-playing' | 'upcoming'>('now-playing');
  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));
  protected readonly isLoadingMovies = signal(true);
  protected readonly movieLoadError = signal(false);
  protected readonly upcomingPreviewLoading = signal(true);
  protected readonly upcomingPreviewError = signal(false);
  protected readonly brokenPosterIds = signal<ReadonlySet<string>>(new Set());
  protected readonly requestText = new FormControl("Yarın 18:00'den sonra Kadıköy'de 100 dakikadan kısa bir film", { nonNullable: true });
  protected readonly recommendations = signal<readonly RecommendationItem[]>([]);
  protected readonly candidateCount = signal(0);
  protected readonly recommendationLoading = signal(false);
  protected readonly recommendationError = signal(false);
  protected readonly hasRequestedRecommendation = signal(false);
  protected readonly filters = new FormGroup({
    search: new FormControl('', { nonNullable: true }),
    genreId: new FormControl('', { nonNullable: true }),
    maximumRuntimeMinutes: new FormControl('', { nonNullable: true }),
  });

  ngOnInit(): void {
    forkJoin({ movies: this.movieCatalog.getNowPlaying({}, 1, this.pageSize), genres: this.movieCatalog.getGenres() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ movies, genres }) => {
          this.movies.set(movies.items);
          this.totalCount.set(movies.totalCount);
          this.page.set(movies.page);
          this.genres.set(genres);
          this.isLoadingMovies.set(false);
        },
        error: () => { this.movieLoadError.set(true); this.isLoadingMovies.set(false); },
      });

    this.movieCatalog.getUpcoming({}, 1, 4)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: response => {
          this.upcomingPreview.set(response.items);
          this.upcomingPreviewLoading.set(false);
        },
        error: () => {
          this.upcomingPreviewError.set(true);
          this.upcomingPreviewLoading.set(false);
        },
      });
  }

  protected loadMovies(requestedPage = 1): void {
    const values = this.filters.getRawValue();
    this.isLoadingMovies.set(true);
    this.movieLoadError.set(false);
    const request = this.catalogMode() === 'now-playing'
      ? this.movieCatalog.getNowPlaying.bind(this.movieCatalog)
      : this.movieCatalog.getUpcoming.bind(this.movieCatalog);
    request({
      search: values.search.trim() || undefined,
      genreId: values.genreId || undefined,
      maximumRuntimeMinutes: values.maximumRuntimeMinutes ? Number(values.maximumRuntimeMinutes) : undefined,
    }, requestedPage, this.pageSize).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        this.movies.set(response.items);
        this.totalCount.set(response.totalCount);
        this.page.set(response.page);
        this.isLoadingMovies.set(false);
      },
      error: () => { this.movieLoadError.set(true); this.isLoadingMovies.set(false); },
    });
  }

  protected resetFilters(): void { this.filters.reset(); this.loadMovies(); }
  protected goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) return;
    this.loadMovies(page);
  }
  protected switchCatalog(mode: 'now-playing' | 'upcoming'): void {
    if (mode === this.catalogMode()) return;
    this.catalogMode.set(mode);
    this.loadMovies(1);
  }
  protected showAllUpcoming(): void {
    if (this.catalogMode() !== 'upcoming') {
      this.catalogMode.set('upcoming');
      this.loadMovies(1);
    }
  }
  protected releaseDate(value: string): string {
    return new Intl.DateTimeFormat('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' })
      .format(new Date(`${value}T00:00:00Z`));
  }
  protected ageLabel(ageRating: number): string { return ageRating === 0 ? 'Genel İzleyici' : `${ageRating}+`; }
  protected posterUrl(movie: MovieListItem): string | null {
    return this.brokenPosterIds().has(movie.id) ? null : tmdbPosterUrl(movie.posterPath, 'w342');
  }
  protected markPosterBroken(movieId: string): void {
    this.brokenPosterIds.update(current => new Set([...current, movieId]));
  }
  protected retryPosters(): void {
    this.brokenPosterIds.set(new Set());
  }
  protected recommend(): void {
    if (this.recommendationLoading()) return;
    const text = this.requestText.value.trim();
    if (!text) return;
    this.hasRequestedRecommendation.set(true);
    this.recommendationLoading.set(true); this.recommendationError.set(false);
    this.recommendationService.recommend(text).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => { this.recommendations.set(response.items); this.candidateCount.set(response.candidateCount); this.recommendationLoading.set(false); },
      error: () => { this.recommendationError.set(true); this.recommendationLoading.set(false); },
    });
  }
  protected istanbulTime(value: string): string {
    return new Intl.DateTimeFormat('tr-TR', { timeZone: 'Europe/Istanbul', weekday: 'short', hour: '2-digit', minute: '2-digit' }).format(new Date(value));
  }
  protected price(item: RecommendationItem): string {
    return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: item.currency }).format(item.price);
  }
}
