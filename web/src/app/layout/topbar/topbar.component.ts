import { Component, output } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  template: `
    <header class="topbar">
      <button type="button" class="topbar__toggle" (click)="toggleSidebar.emit()" aria-label="Toggle sidebar">
        ☰
      </button>

      <div class="topbar__company">
        <span class="topbar__eyebrow">Company</span>
        <strong>{{ companyName }}</strong>
      </div>

      <div class="topbar__spacer"></div>

      <div class="topbar__user">
        <div class="topbar__meta">
          <strong>{{ userName }}</strong>
          <small>{{ userRole }}</small>
        </div>
        <button type="button" class="btn btn--ghost" (click)="logout()">Sign out</button>
      </div>
    </header>
  `,
  styles: [
    `
      .topbar {
        height: var(--topbar-height);
        display: flex;
        align-items: center;
        gap: 1rem;
        padding: 0 1.25rem;
        background: rgba(255, 255, 255, 0.82);
        backdrop-filter: blur(10px);
        border-bottom: 1px solid var(--border);
        position: sticky;
        top: 0;
        z-index: 20;
      }
      .topbar__toggle {
        border: 1px solid var(--border);
        background: var(--surface);
        width: 2.25rem;
        height: 2.25rem;
        border-radius: var(--radius-sm);
        cursor: pointer;
        color: var(--slate-700);
      }
      .topbar__eyebrow {
        display: block;
        font-size: 0.68rem;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        color: var(--text-muted);
      }
      .topbar__company strong {
        font-family: var(--font-display);
        color: var(--slate-900);
      }
      .topbar__spacer {
        flex: 1;
      }
      .topbar__user {
        display: flex;
        align-items: center;
        gap: 0.85rem;
      }
      .topbar__meta {
        text-align: right;
      }
      .topbar__meta strong {
        display: block;
        font-size: 0.92rem;
      }
      .topbar__meta small {
        color: var(--text-muted);
      }
    `,
  ],
})
export class TopbarComponent {
  readonly toggleSidebar = output<void>();

  constructor(private readonly auth: AuthService) {}

  get companyName(): string {
    return this.auth.company()?.name ?? 'Softaxe ERP';
  }

  get userName(): string {
    return this.auth.user()?.fullName ?? 'User';
  }

  get userRole(): string {
    return this.auth.user()?.role ?? 'Operator';
  }

  logout(): void {
    this.auth.logout();
  }
}
