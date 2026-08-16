import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { forkJoin, Observable } from 'rxjs';
import { DevelopmentAdminService, DevelopmentShowtimeItem, SyncLogItem, SyncResult } from './development-admin.service';

@Component({ selector: 'app-development-admin-page', imports: [RouterLink], templateUrl: './development-admin-page.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class DevelopmentAdminPage implements OnInit {
  private readonly service = inject(DevelopmentAdminService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly logs = signal<readonly SyncLogItem[]>([]);
  protected readonly showtimes = signal<readonly DevelopmentShowtimeItem[]>([]);
  protected readonly busy = signal(false);
  protected readonly error = signal(false);
  protected readonly message = signal('');
  ngOnInit(): void { this.reload(); }

  protected syncMovies(): void { this.runSync(this.service.syncMovies()); }
  protected syncShowtimes(): void { this.runSync(this.service.syncShowtimes()); }
  protected toggleCancellation(item: DevelopmentShowtimeItem): void {
    this.busy.set(true);
    this.service.setCancellation(item.id, !item.isCancelled)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => { this.message.set(item.isCancelled ? 'Seans yeniden açıldı.' : 'Seans iptal edildi.'); this.reload(); },
        error: () => { this.error.set(true); this.busy.set(false); },
      });
  }
  protected istanbulTime(value: string): string {
    return new Intl.DateTimeFormat('tr-TR', { timeZone: 'Europe/Istanbul', dateStyle: 'short', timeStyle: 'short' }).format(new Date(value));
  }

  private runSync(request: Observable<SyncResult>): void {
    this.busy.set(true); this.error.set(false); this.message.set('');
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => { this.message.set(`${result.providerId}: ${result.insertedCount} eklendi, ${result.updatedCount} güncellendi.`); this.reload(); },
      error: () => { this.error.set(true); this.busy.set(false); },
    });
  }
  private reload(): void {
    this.busy.set(true);
    forkJoin({ logs: this.service.getLogs(), showtimes: this.service.getShowtimes() })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: ({ logs, showtimes }) => { this.logs.set(logs); this.showtimes.set(showtimes); this.busy.set(false); },
        error: () => { this.error.set(true); this.busy.set(false); },
      });
  }
}
