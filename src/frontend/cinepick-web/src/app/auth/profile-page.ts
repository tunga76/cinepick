import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { AuthService } from './auth.service';
import { RecommendationHistoryItem, UserMovieState, UserProfileService } from '../profile/user-profile.service';

@Component({ selector: 'app-profile-page', imports: [RouterLink, ReactiveFormsModule], templateUrl: './profile-page.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class ProfilePage implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly profileService = inject(UserProfileService);
  protected readonly user = this.authService.currentUser;
  protected readonly busy = signal(false);
  protected readonly loading = signal(false);
  protected readonly loadError = signal('');
  protected readonly movieStates = signal<readonly UserMovieState[]>([]);
  protected readonly history = signal<readonly RecommendationHistoryItem[]>([]);
  protected readonly preferenceMessage = signal('');
  protected readonly savingPreferences = signal(false);
  protected readonly preferences = new FormGroup({
    preferredGenreSlug: new FormControl('', { nonNullable: true }),
    preferredLanguage: new FormControl('', { nonNullable: true }),
    maximumRuntimeMinutes: new FormControl<number | null>(null),
    maximumDistanceKilometers: new FormControl<number | null>(null),
  });

  ngOnInit(): void {
    this.loadProfile();
  }

  protected loadProfile(): void {
    if (this.loading()) return;
    this.loading.set(true);
    this.loadError.set('');
    forkJoin({ states: this.profileService.getMovieStates(),
      preferences: this.profileService.getPreferences(),
      history: this.profileService.getRecommendationHistory() })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: ({ states, preferences, history }) => {
        this.movieStates.set(states); this.history.set(history);
        this.preferences.setValue({
          preferredGenreSlug: preferences.preferredGenreSlug ?? '',
          preferredLanguage: preferences.preferredLanguage ?? '',
          maximumRuntimeMinutes: preferences.maximumRuntimeMinutes,
          maximumDistanceKilometers: preferences.maximumDistanceKilometers,
        });
        this.loading.set(false);
      }, error: () => {
        this.loadError.set('Profil bilgileri yüklenemedi. Lütfen tekrar dene.');
        this.loading.set(false);
      },
      });
  }

  protected savePreferences(): void {
    if (this.loading() || this.loadError() || this.savingPreferences()) return;
    this.preferenceMessage.set('');
    const value = this.preferences.getRawValue();
    this.savingPreferences.set(true);
    this.preferences.disable();
    this.profileService.updatePreferences({
      preferredGenreSlug: value.preferredGenreSlug || null,
      preferredLanguage: value.preferredLanguage || null,
      maximumRuntimeMinutes: value.maximumRuntimeMinutes,
      maximumDistanceKilometers: value.maximumDistanceKilometers,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.finishPreferenceSave('Tercihlerin kaydedildi.'),
      error: () => this.finishPreferenceSave('Tercihler kaydedilemedi. Tekrar deneyebilirsin.'),
    });
  }

  private finishPreferenceSave(message: string): void {
    this.preferences.enable();
    this.savingPreferences.set(false);
    this.preferenceMessage.set(message);
  }

  protected istanbulTime(value: string): string {
    return new Intl.DateTimeFormat('tr-TR', { timeZone: 'Europe/Istanbul',
      dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
  }

  protected logout(): void {
    this.busy.set(true);
    this.authService.logout().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => void this.router.navigateByUrl('/'),
      error: () => this.busy.set(false),
    });
  }
}
