import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Account } from '../models/erp.models';
import { API_BASE_URL } from '../models/api.constants';

@Injectable({ providedIn: 'root' })
export class AccountsService {
  private readonly url = `${API_BASE_URL}/accounts`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<Account[]> {
    return this.http.get<Account[]>(this.url);
  }

  create(payload: Partial<Account>): Observable<Account> {
    return this.http.post<Account>(this.url, payload);
  }
}
