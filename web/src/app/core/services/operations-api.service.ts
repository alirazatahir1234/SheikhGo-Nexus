import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';

export interface VehicleType {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
}
export interface Vehicle {
  id: string;
  code: string;
  vehicleTypeId: string;
  vehicleTypeName?: string;
  make?: string;
  model?: string;
  isActive: boolean;
}
export interface Driver {
  id: string;
  code: string;
  name: string;
  employeeId?: string;
  licenseNo?: string;
  isActive: boolean;
}
export interface GatePass {
  id: string;
  gatePassNo: string;
  direction: string;
  passDate: string;
  vehicleId?: string;
  vehicleNo?: string;
  weighmentId?: string;
  sourceDocType?: string;
  sourceEntityId?: string;
  status: number | string;
  remarks?: string;
}

@Injectable({ providedIn: 'root' })
export class OperationsApiService {
  private readonly base = `${API_BASE_URL}/operations`;
  constructor(private readonly http: HttpClient) {}

  getVehicleTypes(): Observable<VehicleType[]> {
    return this.http.get<VehicleType[]>(`${this.base}/vehicle-types`);
  }
  createVehicleType(payload: Partial<VehicleType>): Observable<VehicleType> {
    return this.http.post<VehicleType>(`${this.base}/vehicle-types`, payload);
  }
  getVehicles(): Observable<Vehicle[]> {
    return this.http.get<Vehicle[]>(`${this.base}/vehicles`);
  }
  createVehicle(payload: Partial<Vehicle>): Observable<Vehicle> {
    return this.http.post<Vehicle>(`${this.base}/vehicles`, payload);
  }
  getDrivers(): Observable<Driver[]> {
    return this.http.get<Driver[]>(`${this.base}/drivers`);
  }
  createDriver(payload: Partial<Driver>): Observable<Driver> {
    return this.http.post<Driver>(`${this.base}/drivers`, payload);
  }
  getGatePasses(): Observable<GatePass[]> {
    return this.http.get<GatePass[]>(`${this.base}/gate-passes`);
  }
  createGatePass(payload: Partial<GatePass>): Observable<GatePass> {
    return this.http.post<GatePass>(`${this.base}/gate-passes`, payload);
  }
  transitionGatePass(id: string, action: string): Observable<GatePass> {
    return this.http.post<GatePass>(`${this.base}/gate-passes/${id}/${action}`, {});
  }
}
