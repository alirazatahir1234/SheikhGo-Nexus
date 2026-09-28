import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
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
      .shell__content {
        padding: 1.5rem;
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
  readonly collapsed = signal(false);
}
