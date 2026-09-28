import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';

export interface Warehouse {
  id: string;
  code: string;
  name: string;
  location?: string;
  isDefault: boolean;
}

export interface PartySettlement {
  id: string;
  docNo: string;
  date: string;
  partyId: string;
  partyName?: string;
  settlementType: string;
  mode: string;
  amount: number;
  remarks?: string;
}

@Injectable({ providedIn: 'root' })
export class WarehousesService {
  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<Warehouse[]> {
    return this.http.get<Warehouse[]>(`${API_BASE_URL}/warehouses`);
  }

  create(payload: Omit<Warehouse, 'id'>): Observable<Warehouse> {
    return this.http.post<Warehouse>(`${API_BASE_URL}/warehouses`, payload);
  }

  update(id: string, payload: Omit<Warehouse, 'id'>): Observable<Warehouse> {
    return this.http.put<Warehouse>(`${API_BASE_URL}/warehouses/${id}`, payload);
  }
}

@Injectable({ providedIn: 'root' })
export class SettlementsService {
  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<PartySettlement[]> {
    return this.http.get<PartySettlement[]>(`${API_BASE_URL}/settlements`);
  }

  create(payload: {
    date: string;
    partyId: string;
    settlementType: string;
    mode: string;
    amount: number;
    remarks?: string;
  }): Observable<PartySettlement> {
    return this.http.post<PartySettlement>(`${API_BASE_URL}/settlements`, payload);
  }
}
