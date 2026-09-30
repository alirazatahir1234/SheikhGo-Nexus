import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { LabApiService, LabSample, LabTestResult, LabTestType } from '../../core/services/lab-api.service';
import { ProductsService } from '../../core/services/products.service';
import { WarehousesService, Warehouse } from '../../core/services/warehouses.service';
import { Product } from '../../core/models/erp.models';
import { bindTabFromRoute, navigateModuleTab } from '../../core/nav/route-tabs';

type LabTab = 'dashboard' | 'types' | 'samples' | 'results';

const LAB_TAB_ROUTE: Record<LabTab, string> = {
  dashboard: 'dashboard',
  types: 'types',
  samples: 'samples',
  results: 'results',
};

@Component({
  selector: 'app-lab',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, DatePipe, DecimalPipe, RouterLink, RouterLinkActive],
  templateUrl: './lab.component.html',
  styleUrl: './lab.component.scss',
})
export class LabComponent implements OnInit {
  private readonly router = inject(Router);
  readonly tab = signal<LabTab>('dashboard');
  readonly types = signal<LabTestType[]>([]);
  readonly samples = signal<LabSample[]>([]);
  readonly results = signal<LabTestResult[]>([]);
  readonly products = signal<Product[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly saving = signal(false);
  readonly today = new Date().toISOString().slice(0, 10);

  readonly typeForm;
  readonly sampleForm;
  readonly resultForm;

  constructor(
    private readonly api: LabApiService,
    private readonly productsApi: ProductsService,
    private readonly warehousesApi: WarehousesService,
    private readonly fb: FormBuilder,
  ) {
    this.typeForm = this.fb.nonNullable.group({
      code: ['MOISTURE', Validators.required],
      name: ['Moisture', Validators.required],
      unit: ['%'],
      maxValue: [14],
      isActive: [true],
    });
    this.sampleForm = this.fb.nonNullable.group({
      sampleDate: [this.today],
      productId: [''],
      warehouseId: [''],
      sourceType: ['Inventory'],
      remarks: [''],
    });
    this.resultForm = this.fb.nonNullable.group({
      sampleId: ['', Validators.required],
      testTypeId: ['', Validators.required],
      value: [12.4, Validators.required],
      narration: [''],
    });
  }

  ngOnInit(): void {
    bindTabFromRoute<LabTab>((tab) => this.tab.set(tab));
    this.productsApi.getAll().subscribe({ next: (p) => this.products.set(p) });
    this.warehousesApi.getAll().subscribe({
      next: (w) => {
        this.warehouses.set(w);
        const main = w.find((x) => x.code === 'MAIN') ?? w[0];
        if (main) this.sampleForm.patchValue({ warehouseId: main.id });
      },
    });
    this.reload();
  }

  setTab(tab: LabTab): void {
    navigateModuleTab(this.router, 'lab', LAB_TAB_ROUTE[tab]);
  }

  reload(): void {
    this.loading.set(true);
    this.api.getTestTypes().subscribe({
      next: (t) => {
        this.types.set(t);
        if (t[0]) this.resultForm.patchValue({ testTypeId: t[0].id });
      },
    });
    this.api.getSamples().subscribe({ next: (s) => this.samples.set(s) });
    this.api.getResults().subscribe({
      next: (r) => {
        this.results.set(r);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load lab results');
      },
    });
  }

  saveType(): void {
    if (this.typeForm.invalid) return;
    this.saving.set(true);
    this.api.upsertTestType(this.typeForm.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.reload();
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Save type failed');
      },
    });
  }

  createSample(): void {
    this.saving.set(true);
    const v = this.sampleForm.getRawValue();
    this.api
      .createSample({
        ...v,
        productId: v.productId || null,
        warehouseId: v.warehouseId || null,
      })
      .subscribe({
        next: (s) => {
          this.saving.set(false);
          this.resultForm.patchValue({ sampleId: s.id });
          this.reload();
          navigateModuleTab(this.router, 'lab', 'results');
        },
        error: (e) => {
          this.saving.set(false);
          this.error.set(e?.error?.message || 'Sample failed');
        },
      });
  }

  createResult(): void {
    if (this.resultForm.invalid) return;
    this.saving.set(true);
    this.api.createResult(this.resultForm.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.reload();
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Result failed');
      },
    });
  }

  approve(id: string): void {
    this.saving.set(true);
    this.api.approveResult(id).subscribe({
      next: () => {
        this.saving.set(false);
        this.reload();
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Approve failed');
      },
    });
  }
}
