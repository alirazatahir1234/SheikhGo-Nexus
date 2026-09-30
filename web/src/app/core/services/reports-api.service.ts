import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';

export interface NamedAmountRow {
  code: string;
  name: string;
  amount: number;
  qty?: number | null;
  extra?: string | null;
}

export interface NamedReport {
  title: string;
  from?: string | null;
  to?: string | null;
  asOf?: string | null;
  grandTotal: number;
  rows: NamedAmountRow[];
  totals?: Record<string, number> | null;
}

export interface ReportKpi {
  key: string;
  label: string;
  value: number;
  unit?: string | null;
  drillDown?: string | null;
}

export interface ReportSection {
  key: string;
  title: string;
  kpis: ReportKpi[];
}

export interface ExecutiveDashboard {
  from: string;
  to: string;
  sections: ReportSection[];
}

export interface CashFlowReport {
  from: string;
  to: string;
  openingCash: number;
  inflows: number;
  outflows: number;
  netChange: number;
  closingCash: number;
  lines: { category: string; label: string; amount: number }[];
}

export interface TrialBalanceReport {
  asOf: string;
  totalDebit: number;
  totalCredit: number;
  lines: {
    accountId: string;
    code: string;
    name: string;
    accountType: string;
    debit: number;
    credit: number;
    balance: number;
  }[];
}

export interface ProfitLossReport {
  from: string;
  to: string;
  revenue: number;
  costOfSales: number;
  grossProfit: number;
  expenses: number;
  netProfit: number;
}

export interface BalanceSheetReport {
  asOf: string;
  totalAssets: number;
  totalLiabilities: number;
  totalEquity: number;
}

export interface AgingReport {
  kind: string;
  asOf: string;
  grandTotal: number;
  rows: {
    partyCode: string;
    partyName: string;
    current: number;
    days1To30: number;
    days31To60: number;
    days61To90: number;
    days90Plus: number;
    total: number;
  }[];
}

export interface InventoryTurnover {
  from: string;
  to: string;
  cogs: number;
  avgInventoryValue: number;
  turnover: number;
}

export interface InventoryDemandForecastLine {
  productId: string;
  code: string;
  name: string;
  onHand: number;
  reorderLevel: number;
  avgDailyUsage: number;
  leadTimeDays: number;
  safetyProxy: number;
  onOrder: number;
  horizons: { days: number; projectedOnHand: number }[];
  stockOutRiskDays?: number | null;
  recommendedOrderQty: number;
  anomalyPct?: number | null;
  confidence: string;
  dataDays: number;
  message?: string | null;
}

export interface InventoryDemandForecastReport {
  asOf: string;
  companyId?: string | null;
  branchId?: string | null;
  lines: InventoryDemandForecastLine[];
  predictionId?: string | null;
}

export interface WarehouseComparisonLine {
  warehouseId: string;
  warehouseCode: string;
  skuCount: number;
  totalQty: number;
  totalValue: number;
}

@Injectable({ providedIn: 'root' })
export class ReportsApiService {
  constructor(private readonly http: HttpClient) {}

  executive(from?: string, to?: string): Observable<ExecutiveDashboard> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<ExecutiveDashboard>(`${API_BASE_URL}/dashboard/executive`, { params });
  }

  named(path: string, from?: string, to?: string, asOf?: string): Observable<NamedReport> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    if (asOf) params = params.set('asOf', asOf);
    return this.http.get<NamedReport>(`${API_BASE_URL}/reports/${path}`, { params });
  }

  warehouseComparison(): Observable<WarehouseComparisonLine[]> {
    return this.http.get<WarehouseComparisonLine[]>(`${API_BASE_URL}/reports/inventory/warehouse-comparison`);
  }

  cashFlow(from: string, to: string): Observable<CashFlowReport> {
    return this.http.get<CashFlowReport>(`${API_BASE_URL}/reports/finance/cash-flow`, {
      params: new HttpParams().set('from', from).set('to', to),
    });
  }

  trialBalance(asOf: string): Observable<TrialBalanceReport> {
    return this.http.get<TrialBalanceReport>(`${API_BASE_URL}/reports/finance/trial-balance`, {
      params: new HttpParams().set('asOf', asOf),
    });
  }

  profitLoss(from: string, to: string): Observable<ProfitLossReport> {
    return this.http.get<ProfitLossReport>(`${API_BASE_URL}/reports/finance/profit-loss`, {
      params: new HttpParams().set('from', from).set('to', to),
    });
  }

  balanceSheet(asOf: string): Observable<BalanceSheetReport> {
    return this.http.get<BalanceSheetReport>(`${API_BASE_URL}/reports/finance/balance-sheet`, {
      params: new HttpParams().set('asOf', asOf),
    });
  }

  arAging(asOf: string): Observable<AgingReport> {
    return this.http.get<AgingReport>(`${API_BASE_URL}/reports/finance/aging/receivables`, {
      params: new HttpParams().set('asOf', asOf),
    });
  }

  apAging(asOf: string): Observable<AgingReport> {
    return this.http.get<AgingReport>(`${API_BASE_URL}/reports/finance/aging/payables`, {
      params: new HttpParams().set('asOf', asOf),
    });
  }

  inventoryValuation(): Observable<unknown> {
    return this.http.get(`${API_BASE_URL}/reports/inventory/valuation`);
  }

  inventoryTurnover(from: string, to: string): Observable<InventoryTurnover> {
    return this.http.get<InventoryTurnover>(`${API_BASE_URL}/reports/inventory/turnover`, {
      params: new HttpParams().set('from', from).set('to', to),
    });
  }

  inventoryDemandForecast(asOf?: string): Observable<InventoryDemandForecastReport> {
    let params = new HttpParams();
    if (asOf) params = params.set('asOf', asOf);
    return this.http.get<InventoryDemandForecastReport>(`${API_BASE_URL}/reports/inventory/demand-forecast`, { params });
  }

  raw(path: string, from?: string, to?: string, asOf?: string): Observable<unknown> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    if (asOf) params = params.set('asOf', asOf);
    return this.http.get(`${API_BASE_URL}/reports/${path}`, { params });
  }

  exportBlob(domain: string, name: string, format: 'csv' | 'xlsx' | 'pdf', from?: string, to?: string, asOf?: string): Observable<Blob> {
    let params = new HttpParams().set('format', format);
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    if (asOf) params = params.set('asOf', asOf);
    return this.http.get(`${API_BASE_URL}/reports/${domain}/${name}/export`, {
      params,
      responseType: 'blob',
    });
  }
}
