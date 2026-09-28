import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Employee } from '../models/erp.models';
import { API_BASE_URL } from '../models/api.constants';

@Injectable({ providedIn: 'root' })
export class EmployeesService {
  private readonly url = `${API_BASE_URL}/employees`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<Employee[]> {
    return this.http.get<Employee[]>(this.url);
  }

  create(payload: Partial<Employee>): Observable<Employee> {
    return this.http.post<Employee>(this.url, payload);
  }
}
