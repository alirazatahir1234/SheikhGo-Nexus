import { Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { ProductsService } from '../../core/services/products.service';
import { Product } from '../../core/models/erp.models';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SearchBoxComponent } from '../../shared/components/search-box/search-box.component';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe, DecimalPipe, PageHeaderComponent, SearchBoxComponent],
  templateUrl: './products.component.html',
})
export class ProductsComponent implements OnInit {
  readonly products = signal<Product[]>([]);
  readonly search = signal('');
  readonly showForm = signal(false);
  readonly editingId = signal<string | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly saving = signal(false);

  readonly filtered = computed(() => {
    const q = this.search().toLowerCase().trim();
    return this.products().filter(
      (p) =>
        !q ||
        p.code.toLowerCase().includes(q) ||
        p.name.toLowerCase().includes(q) ||
        p.category.toLowerCase().includes(q),
    );
  });

  readonly form;

  constructor(
    private readonly api: ProductsService,
    private readonly fb: FormBuilder,
  ) {
    this.form = this.fb.nonNullable.group({
      code: ['', Validators.required],
      name: ['', Validators.required],
      category: ['', Validators.required],
      unit: ['KG', Validators.required],
      salePrice: [0, [Validators.required, Validators.min(0)]],
      purchasePrice: [0, [Validators.required, Validators.min(0)]],
      stockQty: [0, [Validators.min(0)]],
      reorderLevel: [0, [Validators.min(0)]],
    });
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getAll().subscribe({
      next: (data) => {
        this.products.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.products.set([]);
        this.loading.set(false);
        this.error.set('Could not load products.');
      },
    });
  }

  openCreate(): void {
    this.editingId.set(null);
    this.form.reset({
      code: '',
      name: '',
      category: '',
      unit: 'KG',
      salePrice: 0,
      purchasePrice: 0,
      stockQty: 0,
      reorderLevel: 0,
    });
    this.showForm.set(true);
  }

  openEdit(product: Product): void {
    this.editingId.set(product.id);
    this.form.patchValue(product);
    this.showForm.set(true);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    const payload = this.form.getRawValue();
    const id = this.editingId();
    const req = id ? this.api.update(id, payload) : this.api.create(payload);
    req.subscribe({
      next: (saved) => {
        if (id) {
          this.products.update((list) => list.map((p) => (p.id === id ? saved : p)));
        } else {
          this.products.update((list) => [...list, saved]);
        }
        this.saving.set(false);
        this.showForm.set(false);
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Failed to save product.');
      },
    });
  }

  remove(product: Product): void {
    if (!confirm(`Delete ${product.name}?`)) return;
    this.api.delete(product.id).subscribe({
      next: () => this.products.update((list) => list.filter((p) => p.id !== product.id)),
      error: () => this.error.set('Failed to delete product.'),
    });
  }
}
