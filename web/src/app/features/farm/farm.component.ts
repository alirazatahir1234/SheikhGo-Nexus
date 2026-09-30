import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import {
  Farm,
  FarmExpense,
  FarmPerformance,
  FarmService,
  FeedIssue,
  Flock,
} from '../../core/services/farm.service';
import { ProductsService } from '../../core/services/products.service';
import { WarehousesService, Warehouse } from '../../core/services/warehouses.service';
import { Product } from '../../core/models/erp.models';
import { statusLabel } from '../../shared/documents/document-status-stepper.component';
import { bindTabFromRoute, navigateModuleTab } from '../../core/nav/route-tabs';

type FarmTab = 'dashboard' | 'farms' | 'flocks' | 'feed' | 'mortality' | 'expenses' | 'performance';

const FARM_TAB_ROUTE: Record<FarmTab, string> = {
  dashboard: 'dashboard',
  farms: 'farms',
  flocks: 'flocks',
  feed: 'feed',
  mortality: 'mortality',
  expenses: 'expenses',
  performance: 'performance',
};

@Component({
  selector: 'app-farm',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, DatePipe, CurrencyPipe, DecimalPipe, RouterLink, RouterLinkActive],
  templateUrl: './farm.component.html',
  styleUrl: './farm.component.scss',
})
export class FarmComponent implements OnInit {
  private readonly router = inject(Router);
  readonly tab = signal<FarmTab>('dashboard');
  readonly farms = signal<Farm[]>([]);
  readonly flocks = signal<Flock[]>([]);
  readonly feedIssues = signal<FeedIssue[]>([]);
  readonly expenses = signal<FarmExpense[]>([]);
  readonly products = signal<Product[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly selectedFlock = signal<Flock | null>(null);
  readonly performance = signal<FarmPerformance | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly saving = signal(false);
  readonly showForm = signal(false);
  readonly statusLabel = statusLabel;
  readonly today = new Date().toISOString().slice(0, 10);

  readonly activeFlocks = computed(() => this.flocks().filter((f) => f.status === 'Active').length);

  readonly flockForm;
  readonly mortalityForm;
  readonly feedForm;
  readonly expenseForm;

  constructor(
    private readonly api: FarmService,
    private readonly productsApi: ProductsService,
    private readonly warehousesApi: WarehousesService,
    private readonly fb: FormBuilder,
  ) {
    this.flockForm = this.fb.nonNullable.group({
      farmId: ['', Validators.required],
      farmUnitId: [''],
      batchNo: ['', Validators.required],
      birdType: ['Broiler', Validators.required],
      placementDate: [this.today, Validators.required],
      qtyPlaced: [10000, [Validators.required, Validators.min(1)]],
      remarks: [''],
    });
    this.mortalityForm = this.fb.nonNullable.group({
      date: [this.today, Validators.required],
      qty: [50, [Validators.required, Validators.min(1)]],
      reason: [''],
    });
    this.feedForm = this.fb.nonNullable.group({
      farmId: ['', Validators.required],
      flockId: ['', Validators.required],
      warehouseId: ['', Validators.required],
      productId: ['', Validators.required],
      qty: [100, [Validators.required, Validators.min(0.01)]],
      issueDate: [this.today],
    });
    this.expenseForm = this.fb.nonNullable.group({
      farmId: ['', Validators.required],
      flockId: [''],
      expenseDate: [this.today, Validators.required],
      category: ['Feed', Validators.required],
      amount: [1000, [Validators.required, Validators.min(0.01)]],
      narration: [''],
      post: [true],
    });
  }

  ngOnInit(): void {
    bindTabFromRoute<FarmTab>((tab) => this.tab.set(tab));
    this.productsApi.getAll().subscribe({ next: (p) => this.products.set(p) });
    this.warehousesApi.getAll().subscribe({
      next: (w) => {
        this.warehouses.set(w);
        const main = w.find((x) => x.code === 'MAIN') ?? w[0];
        if (main) this.feedForm.patchValue({ warehouseId: main.id });
      },
    });
    this.reload();
  }

  setTab(tab: FarmTab): void {
    navigateModuleTab(this.router, 'farm', FARM_TAB_ROUTE[tab]);
  }

  openNewFlock(): void {
    navigateModuleTab(this.router, 'farm', 'flocks');
    this.showForm.set(true);
  }

  reload(): void {
    this.loading.set(true);
    this.error.set('');
    this.api.getFarms().subscribe({
      next: (f) => {
        this.farms.set(f);
        if (f[0]) {
          this.flockForm.patchValue({ farmId: f[0].id, farmUnitId: f[0].units[0]?.id ?? '' });
          this.feedForm.patchValue({ farmId: f[0].id });
          this.expenseForm.patchValue({ farmId: f[0].id });
        }
      },
    });
    this.api.getFlocks().subscribe({
      next: (rows) => {
        this.flocks.set(rows);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load flocks');
      },
    });
    this.api.getFeedIssues().subscribe({ next: (r) => this.feedIssues.set(r) });
    this.api.getExpenses().subscribe({ next: (r) => this.expenses.set(r) });
  }

  unitsForFarm(): Farm['units'] {
    const farm = this.farms().find((f) => f.id === this.flockForm.value.farmId);
    return farm?.units ?? [];
  }

  createFlock(): void {
    if (this.flockForm.invalid) return;
    this.saving.set(true);
    const v = this.flockForm.getRawValue();
    this.api
      .createFlock({
        ...v,
        farmUnitId: v.farmUnitId || null,
        qtyPlaced: Number(v.qtyPlaced),
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.showForm.set(false);
          this.reload();
        },
        error: (e) => {
          this.saving.set(false);
          this.error.set(e?.error?.message || 'Create flock failed');
        },
      });
  }

  selectFlock(f: Flock): void {
    this.selectedFlock.set(f);
    this.feedForm.patchValue({ farmId: f.farmId, flockId: f.id });
    this.expenseForm.patchValue({ farmId: f.farmId, flockId: f.id });
    this.api.getPerformance(f.id).subscribe({ next: (p) => this.performance.set(p) });
    navigateModuleTab(this.router, 'farm', 'performance');
  }

  addMortality(): void {
    const f = this.selectedFlock();
    if (!f || this.mortalityForm.invalid) return;
    this.saving.set(true);
    this.api.addMortality(f.id, this.mortalityForm.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.reload();
        this.api.getFlocks().subscribe({
          next: (rows) => {
            const updated = rows.find((x) => x.id === f.id);
            if (updated) this.selectFlock(updated);
          },
        });
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Mortality failed');
      },
    });
  }

  createFeed(): void {
    if (this.feedForm.invalid) return;
    const v = this.feedForm.getRawValue();
    this.saving.set(true);
    this.api
      .createFeedIssue({
        farmId: v.farmId,
        flockId: v.flockId,
        warehouseId: v.warehouseId,
        issueDate: v.issueDate,
        lines: [{ productId: v.productId, qty: Number(v.qty) }],
      })
      .subscribe({
        next: (issue) => {
          this.api.transitionFeed(issue.id, 'approve').subscribe({
            next: () =>
              this.api.transitionFeed(issue.id, 'post').subscribe({
                next: () => {
                  this.saving.set(false);
                  this.reload();
                },
                error: (e) => {
                  this.saving.set(false);
                  this.error.set(e?.error?.message || 'Post feed failed');
                },
              }),
            error: (e) => {
              this.saving.set(false);
              this.error.set(e?.error?.message || 'Approve feed failed');
            },
          });
        },
        error: (e) => {
          this.saving.set(false);
          this.error.set(e?.error?.message || 'Create feed failed');
        },
      });
  }

  createExpense(): void {
    if (this.expenseForm.invalid) return;
    this.saving.set(true);
    const v = this.expenseForm.getRawValue();
    this.api
      .createExpense({
        ...v,
        flockId: v.flockId || null,
        amount: Number(v.amount),
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.reload();
        },
        error: (e) => {
          this.saving.set(false);
          this.error.set(e?.error?.message || 'Expense failed');
        },
      });
  }

  feedProducts(): Product[] {
    return this.products().filter((p) => p.code === 'FEED-A' || p.isRawMaterial);
  }
}
