import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Route guard: only lets a navigation proceed if the user is logged in.
 * Attached to protected routes in app.routes.ts. If not authenticated,
 * it redirects to /login and cancels the navigation.
 */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated) {
    return true;
  }
  router.navigate(['/login']);
  return false;
};
