import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import {
  InventoryService,
  StockAdjustmentDoc,
  StockCountDoc,
  StockRow,
  StockTransfer,
} from '../../core/services/inventory.service';
import { ProductsService } from '../../core/services/products.service';
import { WarehousesService, Warehouse } from '../../core/services/warehouses.service';
import { Product } from '../../core/models/erp.models';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SearchBoxComponent } from '../../shared/components/search-box/search-box.component';
import { statusLabel } from '../../shared/documents/document-status-stepper.component';
import { bindTabFromRoute, navigateModuleTab } from '../../core/nav/route-tabs';

type InvTab = 'dashboard' | 'stock' | 'ledger' | 'transfers' | 'adjustments' | 'counts' | 'valuation';

const INV_TAB_ROUTE: Record<InvTab, string> = {
  dashboard: 'dashboard',
  stock: 'stock',
  ledger: 'ledger',
  transfers: 'transfers',
  adjustments: 'adjustments',
  counts: 'counts',
  valuation: 'valuation',
};

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
    RouterLink,
    RouterLinkActive,
  ],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.scss',
})
export class InventoryComponent implements OnInit {
  private readonly router = inject(Router);
  readonly tab = signal<InvTab>('stock');
  readonly search = signal('');
  readonly loading = signal(true);
  readonly error = signal('');
  readonly saving = signal(false);
  readonly showForm = signal(false);

  readonly stock = signal<StockRow[]>([]);
  readonly transfers = signal<StockTransfer[]>([]);
  readonly adjustments = signal<StockAdjustmentDoc[]>([]);
  readonly counts = signal<StockCountDoc[]>([]);
  readonly valuation = signal<StockRow[]>([]);
  readonly ledger = signal<unknown[]>([]);
  readonly products = signal<Product[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);

  readonly statusLabel = statusLabel;
  readonly today = new Date().toISOString().slice(0, 10);

  readonly filteredStock = computed(() => {
    const q = this.search().toLowerCase().trim();
    return this.stock().filter(
      (s) =>
        !q ||
        s.code.toLowerCase().includes(q) ||
        s.name.toLowerCase().includes(q) ||
        (s.warehouseCode || '').toLowerCase().includes(q),
    );
  });

  readonly transferForm;
  readonly adjustmentForm;
  readonly countForm;

  constructor(
    private readonly api: InventoryService,
    private readonly productsApi: ProductsService,
    private readonly warehousesApi: WarehousesService,
    private readonly fb: FormBuilder,
  ) {
    this.transferForm = this.fb.nonNullable.group({
      date: [this.today, Validators.required],
      fromWarehouseId: ['', Validators.required],
      toWarehouseId: ['', Validators.required],
      productId: ['', Validators.required],
      qty: [1, [Validators.required, Validators.min(0.01)]],
      remarks: [''],
    });
    this.adjustmentForm = this.fb.nonNullable.group({
      date: [this.today, Validators.required],
      warehouseId: ['', Validators.required],
      productId: ['', Validators.required],
      qtyDelta: [-1, Validators.required],
      reason: ['Physical count variance', Validators.required],
      remarks: [''],
    });
    this.countForm = this.fb.nonNullable.group({
      date: [this.today, Validators.required],
      warehouseId: ['', Validators.required],
      productId: ['', Validators.required],
      countedQty: [0, Validators.required],
      remarks: [''],
    });
  }

  ngOnInit(): void {
    bindTabFromRoute<InvTab>((tab) => this.applyTab(tab));
    this.productsApi.getAll().subscribe({ next: (d) => this.products.set(d), error: () => this.products.set([]) });
    this.warehousesApi.getAll().subscribe({ next: (d) => this.warehouses.set(d), error: () => this.warehouses.set([]) });
    this.reload();
  }

  private applyTab(tab: InvTab): void {
    this.tab.set(tab);
    this.showForm.set(false);
    this.error.set('');
    if (tab === 'valuation') this.loadValuation();
    if (tab === 'ledger') this.loadLedger();
  }

  setTab(tab: InvTab): void {
    navigateModuleTab(this.router, 'inventory', INV_TAB_ROUTE[tab]);
  }

  reload(): void {
    this.loading.set(true);
    let pending = 4;
    const done = () => {
      pending -= 1;
      if (pending <= 0) this.loading.set(false);
    };
    this.api.getStock().subscribe({
      next: (d) => { this.stock.set(d); done(); },
      error: () => { this.stock.set([]); this.error.set('Could not load stock.'); done(); },
    });
    this.api.getTransfers().subscribe({
      next: (d) => { this.transfers.set(d); done(); },
      error: () => { this.transfers.set([]); done(); },
    });
    this.api.getAdjustments().subscribe({
      next: (d) => { this.adjustments.set(d); done(); },
      error: () => { this.adjustments.set([]); done(); },
    });
    this.api.getCounts().subscribe({
      next: (d) => { this.counts.set(d); done(); },
      error: () => { this.counts.set([]); done(); },
    });
  }

  loadValuation(): void {
    this.api.getValuation().subscribe({
      next: (d) => this.valuation.set(d as StockRow[]),
      error: () => this.valuation.set([]),
    });
  }

  loadLedger(): void {
    const from = `${new Date().getFullYear()}-01-01`;
    this.api.getLedger(from, this.today).subscribe({
      next: (d) => this.ledger.set(d),
      error: () => this.ledger.set([]),
    });
  }

  openForm(): void {
    this.showForm.set(true);
  }

  saveTransfer(): void {
    if (this.transferForm.invalid) {
      this.transferForm.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    const raw = this.transferForm.getRawValue();
    this.api
      .createTransfer({
        date: raw.date,
        fromWarehouseId: raw.fromWarehouseId,
        toWarehouseId: raw.toWarehouseId,
        remarks: raw.remarks,
        post: true,
        lines: [{ productId: raw.productId, qty: Number(raw.qty) }],
      })
      .subscribe({
        next: (t) => {
          this.transfers.update((list) => [t, ...list]);
          this.saving.set(false);
          this.showForm.set(false);
          this.reload();
        },
        error: (e) => {
          this.saving.set(false);
          this.error.set(e?.error?.message || 'Transfer failed.');
        },
      });
  }

  saveAdjustment(): void {
    if (this.adjustmentForm.invalid) {
      this.adjustmentForm.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    const raw = this.adjustmentForm.getRawValue();
    this.api
      .createAdjustment({
        date: raw.date,
        warehouseId: raw.warehouseId,
        reason: raw.reason,
        remarks: raw.remarks,
        post: true,
        lines: [{ productId: raw.productId, qtyDelta: Number(raw.qtyDelta) }],
      })
      .subscribe({
        next: (a) => {
          this.adjustments.update((list) => [a, ...list]);
          this.saving.set(false);
          this.showForm.set(false);
          this.reload();
        },
        error: (e) => {
          this.saving.set(false);
          this.error.set(e?.error?.message || 'Adjustment failed.');
        },
      });
  }

  saveCount(): void {
    if (this.countForm.invalid) {
      this.countForm.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    const raw = this.countForm.getRawValue();
    const stockRow = this.stock().find(
      (s) => s.productId === raw.productId && s.warehouseId === raw.warehouseId,
    );
    this.api
      .createCount({
        date: raw.date,
        warehouseId: raw.warehouseId,
        remarks: raw.remarks,
        lines: [
          {
            productId: raw.productId,
            systemQty: stockRow?.stockQty ?? 0,
            countedQty: Number(raw.countedQty),
          },
        ],
      })
      .subscribe({
        next: (c) => {
          this.api.snapshotCount(c.id).subscribe({
            next: () => {
              this.api.postCount(c.id).subscribe({
                next: (posted) => {
                  this.counts.update((list) => [posted, ...list.filter((x) => x.id !== posted.id)]);
                  this.saving.set(false);
                  this.showForm.set(false);
                  this.reload();
                },
                error: (e) => {
                  this.saving.set(false);
                  this.error.set(e?.error?.message || 'Count post failed.');
                },
              });
            },
            error: (e) => {
              this.saving.set(false);
              this.error.set(e?.error?.message || 'Snapshot failed.');
            },
          });
        },
        error: (e) => {
          this.saving.set(false);
          this.error.set(e?.error?.message || 'Count create failed.');
        },
      });
  }

  totalQty(): number {
    return this.stock().reduce((s, r) => s + Number(r.stockQty || 0), 0);
  }

  totalValue(): number {
    return this.stock().reduce((s, r) => s + Number(r.value || 0), 0);
  }
}
