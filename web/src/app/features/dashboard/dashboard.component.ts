import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ChartData, ChartOptions } from 'chart.js';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, takeUntil } from 'rxjs/operators';
import { DashboardService } from '../../core/services/dashboard.service';
import { DashboardSummary } from '../../core/models/erp.models';
import { KpiCardComponent, KpiTrend } from '../../shared/components/kpi-card/kpi-card.component';
import { DashChartComponent } from '../../shared/components/dash-chart/dash-chart.component';
import { AuthService } from '../../core/services/auth.service';
import { PosApiService, PosDailySalesReport, PosOwnerProfitReport } from '../../core/services/pos-api.service';
import {
  AgingReport,
  CashFlowReport,
  ExecutiveDashboard,
  NamedReport,
  ReportKpi,
  ReportsApiService,
  WarehouseComparisonLine,
} from '../../core/services/reports-api.service';

export interface MetricTile {
  key: string;
  label: string;
  value: number;
  unit?: string | null;
  currency: boolean;
  route?: string;
}

export interface MetricGroup {
  title: string;
  tiles: MetricTile[];
}

export interface ActionItem {
  label: string;
  detail: string;
  route: string;
}

export interface PrimaryKpi {
  label: string;
  value: number;
  hint: string;
  compareHint: string;
  delta: number | null;
  trend: KpiTrend | null;
  sparkline: number[];
  accent: string;
}

export interface ChartPanelState {
  chart: ChartData | null;
  emptyMessage: string;
  emptyFigure?: string;
}

type Preset = 'today' | 'month' | 'fy';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [KpiCardComponent, DashChartComponent, DatePipe, RouterLink, ReactiveFormsModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit, OnDestroy {
  readonly loading = signal(true);
  readonly chartsLoading = signal(false);
  readonly error = signal('');
  readonly summary = signal<DashboardSummary | null>(null);
  readonly executive = signal<ExecutiveDashboard | null>(null);
  readonly execError = signal('');
  readonly activePreset = signal<Preset | null>('month');
  readonly lastUpdated = signal<Date | null>(null);
  readonly statusLive = signal('');
  readonly rangeForm;

  readonly salesTrend = signal<NamedReport | null>(null);
  readonly purchaseTrend = signal<NamedReport | null>(null);
  readonly arAging = signal<AgingReport | null>(null);
  readonly cashFlow = signal<CashFlowReport | null>(null);
  readonly warehouses = signal<WarehouseComparisonLine[]>([]);
  readonly topCustomers = signal<{ name: string; amount: number }[]>([]);
  readonly priceAnalysis = signal<NamedReport | null>(null);
  readonly posToday = signal<PosDailySalesReport | null>(null);
  readonly posProfit = signal<PosOwnerProfitReport | null>(null);

  readonly welcomeLine = computed(
    () => `Welcome back, ${this.auth.user()?.fullName || 'User'}. Here's what's happening in your business.`,
  );

  readonly primaryKpis = signal<PrimaryKpi[]>([]);
  readonly metricGroups = signal<MetricGroup[]>([]);
  readonly actionItems = signal<ActionItem[]>([]);

  readonly salesPurchasesPanel = computed<ChartPanelState>(() => this.buildSalesPurchasesPanel());
  readonly agingPanel = computed<ChartPanelState>(() => this.buildAgingPanel());
  readonly cashPanel = computed<ChartPanelState>(() => this.buildCashPanel());
  readonly warehousePanel = computed<ChartPanelState>(() => this.buildWarehousePanel());
  readonly pricePanel = computed<ChartPanelState>(() => this.buildPricePanel());

  readonly multiLegendOptions: ChartOptions = {
    plugins: { legend: { display: true, position: 'bottom' } },
  };
  readonly noLegendOptions: ChartOptions = {
    plugins: { legend: { display: false } },
  };
  readonly horizontalBarOptions: ChartOptions = {
    indexAxis: 'y',
    plugins: { legend: { display: false } },
  };

  private readonly destroy$ = new Subject<void>();
  private readonly money = new Intl.NumberFormat('en-PK', {
    style: 'currency',
    currency: 'PKR',
    currencyDisplay: 'code',
    maximumFractionDigits: 0,
  });
  private readonly number = new Intl.NumberFormat('en-PK', { maximumFractionDigits: 0 });

  constructor(
    private readonly dashboard: DashboardService,
    private readonly reportsApi: ReportsApiService,
    private readonly posApi: PosApiService,
    private readonly auth: AuthService,
    private readonly fb: FormBuilder,
  ) {
    const today = this.iso(new Date());
    const monthStart = this.iso(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
    this.rangeForm = this.fb.nonNullable.group({ from: [monthStart], to: [today] });
  }

  get canExecutive(): boolean {
    return this.auth.hasPermission('Reports.Executive.View');
  }

  ngOnInit(): void {
    this.loadSummary();
    this.loadPosToday();
    this.reloadBoard();
  }

  private loadPosToday(): void {
    const today = this.iso(new Date());
    this.posApi
      .dailySales(today)
      .pipe(catchError(() => of(null)), takeUntil(this.destroy$))
      .subscribe((r) => {
        this.posToday.set(r);
        this.rebuildActions();
      });
    this.posApi
      .ownerProfit(today)
      .pipe(catchError(() => of(null)), takeUntil(this.destroy$))
      .subscribe((r) => this.posProfit.set(r));
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  applyPreset(preset: Preset): void {
    const today = new Date();
    let from: Date;
    let to = today;
    if (preset === 'today') {
      from = today;
    } else if (preset === 'month') {
      from = new Date(today.getFullYear(), today.getMonth(), 1);
    } else {
      const fy = this.auth.financialYear();
      if (fy?.startDate && fy?.endDate) {
        from = new Date(fy.startDate);
        to = new Date(Math.min(today.getTime(), new Date(fy.endDate).getTime()));
      } else {
        from = new Date(today.getFullYear(), 0, 1);
      }
    }
    this.rangeForm.patchValue({ from: this.iso(from), to: this.iso(to) });
    this.activePreset.set(preset);
    this.reloadBoard();
  }

  refresh(): void {
    this.clearPreset();
    this.reloadBoard();
  }

  clearPreset(): void {
    this.activePreset.set(null);
  }

  moneyFmt(v: number): string {
    return this.money.format(v);
  }

  qtyFmt(v: number, unit?: string | null): string {
    const n = this.number.format(v);
    return unit === 'kg' ? `${n} kg` : n;
  }

  statusClass(status: string): string {
    const s = (status || '').toLowerCase();
    if (s.includes('draft')) return 'tag tag--draft';
    if (s.includes('submit')) return 'tag tag--submitted';
    if (s.includes('verif')) return 'tag tag--verified';
    if (s.includes('approv')) return 'tag tag--approved';
    if (s.includes('post')) return 'tag tag--posted';
    return 'tag';
  }

  private cssVar(name: string, fallback: string): string {
    if (typeof document === 'undefined') return fallback;
    return getComputedStyle(document.documentElement).getPropertyValue(name).trim() || fallback;
  }

  private chartColors(): string[] {
    return [
      this.cssVar('--chart-1', '#0f766e'),
      this.cssVar('--chart-2', '#c2410c'),
      this.cssVar('--chart-3', '#0e7490'),
      this.cssVar('--chart-4', '#a16207'),
      this.cssVar('--chart-5', '#475569'),
      this.cssVar('--chart-6', '#334155'),
    ];
  }

  private buildSalesPurchasesPanel(): ChartPanelState {
    const sales = this.salesTrend();
    const purchases = this.purchaseTrend();
    if (!sales && !purchases) {
      return { chart: null, emptyMessage: 'No sales/purchase trend for this range.' };
    }
    const labelSet = new Map<string, string>();
    for (const r of sales?.rows ?? []) labelSet.set(r.code, r.name);
    for (const r of purchases?.rows ?? []) if (!labelSet.has(r.code)) labelSet.set(r.code, r.name);
    const codes = [...labelSet.keys()].sort();
    if (codes.length < 3) {
      const salesTotal = sales?.grandTotal ?? (sales?.rows ?? []).reduce((s, r) => s + r.amount, 0);
      const buyTotal = purchases?.grandTotal ?? (purchases?.rows ?? []).reduce((s, r) => s + r.amount, 0);
      return {
        chart: null,
        emptyMessage: 'Not enough data in this range to chart a trend.',
        emptyFigure: `Sales ${this.money.format(salesTotal)} · Purchases ${this.money.format(buyTotal)}`,
      };
    }
    const [c1, c2] = this.chartColors();
    const salesMap = new Map((sales?.rows ?? []).map((r) => [r.code, r.amount]));
    const buyMap = new Map((purchases?.rows ?? []).map((r) => [r.code, r.amount]));
    return {
      chart: {
        labels: codes.map((c) => labelSet.get(c) ?? c),
        datasets: [
          {
            label: 'Sales',
            data: codes.map((c) => salesMap.get(c) ?? 0),
            borderColor: c1,
            backgroundColor: this.withAlpha(c1, 0.12),
            tension: 0.35,
            fill: true,
            pointRadius: 2,
          },
          {
            label: 'Purchases',
            data: codes.map((c) => buyMap.get(c) ?? 0),
            borderColor: c2,
            backgroundColor: this.withAlpha(c2, 0.1),
            tension: 0.35,
            fill: true,
            pointRadius: 2,
          },
        ],
      },
      emptyMessage: '',
    };
  }

  private buildAgingPanel(): ChartPanelState {
    const aging = this.arAging();
    if (!aging?.rows?.length) {
      return { chart: null, emptyMessage: 'No receivables aging data.' };
    }
    const colors = this.chartColors();
    const buckets = [
      { label: '0–30', value: aging.rows.reduce((s, r) => s + (r.current ?? 0) + (r.days1To30 ?? 0), 0), color: colors[0] },
      { label: '31–60', value: aging.rows.reduce((s, r) => s + (r.days31To60 ?? 0), 0), color: colors[3] },
      { label: '61–90', value: aging.rows.reduce((s, r) => s + (r.days61To90 ?? 0), 0), color: colors[1] },
      { label: '90+', value: aging.rows.reduce((s, r) => s + (r.days90Plus ?? 0), 0), color: colors[5] },
    ];
    const nonZero = buckets.filter((b) => b.value > 0);
    if (nonZero.length < 2) {
      return {
        chart: null,
        emptyMessage: 'Not enough aging buckets with balances to chart.',
        emptyFigure: `Total AR ${this.money.format(aging.grandTotal)}`,
      };
    }
    return {
      chart: {
        labels: buckets.map((b) => b.label),
        datasets: [
          {
            label: 'Receivables',
            data: buckets.map((b) => b.value),
            backgroundColor: buckets.map((b) => b.color),
            borderRadius: 6,
            maxBarThickness: 48,
          },
        ],
      },
      emptyMessage: '',
    };
  }

  private buildCashPanel(): ChartPanelState {
    const cf = this.cashFlow();
    if (!cf) return { chart: null, emptyMessage: 'No cash-flow data for this range.' };
    const c1 = this.chartColors()[0];
    if (cf.lines?.length >= 2) {
      let running = cf.openingCash;
      const series: number[] = [cf.openingCash];
      const labels = ['Opening'];
      for (const line of cf.lines) {
        running += line.amount;
        series.push(running);
        labels.push(line.label || line.category);
      }
      if (series.length < 3) {
        return {
          chart: null,
          emptyMessage: 'Not enough cash movements to chart.',
          emptyFigure: `Closing ${this.money.format(cf.closingCash)}`,
        };
      }
      return {
        chart: {
          labels,
          datasets: [
            {
              label: 'Cash position',
              data: series,
              borderColor: c1,
              backgroundColor: this.withAlpha(c1, 0.1),
              tension: 0.3,
              fill: true,
              pointRadius: 2,
            },
          ],
        },
        emptyMessage: '',
      };
    }
    return {
      chart: null,
      emptyMessage: 'Cash summary for this range.',
      emptyFigure: `Open ${this.money.format(cf.openingCash)} → Close ${this.money.format(cf.closingCash)}`,
    };
  }

  private buildWarehousePanel(): ChartPanelState {
    const rows = this.warehouses();
    if (!rows.length) return { chart: null, emptyMessage: 'No warehouse stock values.' };
    if (rows.length < 2) {
      const only = rows[0];
      return {
        chart: null,
        emptyMessage: 'Need at least two warehouses to compare.',
        emptyFigure: `${only.warehouseCode}: ${this.money.format(only.totalValue)}`,
      };
    }
    const c3 = this.chartColors()[2];
    return {
      chart: {
        labels: rows.map((r) => r.warehouseCode || 'WH'),
        datasets: [
          {
            label: 'Stock value',
            data: rows.map((r) => r.totalValue),
            backgroundColor: c3,
            borderRadius: 6,
            maxBarThickness: 28,
          },
        ],
      },
      emptyMessage: '',
    };
  }

  private buildPricePanel(): ChartPanelState {
    const report = this.priceAnalysis();
    if (!report?.rows?.length) {
      return { chart: null, emptyMessage: 'No purchase price analysis for this range.' };
    }
    const top = [...report.rows].sort((a, b) => b.amount - a.amount).slice(0, 8);
    if (top.length < 3) {
      const fig = top.map((r) => `${r.name} ${this.money.format(r.amount)}`).join(' · ');
      return {
        chart: null,
        emptyMessage: 'Not enough products to chart price comparison.',
        emptyFigure: fig || undefined,
      };
    }
    const c4 = this.chartColors()[3];
    return {
      chart: {
        labels: top.map((r) => r.name),
        datasets: [
          {
            label: 'Avg rate',
            data: top.map((r) => r.amount),
            borderColor: c4,
            backgroundColor: this.withAlpha(c4, 0.12),
            tension: 0.3,
            fill: true,
            pointRadius: 3,
          },
        ],
      },
      emptyMessage: '',
    };
  }

  private withAlpha(hex: string, alpha: number): string {
    const h = hex.replace('#', '');
    if (h.length !== 6) return hex;
    const r = parseInt(h.slice(0, 2), 16);
    const g = parseInt(h.slice(2, 4), 16);
    const b = parseInt(h.slice(4, 6), 16);
    return `rgba(${r}, ${g}, ${b}, ${alpha})`;
  }

  private loadSummary(): void {
    this.dashboard
      .getSummary()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.summary.set(data);
          this.loading.set(false);
          this.rebuildPrimaryFromSummary();
          this.rebuildActions();
        },
        error: () => {
          this.summary.set({
            salesToday: 0,
            purchasesToday: 0,
            inventoryValue: 0,
            receivables: 0,
            payables: 0,
            recentVouchers: [],
            pendingApprovals: [],
            recentDocuments: [],
          });
          this.error.set('Unable to load live dashboard data. Showing empty state until API is available.');
          this.loading.set(false);
          this.rebuildPrimaryFromSummary();
        },
      });
  }

  private reloadBoard(): void {
    const { from, to } = this.rangeForm.getRawValue();
    const prior = this.priorPeriod(from, to);
    this.chartsLoading.set(true);
    this.statusLive.set('Refreshing dashboard…');

    const executive$ = this.canExecutive
      ? this.reportsApi.executive(from, to).pipe(catchError(() => of(null)))
      : of(null);
    const priorExec$ = this.canExecutive
      ? this.reportsApi.executive(prior.from, prior.to).pipe(catchError(() => of(null)))
      : of(null);

    forkJoin({
      executive: executive$,
      priorExec: priorExec$,
      salesTrend: this.reportsApi.named('sales/trend', from, to).pipe(catchError(() => of(null))),
      purchaseTrend: this.reportsApi.named('procurement/trend', from, to).pipe(catchError(() => of(null))),
      arAging: this.reportsApi.arAging(to).pipe(catchError(() => of(null))),
      cashFlow: this.reportsApi.cashFlow(from, to).pipe(catchError(() => of(null))),
      warehouses: this.reportsApi.warehouseComparison().pipe(catchError(() => of([] as WarehouseComparisonLine[]))),
      customers: this.reportsApi.named('sales/by-customer', from, to).pipe(catchError(() => of(null))),
      prices: this.reportsApi.named('procurement/price-analysis', from, to).pipe(catchError(() => of(null))),
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.chartsLoading.set(false);
          this.lastUpdated.set(new Date());
          this.statusLive.set('Dashboard updated.');
          if (res.executive) {
            this.executive.set(res.executive);
            this.execError.set('');
          } else if (this.canExecutive) {
            this.execError.set('Executive KPIs unavailable.');
          }
          this.salesTrend.set(res.salesTrend);
          this.purchaseTrend.set(res.purchaseTrend);
          this.arAging.set(res.arAging);
          this.cashFlow.set(res.cashFlow);
          this.warehouses.set(res.warehouses ?? []);
          this.topCustomers.set(
            (res.customers?.rows ?? []).slice(0, 5).map((r) => ({ name: r.name, amount: r.amount })),
          );
          this.priceAnalysis.set(res.prices);
          this.rebuildPrimaryFromExecutive(res.executive, res.priorExec, res.salesTrend, res.purchaseTrend);
          this.rebuildMetricGroups(res.executive);
          this.rebuildActions();
        },
        error: () => {
          this.chartsLoading.set(false);
          this.execError.set('Unable to load executive board widgets.');
          this.statusLive.set('Dashboard refresh failed.');
        },
      });
  }

  private rebuildPrimaryFromSummary(): void {
    const data = this.summary();
    if (!data || this.primaryKpis().length) return;
    const colors = this.chartColors();
    this.primaryKpis.set([
      this.kpi('Sales (Invoices)', data.salesToday, 'Posted sales today', '', null, null, [], colors[0]),
      this.kpi('Purchases', data.purchasesToday, 'Posted purchases today', '', null, null, [], colors[1]),
      this.kpi('Receivables', data.receivables, 'Customer balances', '', null, null, [], colors[2]),
      this.kpi('Payables', data.payables, 'Supplier balances', '', null, null, [], colors[4]),
    ]);
  }

  private rebuildPrimaryFromExecutive(
    exec: ExecutiveDashboard | null,
    prior: ExecutiveDashboard | null,
    salesTrend: NamedReport | null,
    purchaseTrend: NamedReport | null,
  ): void {
    const summary = this.summary();
    const sales = this.kpiValue(exec, 'sales', 'sales') ?? summary?.salesToday ?? 0;
    const purchases = this.kpiValue(exec, 'procurement', 'purchases') ?? summary?.purchasesToday ?? 0;
    const ar = this.kpiValue(exec, 'sales', 'ar') ?? summary?.receivables ?? 0;
    const ap = this.kpiValue(exec, 'procurement', 'ap') ?? summary?.payables ?? 0;

    const priorSales = this.kpiValue(prior, 'sales', 'sales');
    const priorPurchases = this.kpiValue(prior, 'procurement', 'purchases');
    const priorAr = this.kpiValue(prior, 'sales', 'ar');
    const priorAp = this.kpiValue(prior, 'procurement', 'ap');

    const colors = this.chartColors();
    this.primaryKpis.set([
      this.kpi(
        'Sales (Invoices)',
        sales,
        'Posted invoices in range',
        this.compareLabel(priorSales),
        this.pctDelta(sales, priorSales),
        null,
        (salesTrend?.rows ?? []).length > 1
          ? (salesTrend?.rows ?? []).map((r) => r.amount)
          : this.sparkFrom(priorSales, sales),
        colors[0],
      ),
      this.kpi(
        'Purchases',
        purchases,
        'Posted purchases in range',
        this.compareLabel(priorPurchases),
        this.pctDelta(purchases, priorPurchases),
        null,
        (purchaseTrend?.rows ?? []).length > 1
          ? (purchaseTrend?.rows ?? []).map((r) => r.amount)
          : this.sparkFrom(priorPurchases, purchases),
        colors[1],
      ),
      this.kpi(
        'Receivables',
        ar,
        'Outstanding AR',
        this.compareLabel(priorAr),
        this.pctDelta(ar, priorAr),
        null,
        this.sparkFrom(priorAr, ar),
        colors[2],
      ),
      this.kpi(
        'Payables',
        ap,
        'Outstanding AP',
        this.compareLabel(priorAp),
        this.pctDelta(ap, priorAp),
        null,
        this.sparkFrom(priorAp, ap),
        colors[4],
      ),
    ]);
  }

  private rebuildMetricGroups(exec: ExecutiveDashboard | null): void {
    if (!exec?.sections?.length) {
      this.metricGroups.set([]);
      return;
    }
    const flat: ReportKpi[] = exec.sections.flatMap((s) => s.kpis);
    const countLike = new Set(['low', 'employees', 'qty', 'flocks', 'mortality', 'weighments', 'cashbanks', 'netWeight']);
    const preferred = ['value', 'low', 'qty', 'flocks', 'mortality', 'payroll', 'employees', 'weighments', 'netWeight', 'cashbanks'];
    const tiles: MetricTile[] = [];
    for (const key of preferred) {
      const k = flat.find((x) => x.key === key);
      if (!k) continue;
      tiles.push({
        key: k.key,
        label: k.label,
        value: k.value,
        unit: k.unit,
        currency: !countLike.has(k.key),
        route: this.drillPath(k.drillDown),
      });
    }
    this.metricGroups.set(tiles.length ? [{ title: 'At a glance', tiles }] : []);
  }

  private rebuildActions(): void {
    const items: ActionItem[] = [];
    const pos = this.posToday();
    if (pos && pos.saleCount > 0) {
      items.push({
        label: `POS today — ${this.money.format(pos.netSales)}`,
        detail: `${pos.saleCount} tickets · cash ${this.money.format(pos.cashTotal)}`,
        route: '/pos/reports',
      });
    } else {
      items.push({
        label: 'Open POS terminal',
        detail: 'Scan-first sweets checkout',
        route: '/pos/terminal',
      });
    }
    const profit = this.posProfit();
    if (profit) {
      items.push({
        label: `POS gross profit — ${this.money.format(profit.grossProfit)}`,
        detail: `COGS ${this.money.format(profit.cogs)} · today`,
        route: '/pos/reports',
      });
    }
    // Pending approvals shown in their own table — do not duplicate here
    const low = this.kpiValue(this.executive(), 'inventory', 'low');
    if (low != null && low > 0) {
      items.push({
        label: `Low stock items — ${this.number.format(low)}`,
        detail: 'SKUs at or below reorder level',
        route: '/inventory',
      });
    }
    const ar = this.arAging();
    const overdue = ar?.rows?.filter((r) => (r.days61To90 ?? 0) + (r.days90Plus ?? 0) > 0).length ?? 0;
    if (overdue > 0) {
      items.push({
        label: `Overdue receivables — ${this.number.format(overdue)} parties`,
        detail: 'Balances aged 61+ days',
        route: '/reports',
      });
    }
    this.actionItems.set(items);
  }

  private kpi(
    label: string,
    value: number,
    hint: string,
    compareHint: string,
    delta: number | null,
    trend: KpiTrend | null,
    sparkline: number[],
    accent: string,
  ): PrimaryKpi {
    return { label, value, hint, compareHint, delta, trend, sparkline, accent };
  }

  private kpiValue(exec: ExecutiveDashboard | null | undefined, section: string, key: string): number | null {
    const sec = exec?.sections?.find((s) => s.key === section);
    const k = sec?.kpis?.find((x) => x.key === key);
    return k ? k.value : null;
  }

  /** Hide meaningless +100% when prior period was zero, and suppress ~0%. */
  private pctDelta(current: number, prior: number | null | undefined): number | null {
    if (prior == null || prior === 0) return null;
    const d = ((current - prior) / Math.abs(prior)) * 100;
    return Math.abs(d) < 0.05 ? null : d;
  }

  private sparkFrom(prior: number | null | undefined, current: number): number[] {
    if (prior != null) return [prior, current];
    return [current * 0.92, current];
  }

  private compareLabel(prior: number | null | undefined): string {
    if (prior == null) return '';
    return `vs prior period: ${this.money.format(prior)}`;
  }

  private priorPeriod(from: string, to: string): { from: string; to: string } {
    const start = new Date(from);
    const end = new Date(to);
    const days = Math.max(1, Math.round((end.getTime() - start.getTime()) / 86400000) + 1);
    const priorEnd = new Date(start);
    priorEnd.setDate(priorEnd.getDate() - 1);
    const priorStart = new Date(priorEnd);
    priorStart.setDate(priorStart.getDate() - (days - 1));
    return { from: this.iso(priorStart), to: this.iso(priorEnd) };
  }

  private iso(d: Date): string {
    const y = d.getFullYear();
    const m = `${d.getMonth() + 1}`.padStart(2, '0');
    const day = `${d.getDate()}`.padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  drillPath(drill?: string | null): string {
    if (!drill) return '/reports';
    return drill.split('?')[0] || '/reports';
  }
}
