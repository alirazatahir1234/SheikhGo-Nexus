import { Component, OnInit, output, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../core/services/auth.service';
import { BranchBrief, CompanyBrief, FinancialYearBrief } from '../../core/models/auth.model';
import { API_BASE_URL } from '../../core/models/api.constants';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [FormsModule],
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
        <input type="search" placeholder="Search… (coming soon)" disabled />
      </div>

      <div class="topbar__spacer"></div>

      <button type="button" class="topbar__bell" title="Notifications" aria-label="Notifications">🔔</button>

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
        background: rgba(255, 255, 255, 0.88);
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
        font-size: 0.65rem;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        color: var(--text-muted);
      }
      .topbar__company strong,
      .topbar__fy strong {
        font-family: var(--font-display);
        color: var(--slate-900);
        font-size: 0.95rem;
      }
      .topbar__company select,
      .topbar__branch select {
        border: 1px solid var(--border);
        border-radius: var(--radius-sm);
        padding: 0.25rem 0.5rem;
        font-size: 0.85rem;
        background: var(--surface);
        min-width: 8rem;
      }
      .topbar__search input {
        border: 1px solid var(--border);
        border-radius: 999px;
        padding: 0.4rem 1rem;
        min-width: 14rem;
        background: #f8fafc;
        color: var(--text-muted);
        font-size: 0.85rem;
      }
      .topbar__spacer {
        flex: 1;
      }
      .topbar__bell {
        border: none;
        background: transparent;
        font-size: 1.1rem;
        cursor: default;
        opacity: 0.7;
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
      @media (max-width: 900px) {
        .topbar__search {
          display: none;
        }
      }
    `,
  ],
})
export class TopbarComponent implements OnInit {
  readonly toggleSidebar = output<void>();
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
      error: () => {
        /* admin API may not be available */
      },
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
    return this.auth.company()?.name ?? 'SheikhGo Nexus';
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
