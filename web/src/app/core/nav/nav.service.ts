import { Injectable, computed, inject, signal } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { DEFAULT_ENABLED_MODULES, NAV_TREE, NavItem } from './nav.config';
import { NavBadgeService } from './nav-badge.service';

const MODULES_KEY = 'nexus_enabled_modules';

@Injectable({ providedIn: 'root' })
export class NavService {
  private readonly auth = inject(AuthService);
  private readonly badges = inject(NavBadgeService);

  private readonly modulesSignal = signal<string[]>(this.readModules());

  readonly enabledModules = this.modulesSignal.asReadonly();

  readonly visibleTree = computed(() => this.filterTree(NAV_TREE));

  setEnabledModules(modules: string[]): void {
    const next = modules.length ? modules : [...DEFAULT_ENABLED_MODULES];
    this.modulesSignal.set(next);
    localStorage.setItem(MODULES_KEY, JSON.stringify(next));
  }

  /** Flat leaf items with routes (for command palette). */
  visibleLeaves(): NavItem[] {
    const out: NavItem[] = [];
    const walk = (items: NavItem[], trail: string[]) => {
      for (const item of items) {
        if (!this.isVisible(item)) continue;
        const nextTrail = [...trail, item.label];
        if (item.route) {
          out.push({ ...item, label: nextTrail.join(' → ') });
        }
        if (item.children?.length) walk(item.children, nextTrail);
      }
    };
    walk(this.visibleTree(), []);
    return out;
  }

  /** Breadcrumb trail for a URL path. */
  breadcrumbsForUrl(url: string): { label: string; route?: string }[] {
    const path = url.split('?')[0];
    const found = this.findTrail(NAV_TREE, path, []);
    if (found?.length) {
      // Deduplicate consecutive identical labels (e.g. Overview section + Overview leaf)
      const trail: { label: string; route?: string }[] = [];
      for (const n of found) {
        const prev = trail[trail.length - 1];
        if (prev && prev.label === n.label && prev.route === n.route) continue;
        // Skip section-only nodes that only repeat the next child's context without a route,
        // when the next leaf is "Dashboard" under "Overview" — keep Overview once as home.
        trail.push({ label: n.label, route: n.route });
      }
      // If first crumb has no route and second is /dashboard, point first at dashboard
      if (trail.length >= 2 && !trail[0].route && trail[1].route === '/dashboard') {
        trail[0] = { label: trail[0].label, route: '/dashboard' };
        // Drop redundant "Dashboard" when it duplicates Overview home
        if (trail[0].label === 'Overview' && trail[1].label === 'Dashboard') {
          return [trail[0]];
        }
      }
      return trail;
    }
    const parts = path.split('/').filter(Boolean);
    const last = parts[parts.length - 1]?.replace(/-/g, ' ');
    return last
      ? [{ label: 'Overview', route: '/dashboard' }, { label: last }]
      : [{ label: 'Overview', route: '/dashboard' }];
  }

  badgeCount(item: NavItem): number {
    return item.badgeKey ? this.badges.count(item.badgeKey) : 0;
  }

  private readModules(): string[] {
    try {
      const raw = localStorage.getItem(MODULES_KEY);
      if (raw) {
        const parsed = JSON.parse(raw) as string[];
        if (Array.isArray(parsed) && parsed.length) return parsed;
      }
    } catch {
      /* ignore */
    }
    return [...DEFAULT_ENABLED_MODULES];
  }

  private isVisible(item: NavItem): boolean {
    if (item.permissions?.length && !this.auth.hasAnyPermission(...item.permissions)) {
      return false;
    }
    if (item.modules?.length) {
      const enabled = this.modulesSignal();
      if (!item.modules.some((m) => enabled.includes(m))) return false;
    }
    return true;
  }

  private filterTree(items: NavItem[]): NavItem[] {
    const result: NavItem[] = [];
    for (const item of items) {
      if (!this.isVisible(item)) continue;
      const children = item.children ? this.filterTree(item.children) : undefined;
      if (item.route || (children && children.length > 0)) {
        result.push({ ...item, children });
      }
    }
    return result;
  }

  private findTrail(items: NavItem[], path: string, parents: NavItem[]): NavItem[] | null {
    for (const item of items) {
      const chain = [...parents, item];
      if (item.route && this.routeMatches(path, item.route)) return chain;
      if (item.children) {
        const hit = this.findTrail(item.children, path, chain);
        if (hit) return hit;
      }
    }
    return null;
  }

  private routeMatches(url: string, route: string): boolean {
    if (url === route || url.startsWith(route + '/')) return true;
    // Match parent module path prefixes for detail routes
    const routeBase = route.replace(/\/[^/]+$/, '');
    if (route.endsWith('/orders') || route.endsWith('/payroll')) {
      return url.startsWith(route);
    }
    return false;
  }
}
