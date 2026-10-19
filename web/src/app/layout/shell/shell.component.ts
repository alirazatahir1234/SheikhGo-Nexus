import { Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd, RouterLink } from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { TopbarComponent } from '../topbar/topbar.component';
import { CommandPaletteComponent } from '../command-palette/command-palette.component';
import { NavService } from '../../core/nav/nav.service';
import { PosNotifyService } from '../../core/services/pos-notify.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, SidebarComponent, TopbarComponent, CommandPaletteComponent],
  template: `
    <a class="skip-link" href="#main-content">Skip to content</a>
    <div class="shell" [class.shell--collapsed]="collapsed()">
      <app-sidebar class="shell__sidebar" [collapsed]="collapsed()" />
      <div class="shell__main">
        <app-topbar (toggleSidebar)="collapsed.set(!collapsed())" (openSearch)="openPalette()" />
        <div class="shell__breadcrumb">
          @for (c of crumbs(); track $index; let last = $last) {
            @if (!last && c.route) {
              <a [routerLink]="c.route">{{ c.label }}</a>
              <span class="sep">/</span>
            } @else if (!last) {
              <span>{{ c.label }}</span>
              <span class="sep">/</span>
            } @else {
              <span class="current">{{ c.label }}</span>
            }
          }
        </div>
        <main id="main-content" class="shell__content" tabindex="-1">
          <router-outlet />
        </main>
      </div>
    </div>
    <app-command-palette />
    @if (notify.toast(); as t) {
      <div
        class="pos-toast"
        [class.pos-toast--status]="t.type === 'Status'"
        role="status"
        (click)="notify.dismiss()"
      >
        <strong>{{ t.type }}</strong>
        <span>{{ t.message }}</span>
      </div>
    }
    @if (notify.hubStatus() === 'Reconnecting') {
      <div class="pos-hub-chip" role="status">Live · reconnecting</div>
    }
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .pos-toast {
        position: fixed;
        right: 1rem;
        bottom: 1rem;
        z-index: 80;
        max-width: min(22rem, calc(100vw - 2rem));
        padding: 0.85rem 1rem;
        border-radius: 0.65rem;
        background: #0f766e;
        color: #fff;
        box-shadow: 0 10px 28px rgba(15, 23, 42, 0.18);
        display: flex;
        flex-direction: column;
        gap: 0.2rem;
        cursor: pointer;
        font-size: 0.9rem;
      }
      .pos-toast strong {
        font-size: 0.75rem;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        opacity: 0.85;
      }
      .pos-toast--status {
        background: #334155;
      }
      .pos-hub-chip {
        position: fixed;
        right: 1rem;
        bottom: 4.5rem;
        z-index: 79;
        padding: 0.35rem 0.7rem;
        border-radius: 999px;
        background: #92400e;
        color: #fff;
        font-size: 0.75rem;
        font-weight: 600;
        letter-spacing: 0.02em;
        box-shadow: 0 6px 16px rgba(15, 23, 42, 0.16);
      }
      .shell {
        display: grid;
        grid-template-columns: var(--sidebar-width) minmax(0, 1fr);
        min-height: 100dvh;
        background:
          radial-gradient(circle at top right, rgba(20, 184, 166, 0.08), transparent 28%),
          linear-gradient(180deg, var(--canvas) 0%, color-mix(in srgb, var(--canvas) 88%, #c5d4db) 100%);
        transition: grid-template-columns 0.2s ease;
      }
      .shell--collapsed {
        grid-template-columns: 4.5rem minmax(0, 1fr);
      }
      :host ::ng-deep .shell__sidebar,
      :host ::ng-deep app-sidebar {
        position: sticky;
        top: 0;
        height: 100dvh;
        align-self: start;
        overflow: hidden;
      }
      .shell__main {
        min-width: 0;
        display: flex;
        flex-direction: column;
        min-height: 100dvh;
      }
      .shell__breadcrumb {
        padding: 0.65rem 1.5rem 0;
        font-size: 0.8rem;
        color: var(--text-muted);
        display: flex;
        align-items: center;
        flex-wrap: wrap;
        gap: 0.15rem;
        max-width: var(--content-max);
        width: 100%;
        margin-inline: auto;
      }
      .shell__breadcrumb a {
        color: var(--text-muted);
        text-decoration: none;
      }
      .shell__breadcrumb a:hover {
        color: var(--slate-800);
      }
      .shell__breadcrumb .sep {
        margin: 0 0.35rem;
        opacity: 0.5;
      }
      .shell__breadcrumb .current {
        color: var(--slate-800);
        font-weight: 600;
      }
      .shell__content {
        padding: 1rem 1.5rem 1.5rem;
        flex: 1;
        max-width: var(--content-max);
        width: 100%;
        margin-inline: auto;
      }
      @media (max-width: 900px) {
        .shell,
        .shell--collapsed {
          grid-template-columns: 1fr;
        }
        :host ::ng-deep app-sidebar {
          display: none;
        }
      }
    `,
  ],
})
export class ShellComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly nav = inject(NavService);
  readonly notify = inject(PosNotifyService);
  private readonly palette = viewChild(CommandPaletteComponent);
  readonly collapsed = signal(false);

  readonly crumbs = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => this.nav.breadcrumbsForUrl(e.urlAfterRedirects)),
      startWith(this.nav.breadcrumbsForUrl(this.router.url)),
    ),
    { initialValue: this.nav.breadcrumbsForUrl('/dashboard') },
  );

  ngOnInit(): void {
    void this.notify.start();
  }

  openPalette(): void {
    this.palette()?.openPalette();
  }
}
