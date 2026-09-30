import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
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
  grandTotal: number;
  status: number | string;
  salesOrderId?: string;
  deliveryOrderId?: string;
  salesInvoiceId?: string;
  remarks?: string;
  lines: PosSaleLine[];
  payments: PosPayment[];
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
  getSales(): Observable<PosSale[]> {
    return this.http.get<PosSale[]>(`${this.base}/sales`);
  }
  createSale(payload: {
    shiftId: string;
    saleDate?: string;
    remarks?: string;
    lines: PosSaleLine[];
    payments: PosPayment[];
  }): Observable<PosSale> {
    return this.http.post<PosSale>(`${this.base}/sales`, payload);
  }
}
