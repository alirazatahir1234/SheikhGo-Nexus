import { Component, inject, input, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavService } from '../../core/nav/nav.service';
import { NavItem } from '../../core/nav/nav.config';
import { NavIconComponent } from '../../core/nav/nav-icon.component';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, NavIconComponent],
  template: `
    <aside class="sidebar" [class.sidebar--collapsed]="collapsed()">
      <div class="sidebar__brand">
        <span class="sidebar__mark">◈</span>
        @if (!collapsed()) {
          <div>
            <strong>SheikhGo-Nexus-ERP</strong>
            <small>Enterprise ERP</small>
          </div>
        }
      </div>

      <nav class="sidebar__nav">
        @for (section of visibleTree(); track section.id) {
          @if (section.section && !collapsed()) {
            <div class="sidebar__section">{{ section.section }}</div>
          }
          @for (item of section.children ?? [section]; track item.id) {
            @if (item.children?.length) {
              <div class="sidebar__group" [class.is-open]="isExpanded(item.id)">
                <button
                  type="button"
                  class="sidebar__group-btn"
                  [title]="item.label"
                  [attr.aria-expanded]="isExpanded(item.id)"
                  [attr.aria-controls]="'nav-group-' + item.id"
                  (click)="toggle(item.id)"
                >
                  <app-nav-icon [name]="item.icon" />
                  @if (!collapsed()) {
                    <span class="sidebar__label">{{ item.label }}</span>
                    @if (badge(item); as n) {
                      @if (n > 0) {
                        <span class="sidebar__badge">{{ n }}</span>
                      }
                    }
                    <app-nav-icon class="sidebar__chevron" [name]="isExpanded(item.id) ? 'chevron-down' : 'chevron-right'" [size]="14" />
                  }
                </button>
                @if (isExpanded(item.id) && !collapsed()) {
                  <div class="sidebar__children" [id]="'nav-group-' + item.id" role="group" [attr.aria-label]="item.label">
                    @for (child of item.children; track child.id) {
                      @if (child.route) {
                        <a
                          [routerLink]="child.route"
                          routerLinkActive="is-active"
                          [title]="child.label"
                        >
                          <app-nav-icon [name]="child.icon" [size]="16" />
                          <span>{{ child.label }}</span>
                          @if (badge(child); as n) {
                            @if (n > 0) {
                              <span class="sidebar__badge">{{ n }}</span>
                            }
                          }
                        </a>
                      }
                    }
                  </div>
                }
              </div>
            } @else if (item.route) {
              <a [routerLink]="item.route" routerLinkActive="is-active" [title]="item.label">
                <app-nav-icon [name]="item.icon" />
                @if (!collapsed()) {
                  <span>{{ item.label }}</span>
                  @if (badge(item); as n) {
                    @if (n > 0) {
                      <span class="sidebar__badge">{{ n }}</span>
                    }
                  }
                }
              </a>
            }
          }
        }
      </nav>
    </aside>
  `,
  styles: [
    `
      :host {
        display: block;
        height: 100%;
      }
      .sidebar {
        width: var(--sidebar-width);
        height: 100%;
        background: linear-gradient(180deg, #ffffff 0%, #f3f8f9 100%);
        color: var(--text);
        display: flex;
        flex-direction: column;
        border-right: 1px solid var(--border);
        transition: width 0.2s ease;
        overflow: hidden;
      }
      .sidebar--collapsed {
        width: 4.5rem;
      }
      .sidebar__brand {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        padding: 1.25rem 1rem;
        border-bottom: 1px solid var(--border);
      }
      .sidebar__mark {
        width: 2.4rem;
        height: 2.4rem;
        border-radius: 0.55rem;
        background: linear-gradient(135deg, #0d9488, #14b8a6);
        color: #042f2e;
        display: grid;
        place-items: center;
        font-family: var(--font-display);
        font-weight: 800;
        font-size: 1.1rem;
        flex-shrink: 0;
      }
      .sidebar__brand strong {
        display: block;
        font-family: var(--font-display);
        font-size: 0.95rem;
        letter-spacing: -0.02em;
        color: var(--slate-900);
        line-height: 1.2;
      }
      .sidebar__brand small {
        color: var(--teal-600);
        font-size: 0.65rem;
        letter-spacing: 0.1em;
        text-transform: uppercase;
      }
      .sidebar__section {
        font-size: 0.65rem;
        letter-spacing: 0.1em;
        text-transform: uppercase;
        color: var(--text-muted);
        padding: 0.85rem 0.75rem 0.35rem;
      }
      .sidebar__nav {
        display: flex;
        flex-direction: column;
        gap: 0.15rem;
        padding: 0.75rem 0.65rem 1.5rem;
        overflow-y: auto;
        overscroll-behavior: contain;
        flex: 1;
        min-height: 0;
      }
      a,
      .sidebar__group-btn {
        display: flex;
        align-items: center;
        gap: 0.7rem;
        padding: 0.65rem 0.75rem;
        border-radius: var(--radius-sm);
        color: var(--slate-700);
        text-decoration: none;
        font-size: 0.9rem;
        font-weight: 500;
        transition: background 0.15s ease, color 0.15s ease;
        width: 100%;
        border: none;
        background: transparent;
        cursor: pointer;
        text-align: left;
        font-family: inherit;
      }
      a:hover,
      .sidebar__group-btn:hover {
        background: color-mix(in srgb, var(--teal-500) 10%, transparent);
        color: var(--slate-900);
      }
      a.is-active {
        background: color-mix(in srgb, var(--teal-500) 16%, transparent);
        color: var(--teal-600);
        font-weight: 600;
      }
      .sidebar__label {
        flex: 1;
      }
      .sidebar__chevron {
        margin-left: auto;
        opacity: 0.6;
      }
      .sidebar__children {
        display: flex;
        flex-direction: column;
        gap: 0.1rem;
        padding: 0.15rem 0 0.35rem 0.85rem;
      }
      .sidebar__children a {
        font-size: 0.82rem;
        padding: 0.45rem 0.65rem;
      }
      .sidebar__badge {
        margin-left: auto;
        background: #ef4444;
        color: #fff;
        font-size: 0.65rem;
        font-weight: 700;
        min-width: 1.25rem;
        height: 1.25rem;
        border-radius: 999px;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        padding: 0 0.35rem;
      }
    `,
  ],
})
export class SidebarComponent {
  private readonly nav = inject(NavService);
  private readonly router = inject(Router);
  readonly collapsed = input(false);

  readonly visibleTree = this.nav.visibleTree;
  readonly expanded = signal<Set<string>>(new Set());

  constructor() {
    this.expandForUrl(this.router.url);
    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe((e) => this.expandForUrl(e.urlAfterRedirects));
  }

  toggle(id: string): void {
    this.expanded.update((set) => {
      const next = new Set(set);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  isExpanded(id: string): boolean {
    return this.expanded().has(id);
  }

  badge(item: NavItem): number {
    return this.nav.badgeCount(item);
  }

  private expandForUrl(url: string): void {
    const path = url.split('?')[0];
    const next = new Set(this.expanded());
    for (const section of this.visibleTree()) {
      for (const item of section.children ?? []) {
        const matches =
          (item.route && (path === item.route || path.startsWith(item.route + '/'))) ||
          item.children?.some((c) => c.route && (path === c.route || path.startsWith(c.route + '/')));
        if (matches) next.add(item.id);
      }
    }
    this.expanded.set(next);
  }
}
