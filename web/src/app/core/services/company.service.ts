import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Company } from '../models/erp.models';
import { API_BASE_URL } from '../models/api.constants';

@Injectable({ providedIn: 'root' })
export class CompanyService {
  private readonly url = `${API_BASE_URL}/company`;

  constructor(private readonly http: HttpClient) {}

  get(): Observable<Company> {
    return this.http.get<Company>(this.url);
  }

  update(payload: Partial<Company>): Observable<Company> {
    return this.http.put<Company>(this.url, payload);
  }
}
