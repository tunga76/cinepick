import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { GenreListItem, MovieCatalogService, MovieListItem } from './movie-catalog.service';
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
  protected readonly genres = signal<readonly GenreListItem[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly isLoadingMovies = signal(true);
  protected readonly movieLoadError = signal(false);
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
    forkJoin({ movies: this.movieCatalog.getNowPlaying(), genres: this.movieCatalog.getGenres() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ movies, genres }) => {
          this.movies.set(movies.items);
          this.totalCount.set(movies.totalCount);
          this.genres.set(genres);
          this.isLoadingMovies.set(false);
        },
        error: () => { this.movieLoadError.set(true); this.isLoadingMovies.set(false); },
      });
  }

  protected loadMovies(): void {
    const values = this.filters.getRawValue();
    this.isLoadingMovies.set(true);
    this.movieLoadError.set(false);
    this.movieCatalog.getNowPlaying({
      search: values.search.trim() || undefined,
      genreId: values.genreId || undefined,
      maximumRuntimeMinutes: values.maximumRuntimeMinutes ? Number(values.maximumRuntimeMinutes) : undefined,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        this.movies.set(response.items);
        this.totalCount.set(response.totalCount);
        this.isLoadingMovies.set(false);
      },
      error: () => { this.movieLoadError.set(true); this.isLoadingMovies.set(false); },
    });
  }

  protected resetFilters(): void { this.filters.reset(); this.loadMovies(); }
  protected ageLabel(ageRating: number): string { return ageRating === 0 ? 'Genel İzleyici' : `${ageRating}+`; }
  protected recommend(): void {
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
