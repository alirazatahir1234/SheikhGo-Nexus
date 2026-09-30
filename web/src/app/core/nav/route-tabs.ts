import { DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, NavigationEnd, Router, Routes, Route } from '@angular/router';
import { filter } from 'rxjs';
import { Type } from '@angular/core';

/** Bind component tab (+ optional :id) from the active route's `data.tab` / `paramMap`. */
export function bindTabFromRoute<T extends string>(
  apply: (tab: T, id: string | null) => void,
  destroyRef = inject(DestroyRef),
  route = inject(ActivatedRoute),
  router = inject(Router),
): void {
  const sync = (): void => {
    const tab = (route.snapshot.data['tab'] as T | undefined) ?? null;
    const id = route.snapshot.paramMap.get('id');
    if (tab) apply(tab, id);
  };
  sync();
  router.events
    .pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      takeUntilDestroyed(destroyRef),
    )
    .subscribe(() => sync());
}

/** Build componentless parent children that all load the same feature component. */
export function tabChildren(
  load: () => Promise<Record<string, Type<unknown>>>,
  exportName: string,
  defs: ReadonlyArray<{ path: string; tab: string }>,
  defaultPath: string,
): Routes {
  return [
    { path: '', pathMatch: 'full', redirectTo: defaultPath },
    ...defs.map(
      (d): Route => ({
        path: d.path,
        loadComponent: () => load().then((m) => m[exportName]),
        data: { tab: d.tab },
      }),
    ),
  ];
}

export function navigateModuleTab(
  router: Router,
  base: string,
  segment: string,
  id?: string | null,
): void {
  const commands = id ? ['/', base, segment, id] : ['/', base, segment];
  void router.navigate(commands);
}
