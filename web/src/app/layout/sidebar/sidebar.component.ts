import { Component, computed, inject, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

export interface NavItem {
  label: string;
  path: string;
  icon: string;
  section?: string;
  /** Any of these permissions grants visibility. Empty = always visible when authenticated. */
  permissions?: string[];
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <aside class="sidebar" [class.sidebar--collapsed]="collapsed()">
      <div class="sidebar__brand">
        <span class="sidebar__mark">◈</span>
        @if (!collapsed()) {
          <div>
            <strong>SheikhGo Nexus</strong>
            <small>Enterprise ERP</small>
          </div>
        }
      </div>

      <nav class="sidebar__nav">
        @for (item of visibleItems(); track item.path) {
          @if (item.section && !collapsed()) {
            <div class="sidebar__section">{{ item.section }}</div>
          }
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
        background: linear-gradient(180deg, #0f172a 0%, #1e293b 100%);
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
        color: #fff;
        line-height: 1.2;
      }
      .sidebar__brand small {
        color: #5eead4;
        font-size: 0.65rem;
        letter-spacing: 0.1em;
        text-transform: uppercase;
      }
      .sidebar__section {
        font-size: 0.65rem;
        letter-spacing: 0.1em;
        text-transform: uppercase;
        color: #64748b;
        padding: 0.85rem 0.75rem 0.35rem;
      }
      .sidebar__nav {
        display: flex;
        flex-direction: column;
        gap: 0.15rem;
        padding: 0.75rem 0.65rem 1.5rem;
        overflow-y: auto;
      }
      a {
        display: flex;
        align-items: center;
        gap: 0.7rem;
        padding: 0.65rem 0.75rem;
        border-radius: var(--radius-sm);
        color: #b7c8d1;
        text-decoration: none;
        font-size: 0.9rem;
        font-weight: 500;
        transition: background 0.15s ease, color 0.15s ease;
      }
      a:hover {
        background: rgba(255, 255, 255, 0.06);
        color: #fff;
      }
      a.is-active {
        background: rgba(20, 184, 166, 0.2);
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
  private readonly auth = inject(AuthService);
  readonly collapsed = input(false);

  readonly items: NavItem[] = [
    { label: 'Dashboard', path: '/dashboard', icon: '▣', section: 'Overview' },
    { label: 'Finance', path: '/coming-soon/finance', icon: '☰', section: 'Domains' },
    { label: 'Sales', path: '/coming-soon/sales', icon: '↗' },
    {
      label: 'Procurement',
      path: '/purchase',
      icon: '↙',
      permissions: ['Procurement.PurchaseOrder.View', 'Procurement.GoodsReceipt.View', 'Procurement.PurchaseInvoice.View'],
    },
    { label: 'Inventory', path: '/inventory', icon: '⧉', permissions: ['Inventory.Product.View'] },
    { label: 'Production', path: '/coming-soon/production', icon: '⚙' },
    { label: 'HR & Payroll', path: '/coming-soon/hr', icon: '⊡' },
    { label: 'Farm', path: '/coming-soon/farm', icon: '☀' },
    { label: 'Quality', path: '/coming-soon/quality', icon: '⚗' },
    { label: 'Operations', path: '/coming-soon/operations', icon: '⚖' },
    { label: 'POS', path: '/coming-soon/pos', icon: '▣' },
    { label: 'Reports', path: '/coming-soon/reports', icon: '▦' },
    { label: 'Companies', path: '/administration/companies', icon: '▣', section: 'Administration', permissions: ['Admin.Company.View'] },
    { label: 'Branches', path: '/administration/branches', icon: '⌂', permissions: ['Admin.Branch.View'] },
    { label: 'Users', path: '/administration/users', icon: '☺', permissions: ['Admin.User.Edit'] },
    { label: 'Roles', path: '/administration/roles', icon: '⚑', permissions: ['Admin.Role.View'] },
    { label: 'Financial years', path: '/administration/financial-years', icon: '◷', permissions: ['Admin.FinancialYear.View'] },
    { label: 'Audit log', path: '/administration/audit', icon: '▤', permissions: ['Admin.Audit.View'] },
    { label: 'Document types', path: '/administration/document-types', icon: '☰', permissions: ['Admin.DocumentType.View'] },
  ];

  readonly visibleItems = computed(() =>
    this.items.filter((item) => {
      if (!item.permissions?.length) return true;
      return this.auth.hasAnyPermission(...item.permissions);
    }),
  );
}
