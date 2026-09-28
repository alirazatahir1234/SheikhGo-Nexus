import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Party } from '../models/erp.models';
import { API_BASE_URL } from '../models/api.constants';

@Injectable({ providedIn: 'root' })
export class PartiesService {
  private readonly url = `${API_BASE_URL}/parties`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<Party[]> {
    return this.http.get<Party[]>(this.url);
  }

  create(payload: Partial<Party>): Observable<Party> {
    return this.http.post<Party>(this.url, payload);
  }

  update(id: string, payload: Partial<Party>): Observable<Party> {
    return this.http.put<Party>(`${this.url}/${id}`, payload);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
