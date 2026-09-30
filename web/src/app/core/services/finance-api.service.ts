import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';

export interface JournalLine {
  id?: string;
  accountId: string;
  accountCode?: string;
  accountName?: string;
  debit: number;
  credit: number;
  description?: string;
}

export interface JournalEntry {
  id: string;
  journalNo: string;
  postingDate: string;
  sourceType: string | number;
  sourceEntityId?: string | null;
  status: string | number;
  narration?: string;
  totalDebit: number;
  totalCredit: number;
  lines: JournalLine[];
}

export interface CashBankAccount {
  id: string;
  code: string;
  name: string;
  accountKind: string | number;
  glAccountId: string;
  glAccountCode?: string;
  isActive: boolean;
  isDefault: boolean;
}

export interface SupplierPayment {
  id: string;
  paymentNo: string;
  date: string;
  partyId: string;
  partyName?: string;
  cashBankAccountId?: string | null;
  amount: number;
  status: string | number;
  remarks?: string;
  applications: { salesInvoiceId?: string; purchaseInvoiceId: string; invoiceNo?: string; amountApplied: number }[];
}

export interface FiscalPeriod {
  id: string;
  financialYearId: string;
  name: string;
  periodNumber: number;
  startDate: string;
  endDate: string;
  isClosed: boolean;
}

export interface TrialBalanceReport {
  asOf: string;
  totalDebit: number;
  totalCredit: number;
  lines: { accountId: string; code: string; name: string; accountType: string; debit: number; credit: number; balance: number }[];
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
  assets: { code: string; name: string; amount: number }[];
  liabilities: { code: string; name: string; amount: number }[];
  equity: { code: string; name: string; amount: number }[];
}

export interface LedgerReport {
  accountId: string;
  accountCode: string;
  accountName: string;
  from: string;
  to: string;
  openingBalance: number;
  closingBalance: number;
  lines: { date: string; voucherNo: string; debit: number; credit: number; balance: number; narration?: string }[];
}

@Injectable({ providedIn: 'root' })
export class FinanceApiService {
  private readonly url = `${API_BASE_URL}/finance`;
  private readonly reports = `${API_BASE_URL}/reports`;

  constructor(private readonly http: HttpClient) {}

  getAccounts() {
    return this.http.get<unknown[]>(`${this.url}/accounts`);
  }

  getJournals(): Observable<JournalEntry[]> {
    return this.http.get<JournalEntry[]>(`${this.url}/journals`);
  }

  createJournal(payload: unknown): Observable<JournalEntry> {
    return this.http.post<JournalEntry>(`${this.url}/journals`, payload);
  }

  postJournal(id: string): Observable<JournalEntry> {
    return this.http.post<JournalEntry>(`${this.url}/journals/${id}/post`, {});
  }

  getCashBank(): Observable<CashBankAccount[]> {
    return this.http.get<CashBankAccount[]>(`${this.url}/cash-bank`);
  }

  getSupplierPayments(): Observable<SupplierPayment[]> {
    return this.http.get<SupplierPayment[]>(`${this.url}/supplier-payments`);
  }

  createSupplierPayment(payload: unknown): Observable<SupplierPayment> {
    return this.http.post<SupplierPayment>(`${this.url}/supplier-payments`, payload);
  }

  transitionSupplierPayment(id: string, action: string): Observable<SupplierPayment> {
    return this.http.post<SupplierPayment>(`${this.url}/supplier-payments/${id}/${action}`, {});
  }

  getPeriods(): Observable<FiscalPeriod[]> {
    return this.http.get<FiscalPeriod[]>(`${this.url}/periods`);
  }

  closePeriod(id: string): Observable<FiscalPeriod> {
    return this.http.post<FiscalPeriod>(`${this.url}/periods/${id}/close`, {});
  }

  openPeriod(id: string): Observable<FiscalPeriod> {
    return this.http.post<FiscalPeriod>(`${this.url}/periods/${id}/open`, {});
  }

  trialBalance(asOf: string): Observable<TrialBalanceReport> {
    return this.http.get<TrialBalanceReport>(`${this.reports}/trial-balance`, { params: { asOf } });
  }

  profitLoss(from: string, to: string): Observable<ProfitLossReport> {
    return this.http.get<ProfitLossReport>(`${this.reports}/profit-loss`, { params: { from, to } });
  }

  balanceSheet(asOf: string): Observable<BalanceSheetReport> {
    return this.http.get<BalanceSheetReport>(`${this.reports}/balance-sheet`, { params: { asOf } });
  }

  ledger(accountId: string, from: string, to: string): Observable<LedgerReport> {
    return this.http.get<LedgerReport>(`${this.reports}/ledger`, { params: { accountId, from, to } });
  }
}
