import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import {
  Bom,
  ProductionCosting,
  ProductionOrder,
  ProductionService,
} from '../../core/services/production.service';
import { ProductsService } from '../../core/services/products.service';
import { WarehousesService, Warehouse } from '../../core/services/warehouses.service';
import { Product } from '../../core/models/erp.models';
import {
  DocumentStatusStepperComponent,
  statusLabel,
} from '../../shared/documents/document-status-stepper.component';
import { bindTabFromRoute, navigateModuleTab } from '../../core/nav/route-tabs';

type ProdTab = 'dashboard' | 'boms' | 'orders' | 'detail' | 'costing';

@Component({
  selector: 'app-production',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    FormsModule,
    RouterLink,
    RouterLinkActive,
    PageHeaderComponent,
    DatePipe,
    CurrencyPipe,
    DecimalPipe,
    DocumentStatusStepperComponent,
  ],
  templateUrl: './production.component.html',
  styleUrl: './production.component.scss',
})
export class ProductionComponent implements OnInit {
  private readonly router = inject(Router);
  readonly boms = signal<Bom[]>([]);
  readonly orders = signal<ProductionOrder[]>([]);
  readonly products = signal<Product[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly selected = signal<ProductionOrder | null>(null);
  readonly costing = signal<ProductionCosting | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly tab = signal<ProdTab>('dashboard');
  readonly showBom = signal(false);
  readonly showOrder = signal(false);
  readonly saving = signal(false);

  readonly statusLabel = statusLabel;
  readonly today = new Date().toISOString().slice(0, 10);

  readonly bomForm;
  readonly orderForm;
  readonly varianceForm;
  readonly rmLines = signal<{ componentProductId: string; qtyPer: number }[]>([
    { componentProductId: '', qtyPer: 0 },
  ]);

  readonly activeBomCount = computed(() => this.boms().filter((b) => b.isActive).length);
  readonly openOrderCount = computed(
    () =>
      this.orders().filter(
        (o) => !['Completed', 'Closed', 'Cancelled'].includes(o.status),
      ).length,
  );
  readonly completedCount = computed(
    () => this.orders().filter((o) => o.status === 'Completed' || o.status === 'Closed').length,
  );

  constructor(
    private readonly api: ProductionService,
    private readonly productsApi: ProductsService,
    private readonly warehousesApi: WarehousesService,
    private readonly fb: FormBuilder,
  ) {
    this.bomForm = this.fb.nonNullable.group({
      code: ['', Validators.required],
      name: ['', Validators.required],
      finishedProductId: ['', Validators.required],
      version: [1, Validators.required],
      scrapPercent: [0],
      isActive: [true],
    });
    this.orderForm = this.fb.nonNullable.group({
      bomId: ['', Validators.required],
      warehouseId: ['', Validators.required],
      orderDate: [this.today, Validators.required],
      qtyOrdered: [5, [Validators.required, Validators.min(0.01)]],
      laborCost: [0],
      overheadCost: [0],
      remarks: [''],
    });
    this.varianceForm = this.fb.nonNullable.group({
      kind: ['Wastage', Validators.required],
      productId: ['', Validators.required],
      qty: [1, [Validators.required, Validators.min(0.01)]],
      narration: [''],
    });
  }

  ngOnInit(): void {
    bindTabFromRoute<ProdTab>((tab, id) => {
      this.tab.set(tab);
      if (tab === 'detail' && id) {
        const existing = this.orders().find((o) => o.id === id);
        if (existing) {
          this.selected.set(existing);
          this.api.getCosting(id).subscribe({
            next: (c) => this.costing.set(c),
            error: () => this.costing.set(null),
          });
        } else {
          this.api.getOrders().subscribe({
            next: (o) => {
              this.orders.set(o);
              const found = o.find((x) => x.id === id);
              if (found) {
                this.selected.set(found);
                this.api.getCosting(id).subscribe({
                  next: (c) => this.costing.set(c),
                  error: () => this.costing.set(null),
                });
              }
            },
          });
        }
      }
    });
    this.productsApi.getAll().subscribe({ next: (p) => this.products.set(p) });
    this.warehousesApi.getAll().subscribe({
      next: (w) => {
        this.warehouses.set(w);
        const main = w.find((x) => x.code === 'MAIN') ?? w[0];
        if (main) this.orderForm.patchValue({ warehouseId: main.id });
      },
    });
    this.reload();
  }

  goTab(segment: string, id?: string | null): void {
    navigateModuleTab(this.router, 'production', segment, id);
  }

  reload(): void {
    this.loading.set(true);
    this.error.set('');
    this.api.getBoms().subscribe({
      next: (b) => this.boms.set(b),
      error: () => this.error.set('Could not load BOMs'),
    });
    this.api.getOrders().subscribe({
      next: (o) => {
        this.orders.set(o);
        this.loading.set(false);
        const sel = this.selected();
        if (sel) {
          const refreshed = o.find((x) => x.id === sel.id);
          if (refreshed) this.selected.set(refreshed);
        }
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load production orders');
      },
    });
  }

  finishedGoods(): Product[] {
    return this.products().filter((p) => p.isFinishedGood);
  }

  rawMaterials(): Product[] {
    return this.products().filter((p) => p.isRawMaterial);
  }

  addRmLine(): void {
    this.rmLines.update((rows) => [...rows, { componentProductId: '', qtyPer: 0 }]);
  }

  openDetail(order: ProductionOrder): void {
    this.selected.set(order);
    navigateModuleTab(this.router, 'production', 'orders', order.id);
    this.api.getCosting(order.id).subscribe({
      next: (c) => this.costing.set(c),
      error: () => this.costing.set(null),
    });
  }

  saveBom(): void {
    if (this.bomForm.invalid) return;
    const lines = this.rmLines()
      .filter((l) => l.componentProductId && l.qtyPer > 0)
      .map((l, i) => ({
        componentProductId: l.componentProductId,
        qtyPer: l.qtyPer,
        lineNo: i + 1,
      }));
    if (!lines.length) {
      this.error.set('Add at least one raw material line');
      return;
    }
    this.saving.set(true);
    this.api.createBom({ ...this.bomForm.getRawValue(), lines }).subscribe({
      next: () => {
        this.saving.set(false);
        this.showBom.set(false);
        this.reload();
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'BOM save failed');
      },
    });
  }

  createOrder(): void {
    if (this.orderForm.invalid) return;
    this.saving.set(true);
    this.api.createOrder(this.orderForm.getRawValue()).subscribe({
      next: (o) => {
        this.saving.set(false);
        this.showOrder.set(false);
        this.reload();
        this.openDetail(o);
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Create order failed');
      },
    });
  }

  act(action: string): void {
    const o = this.selected();
    if (!o) return;
    this.saving.set(true);
    this.api.transition(o.id, action).subscribe({
      next: (updated) => {
        this.saving.set(false);
        this.selected.set(updated);
        this.reload();
        this.api.getCosting(updated.id).subscribe({ next: (c) => this.costing.set(c) });
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || `${action} failed`);
      },
    });
  }

  addVariance(): void {
    const o = this.selected();
    if (!o || this.varianceForm.invalid) return;
    this.saving.set(true);
    this.api.addVariance(o.id, this.varianceForm.getRawValue()).subscribe({
      next: (updated) => {
        this.saving.set(false);
        this.selected.set(updated);
        this.varianceForm.reset({ kind: 'Wastage', productId: '', qty: 1, narration: '' });
        this.reload();
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Variance failed');
      },
    });
  }

  docStatus(o: ProductionOrder): string | number {
    return o.documentStatus;
  }
}
