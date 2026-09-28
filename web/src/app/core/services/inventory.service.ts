import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { StockAdjustment, StockItem } from '../models/erp.models';
import { API_BASE_URL } from '../models/api.constants';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  constructor(private readonly http: HttpClient) {}

  getStock(): Observable<StockItem[]> {
    return this.http.get<StockItem[]>(`${API_BASE_URL}/inventory/stock`);
  }

  getAdjustments(): Observable<StockAdjustment[]> {
    return this.http.get<StockAdjustment[]>(`${API_BASE_URL}/inventory/adjustments`);
  }

  createAdjustment(payload: {
    date: string;
    productId: string;
    qtyChange: number;
    reason: string;
    remarks?: string;
  }): Observable<StockAdjustment> {
    return this.http.post<StockAdjustment>(`${API_BASE_URL}/inventory/adjustments`, payload);
  }
}
