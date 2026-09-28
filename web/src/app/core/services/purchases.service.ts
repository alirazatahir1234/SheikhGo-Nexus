import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PurchaseInvoice } from '../models/erp.models';
import { API_BASE_URL } from '../models/api.constants';

@Injectable({ providedIn: 'root' })
export class PurchasesService {
  private readonly url = `${API_BASE_URL}/purchases`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<PurchaseInvoice[]> {
    return this.http.get<PurchaseInvoice[]>(this.url);
  }

  create(payload: Partial<PurchaseInvoice>): Observable<PurchaseInvoice> {
    return this.http.post<PurchaseInvoice>(this.url, payload);
  }
}
