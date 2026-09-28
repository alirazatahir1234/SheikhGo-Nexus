import { Component, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

export interface NavItem {
  label: string;
  path: string;
  icon: string;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <aside class="sidebar" [class.sidebar--collapsed]="collapsed()">
      <div class="sidebar__brand">
        <span class="sidebar__mark">SX</span>
        @if (!collapsed()) {
          <div>
            <strong>Softaxe</strong>
            <small>ERP</small>
          </div>
        }
      </div>

      <nav class="sidebar__nav">
        @for (item of items; track item.path) {
          <a
            [routerLink]="item.path"
            routerLinkActive="is-active"
            [title]="item.label"
          >
            <span class="sidebar__icon" aria-hidden="true">{{ item.icon }}</span>
            @if (!collapsed()) {
              <span>{{ item.label }}</span>
            }
          </a>
        }
      </nav>
    </aside>
  `,
  styles: [
    `
      .sidebar {
        width: var(--sidebar-width);
        background: linear-gradient(180deg, var(--slate-900) 0%, var(--slate-800) 100%);
        color: #d7e4ea;
        display: flex;
        flex-direction: column;
        min-height: 100vh;
        border-right: 1px solid rgba(255, 255, 255, 0.06);
        transition: width 0.2s ease;
      }
      .sidebar--collapsed {
        width: 4.5rem;
      }
      .sidebar__brand {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        padding: 1.25rem 1rem;
        border-bottom: 1px solid rgba(255, 255, 255, 0.08);
      }
      .sidebar__mark {
        width: 2.4rem;
        height: 2.4rem;
        border-radius: 0.55rem;
        background: var(--teal-500);
        color: #06242a;
        display: grid;
        place-items: center;
        font-family: var(--font-display);
        font-weight: 800;
        flex-shrink: 0;
      }
      .sidebar__brand strong {
        display: block;
        font-family: var(--font-display);
        font-size: 1.05rem;
        letter-spacing: -0.02em;
        color: #fff;
      }
      .sidebar__brand small {
        color: var(--teal-300);
        font-size: 0.7rem;
        letter-spacing: 0.12em;
        text-transform: uppercase;
      }
      .sidebar__nav {
        display: flex;
        flex-direction: column;
        gap: 0.2rem;
        padding: 1rem 0.65rem;
        overflow-y: auto;
      }
      a {
        display: flex;
        align-items: center;
        gap: 0.7rem;
        padding: 0.7rem 0.75rem;
        border-radius: var(--radius-sm);
        color: #b7c8d1;
        text-decoration: none;
        font-size: 0.92rem;
        font-weight: 500;
        transition: background 0.15s ease, color 0.15s ease;
      }
      a:hover {
        background: rgba(255, 255, 255, 0.06);
        color: #fff;
      }
      a.is-active {
        background: rgba(20, 184, 166, 0.18);
        color: #fff;
      }
      .sidebar__icon {
        width: 1.25rem;
        text-align: center;
        opacity: 0.9;
      }
    `,
  ],
})
export class SidebarComponent {
  readonly collapsed = input(false);

  readonly items: NavItem[] = [
    { label: 'Dashboard', path: '/dashboard', icon: '▣' },
    { label: 'Accounts', path: '/accounts', icon: '☰' },
    { label: 'Parties', path: '/parties', icon: '◉' },
    { label: 'Products', path: '/products', icon: '▦' },
    { label: 'Vouchers', path: '/vouchers', icon: '▤' },
    { label: 'Sales', path: '/sales', icon: '↗' },
    { label: 'Purchase', path: '/purchase', icon: '↙' },
    { label: 'Inventory', path: '/inventory', icon: '⧉' },
    { label: 'Stock transfer', path: '/stock-transfer', icon: '↔' },
    { label: 'Warehouses', path: '/warehouses', icon: '⌂' },
    { label: 'Receipts', path: '/settlements', icon: '⇄' },
    { label: 'Returns', path: '/returns', icon: '↺' },
    { label: 'Contracts', path: '/contracts', icon: '☰' },
    { label: 'Lab / QC', path: '/lab', icon: '⚗' },
    { label: 'Cost centres', path: '/cost-centres', icon: '◎' },
    { label: 'Cheques', path: '/cheques', icon: '▤' },
    { label: 'Sales force', path: '/salesforce', icon: '↗' },
    { label: 'Demands', path: '/demands', icon: '☑' },
    { label: 'Production', path: '/production', icon: '⚙' },
    { label: 'Farm', path: '/farm', icon: '☀' },
    { label: 'Payroll', path: '/payroll', icon: '⊡' },
    { label: 'Leaves', path: '/leaves', icon: '✈' },
    { label: 'HR finance', path: '/hr-finance', icon: '₴' },
    { label: 'Attendance', path: '/attendance', icon: '◷' },
    { label: 'Brands', path: '/brands', icon: '★' },
    { label: 'Budgets', path: '/budgets', icon: '▤' },
    { label: 'Day close', path: '/day-close', icon: '⊘' },
    { label: 'Weighbridge', path: '/weighbridge', icon: '⚖' },
    { label: 'POS', path: '/pos', icon: '▣' },
    { label: 'Reports', path: '/reports', icon: '▦' },
    { label: 'Users', path: '/users', icon: '☺' },
    { label: 'Companies', path: '/platform/tenants', icon: '▣' },
    { label: 'Company', path: '/settings/company', icon: '⚙' },
  ];
}
