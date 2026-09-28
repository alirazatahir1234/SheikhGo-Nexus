import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated()) return true;
  return router.createUrlTree(['/login']);
};

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isAuthenticated()) return true;
  return router.createUrlTree(['/dashboard']);
};

/** Route data: { permission: 'Admin.Role.View' } or { permissions: ['A','B'], permissionMode: 'any'|'all' } */
export const permissionGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isAuthenticated()) return router.createUrlTree(['/login']);

  const single = route.data['permission'] as string | undefined;
  const many = route.data['permissions'] as string[] | undefined;
  const mode = (route.data['permissionMode'] as 'any' | 'all' | undefined) ?? 'any';

  if (single && auth.hasPermission(single)) return true;
  if (many?.length) {
    const ok = mode === 'all' ? many.every((p) => auth.hasPermission(p)) : many.some((p) => auth.hasPermission(p));
    if (ok) return true;
  }
  if (!single && !many?.length) return true;

  return router.createUrlTree(['/dashboard']);
};
