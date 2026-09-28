import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Product } from '../models/erp.models';
import { API_BASE_URL } from '../models/api.constants';

@Injectable({ providedIn: 'root' })
export class ProductsService {
  private readonly url = `${API_BASE_URL}/products`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<Product[]> {
    return this.http.get<Product[]>(this.url);
  }

  create(payload: Partial<Product>): Observable<Product> {
    return this.http.post<Product>(this.url, payload);
  }

  update(id: string, payload: Partial<Product>): Observable<Product> {
    return this.http.put<Product>(`${this.url}/${id}`, payload);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
