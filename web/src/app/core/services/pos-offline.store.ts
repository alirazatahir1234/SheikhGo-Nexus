import { Injectable, computed, signal } from '@angular/core';
import { Product } from '../models/erp.models';

/**
 * Single-terminal offline POS MVP.
 *
 * Limitations (intentional — do NOT expand without a design review):
 * - No multi-terminal conflict resolution / stock locks
 * - Catalog snapshot can be stale; server remains source of truth on sync
 * - Queued sales use client idempotency keys; duplicates are server-deduped only
 * - localStorage only (IndexedDB upgrade later if payload size grows)
 */
const CATALOG_KEY = 'nexus.pos.offline.catalog';
const QUEUE_KEY = 'nexus.pos.offline.queue';

export interface OfflineSalePayload {
  shiftId: string;
  discount?: number;
  idempotencyKey: string;
  partyId?: string | null;
  lines: { productId: string; qty: number; rate: number; amount: number }[];
  payments: { tender: string; amount: number }[];
  queuedAt: string;
}

@Injectable({ providedIn: 'root' })
export class PosOfflineStore {
  readonly online = signal(typeof navigator !== 'undefined' ? navigator.onLine : true);
  readonly pending = signal<OfflineSalePayload[]>(this.readQueue());
  readonly pendingCount = computed(() => this.pending().length);
  readonly badge = computed(() => {
    if (!this.online()) {
      const n = this.pendingCount();
      return n > 0 ? `Offline · Pending sync ${n}` : 'Offline';
    }
    const n = this.pendingCount();
    return n > 0 ? `Pending sync ${n}` : '';
  });

  constructor() {
    if (typeof window !== 'undefined') {
      window.addEventListener('online', () => this.online.set(true));
      window.addEventListener('offline', () => this.online.set(false));
    }
  }

  cacheCatalog(products: Product[]): void {
    try {
      localStorage.setItem(
        CATALOG_KEY,
        JSON.stringify({ at: new Date().toISOString(), products }),
      );
    } catch {
      /* quota */
    }
  }

  readCatalog(): Product[] | null {
    try {
      const raw = localStorage.getItem(CATALOG_KEY);
      if (!raw) return null;
      const parsed = JSON.parse(raw) as { products?: Product[] };
      return parsed.products ?? null;
    } catch {
      return null;
    }
  }

  enqueueSale(payload: OfflineSalePayload): void {
    const next = [...this.pending(), payload];
    this.pending.set(next);
    this.writeQueue(next);
  }

  dequeueByKey(idempotencyKey: string): void {
    const next = this.pending().filter((p) => p.idempotencyKey !== idempotencyKey);
    this.pending.set(next);
    this.writeQueue(next);
  }

  private readQueue(): OfflineSalePayload[] {
    try {
      const raw = localStorage.getItem(QUEUE_KEY);
      if (!raw) return [];
      return JSON.parse(raw) as OfflineSalePayload[];
    } catch {
      return [];
    }
  }

  private writeQueue(rows: OfflineSalePayload[]): void {
    try {
      localStorage.setItem(QUEUE_KEY, JSON.stringify(rows));
    } catch {
      /* quota */
    }
  }
}
