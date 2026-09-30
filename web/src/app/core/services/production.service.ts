import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';

export interface BomLine {
  id?: string;
  componentProductId: string;
  componentProductName?: string;
  qtyPer: number;
  uomId?: string | null;
  uomCode?: string | null;
  scrapPercent?: number;
  lineNo?: number;
}

export interface Bom {
  id: string;
  code: string;
  name: string;
  finishedProductId: string;
  finishedProductName?: string;
  version: number;
  effectiveFrom: string;
  effectiveTo?: string | null;
  scrapPercent: number;
  isActive: boolean;
  lines: BomLine[];
}

export interface ProductionComponent {
  id?: string;
  productId: string;
  productCode?: string;
  productName?: string;
  plannedQty: number;
  actualQty: number;
  unitCost: number;
  totalCost: number;
}

export interface ProductionOutput {
  id?: string;
  productId: string;
  productCode?: string;
  productName?: string;
  qty: number;
  unitCost: number;
  totalCost: number;
  isByProduct: boolean;
}

export interface ProductionVariance {
  id?: string;
  kind: string;
  productId: string;
  productCode?: string;
  productName?: string;
  qty: number;
  unitCost: number;
  totalCost: number;
  narration?: string;
}

export interface ProductionOrder {
  id: string;
  orderNo: string;
  orderDate: string;
  bomId: string;
  bomCode?: string;
  finishedProductId: string;
  finishedProductName?: string;
  warehouseId: string;
  warehouseName?: string;
  qtyOrdered: number;
  qtyCompleted: number;
  status: string;
  documentStatus: number | string;
  materialCost: number;
  laborCost: number;
  overheadCost: number;
  totalCost: number;
  unitCost: number;
  remarks?: string;
  components: ProductionComponent[];
  outputs: ProductionOutput[];
  variances: ProductionVariance[];
}

export interface ProductionCosting {
  orderId: string;
  orderNo: string;
  materialCost: number;
  laborCost: number;
  overheadCost: number;
  totalCost: number;
  unitCost: number;
  qtyCompleted: number;
}

@Injectable({ providedIn: 'root' })
export class ProductionService {
  constructor(private readonly http: HttpClient) {}

  getBoms(): Observable<Bom[]> {
    return this.http.get<Bom[]>(`${API_BASE_URL}/production/boms`);
  }

  getBom(id: string): Observable<Bom> {
    return this.http.get<Bom>(`${API_BASE_URL}/production/boms/${id}`);
  }

  createBom(body: unknown): Observable<Bom> {
    return this.http.post<Bom>(`${API_BASE_URL}/production/boms`, body);
  }

  getOrders(): Observable<ProductionOrder[]> {
    return this.http.get<ProductionOrder[]>(`${API_BASE_URL}/production/orders`);
  }

  getOrder(id: string): Observable<ProductionOrder> {
    return this.http.get<ProductionOrder>(`${API_BASE_URL}/production/orders/${id}`);
  }

  createOrder(body: unknown): Observable<ProductionOrder> {
    return this.http.post<ProductionOrder>(`${API_BASE_URL}/production/orders`, body);
  }

  transition(id: string, action: string): Observable<ProductionOrder> {
    return this.http.post<ProductionOrder>(`${API_BASE_URL}/production/orders/${id}/${action}`, {});
  }

  addVariance(id: string, body: { kind: string; productId: string; qty: number; narration?: string }): Observable<ProductionOrder> {
    return this.http.post<ProductionOrder>(`${API_BASE_URL}/production/orders/${id}/variances`, body);
  }

  getCosting(orderId: string): Observable<ProductionCosting> {
    return this.http.get<ProductionCosting>(`${API_BASE_URL}/production/costing/${orderId}`);
  }
}
