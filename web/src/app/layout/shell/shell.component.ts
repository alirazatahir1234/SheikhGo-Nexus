import { Component, inject, signal } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { TopbarComponent } from '../topbar/topbar.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, TopbarComponent],
  template: `
    <div class="shell" [class.shell--collapsed]="collapsed()">
      <app-sidebar [collapsed]="collapsed()" />
      <div class="shell__main">
        <app-topbar (toggleSidebar)="collapsed.set(!collapsed())" />
        <div class="shell__breadcrumb">
          <span>SheikhGo Nexus</span>
          <span class="sep">/</span>
          <span class="current">{{ crumb() }}</span>
        </div>
        <main class="shell__content">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
  styles: [
    `
      .shell {
        display: grid;
        grid-template-columns: var(--sidebar-width) 1fr;
        min-height: 100vh;
        background:
          radial-gradient(circle at top right, rgba(20, 184, 166, 0.08), transparent 28%),
          linear-gradient(180deg, var(--canvas) 0%, #e8eef1 100%);
        transition: grid-template-columns 0.2s ease;
      }
      .shell--collapsed {
        grid-template-columns: 4.5rem 1fr;
      }
      .shell__main {
        min-width: 0;
        display: flex;
        flex-direction: column;
      }
      .shell__breadcrumb {
        padding: 0.65rem 1.5rem 0;
        font-size: 0.8rem;
        color: var(--text-muted);
      }
      .shell__breadcrumb .sep {
        margin: 0 0.35rem;
        opacity: 0.5;
      }
      .shell__breadcrumb .current {
        color: var(--slate-800);
        font-weight: 600;
        text-transform: capitalize;
      }
      .shell__content {
        padding: 1rem 1.5rem 1.5rem;
        flex: 1;
      }
      @media (max-width: 900px) {
        .shell,
        .shell--collapsed {
          grid-template-columns: 1fr;
        }
        app-sidebar {
          display: none;
        }
      }
    `,
  ],
})
export class ShellComponent {
  private readonly router = inject(Router);
  readonly collapsed = signal(false);

  readonly crumb = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => {
        const parts = e.urlAfterRedirects.split('/').filter(Boolean);
        return parts[parts.length - 1]?.replace(/-/g, ' ') || 'Dashboard';
      }),
      startWith('Dashboard'),
    ),
    { initialValue: 'Dashboard' },
  );
}
