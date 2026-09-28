import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';

export interface TenantListItem {
  id: string;
  name: string;
  code: string;
  slug: string;
  status: string;
  industry: number | string;
  isActive: boolean;
  companyName?: string;
  city?: string;
  userCount: number;
  modules: string[];
  createdAt: string;
}

export interface TenantDetail extends TenantListItem {
  company?: {
    id: string;
    name: string;
    shortName: string;
    city?: string;
    country?: string;
    phone?: string;
    email?: string;
    currency?: string;
    ntn?: string;
    industry: number | string;
  };
  users: { id: string; email: string; fullName: string; role: string; isPlatformAdmin: boolean }[];
}

export interface ProvisionTenantRequest {
  name: string;
  code: string;
  slug?: string;
  industry: number;
  adminFullName: string;
  adminEmail: string;
  adminPassword: string;
  city?: string;
  address?: string;
  phone?: string;
  country: string;
  currency: string;
  ntn?: string;
  modules?: string[];
}

@Injectable({ providedIn: 'root' })
export class TenantsService {
  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<TenantListItem[]> {
    return this.http.get<TenantListItem[]>(`${API_BASE_URL}/tenants`);
  }

  getById(id: string): Observable<TenantDetail> {
    return this.http.get<TenantDetail>(`${API_BASE_URL}/tenants/${id}`);
  }

  catalog(): Observable<string[]> {
    return this.http.get<string[]>(`${API_BASE_URL}/tenants/modules/catalog`);
  }

  provision(body: ProvisionTenantRequest): Observable<{ tenantId: string; message: string }> {
    return this.http.post<{ tenantId: string; message: string }>(`${API_BASE_URL}/tenants/provision`, body);
  }

  updateModules(id: string, modules: string[]): Observable<TenantDetail> {
    return this.http.put<TenantDetail>(`${API_BASE_URL}/tenants/${id}/modules`, modules);
  }

  setActive(id: string, isActive: boolean): Observable<void> {
    return this.http.put<void>(`${API_BASE_URL}/tenants/${id}/active`, isActive);
  }
}
