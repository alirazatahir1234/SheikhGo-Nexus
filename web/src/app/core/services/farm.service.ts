import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';

export interface FarmFlock {
  id: string;
  code: string;
  name: string;
  birdType: string;
  placementDate: string;
  placedQty: number;
  currentQty: number;
  mortalityQty: number;
  shed?: string;
  status: string;
  feedConsumedKg: number;
  totalExpense: number;
}
export interface FarmEvent {
  id: string;
  farmFlockId: string;
  date: string;
  eventType: string;
  qty: number;
  amount: number;
  productId?: string;
  remarks?: string;
}

@Injectable({ providedIn: 'root' })
export class FarmService {
  constructor(private readonly http: HttpClient) {}
  getFlocks(): Observable<FarmFlock[]> {
    return this.http.get<FarmFlock[]>(`${API_BASE_URL}/farm/flocks`);
  }
  createFlock(body: unknown): Observable<FarmFlock> {
    return this.http.post<FarmFlock>(`${API_BASE_URL}/farm/flocks`, body);
  }
  getEvents(id: string): Observable<FarmEvent[]> {
    return this.http.get<FarmEvent[]>(`${API_BASE_URL}/farm/flocks/${id}/events`);
  }
  addEvent(id: string, body: unknown): Observable<FarmEvent> {
    return this.http.post<FarmEvent>(`${API_BASE_URL}/farm/flocks/${id}/events`, body);
  }
  close(id: string): Observable<void> {
    return this.http.post<void>(`${API_BASE_URL}/farm/flocks/${id}/close`, {});
  }
}
