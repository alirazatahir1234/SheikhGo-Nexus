import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';
import { User } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class UsersService {
  constructor(private readonly http: HttpClient) {}
  getAll(): Observable<User[]> {
    return this.http.get<User[]>(`${API_BASE_URL}/users`);
  }
  create(body: { email: string; fullName: string; password: string; role: string }): Observable<User> {
    return this.http.post<User>(`${API_BASE_URL}/users`, body);
  }
  update(id: string, body: { fullName: string; role: string; isActive: boolean }): Observable<User> {
    return this.http.put<User>(`${API_BASE_URL}/users/${id}`, body);
  }
}
