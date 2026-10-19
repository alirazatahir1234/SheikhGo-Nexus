import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Product } from '../models/erp.models';
import { API_BASE_URL } from '../models/api.constants';

@Injectable({ providedIn: 'root' })
export class ProductsService {
  private readonly url = `${API_BASE_URL}/products`;

  constructor(private readonly http: HttpClient) {}

  getAll(search?: string): Observable<Product[]> {
    const q = search?.trim() ? `?search=${encodeURIComponent(search.trim())}` : '';
    return this.http.get<Product[]>(`${this.url}${q}`);
  }

  getByBarcode(barcode: string, warehouseId?: string | null): Observable<Product> {
    const q =
      warehouseId && warehouseId.trim()
        ? `?warehouseId=${encodeURIComponent(warehouseId.trim())}`
        : '';
    return this.http.get<Product>(`${this.url}/barcode/${encodeURIComponent(barcode)}${q}`);
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
