export interface User {
  id: string;
  email: string;
  fullName: string;
  role: string;
  isPlatformAdmin?: boolean;
}

export interface CompanyBrief {
  id: string;
  name: string;
  shortName?: string;
  city?: string;
  currency?: string;
}

export interface BranchBrief {
  id: string;
  companyId: string;
  code: string;
  name: string;
  isDefault: boolean;
}

export interface FinancialYearBrief {
  id: string;
  companyId: string;
  code: string;
  startDate: string;
  endDate: string;
  isCurrent: boolean;
  isClosed: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
}

export interface AuthResponse {
  token: string;
  user: User;
  company: CompanyBrief;
  companies?: CompanyBrief[];
  defaultBranch?: BranchBrief | null;
  currentFinancialYear?: FinancialYearBrief | null;
  permissions?: string[];
}

export interface SessionContext {
  companyId: string | null;
  branchId: string | null;
  financialYearId: string | null;
}
