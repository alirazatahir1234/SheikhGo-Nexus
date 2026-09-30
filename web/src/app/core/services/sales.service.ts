import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';
import {
  AuditLogEntry,
  DocTransitionAction,
  DocumentWorkflowActors,
  PurchaseLine,
} from './purchases.service';

export type { DocTransitionAction, DocumentWorkflowActors, AuditLogEntry };

export interface SalesOrder {
  id: string;
  orderNo: string;
  date: string;
  partyId: string;
  partyName?: string;
  warehouseId?: string | null;
  status: string | number;
  subTotal: number;
  tax: number;
  grandTotal: number;
  remarks?: string;
  lines: PurchaseLine[];
  workflow?: DocumentWorkflowActors | null;
}

export interface DeliveryOrder {
  id: string;
  deliveryNo: string;
  date: string;
  partyId: string;
  partyName?: string;
  warehouseId: string;
  salesOrderId: string;
  status: string | number;
  remarks?: string;
  lines: PurchaseLine[];
  workflow?: DocumentWorkflowActors | null;
}

export interface SalesInvoiceDoc {
  id: string;
  invoiceNo: string;
  date: string;
  partyId: string;
  partyName?: string;
  deliveryOrderId?: string | null;
  salesOrderId?: string | null;
  status: string | number;
  subTotal: number;
  discount: number;
  tax: number;
  grandTotal: number;
  paidAmount: number;
  remarks?: string;
  lines: PurchaseLine[];
  workflow?: DocumentWorkflowActors | null;
}

export interface PaymentApplication {
  id?: string;
  salesInvoiceId: string;
  invoiceNo?: string;
  amountApplied: number;
}

export interface CustomerPayment {
  id: string;
  paymentNo: string;
  date: string;
  partyId: string;
  partyName?: string;
  amount: number;
  status: string | number;
  remarks?: string;
  applications: PaymentApplication[];
  workflow?: DocumentWorkflowActors | null;
}

export interface ReceivableBalance {
  partyId: string;
  partyName?: string;
  balance: number;
}

@Injectable({ providedIn: 'root' })
export class SalesService {
  private readonly url = `${API_BASE_URL}/sales`;

  constructor(private readonly http: HttpClient) {}

  getOrders(): Observable<SalesOrder[]> {
    return this.http.get<SalesOrder[]>(`${this.url}/orders`);
  }

  getOrder(id: string): Observable<SalesOrder> {
    return this.http.get<SalesOrder>(`${this.url}/orders/${id}`);
  }

  createOrder(payload: unknown): Observable<SalesOrder> {
    return this.http.post<SalesOrder>(`${this.url}/orders`, payload);
  }

  transitionOrder(id: string, action: DocTransitionAction): Observable<SalesOrder> {
    return this.http.post<SalesOrder>(`${this.url}/orders/${id}/${action}`, {});
  }

  getDeliveries(): Observable<DeliveryOrder[]> {
    return this.http.get<DeliveryOrder[]>(`${this.url}/deliveries`);
  }

  getDelivery(id: string): Observable<DeliveryOrder> {
    return this.http.get<DeliveryOrder>(`${this.url}/deliveries/${id}`);
  }

  createDelivery(payload: unknown): Observable<DeliveryOrder> {
    return this.http.post<DeliveryOrder>(`${this.url}/deliveries`, payload);
  }

  transitionDelivery(id: string, action: DocTransitionAction): Observable<DeliveryOrder> {
    return this.http.post<DeliveryOrder>(`${this.url}/deliveries/${id}/${action}`, {});
  }

  getInvoices(): Observable<SalesInvoiceDoc[]> {
    return this.http.get<SalesInvoiceDoc[]>(`${this.url}/invoices`);
  }

  getInvoice(id: string): Observable<SalesInvoiceDoc> {
    return this.http.get<SalesInvoiceDoc>(`${this.url}/invoices/${id}`);
  }

  createInvoice(payload: unknown): Observable<SalesInvoiceDoc> {
    return this.http.post<SalesInvoiceDoc>(`${this.url}/invoices`, payload);
  }

  transitionInvoice(id: string, action: DocTransitionAction): Observable<SalesInvoiceDoc> {
    return this.http.post<SalesInvoiceDoc>(`${this.url}/invoices/${id}/${action}`, {});
  }

  getPayments(): Observable<CustomerPayment[]> {
    return this.http.get<CustomerPayment[]>(`${this.url}/payments`);
  }

  getPayment(id: string): Observable<CustomerPayment> {
    return this.http.get<CustomerPayment>(`${this.url}/payments/${id}`);
  }

  createPayment(payload: unknown): Observable<CustomerPayment> {
    return this.http.post<CustomerPayment>(`${this.url}/payments`, payload);
  }

  transitionPayment(id: string, action: DocTransitionAction): Observable<CustomerPayment> {
    return this.http.post<CustomerPayment>(`${this.url}/payments/${id}/${action}`, {});
  }

  getReceivable(partyId: string): Observable<ReceivableBalance> {
    return this.http.get<ReceivableBalance>(`${this.url}/receivables/${partyId}`);
  }

  getAudit(
    entityName: 'SalesOrder' | 'DeliveryOrder' | 'SalesInvoice' | 'CustomerPayment',
    id: string,
  ): Observable<AuditLogEntry[]> {
    return this.http.get<AuditLogEntry[]>(`${this.url}/audit/${entityName}/${id}`);
  }
}
