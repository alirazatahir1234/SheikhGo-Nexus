import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';

export interface LedgerReport {
  accountId: string;
  accountCode: string;
  accountName: string;
  from: string;
  to: string;
  openingBalance: number;
  closingBalance: number;
  lines: {
    date: string;
    voucherNo: string;
    voucherType: string;
    narration?: string;
    debit: number;
    credit: number;
    balance: number;
  }[];
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

export interface AgingReport {
  kind: 'AR' | 'AP' | string;
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

@Injectable({ providedIn: 'root' })
export class ReportsService {
  constructor(private readonly http: HttpClient) {}

  ledger(accountId: string, from: string, to: string): Observable<LedgerReport> {
    const params = new HttpParams().set('accountId', accountId).set('from', from).set('to', to);
    return this.http.get<LedgerReport>(`${API_BASE_URL}/reports/ledger`, { params });
  }

  trialBalance(asOf: string): Observable<TrialBalanceReport> {
    return this.http.get<TrialBalanceReport>(`${API_BASE_URL}/reports/trial-balance`, {
      params: new HttpParams().set('asOf', asOf),
    });
  }

  receivablesAging(asOf?: string): Observable<AgingReport> {
    let params = new HttpParams();
    if (asOf) params = params.set('asOf', asOf);
    return this.http.get<AgingReport>(`${API_BASE_URL}/reports/aging/receivables`, { params });
  }

  payablesAging(asOf?: string): Observable<AgingReport> {
    let params = new HttpParams();
    if (asOf) params = params.set('asOf', asOf);
    return this.http.get<AgingReport>(`${API_BASE_URL}/reports/aging/payables`, { params });
  }
}
