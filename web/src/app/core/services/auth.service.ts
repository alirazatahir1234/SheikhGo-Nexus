import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { Observable } from 'rxjs';
import {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  User,
  CompanyBrief,
  BranchBrief,
  FinancialYearBrief,
} from '../models/auth.model';
import {
  API_BASE_URL,
  TOKEN_KEY,
  USER_KEY,
  COMPANY_KEY,
  PERMISSIONS_KEY,
  BRANCH_KEY,
  FY_KEY,
  COMPANIES_KEY,
} from '../models/api.constants';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly userSignal = signal<User | null>(this.readJson<User>(USER_KEY));
  private readonly companySignal = signal<CompanyBrief | null>(this.readJson<CompanyBrief>(COMPANY_KEY));
  private readonly companiesSignal = signal<CompanyBrief[]>(this.readJson<CompanyBrief[]>(COMPANIES_KEY) ?? []);
  private readonly branchSignal = signal<BranchBrief | null>(this.readJson<BranchBrief>(BRANCH_KEY));
  private readonly fySignal = signal<FinancialYearBrief | null>(this.readJson<FinancialYearBrief>(FY_KEY));
  private readonly permissionsSignal = signal<string[]>(this.readJson<string[]>(PERMISSIONS_KEY) ?? []);

  readonly user = this.userSignal.asReadonly();
  readonly company = this.companySignal.asReadonly();
  readonly companies = this.companiesSignal.asReadonly();
  readonly branch = this.branchSignal.asReadonly();
  readonly financialYear = this.fySignal.asReadonly();
  readonly permissions = this.permissionsSignal.asReadonly();
  readonly isAuthenticated = computed(() => !!this.getToken() && !!this.userSignal());
  readonly isAdmin = computed(
    () =>
      !!this.userSignal()?.isPlatformAdmin ||
      (this.userSignal()?.role ?? '').toLowerCase() === 'admin' ||
      this.hasPermission('Admin.Company.View'),
  );

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router,
  ) {}

  login(payload: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${API_BASE_URL}/auth/login`, payload).pipe(
      tap((res) => this.persistSession(res)),
    );
  }

  register(payload: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${API_BASE_URL}/auth/register`, payload).pipe(
      tap((res) => this.persistSession(res)),
    );
  }

  logout(): void {
    for (const key of [TOKEN_KEY, USER_KEY, COMPANY_KEY, PERMISSIONS_KEY, BRANCH_KEY, FY_KEY, COMPANIES_KEY, 'nexus_branch_id', 'nexus_company_id', 'nexus_fy_id']) {
      localStorage.removeItem(key);
    }
    this.userSignal.set(null);
    this.companySignal.set(null);
    this.companiesSignal.set([]);
    this.branchSignal.set(null);
    this.fySignal.set(null);
    this.permissionsSignal.set([]);
    void this.router.navigateByUrl('/login');
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  hasPermission(code: string): boolean {
    if (!code) return true;
    if (this.userSignal()?.isPlatformAdmin) return true;
    if ((this.userSignal()?.role ?? '').toLowerCase() === 'admin') return true;
    const perms = this.permissionsSignal();
    return perms.some((p) => p.toLowerCase() === code.toLowerCase());
  }

  hasAnyPermission(...codes: string[]): boolean {
    return codes.some((c) => this.hasPermission(c));
  }

  setBranch(branch: BranchBrief | null): void {
    this.branchSignal.set(branch);
    if (branch) {
      localStorage.setItem(BRANCH_KEY, JSON.stringify(branch));
      localStorage.setItem('nexus_branch_id', branch.id);
    } else {
      localStorage.removeItem(BRANCH_KEY);
      localStorage.removeItem('nexus_branch_id');
    }
  }

  setCompany(company: CompanyBrief): void {
    this.companySignal.set(company);
    localStorage.setItem(COMPANY_KEY, JSON.stringify(company));
    localStorage.setItem('nexus_company_id', company.id);
  }

  setFinancialYear(fy: FinancialYearBrief | null): void {
    this.fySignal.set(fy);
    if (fy) {
      localStorage.setItem(FY_KEY, JSON.stringify(fy));
      localStorage.setItem('nexus_fy_id', fy.id);
    } else {
      localStorage.removeItem(FY_KEY);
      localStorage.removeItem('nexus_fy_id');
    }
  }

  private persistSession(res: AuthResponse): void {
    if (!res?.token || !res?.user) {
      throw new Error('Invalid login response from server');
    }
    localStorage.setItem(TOKEN_KEY, res.token);
    localStorage.setItem(USER_KEY, JSON.stringify(res.user));
    const company = res.company ?? { id: '', name: 'SheikhGo Nexus' };
    localStorage.setItem(COMPANY_KEY, JSON.stringify(company));
    localStorage.setItem('nexus_company_id', company.id);

    const companies = res.companies?.length ? res.companies : [company];
    localStorage.setItem(COMPANIES_KEY, JSON.stringify(companies));

    const permissions = res.permissions ?? [];
    localStorage.setItem(PERMISSIONS_KEY, JSON.stringify(permissions));

    this.userSignal.set(res.user);
    this.companySignal.set(company);
    this.companiesSignal.set(companies);
    this.permissionsSignal.set(permissions);

    if (res.defaultBranch) {
      this.setBranch(res.defaultBranch);
    }
    if (res.currentFinancialYear) {
      this.setFinancialYear(res.currentFinancialYear);
    }
  }

  private readJson<T>(key: string): T | null {
    const raw = localStorage.getItem(key);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as T;
    } catch {
      return null;
    }
  }
}
