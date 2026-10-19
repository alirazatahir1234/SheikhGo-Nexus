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
  customerName = '';
  phone = '';
  address = '';
  notes = '';

  readonly total = computed(() =>
    Math.round(this.cart().reduce((s, l) => s + l.qty * l.product.salePrice, 0) * 100) / 100,
  );

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

  add(p: StoreProduct): void {
    this.cart.update((lines) => {
      const i = lines.findIndex((l) => l.product.id === p.id);
      if (i >= 0) {
        const copy = [...lines];
        copy[i] = { ...copy[i], qty: copy[i].qty + (p.sellByWeight ? 0.25 : 1) };
        return copy;
      }
      return [...lines, { product: p, qty: p.sellByWeight ? 0.25 : 1 }];
    });
  }

  setQty(i: number, qty: number): void {
    this.cart.update((lines) => {
      const copy = [...lines];
      copy[i] = { ...copy[i], qty: Math.max(0.001, qty) };
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
    this.http
      .post<{ orderNo: string; grandTotal: number }>(`${API_BASE_URL}/storefront/orders`, {
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
          this.notice.set(`Order ${o.orderNo} placed for PKR ${o.grandTotal}.`);
        },
        error: (e) => {
          this.placing.set(false);
          this.error.set(e?.error?.message || 'Checkout failed — sign in if required.');
        },
      });
  }
}
