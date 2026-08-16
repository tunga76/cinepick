import { Routes } from '@angular/router';
import { MovieCatalogPage } from './movies/movie-catalog-page';
import { MovieDetailPage } from './movies/movie-detail-page';
import { CinemaListPage } from './cinemas/cinema-list-page';
import { CinemaDetailPage } from './cinemas/cinema-detail-page';
import { DevelopmentAdminPage } from './development/development-admin-page';
import { AccountPage } from './auth/account-page';
import { ProfilePage } from './auth/profile-page';
import { authGuard } from './auth/auth.guard';
import { adminGuard } from './auth/admin.guard';

export const routes: Routes = [
  { path: '', component: MovieCatalogPage, title: 'CinePick | Vizyondaki Filmler' },
  { path: 'movies/:id', component: MovieDetailPage, title: 'CinePick | Film Detayı' },
  { path: 'cinemas', component: CinemaListPage, title: 'CinePick | Sinemalar' },
  { path: 'cinemas/:id', component: CinemaDetailPage, title: 'CinePick | Sinema Detayı' },
  { path: 'admin', component: DevelopmentAdminPage, canActivate: [adminGuard], title: 'CinePick | Yönetim' },
  { path: 'development/admin', redirectTo: 'admin' },
  { path: 'account', component: AccountPage, title: 'CinePick | Hesap' },
  { path: 'profile', component: ProfilePage, canActivate: [authGuard], title: 'CinePick | Profil' },
  { path: '**', redirectTo: '' },
];
