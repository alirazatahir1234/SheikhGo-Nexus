import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from '../../core/models/api.constants';
import { RouterLink } from '@angular/router';

interface StoreProduct {
  id: string;
  code: string;
  name: string;
  category?: string;
  unit: string;
  salePrice: number;
  stockQty: number;
  barcode?: string;
  sellByWeight: boolean;
  stockStatus: string;
}

interface CartLine {
  product: StoreProduct;
  qty: number;
}

@Component({
  selector: 'app-storefront',
  standalone: true,
  imports: [CurrencyPipe, DecimalPipe, FormsModule, RouterLink],
  templateUrl: './storefront.component.html',
  styleUrl: './storefront.component.scss',
})
export class StorefrontComponent implements OnInit {
  private readonly http = inject(HttpClient);
  readonly products = signal<StoreProduct[]>([]);
  readonly cart = signal<CartLine[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly notice = signal('');
  readonly placing = signal(false);
  readonly category = signal('All');
  readonly cartOpen = signal(false);
  customerName = '';
  phone = '';
  address = '';
  notes = '';

  readonly categories = computed(() => {
    const set = new Set<string>();
    for (const p of this.products()) {
      if (p.category?.trim()) set.add(p.category.trim());
    }
    return ['All', ...Array.from(set).sort()];
  });

  readonly filtered = computed(() => {
    const cat = this.category();
    return this.products().filter((p) => (cat === 'All' ? true : (p.category || '') === cat));
  });

  readonly total = computed(() =>
    Math.round(this.cart().reduce((s, l) => s + l.qty * l.product.salePrice, 0) * 100) / 100,
  );

  readonly cartCount = computed(() => this.cart().length);

  ngOnInit(): void {
    this.http.get<StoreProduct[]>(`${API_BASE_URL}/storefront/products`).subscribe({
      next: (rows) => {
        this.products.set(rows);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load catalog.');
      },
    });
  }

  stockClass(p: StoreProduct): string {
    if (p.stockQty <= 0) return 'stock stock--out';
    if (p.stockQty <= 5 || p.stockStatus === 'Low stock') return 'stock stock--low';
    return 'stock stock--ok';
  }

  add(p: StoreProduct): void {
    if (p.stockQty <= 0) {
      this.error.set(`${p.name} is out of stock.`);
      return;
    }
    this.error.set('');
    this.cart.update((lines) => {
      const i = lines.findIndex((l) => l.product.id === p.id);
      const step = p.sellByWeight ? 0.25 : 1;
      if (i >= 0) {
        const nextQty = Math.round((lines[i].qty + step) * 1000) / 1000;
        if (nextQty > p.stockQty) {
          this.error.set(`Only ${p.stockQty} ${p.unit} available for ${p.name}.`);
          return lines;
        }
        const copy = [...lines];
        copy[i] = { ...copy[i], qty: nextQty };
        return copy;
      }
      return [...lines, { product: p, qty: step }];
    });
    this.cartOpen.set(true);
  }

  setQty(i: number, qty: number): void {
    this.cart.update((lines) => {
      const copy = [...lines];
      const max = copy[i].product.stockQty;
      const next = Math.max(0.001, Math.min(max, qty));
      copy[i] = { ...copy[i], qty: next };
      return copy;
    });
  }

  remove(i: number): void {
    this.cart.update((lines) => lines.filter((_, idx) => idx !== i));
  }

  checkout(): void {
    if (!this.customerName.trim()) {
      this.error.set('Enter your name.');
      return;
    }
    if (!this.cart().length) {
      this.error.set('Cart is empty.');
      return;
    }
    this.placing.set(true);
    this.error.set('');
    this.notice.set('');
    this.http
      .post<{ orderNo: string; grandTotal: number; status: string }>(`${API_BASE_URL}/storefront/orders`, {
        customerName: this.customerName,
        phone: this.phone,
        address: this.address,
        notes: this.notes,
        lines: this.cart().map((l) => ({ productId: l.product.id, qty: l.qty })),
      })
      .subscribe({
        next: (o) => {
          this.placing.set(false);
          this.cart.set([]);
          this.cartOpen.set(false);
          this.notice.set(
            `Order ${o.orderNo} placed (${o.status}) for PKR ${o.grandTotal}. We will confirm delivery shortly.`,
          );
        },
        error: (e) => {
          this.placing.set(false);
          this.error.set(e?.error?.message || 'Checkout failed.');
        },
      });
  }
}
