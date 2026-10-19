import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';

export interface PosTerminal {
  id: string;
  code: string;
  name: string;
  warehouseId: string;
  warehouseName?: string;
  branchId?: string;
  isActive: boolean;
}
export interface PosShift {
  id: string;
  shiftNo: string;
  terminalId: string;
  terminalCode?: string;
  openedAt: string;
  openingCash: number;
  closedAt?: string;
  expectedCash?: number;
  actualCash?: number;
  variance?: number;
  status: string;
}
export interface PosSaleLine {
  id?: string;
  productId: string;
  productName?: string;
  qty: number;
  rate: number;
  amount: number;
}
export interface PosPayment {
  id?: string;
  tender: string;
  amount: number;
  cashBankAccountId?: string;
  customerPaymentId?: string;
}
export interface PosSale {
  id: string;
  saleNo: string;
  shiftId: string;
  terminalId: string;
  partyId: string;
  partyName?: string;
  saleDate: string;
  subTotal: number;
  discount: number;
  grandTotal: number;
  status: number | string;
  salesOrderId?: string;
  deliveryOrderId?: string;
  salesInvoiceId?: string;
  remarks?: string;
  lines: PosSaleLine[];
  payments: PosPayment[];
  idempotencyKey?: string;
  saleReturnId?: string;
}
export interface PosDailySalesReport {
  date: string;
  saleCount: number;
  grossSales: number;
  discounts: number;
  netSales: number;
  cashTotal: number;
  cardTotal: number;
  otherTotal: number;
  byCashier: { userId: string; userName?: string; saleCount: number; netSales: number }[];
  topProducts: { productId: string; productName?: string; qty: number; amount: number }[];
  returnedSaleCount?: number;
  cashRefundsExcluded?: number;
}
export interface PosOwnerProfitReport {
  date: string;
  netSales: number;
  cogs: number;
  grossProfit: number;
  expenses: number;
  operatingProfit: number;
}
export interface PosShiftReconciliation {
  shiftId: string;
  shiftNo: string;
  status: string;
  openingCash: number;
  cashSales: number;
  cardSales: number;
  otherSales: number;
  cashRefunds: number;
  saleCount: number;
  returnedSaleCount: number;
  expectedCash: number;
  actualCash?: number | null;
  variance?: number | null;
}

@Injectable({ providedIn: 'root' })
export class PosApiService {
  private readonly base = `${API_BASE_URL}/pos`;
  constructor(private readonly http: HttpClient) {}

  getTerminals(): Observable<PosTerminal[]> {
    return this.http.get<PosTerminal[]>(`${this.base}/terminals`);
  }
  createTerminal(payload: Partial<PosTerminal>): Observable<PosTerminal> {
    return this.http.post<PosTerminal>(`${this.base}/terminals`, payload);
  }
  getShifts(): Observable<PosShift[]> {
    return this.http.get<PosShift[]>(`${this.base}/shifts`);
  }
  openShift(payload: { terminalId: string; openingCash: number }): Observable<PosShift> {
    return this.http.post<PosShift>(`${this.base}/shifts/open`, payload);
  }
  closeShift(id: string, actualCash: number): Observable<PosShift> {
    return this.http.post<PosShift>(`${this.base}/shifts/${id}/close`, { actualCash });
  }
  shiftReconciliation(id: string): Observable<PosShiftReconciliation> {
    return this.http.get<PosShiftReconciliation>(`${this.base}/shifts/${id}/reconciliation`);
  }
  getSales(): Observable<PosSale[]> {
    return this.http.get<PosSale[]>(`${this.base}/sales`);
  }
  getSale(id: string): Observable<PosSale> {
    return this.http.get<PosSale>(`${this.base}/sales/${id}`);
  }
  createSale(payload: {
    shiftId: string;
    saleDate?: string;
    remarks?: string;
    discount?: number;
    idempotencyKey?: string;
    partyId?: string | null;
    lines: PosSaleLine[];
    payments: PosPayment[];
  }): Observable<PosSale> {
    return this.http.post<PosSale>(`${this.base}/sales`, payload);
  }
  returnSale(
    id: string,
    payload?: { remarks?: string; lines?: PosSaleLine[] },
  ): Observable<PosSale> {
    return this.http.post<PosSale>(`${this.base}/sales/${id}/returns`, {
      posSaleId: id,
      remarks: payload?.remarks ?? null,
      lines: payload?.lines ?? null,
    });
  }
  dailySales(date?: string): Observable<PosDailySalesReport> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);
    return this.http.get<PosDailySalesReport>(`${this.base}/reports/daily-sales`, { params });
  }
  ownerProfit(date?: string): Observable<PosOwnerProfitReport> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);
    return this.http.get<PosOwnerProfitReport>(`${this.base}/reports/owner-profit`, { params });
  }
}
