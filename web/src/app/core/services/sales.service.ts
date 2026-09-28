import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SaleInvoice } from '../models/erp.models';
import { API_BASE_URL } from '../models/api.constants';

@Injectable({ providedIn: 'root' })
export class SalesService {
  private readonly url = `${API_BASE_URL}/sales`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<SaleInvoice[]> {
    return this.http.get<SaleInvoice[]>(this.url);
  }

  create(payload: Partial<SaleInvoice>): Observable<SaleInvoice> {
    return this.http.post<SaleInvoice>(this.url, payload);
  }
}
