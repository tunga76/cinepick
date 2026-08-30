import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal, ViewEncapsulation } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './auth/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
})
export class App implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly user = this.auth.currentUser;
  protected readonly isAdmin = computed(() => this.user()?.roles.includes('Admin') ?? false);
  protected readonly menuOpen = signal(false);
  protected readonly logoutBusy = signal(false);
  protected readonly logoutError = signal('');

  ngOnInit(): void { this.auth.refresh().subscribe({ error: () => undefined }); }

  protected closeMenu(): void { this.menuOpen.set(false); }
  protected dismissMenu(event: Event, toggle: HTMLButtonElement): void {
    if (!this.menuOpen()) return;
    event.preventDefault();
    event.stopPropagation();
    this.closeMenu();
    toggle.focus();
  }
  protected logout(): void {
    if (this.logoutBusy()) return;
    this.logoutError.set('');
    this.logoutBusy.set(true);
    this.auth.logout().subscribe({
      next: () => { this.logoutBusy.set(false); this.closeMenu(); void this.router.navigateByUrl('/'); },
      error: () => {
        this.logoutBusy.set(false);
        this.logoutError.set('Çıkış işlemi doğrulanamadı. Tekrar Çıkış düğmesine basarak deneyebilirsin.');
      },
    });
  }
}
