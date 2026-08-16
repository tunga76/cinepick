import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { MovieCatalogService, MovieDetail } from './movie-catalog.service';
import { UserMovieState, UserProfileService } from '../profile/user-profile.service';

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
  protected readonly movie = signal<MovieDetail | null>(null);
  protected readonly hasError = signal(false);
  protected readonly movieState = signal<UserMovieState | null>(null);
  protected readonly isAuthenticated = signal(false);
  protected readonly stateBusy = signal(false);
  protected readonly stateMessage = signal('');
  protected rating: number | null = null;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.hasError.set(true); return; }
    this.catalog.getById(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (movie) => this.movie.set(movie),
      error: () => this.hasError.set(true),
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
