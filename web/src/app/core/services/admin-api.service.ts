import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';

export interface AdminCompany {
  id: string;
  name: string;
  shortName: string;
  address?: string;
  city?: string;
  country: string;
  phone?: string;
  email?: string;
  currency: string;
  ntn?: string;
  industry: number | string;
  defaultSaleTaxPercent: number;
  defaultPurchaseTaxPercent: number;
}

export interface AdminBranch {
  id: string;
  companyId: string;
  code: string;
  name: string;
  isDefault: boolean;
}

export interface AdminRole {
  id: string;
  code: string;
  name: string;
}

export interface AdminPermission {
  id: string;
  code: string;
  name: string;
  module?: string;
}

export interface RolePermissions {
  roleId: string;
  permissionCodes: string[];
}

export interface FinancialYear {
  id: string;
  companyId: string;
  code: string;
  startDate: string;
  endDate: string;
  isCurrent: boolean;
  isClosed: boolean;
}

export interface FiscalPeriod {
  id: string;
  financialYearId: string;
  name: string;
  periodNumber: number;
  startDate: string;
  endDate: string;
  isClosed: boolean;
}

export interface AuditLogRow {
  id: string;
  entityName: string;
  entityId: string;
  action: string;
  oldValues?: string;
  newValues?: string;
  userId?: string;
  timestamp: string;
  ipAddress?: string | null;
}

export interface DocumentTypeRow {
  id: string;
  code: string;
  name: string;
  requiresVerification: boolean;
  requiresApproval: boolean;
  prefix?: string;
  affectsInventory: boolean;
  affectsGl: boolean;
}

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private readonly url = `${API_BASE_URL}/admin`;

  constructor(private readonly http: HttpClient) {}

  getCompany(): Observable<AdminCompany> {
    return this.http.get<AdminCompany>(`${this.url}/companies`);
  }

  updateCompany(body: Partial<AdminCompany>): Observable<AdminCompany> {
    return this.http.put<AdminCompany>(`${this.url}/companies`, body);
  }

  getBranches(): Observable<AdminBranch[]> {
    return this.http.get<AdminBranch[]>(`${this.url}/branches`);
  }

  createBranch(body: { code: string; name: string; isDefault: boolean }): Observable<AdminBranch> {
    return this.http.post<AdminBranch>(`${this.url}/branches`, body);
  }

  updateBranch(id: string, body: { code: string; name: string; isDefault: boolean }): Observable<AdminBranch> {
    return this.http.put<AdminBranch>(`${this.url}/branches/${id}`, body);
  }

  getRoles(): Observable<AdminRole[]> {
    return this.http.get<AdminRole[]>(`${this.url}/roles`);
  }

  createRole(body: { code: string; name: string }): Observable<AdminRole> {
    return this.http.post<AdminRole>(`${this.url}/roles`, body);
  }

  getPermissions(): Observable<AdminPermission[]> {
    return this.http.get<AdminPermission[]>(`${this.url}/permissions`);
  }

  getRolePermissions(roleId: string): Observable<RolePermissions> {
    return this.http.get<RolePermissions>(`${this.url}/roles/${roleId}/permissions`);
  }

  setRolePermissions(roleId: string, permissionCodes: string[]): Observable<RolePermissions> {
    return this.http.put<RolePermissions>(`${this.url}/roles/${roleId}/permissions`, {
      roleId,
      permissionCodes,
    });
  }

  setUserRoles(userId: string, roleIds: string[]): Observable<void> {
    return this.http.put<void>(`${this.url}/users/${userId}/roles`, { roleIds });
  }

  getFinancialYears(): Observable<FinancialYear[]> {
    return this.http.get<FinancialYear[]>(`${this.url}/financial-years`);
  }

  createFinancialYear(body: { code: string; startDate: string; endDate: string }): Observable<FinancialYear> {
    return this.http.post<FinancialYear>(`${this.url}/financial-years`, body);
  }

  setCurrentFinancialYear(id: string): Observable<FinancialYear> {
    return this.http.put<FinancialYear>(`${this.url}/financial-years/${id}/set-current`, {});
  }

  getFiscalPeriods(yearId: string): Observable<FiscalPeriod[]> {
    return this.http.get<FiscalPeriod[]>(`${this.url}/financial-years/${yearId}/fiscal-periods`);
  }

  createFiscalPeriod(
    yearId: string,
    body: { name: string; periodNumber: number; startDate: string; endDate: string },
  ): Observable<FiscalPeriod> {
    return this.http.post<FiscalPeriod>(`${this.url}/financial-years/${yearId}/fiscal-periods`, body);
  }

  getAuditLogs(params?: { entityName?: string; entityId?: string; take?: number }): Observable<AuditLogRow[]> {
    const q = new URLSearchParams();
    if (params?.entityName) q.set('entityName', params.entityName);
    if (params?.entityId) q.set('entityId', params.entityId);
    if (params?.take) q.set('take', String(params.take));
    const qs = q.toString();
    return this.http.get<AuditLogRow[]>(`${this.url}/audit-logs${qs ? `?${qs}` : ''}`);
  }

  getDocumentTypes(): Observable<DocumentTypeRow[]> {
    return this.http.get<DocumentTypeRow[]>(`${this.url}/document-types`);
  }

  updateDocumentType(id: string, body: DocumentTypeRow): Observable<DocumentTypeRow> {
    return this.http.put<DocumentTypeRow>(`${this.url}/document-types/${id}`, body);
  }
}
