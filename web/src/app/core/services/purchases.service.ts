import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';

export type DocumentStatusName =
  | 'Draft'
  | 'Submitted'
  | 'Verified'
  | 'Approved'
  | 'Posted'
  | 'Cancelled';

export type DocTransitionAction = 'submit' | 'verify' | 'approve' | 'post' | 'cancel';

export interface PurchaseLine {
  id?: string;
  productId: string;
  productName?: string;
  qty: number;
  rate: number;
  amount: number;
}

export interface PurchaseOrder {
  id: string;
  orderNo: string;
  date: string;
  partyId: string;
  partyName?: string;
  warehouseId?: string | null;
  status: DocumentStatusName | number | string;
  subTotal: number;
  tax: number;
  grandTotal: number;
  remarks?: string;
  lines: PurchaseLine[];
}

export interface GoodsReceipt {
  id: string;
  receiptNo: string;
  date: string;
  partyId: string;
  partyName?: string;
  warehouseId: string;
  purchaseOrderId?: string | null;
  status: DocumentStatusName | number | string;
  remarks?: string;
  lines: PurchaseLine[];
}

export interface PurchaseInvoice {
  id: string;
  invoiceNo: string;
  date: string;
  partyId: string;
  partyName?: string;
  status: DocumentStatusName | number | string;
  subTotal: number;
  discount: number;
  tax: number;
  grandTotal: number;
  remarks?: string;
  lines: PurchaseLine[];
  goodsReceiptId?: string | null;
  purchaseOrderId?: string | null;
}

export interface PayableBalance {
  partyId: string;
  partyName?: string;
  balance: number;
}

export interface AuditLogEntry {
  id: string;
  entityName: string;
  entityId: string;
  action: string;
  oldValues?: string | null;
  newValues?: string | null;
  userId?: string | null;
  timestamp: string;
}

export interface PurchaseOrderCreatePayload {
  date: string;
  partyId: string;
  warehouseId?: string | null;
  tax: number;
  remarks?: string;
  post?: boolean;
  lines: PurchaseLine[];
}

export interface GoodsReceiptCreatePayload {
  date: string;
  partyId: string;
  warehouseId: string;
  purchaseOrderId?: string | null;
  remarks?: string;
  post?: boolean;
  lines: PurchaseLine[];
}

export interface PurchaseInvoiceCreatePayload {
  date: string;
  partyId: string;
  discount: number;
  tax: number;
  remarks?: string;
  post?: boolean;
  lines: PurchaseLine[];
  goodsReceiptId?: string | null;
  purchaseOrderId?: string | null;
}

@Injectable({ providedIn: 'root' })
export class PurchasesService {
  private readonly url = `${API_BASE_URL}/purchases`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<PurchaseInvoice[]> {
    return this.http.get<PurchaseInvoice[]>(this.url);
  }

  create(payload: Partial<PurchaseInvoice> | PurchaseInvoiceCreatePayload): Observable<PurchaseInvoice> {
    return this.http.post<PurchaseInvoice>(this.url, payload);
  }

  getOrders(): Observable<PurchaseOrder[]> {
    return this.http.get<PurchaseOrder[]>(`${this.url}/orders`);
  }

  createOrder(payload: PurchaseOrderCreatePayload): Observable<PurchaseOrder> {
    return this.http.post<PurchaseOrder>(`${this.url}/orders`, payload);
  }

  updateOrder(id: string, payload: PurchaseOrderCreatePayload): Observable<PurchaseOrder> {
    return this.http.put<PurchaseOrder>(`${this.url}/orders/${id}`, payload);
  }

  transitionOrder(id: string, action: DocTransitionAction): Observable<PurchaseOrder> {
    return this.http.post<PurchaseOrder>(`${this.url}/orders/${id}/${action}`, {});
  }

  getReceipts(): Observable<GoodsReceipt[]> {
    return this.http.get<GoodsReceipt[]>(`${this.url}/receipts`);
  }

  createReceipt(payload: GoodsReceiptCreatePayload): Observable<GoodsReceipt> {
    return this.http.post<GoodsReceipt>(`${this.url}/receipts`, payload);
  }

  transitionReceipt(id: string, action: DocTransitionAction): Observable<GoodsReceipt> {
    return this.http.post<GoodsReceipt>(`${this.url}/receipts/${id}/${action}`, {});
  }

  transitionInvoice(id: string, action: DocTransitionAction): Observable<PurchaseInvoice> {
    return this.http.post<PurchaseInvoice>(`${this.url}/invoices/${id}/${action}`, {});
  }

  getPayables(partyId: string): Observable<PayableBalance> {
    return this.http.get<PayableBalance>(`${this.url}/payables/${partyId}`);
  }

  getAudit(entityName: 'PurchaseOrder' | 'GoodsReceipt' | 'PurchaseInvoice', id: string): Observable<AuditLogEntry[]> {
    return this.http.get<AuditLogEntry[]>(`${this.url}/audit/${entityName}/${id}`);
  }
}
