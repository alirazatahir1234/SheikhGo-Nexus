import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { Bom, ProductionOrder, ProductionService } from '../../core/services/production.service';
import { ProductsService } from '../../core/services/products.service';
import { Product } from '../../core/models/erp.models';

@Component({
  selector: 'app-production',
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule, PageHeaderComponent, DatePipe],
  templateUrl: './production.component.html',
  styleUrl: './production.component.scss',
})
export class ProductionComponent implements OnInit {
  readonly boms = signal<Bom[]>([]);
  readonly orders = signal<ProductionOrder[]>([]);
  readonly products = signal<Product[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly tab = signal<'boms' | 'orders'>('orders');
  readonly showBom = signal(false);
  readonly showOrder = signal(false);
  readonly saving = signal(false);

  readonly bomForm;
  readonly orderForm;
  readonly rmLines = signal<{ rawMaterialId: string; qty: number }[]>([{ rawMaterialId: '', qty: 0 }]);

  constructor(
    private readonly api: ProductionService,
    private readonly productsApi: ProductsService,
    private readonly fb: FormBuilder,
  ) {
    this.bomForm = this.fb.nonNullable.group({
      code: ['', Validators.required],
      name: ['', Validators.required],
      finishedProductId: ['', Validators.required],
      outputQty: [50, Validators.required],
    });
    this.orderForm = this.fb.nonNullable.group({
      bomId: ['', Validators.required],
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      batchQty: [1, Validators.required],
      remarks: [''],
      post: [true],
    });
  }

  ngOnInit(): void {
    this.productsApi.getAll().subscribe({ next: (p) => this.products.set(p) });
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.api.getBoms().subscribe({
      next: (b) => this.boms.set(b),
      error: () => this.error.set('Could not load BOMs'),
    });
    this.api.getOrders().subscribe({
      next: (o) => {
        this.orders.set(o);
        this.loading.set(false);
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
    this.rmLines.update((rows) => [...rows, { rawMaterialId: '', qty: 0 }]);
  }

  saveBom(): void {
    if (this.bomForm.invalid) return;
    const lines = this.rmLines().filter((l) => l.rawMaterialId && l.qty > 0);
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

  runOrder(): void {
    if (this.orderForm.invalid) return;
    this.saving.set(true);
    this.api.createOrder(this.orderForm.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.showOrder.set(false);
        this.reload();
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Production failed');
      },
    });
  }
}
