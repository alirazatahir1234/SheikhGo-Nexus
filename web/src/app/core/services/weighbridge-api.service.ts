import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';

export interface WeighbridgeScale {
  id: string;
  code: string;
  name: string;
  location?: string;
  isActive: boolean;
}
export interface Weighment {
  id: string;
  ticketNo: string;
  date: string;
  direction: string;
  vehicleNo?: string;
  vehicleId?: string;
  partyId?: string;
  partyName?: string;
  productId?: string;
  productName?: string;
  firstWeight: number;
  secondWeight?: number;
  netWeight?: number;
  status: string;
  gatePassId?: string;
  remarks?: string;
}

@Injectable({ providedIn: 'root' })
export class WeighbridgeApiService {
  private readonly base = `${API_BASE_URL}/weighbridge`;
  constructor(private readonly http: HttpClient) {}

  getScales(): Observable<WeighbridgeScale[]> {
    return this.http.get<WeighbridgeScale[]>(`${this.base}/scales`);
  }
  getAll(): Observable<Weighment[]> {
    return this.http.get<Weighment[]>(`${this.base}/weighments`);
  }
  create(payload: {
    vehicleId: string;
    direction?: string;
    scaleId?: string | null;
    partyId?: string | null;
    productId?: string | null;
    remarks?: string;
  }): Observable<Weighment> {
    return this.http.post<Weighment>(`${this.base}/weighments`, payload);
  }
  recordFirst(id: string, firstWeight: number): Observable<Weighment> {
    return this.http.post<Weighment>(`${this.base}/weighments/${id}/first`, { firstWeight });
  }
  complete(id: string, secondWeight: number): Observable<Weighment> {
    return this.http.post<Weighment>(`${this.base}/weighments/${id}/complete`, { secondWeight });
  }
}
