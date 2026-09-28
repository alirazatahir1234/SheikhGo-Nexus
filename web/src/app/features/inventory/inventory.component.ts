import { Component, OnInit, computed, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { InventoryService } from '../../core/services/inventory.service';
import { ProductsService } from '../../core/services/products.service';
import { Product, StockAdjustment, StockItem } from '../../core/models/erp.models';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SearchBoxComponent } from '../../shared/components/search-box/search-box.component';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [
    CurrencyPipe,
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    PageHeaderComponent,
    SearchBoxComponent,
  ],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.scss',
})
export class InventoryComponent implements OnInit {
  readonly stock = signal<StockItem[]>([]);
  readonly adjustments = signal<StockAdjustment[]>([]);
  readonly products = signal<Product[]>([]);
  readonly search = signal('');
  readonly loading = signal(true);
  readonly error = signal('');
  readonly tab = signal<'stock' | 'adjustments'>('stock');
  readonly showForm = signal(false);
  readonly saving = signal(false);

  readonly filtered = computed(() => {
    const q = this.search().toLowerCase().trim();
    return this.stock().filter(
      (s) =>
        !q ||
        s.code.toLowerCase().includes(q) ||
        s.name.toLowerCase().includes(q) ||
        s.category.toLowerCase().includes(q),
    );
  });

  readonly form;

  constructor(
    private readonly api: InventoryService,
    private readonly productsApi: ProductsService,
    private readonly fb: FormBuilder,
  ) {
    this.form = this.fb.nonNullable.group({
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      productId: ['', Validators.required],
      qtyChange: [0, Validators.required],
      reason: ['Stock count', Validators.required],
      remarks: [''],
    });
  }

  ngOnInit(): void {
    this.productsApi.getAll().subscribe({
      next: (data) => this.products.set(data),
      error: () => this.products.set([]),
    });
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.api.getStock().subscribe({
      next: (data) => {
        this.stock.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.stock.set([]);
        this.loading.set(false);
        this.error.set('Could not load stock summary.');
      },
    });
    this.api.getAdjustments().subscribe({
      next: (data) => this.adjustments.set(data),
      error: () => this.adjustments.set([]),
    });
  }

  openForm(): void {
    this.form.reset({
      date: new Date().toISOString().slice(0, 10),
      productId: '',
      qtyChange: 0,
      reason: 'Stock count',
      remarks: '',
    });
    this.showForm.set(true);
    this.tab.set('adjustments');
  }

  save(): void {
    if (this.form.invalid || Number(this.form.value.qtyChange) === 0) {
      this.form.markAllAsTouched();
      this.error.set('Enter a non-zero quantity change.');
      return;
    }
    this.saving.set(true);
    this.api.createAdjustment(this.form.getRawValue()).subscribe({
      next: (created) => {
        this.adjustments.update((list) => [created, ...list]);
        this.saving.set(false);
        this.showForm.set(false);
        this.error.set('');
        this.reload();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err?.error?.message || 'Failed to post stock adjustment.');
      },
    });
  }
}
