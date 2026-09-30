import { Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { AuthService } from '../../core/services/auth.service';
import {
  AgingReport,
  BalanceSheetReport,
  CashFlowReport,
  InventoryDemandForecastReport,
  NamedReport,
  ProfitLossReport,
  ReportsApiService,
  TrialBalanceReport,
} from '../../core/services/reports-api.service';
import { ReportsService, LedgerReport } from '../../core/services/reports.service';
import { AccountsService } from '../../core/services/accounts.service';
import { Account } from '../../core/models/erp.models';

type DomainKey =
  | 'finance'
  | 'sales'
  | 'procurement'
  | 'inventory'
  | 'hr'
  | 'production'
  | 'farm'
  | 'quality'
  | 'operations'
  | 'pos';

interface ReportDef {
  id: string;
  label: string;
  path?: string;
  kind: 'named' | 'cashflow' | 'tb' | 'pl' | 'bs' | 'ar' | 'ap' | 'ledger' | 'inv-val' | 'inv-turn' | 'inv-list' | 'inv-forecast';
  needsRange?: boolean;
  needsAsOf?: boolean;
  exportable?: boolean;
}

interface DomainDef {
  key: DomainKey;
  label: string;
  permission: string;
  alias?: string;
  reports: ReportDef[];
}

const DOMAINS: DomainDef[] = [
  {
    key: 'finance',
    label: 'Finance',
    permission: 'Reports.Finance.View',
    alias: 'Finance.Reports.View',
    reports: [
      { id: 'ledger', label: 'Account ledger', kind: 'ledger', needsRange: true },
      { id: 'trial-balance', label: 'Trial balance', kind: 'tb', needsAsOf: true },
      { id: 'profit-loss', label: 'Profit & loss', kind: 'pl', needsRange: true },
      { id: 'balance-sheet', label: 'Balance sheet', kind: 'bs', needsAsOf: true },
      { id: 'cash-flow', label: 'Cash flow', kind: 'cashflow', needsRange: true },
      { id: 'ar', label: 'AR aging', kind: 'ar', needsAsOf: true },
      { id: 'ap', label: 'AP aging', kind: 'ap', needsAsOf: true },
    ],
  },
  {
    key: 'sales',
    label: 'Sales',
    permission: 'Reports.Sales.View',
    reports: [
      { id: 'summary', label: 'Summary', path: 'sales/summary', kind: 'named', needsRange: true, exportable: true },
      { id: 'by-customer', label: 'By customer', path: 'sales/by-customer', kind: 'named', needsRange: true, exportable: true },
      { id: 'by-product', label: 'By product', path: 'sales/by-product', kind: 'named', needsRange: true, exportable: true },
      { id: 'by-category', label: 'By category', path: 'sales/by-category', kind: 'named', needsRange: true, exportable: true },
      { id: 'by-branch', label: 'By branch', path: 'sales/by-branch', kind: 'named', needsRange: true, exportable: true },
      { id: 'trend', label: 'Trend', path: 'sales/trend', kind: 'named', needsRange: true, exportable: true },
      { id: 'collections', label: 'Collections', path: 'sales/collections', kind: 'named', needsRange: true, exportable: true },
    ],
  },
  {
    key: 'procurement',
    label: 'Procurement',
    permission: 'Reports.Procurement.View',
    reports: [
      { id: 'summary', label: 'Summary', path: 'procurement/summary', kind: 'named', needsRange: true, exportable: true },
      { id: 'by-supplier', label: 'By supplier', path: 'procurement/by-supplier', kind: 'named', needsRange: true, exportable: true },
      { id: 'by-product', label: 'By product', path: 'procurement/by-product', kind: 'named', needsRange: true, exportable: true },
      { id: 'outstanding', label: 'Outstanding', path: 'procurement/outstanding', kind: 'named', needsAsOf: true, exportable: true },
      { id: 'price-analysis', label: 'Price analysis', path: 'procurement/price-analysis', kind: 'named', needsRange: true, exportable: true },
      { id: 'grn-vs-invoice', label: 'GRN vs invoice', path: 'procurement/grn-vs-invoice', kind: 'named', needsRange: true, exportable: true },
      { id: 'trend', label: 'Trend', path: 'procurement/trend', kind: 'named', needsRange: true, exportable: true },
    ],
  },
  {
    key: 'inventory',
    label: 'Inventory',
    permission: 'Reports.Inventory.View',
    alias: 'Inventory.Reports.View',
    reports: [
      { id: 'valuation', label: 'Valuation', kind: 'inv-val' },
      { id: 'demand-forecast', label: 'Demand forecast', kind: 'inv-forecast', needsAsOf: true },
      { id: 'slow-moving', label: 'Slow moving', path: 'inventory/slow-moving', kind: 'inv-list', needsRange: true },
      { id: 'fast-moving', label: 'Fast moving', path: 'inventory/fast-moving', kind: 'inv-list', needsRange: true },
      { id: 'aging', label: 'Stock aging', path: 'inventory/aging', kind: 'inv-list', needsAsOf: true },
      { id: 'warehouse-comparison', label: 'Warehouse comparison', path: 'inventory/warehouse-comparison', kind: 'inv-list' },
      { id: 'adjustments', label: 'Adjustments', path: 'inventory/adjustments', kind: 'named', needsRange: true },
      { id: 'turnover', label: 'Turnover', kind: 'inv-turn', needsRange: true },
    ],
  },
  {
    key: 'hr',
    label: 'HR',
    permission: 'Reports.HR.View',
    reports: [
      { id: 'employees', label: 'Employees', path: 'hr/employees', kind: 'named', exportable: true },
      { id: 'attendance', label: 'Attendance', path: 'hr/attendance', kind: 'named' },
      { id: 'leave', label: 'Leave', path: 'hr/leave', kind: 'named' },
      { id: 'payroll', label: 'Payroll', path: 'hr/payroll', kind: 'named', needsRange: true, exportable: true },
      { id: 'department-cost', label: 'Department cost', path: 'hr/department-cost', kind: 'named', exportable: true },
      { id: 'overtime', label: 'Overtime', path: 'hr/overtime', kind: 'named', needsRange: true, exportable: true },
    ],
  },
  {
    key: 'production',
    label: 'Production',
    permission: 'Reports.Production.View',
    reports: [
      { id: 'summary', label: 'Summary', path: 'production/summary', kind: 'named', needsRange: true, exportable: true },
      { id: 'order-status', label: 'Order status', path: 'production/order-status', kind: 'named', exportable: true },
      { id: 'consumption', label: 'Consumption', path: 'production/consumption', kind: 'named', needsRange: true, exportable: true },
      { id: 'cost', label: 'Cost', path: 'production/cost', kind: 'named', needsRange: true, exportable: true },
      { id: 'yield', label: 'Yield', path: 'production/yield', kind: 'named', needsRange: true, exportable: true },
      { id: 'finished-goods', label: 'Finished goods', path: 'production/finished-goods', kind: 'named', needsRange: true, exportable: true },
      { id: 'wip', label: 'WIP', path: 'production/wip', kind: 'named', exportable: true },
    ],
  },
  {
    key: 'farm',
    label: 'Farm',
    permission: 'Reports.Farm.View',
    reports: [
      { id: 'flocks', label: 'Flocks', path: 'farm/flocks', kind: 'named', exportable: true },
      { id: 'mortality', label: 'Mortality', path: 'farm/mortality', kind: 'named', needsRange: true, exportable: true },
      { id: 'feed', label: 'Feed', path: 'farm/feed', kind: 'named', needsRange: true, exportable: true },
      { id: 'expenses', label: 'Expenses', path: 'farm/expenses', kind: 'named', needsRange: true, exportable: true },
      { id: 'performance', label: 'Performance', path: 'farm/performance', kind: 'named', exportable: true },
    ],
  },
  {
    key: 'quality',
    label: 'Quality',
    permission: 'Reports.Quality.View',
    reports: [
      { id: 'samples', label: 'Samples', path: 'quality/samples', kind: 'named', needsRange: true, exportable: true },
      { id: 'results', label: 'Results', path: 'quality/results', kind: 'named', needsRange: true, exportable: true },
      { id: 'pass-fail', label: 'Pass / fail / hold', path: 'quality/pass-fail', kind: 'named', needsRange: true, exportable: true },
    ],
  },
  {
    key: 'operations',
    label: 'Operations',
    permission: 'Reports.Operations.View',
    reports: [
      { id: 'weighments', label: 'Weighments', path: 'operations/weighments', kind: 'named', needsRange: true, exportable: true },
      { id: 'vehicles', label: 'Vehicles', path: 'operations/vehicles', kind: 'named', needsRange: true, exportable: true },
      { id: 'gate-passes', label: 'Gate passes', path: 'operations/gate-passes', kind: 'named', needsRange: true, exportable: true },
    ],
  },
  {
    key: 'pos',
    label: 'POS',
    permission: 'Reports.POS.View',
    reports: [
      { id: 'sales', label: 'Sales', path: 'pos/sales', kind: 'named', needsRange: true, exportable: true },
      { id: 'payments', label: 'Tenders', path: 'pos/payments', kind: 'named', needsRange: true, exportable: true },
      { id: 'shifts', label: 'Shifts', path: 'pos/shifts', kind: 'named', needsRange: true, exportable: true },
      { id: 'products', label: 'Products', path: 'pos/products', kind: 'named', needsRange: true, exportable: true },
    ],
  },
];

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, CurrencyPipe, DatePipe, DecimalPipe],
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss',
})
export class ReportsComponent implements OnInit {
  readonly domains = DOMAINS;
  readonly domain = signal<DomainKey>('finance');
  readonly reportId = signal('trial-balance');
  readonly loading = signal(false);
  readonly error = signal('');
  readonly accounts = signal<Account[]>([]);

  readonly named = signal<NamedReport | null>(null);
  readonly cashFlow = signal<CashFlowReport | null>(null);
  readonly trial = signal<TrialBalanceReport | null>(null);
  readonly pl = signal<ProfitLossReport | null>(null);
  readonly bs = signal<BalanceSheetReport | null>(null);
  readonly aging = signal<AgingReport | null>(null);
  readonly ledger = signal<LedgerReport | null>(null);
  readonly invRows = signal<Record<string, unknown>[]>([]);
  readonly invTurn = signal<{ cogs: number; avgInventoryValue: number; turnover: number } | null>(null);
  readonly invValTotal = signal<number | null>(null);
  readonly invForecast = signal<InventoryDemandForecastReport | null>(null);

  readonly filterForm;

  readonly visibleDomains = computed(() =>
    this.domains.filter(
      (d) =>
        this.auth.hasPermission(d.permission) ||
        (d.alias ? this.auth.hasPermission(d.alias) : false) ||
        this.auth.hasPermission('Reports.Executive.View'),
    ),
  );

  readonly currentDomain = computed(() => this.domains.find((d) => d.key === this.domain()) ?? this.domains[0]);
  readonly currentReport = computed(
    () => this.currentDomain().reports.find((r) => r.id === this.reportId()) ?? this.currentDomain().reports[0],
  );

  constructor(
    private readonly api: ReportsApiService,
    private readonly legacyReports: ReportsService,
    private readonly accountsApi: AccountsService,
    private readonly auth: AuthService,
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
  ) {
    const today = new Date().toISOString().slice(0, 10);
    const monthStart = new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().slice(0, 10);
    this.filterForm = this.fb.nonNullable.group({
      from: [monthStart],
      to: [today],
      asOf: [today],
      accountId: [''],
    });
  }

  ngOnInit(): void {
    this.accountsApi.getAll().subscribe({
      next: (rows) => this.accounts.set(rows.filter((a) => a.isPostable)),
    });

    this.route.queryParamMap.subscribe((q) => {
      const d = (q.get('domain') || 'finance') as DomainKey;
      const tab = q.get('tab');
      if (this.domains.some((x) => x.key === d)) {
        this.domain.set(d);
        const reports = this.domains.find((x) => x.key === d)!.reports;
        this.reportId.set(tab && reports.some((r) => r.id === tab) ? tab : reports[0].id);
      }
    });
  }

  selectDomain(key: DomainKey): void {
    const d = this.domains.find((x) => x.key === key);
    if (!d) return;
    this.clearResults();
    this.domain.set(key);
    this.reportId.set(d.reports[0].id);
    void this.router.navigate([], { queryParams: { domain: key, tab: d.reports[0].id }, queryParamsHandling: 'merge' });
  }

  selectReport(id: string): void {
    this.clearResults();
    this.reportId.set(id);
    void this.router.navigate([], { queryParams: { domain: this.domain(), tab: id }, queryParamsHandling: 'merge' });
  }

  run(): void {
    const r = this.currentReport();
    const v = this.filterForm.getRawValue();
    this.loading.set(true);
    this.error.set('');
    this.clearResults();

    const fail = (err: { error?: { message?: string } }) => {
      this.loading.set(false);
      this.error.set(err?.error?.message || 'Report failed.');
    };
    const done = () => this.loading.set(false);

    switch (r.kind) {
      case 'named':
        this.api.named(r.path!, v.from, v.to, v.asOf).subscribe({ next: (res) => { this.named.set(res); done(); }, error: fail });
        break;
      case 'cashflow':
        this.api.cashFlow(v.from, v.to).subscribe({ next: (res) => { this.cashFlow.set(res); done(); }, error: fail });
        break;
      case 'tb':
        this.api.trialBalance(v.asOf).subscribe({ next: (res) => { this.trial.set(res); done(); }, error: fail });
        break;
      case 'pl':
        this.api.profitLoss(v.from, v.to).subscribe({ next: (res) => { this.pl.set(res); done(); }, error: fail });
        break;
      case 'bs':
        this.api.balanceSheet(v.asOf).subscribe({ next: (res) => { this.bs.set(res); done(); }, error: fail });
        break;
      case 'ar':
        this.api.arAging(v.asOf).subscribe({ next: (res) => { this.aging.set(res); done(); }, error: fail });
        break;
      case 'ap':
        this.api.apAging(v.asOf).subscribe({ next: (res) => { this.aging.set(res); done(); }, error: fail });
        break;
      case 'ledger':
        if (!v.accountId) {
          this.loading.set(false);
          this.error.set('Select an account.');
          return;
        }
        this.legacyReports.ledger(v.accountId, v.from, v.to).subscribe({ next: (res) => { this.ledger.set(res); done(); }, error: fail });
        break;
      case 'inv-val':
        this.api.inventoryValuation().subscribe({
          next: (rows) => {
            const list = Array.isArray(rows) ? (rows as Record<string, unknown>[]) : [];
            this.invRows.set(list);
            this.invValTotal.set(list.reduce((s, x) => s + Number(x['value'] ?? x['Value'] ?? 0), 0));
            done();
          },
          error: fail,
        });
        break;
      case 'inv-turn':
        this.api.inventoryTurnover(v.from, v.to).subscribe({ next: (res) => { this.invTurn.set(res); done(); }, error: fail });
        break;
      case 'inv-forecast':
        this.api.inventoryDemandForecast(v.asOf).subscribe({ next: (res) => { this.invForecast.set(res); done(); }, error: fail });
        break;
      case 'inv-list':
        this.api.raw(r.path!, v.from, v.to, v.asOf).subscribe({
          next: (rows) => {
            if (Array.isArray(rows)) this.invRows.set(rows as Record<string, unknown>[]);
            else if (rows && typeof rows === 'object' && 'rows' in (rows as object)) this.named.set(rows as NamedReport);
            done();
          },
          error: fail,
        });
        break;
      default:
        done();
    }
  }

  export(format: 'csv' | 'xlsx' | 'pdf'): void {
    const r = this.currentReport();
    if (!r.exportable) {
      this.error.set('Export is available for named domain reports.');
      return;
    }
    const v = this.filterForm.getRawValue();
    this.api.exportBlob(this.domain(), r.id, format, v.from, v.to, v.asOf).subscribe({
      next: (blob) => {
        const a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = `${this.domain()}-${r.id}.${format === 'xlsx' ? 'xlsx' : format}`;
        a.click();
        URL.revokeObjectURL(a.href);
      },
      error: () => this.error.set('Export failed.'),
    });
  }

  print(): void {
    window.print();
  }

  cell(row: Record<string, unknown>, keys: string[]): string {
    for (const k of keys) {
      const v = row[k] ?? row[k.charAt(0).toUpperCase() + k.slice(1)];
      if (v !== undefined && v !== null) return String(v);
    }
    return '—';
  }

  private clearResults(): void {
    this.named.set(null);
    this.cashFlow.set(null);
    this.trial.set(null);
    this.pl.set(null);
    this.bs.set(null);
    this.aging.set(null);
    this.ledger.set(null);
    this.invRows.set([]);
    this.invTurn.set(null);
    this.invValTotal.set(null);
    this.invForecast.set(null);
  }
}
