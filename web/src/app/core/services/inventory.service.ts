import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';

export interface StockRow {
  productId: string;
  code: string;
  name: string;
  unit: string;
  stockQty: number;
  reorderLevel: number;
  lowStock: boolean;
  warehouseId?: string;
  warehouseCode?: string;
  averageCost?: number;
  value?: number;
}

export interface StockTransfer {
  id: string;
  transferNo: string;
  date: string;
  fromWarehouseId: string;
  fromWarehouseName?: string;
  toWarehouseId: string;
  toWarehouseName?: string;
  status: string | number;
  remarks?: string;
  lines: { productId: string; productName?: string; qty: number }[];
}

export interface StockAdjustmentDoc {
  id: string;
  adjustmentNo: string;
  date: string;
  warehouseId: string;
  warehouseName?: string;
  status: string | number;
  reason: string;
  remarks?: string;
  lines: { productId: string; productName?: string; qtyDelta: number }[];
}

export interface StockCountDoc {
  id: string;
  countNo: string;
  date: string;
  warehouseId: string;
  warehouseName?: string;
  status: string | number;
  snapshotAt?: string;
  lines: { productId: string; productName?: string; systemQty: number; countedQty: number; variance: number }[];
}

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly url = `${API_BASE_URL}/inventory`;

  constructor(private readonly http: HttpClient) {}

  getStock(warehouseId?: string): Observable<StockRow[]> {
    let params = new HttpParams();
    if (warehouseId) params = params.set('warehouseId', warehouseId);
    return this.http.get<StockRow[]>(`${this.url}/stock`, { params });
  }

  getLedger(from: string, to: string, productId?: string, warehouseId?: string) {
    let params = new HttpParams().set('from', from).set('to', to);
    if (productId) params = params.set('productId', productId);
    if (warehouseId) params = params.set('warehouseId', warehouseId);
    return this.http.get<unknown[]>(`${this.url}/ledger`, { params });
  }

  getValuation(warehouseId?: string) {
    let params = new HttpParams();
    if (warehouseId) params = params.set('warehouseId', warehouseId);
    return this.http.get<unknown[]>(`${this.url}/valuation`, { params });
  }

  getTransfers() {
    return this.http.get<StockTransfer[]>(`${API_BASE_URL}/stock-transfers`);
  }

  createTransfer(payload: unknown) {
    return this.http.post<StockTransfer>(`${API_BASE_URL}/stock-transfers`, payload);
  }

  getAdjustments() {
    return this.http.get<StockAdjustmentDoc[]>(`${this.url}/adjustments`);
  }

  createAdjustment(payload: unknown) {
    return this.http.post<StockAdjustmentDoc>(`${this.url}/adjustments`, payload);
  }

  getCounts() {
    return this.http.get<StockCountDoc[]>(`${this.url}/stock-counts`);
  }

  createCount(payload: unknown) {
    return this.http.post<StockCountDoc>(`${this.url}/stock-counts`, payload);
  }

  snapshotCount(id: string) {
    return this.http.post<StockCountDoc>(`${this.url}/stock-counts/${id}/snapshot`, {});
  }

  postCount(id: string) {
    return this.http.post<StockCountDoc>(`${this.url}/stock-counts/${id}/post`, {});
  }
}
