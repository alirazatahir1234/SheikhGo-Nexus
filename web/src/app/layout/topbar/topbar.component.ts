import { Component, OnInit, output, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../core/services/auth.service';
import { BranchBrief, FinancialYearBrief } from '../../core/models/auth.model';
import { API_BASE_URL } from '../../core/models/api.constants';
import { NavIconComponent } from '../../core/nav/nav-icon.component';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [FormsModule, NavIconComponent],
  template: `
    <header class="topbar">
      <button type="button" class="topbar__toggle" (click)="toggleSidebar.emit()" aria-label="Toggle sidebar">
        ☰
      </button>

      @if (companies().length > 1) {
        <label class="topbar__company">
          <span class="topbar__eyebrow">Company</span>
          <select [ngModel]="selectedCompanyId()" (ngModelChange)="onCompanyChange($event)">
            @for (c of companies(); track c.id) {
              <option [value]="c.id">{{ c.name }}</option>
            }
          </select>
        </label>
      } @else {
        <div class="topbar__company">
          <span class="topbar__eyebrow">Company</span>
          <strong>{{ companyName }}</strong>
        </div>
      }

      <label class="topbar__branch">
        <span class="topbar__eyebrow">Branch</span>
        <select [ngModel]="selectedBranchId()" (ngModelChange)="onBranchChange($event)">
          @for (b of branches(); track b.id) {
            <option [value]="b.id">{{ b.name }}</option>
          }
        </select>
      </label>

      <div class="topbar__fy">
        <span class="topbar__eyebrow">FY</span>
        <strong>{{ fyLabel() }}</strong>
      </div>

      <div class="topbar__search">
        <button type="button" class="topbar__search-btn" (click)="openSearch.emit()" title="Search (⌘K)">
          <span>Search Nexus…</span>
          <kbd>⌘K</kbd>
        </button>
      </div>

      <div class="topbar__spacer"></div>

      <button type="button" class="topbar__bell" title="Notifications" aria-label="Notifications">
        <app-nav-icon name="bell" [size]="18" />
      </button>

      <div class="topbar__user">
        <div class="topbar__meta">
          <strong>{{ userName }}</strong>
          <small>{{ userRole }}</small>
        </div>
        <button type="button" class="btn btn--ghost topbar__signout" (click)="logout()">Sign out</button>
      </div>
    </header>
  `,
  styles: [
    `
      :host {
        display: block;
        position: sticky;
        top: 0;
        z-index: 20;
      }
      .topbar {
        height: var(--topbar-height);
        display: flex;
        align-items: center;
        gap: 1rem;
        padding: 0 1.25rem;
        background: color-mix(in srgb, var(--surface) 88%, transparent);
        backdrop-filter: blur(10px);
        border-bottom: 1px solid var(--border);
      }
      .topbar__toggle {
        border: 1px solid var(--border);
        background: var(--surface);
        width: 2.25rem;
        height: 2.25rem;
        border-radius: var(--radius-sm);
        cursor: pointer;
        color: var(--slate-700);
        flex-shrink: 0;
      }
      .topbar__eyebrow {
        display: block;
        font-size: 0.65rem;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        color: var(--text-muted);
      }
      .topbar__company,
      .topbar__branch,
      .topbar__fy {
        flex-shrink: 0;
      }
      .topbar__company strong,
      .topbar__fy strong {
        font-family: var(--font-display);
        color: var(--slate-900);
        font-size: 0.95rem;
        white-space: nowrap;
      }
      .topbar__company select,
      .topbar__branch select {
        border: 1px solid var(--border);
        border-radius: var(--radius-sm);
        padding: 0.25rem 0.5rem;
        font-size: 0.85rem;
        background: var(--surface);
        min-width: 8rem;
        max-width: 12rem;
      }
      .topbar__search {
        max-width: 360px;
        flex: 1 1 12rem;
        min-width: 0;
      }
      .topbar__search-btn {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        border: 1px solid var(--border);
        border-radius: 999px;
        padding: 0.4rem 0.75rem 0.4rem 1rem;
        width: 100%;
        max-width: 360px;
        background: color-mix(in srgb, var(--surface) 92%, var(--border));
        color: var(--text-muted);
        font-size: 0.85rem;
        cursor: pointer;
        font-family: inherit;
      }
      .topbar__search-btn span {
        flex: 1;
        text-align: left;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }
      .topbar__search-btn kbd {
        font-size: 0.65rem;
        border: 1px solid var(--border);
        border-radius: 4px;
        padding: 0.1rem 0.35rem;
        background: var(--surface);
        flex-shrink: 0;
      }
      .topbar__spacer {
        flex: 1;
        min-width: 0.5rem;
      }
      .topbar__bell {
        border: 1px solid var(--border);
        background: var(--surface);
        width: 2.25rem;
        height: 2.25rem;
        border-radius: var(--radius-sm);
        display: inline-flex;
        align-items: center;
        justify-content: center;
        cursor: default;
        color: var(--slate-700);
        flex-shrink: 0;
      }
      .topbar__user {
        display: flex;
        align-items: center;
        gap: 0.85rem;
        flex-shrink: 0;
        white-space: nowrap;
      }
      .topbar__meta {
        text-align: right;
        white-space: nowrap;
      }
      .topbar__meta strong {
        display: block;
        font-size: 0.92rem;
        white-space: nowrap;
      }
      .topbar__meta small {
        color: var(--text-muted);
        white-space: nowrap;
      }
      .topbar__signout {
        white-space: nowrap;
        flex-shrink: 0;
      }
      @media (max-width: 1100px) {
        .topbar__search {
          display: none;
        }
      }
      @media (max-width: 900px) {
        .topbar__company,
        .topbar__branch,
        .topbar__fy {
          display: none;
        }
      }
    `,
  ],
})
export class TopbarComponent implements OnInit {
  readonly toggleSidebar = output<void>();
  readonly openSearch = output<void>();
  readonly branches = signal<BranchBrief[]>([]);
  readonly selectedBranchId = signal('');
  readonly selectedCompanyId = signal('');
  readonly companies = computed(() => this.auth.companies());
  readonly fyLabel = computed(() => this.auth.financialYear()?.code ?? '—');

  constructor(
    private readonly auth: AuthService,
    private readonly http: HttpClient,
  ) {}

  ngOnInit(): void {
    this.selectedCompanyId.set(this.auth.company()?.id ?? '');
    this.loadBranches();
    if (!this.auth.financialYear()) {
      this.loadFinancialYear();
    }
  }

  private loadBranches(): void {
    this.http.get<BranchBrief[]>(`${API_BASE_URL}/admin/branches`).subscribe({
      next: (list) => {
        this.branches.set(list);
        const stored = this.auth.branch()?.id;
        const def = list.find((b) => b.id === stored) ?? list.find((b) => b.isDefault) ?? list[0];
        if (def) {
          this.selectedBranchId.set(def.id);
          this.auth.setBranch(def);
        }
      },
      error: () => undefined,
    });
  }

  private loadFinancialYear(): void {
    this.http.get<FinancialYearBrief[]>(`${API_BASE_URL}/admin/financial-years`).subscribe({
      next: (list) => {
        const current = list.find((y) => y.isCurrent) ?? list[0] ?? null;
        this.auth.setFinancialYear(current);
      },
      error: () => undefined,
    });
  }

  onCompanyChange(id: string): void {
    const company = this.companies().find((c) => c.id === id);
    if (!company) return;
    this.selectedCompanyId.set(id);
    this.auth.setCompany(company);
    this.loadBranches();
  }

  onBranchChange(id: string): void {
    const branch = this.branches().find((b) => b.id === id) ?? null;
    this.selectedBranchId.set(id);
    this.auth.setBranch(branch);
  }

  get companyName(): string {
    return this.auth.company()?.name ?? 'SheikhGo-Nexus-ERP';
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
