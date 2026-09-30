import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';

export interface FarmUnit {
  id: string;
  farmId: string;
  code: string;
  name: string;
  capacity?: number;
  isActive: boolean;
}
export interface Farm {
  id: string;
  code: string;
  name: string;
  farmType: string;
  branchId?: string;
  isActive: boolean;
  units: FarmUnit[];
}
export interface Flock {
  id: string;
  batchNo: string;
  farmId: string;
  farmName?: string;
  farmUnitId?: string;
  farmUnitName?: string;
  birdType: string;
  placementDate: string;
  qtyPlaced: number;
  qtyCurrent: number;
  qtyMortality: number;
  status: string;
  remarks?: string;
}
export interface Mortality {
  id: string;
  flockId: string;
  date: string;
  qty: number;
  reason?: string;
}
export interface FeedIssue {
  id: string;
  issueNo: string;
  issueDate: string;
  farmId: string;
  flockId: string;
  warehouseId: string;
  status: number | string;
  lines: { productId: string; productCode?: string; qty: number; totalCost: number }[];
}
export interface FarmExpense {
  id: string;
  expenseNo: string;
  expenseDate: string;
  farmId: string;
  flockId?: string;
  category: string;
  amount: number;
  status: number | string;
  narration?: string;
}
export interface FarmPerformance {
  flockId: string;
  batchNo: string;
  openingQty: number;
  placements: number;
  mortalityQty: number;
  closingQty: number;
  mortalityPercent: number;
  feedConsumedKg: number;
  totalExpense: number;
  costPerBird?: number;
}

@Injectable({ providedIn: 'root' })
export class FarmService {
  constructor(private readonly http: HttpClient) {}

  getFarms(): Observable<Farm[]> {
    return this.http.get<Farm[]>(`${API_BASE_URL}/farm/farms`);
  }
  createFarm(body: unknown): Observable<Farm> {
    return this.http.post<Farm>(`${API_BASE_URL}/farm/farms`, body);
  }
  createUnit(body: unknown): Observable<FarmUnit> {
    return this.http.post<FarmUnit>(`${API_BASE_URL}/farm/units`, body);
  }
  getFlocks(): Observable<Flock[]> {
    return this.http.get<Flock[]>(`${API_BASE_URL}/farm/flocks`);
  }
  createFlock(body: unknown): Observable<Flock> {
    return this.http.post<Flock>(`${API_BASE_URL}/farm/flocks`, body);
  }
  closeFlock(id: string): Observable<void> {
    return this.http.post<void>(`${API_BASE_URL}/farm/flocks/${id}/close`, {});
  }
  getMortalities(flockId: string): Observable<Mortality[]> {
    return this.http.get<Mortality[]>(`${API_BASE_URL}/farm/flocks/${flockId}/mortality`);
  }
  addMortality(flockId: string, body: unknown): Observable<Mortality> {
    return this.http.post<Mortality>(`${API_BASE_URL}/farm/flocks/${flockId}/mortality`, body);
  }
  getFeedIssues(): Observable<FeedIssue[]> {
    return this.http.get<FeedIssue[]>(`${API_BASE_URL}/farm/feed-issues`);
  }
  createFeedIssue(body: unknown): Observable<FeedIssue> {
    return this.http.post<FeedIssue>(`${API_BASE_URL}/farm/feed-issues`, body);
  }
  transitionFeed(id: string, action: string): Observable<FeedIssue> {
    return this.http.post<FeedIssue>(`${API_BASE_URL}/farm/feed-issues/${id}/${action}`, {});
  }
  getExpenses(): Observable<FarmExpense[]> {
    return this.http.get<FarmExpense[]>(`${API_BASE_URL}/farm/expenses`);
  }
  createExpense(body: unknown): Observable<FarmExpense> {
    return this.http.post<FarmExpense>(`${API_BASE_URL}/farm/expenses`, body);
  }
  getPerformance(flockId: string): Observable<FarmPerformance> {
    return this.http.get<FarmPerformance>(`${API_BASE_URL}/farm/performance/${flockId}`);
  }
}
