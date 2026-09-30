import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from '../models/api.constants';

@Injectable({ providedIn: 'root' })
export class NavBadgeService {
  private readonly http = inject(HttpClient);
  private readonly counts = signal<Record<string, number>>({});

  constructor() {
    this.refreshWeighbridge();
  }

  count(key: string): number {
    return this.counts()[key] ?? 0;
  }

  refreshWeighbridge(): void {
    this.http.get<{ status: string }[]>(`${API_BASE_URL}/weighbridge/weighments`).subscribe({
      next: (rows) => {
        const list = Array.isArray(rows) ? rows : [];
        const open = list.filter((t) => {
          const status = String(t.status ?? '');
          return status === 'FirstTaken' || status === '1';
        }).length;
        this.counts.update((c) => ({ ...c, weighbridgeOpen: open }));
      },
      error: () => this.counts.update((c) => ({ ...c, weighbridgeOpen: 0 })),
    });
  }
}
