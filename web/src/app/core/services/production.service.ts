import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';

export interface BomLine {
  id?: string;
  rawMaterialId: string;
  rawMaterialName?: string;
  qty: number;
}
export interface Bom {
  id: string;
  code: string;
  name: string;
  finishedProductId: string;
  finishedProductName?: string;
  outputQty: number;
  isActive: boolean;
  lines: BomLine[];
}
export interface ProductionOrder {
  id: string;
  orderNo: string;
  bomId: string;
  bomName?: string;
  date: string;
  batchQty: number;
  outputQty: number;
  status: string;
  remarks?: string;
}

@Injectable({ providedIn: 'root' })
export class ProductionService {
  constructor(private readonly http: HttpClient) {}
  getBoms(): Observable<Bom[]> {
    return this.http.get<Bom[]>(`${API_BASE_URL}/production/boms`);
  }
  createBom(body: unknown): Observable<Bom> {
    return this.http.post<Bom>(`${API_BASE_URL}/production/boms`, body);
  }
  getOrders(): Observable<ProductionOrder[]> {
    return this.http.get<ProductionOrder[]>(`${API_BASE_URL}/production/orders`);
  }
  createOrder(body: unknown): Observable<ProductionOrder> {
    return this.http.post<ProductionOrder>(`${API_BASE_URL}/production/orders`, body);
  }
}
