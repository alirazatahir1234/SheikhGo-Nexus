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
}
