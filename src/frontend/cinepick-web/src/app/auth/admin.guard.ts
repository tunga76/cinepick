import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from './auth.service';

export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.refresh().pipe(map(user => user?.roles.includes('Admin')
    ? true : router.createUrlTree(['/account'], { queryParams: { returnUrl: '/admin' } })));
};
