import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';

export interface LabTestType {
  id: string;
  code: string;
  name: string;
  unit?: string;
  minValue?: number;
  maxValue?: number;
  isActive: boolean;
}
export interface LabTestResult {
  id: string;
  sampleId: string;
  sampleNo?: string;
  testTypeId: string;
  testTypeCode?: string;
  testTypeName?: string;
  testedAt: string;
  value: number;
  decision: string;
  status: string;
  narration?: string;
}
export interface LabSample {
  id: string;
  sampleNo: string;
  sampleDate: string;
  productId?: string;
  productName?: string;
  warehouseId?: string;
  warehouseName?: string;
  sourceType: string;
  status: string;
  remarks?: string;
  results: LabTestResult[];
}

@Injectable({ providedIn: 'root' })
export class LabApiService {
  constructor(private readonly http: HttpClient) {}

  getTestTypes(): Observable<LabTestType[]> {
    return this.http.get<LabTestType[]>(`${API_BASE_URL}/lab/test-types`);
  }
  upsertTestType(body: unknown): Observable<LabTestType> {
    return this.http.post<LabTestType>(`${API_BASE_URL}/lab/test-types`, body);
  }
  getSamples(): Observable<LabSample[]> {
    return this.http.get<LabSample[]>(`${API_BASE_URL}/lab/samples`);
  }
  createSample(body: unknown): Observable<LabSample> {
    return this.http.post<LabSample>(`${API_BASE_URL}/lab/samples`, body);
  }
  getResults(): Observable<LabTestResult[]> {
    return this.http.get<LabTestResult[]>(`${API_BASE_URL}/lab/results`);
  }
  createResult(body: unknown): Observable<LabTestResult> {
    return this.http.post<LabTestResult>(`${API_BASE_URL}/lab/results`, body);
  }
  approveResult(id: string): Observable<LabTestResult> {
    return this.http.post<LabTestResult>(`${API_BASE_URL}/lab/results/${id}/approve`, {});
  }
}
