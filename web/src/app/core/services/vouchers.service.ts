import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Voucher } from '../models/erp.models';
import { API_BASE_URL } from '../models/api.constants';

@Injectable({ providedIn: 'root' })
export class VouchersService {
  private readonly url = `${API_BASE_URL}/vouchers`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<Voucher[]> {
    return this.http.get<Voucher[]>(this.url);
  }

  create(payload: Partial<Voucher>): Observable<Voucher> {
    return this.http.post<Voucher>(this.url, payload);
  }
}
