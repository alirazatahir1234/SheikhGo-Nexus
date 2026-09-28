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
} from '../models/auth.model';
import { API_BASE_URL, TOKEN_KEY, USER_KEY, COMPANY_KEY } from '../models/api.constants';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly userSignal = signal<User | null>(this.readUser());
  private readonly companySignal = signal<CompanyBrief | null>(this.readCompany());

  readonly user = this.userSignal.asReadonly();
  readonly company = this.companySignal.asReadonly();
  readonly isAuthenticated = computed(() => !!this.getToken() && !!this.userSignal());

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
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(COMPANY_KEY);
    this.userSignal.set(null);
    this.companySignal.set(null);
    void this.router.navigateByUrl('/login');
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  private persistSession(res: AuthResponse): void {
    if (!res?.token || !res?.user) {
      throw new Error('Invalid login response from server');
    }
    localStorage.setItem(TOKEN_KEY, res.token);
    localStorage.setItem(USER_KEY, JSON.stringify(res.user));
    localStorage.setItem(COMPANY_KEY, JSON.stringify(res.company ?? { id: '', name: 'Softaxe ERP' }));
    this.userSignal.set(res.user);
    this.companySignal.set(res.company ?? null);
  }

  private readUser(): User | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as User;
    } catch {
      return null;
    }
  }

  private readCompany(): CompanyBrief | null {
    const raw = localStorage.getItem(COMPANY_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as CompanyBrief;
    } catch {
      return null;
    }
  }
}
