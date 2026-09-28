import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ExtendedModulesService } from '../../core/services/extended-modules.service';
import { ProductsService } from '../../core/services/products.service';
import { Product } from '../../core/models/erp.models';

@Component({
  selector: 'app-brands',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, CurrencyPipe],
  templateUrl: './brands.component.html',
  styleUrl: './brands.component.scss',
})
export class BrandsComponent implements OnInit {
  readonly brands = signal<any[]>([]);
  readonly conversions = signal<any[]>([]);
  readonly products = signal<Product[]>([]);
  readonly tab = signal<'brands' | 'conversions'>('brands');
  readonly show = signal<'brand' | 'conv' | null>(null);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly brandForm;
  readonly convForm;

  constructor(
    private readonly api: ExtendedModulesService,
    private readonly productsApi: ProductsService,
    private readonly fb: FormBuilder,
  ) {
    this.brandForm = this.fb.nonNullable.group({ code: ['', Validators.required], name: ['', Validators.required], isActive: [true] });
    this.convForm = this.fb.nonNullable.group({
      productId: ['', Validators.required], unit: ['Bag'], packing: [1], packingSize: [50],
      purchaseRate: [0], minRate: [0], maxRate: [0],
    });
  }

  ngOnInit(): void {
    this.productsApi.getAll().subscribe({ next: (p) => this.products.set(p) });
    this.reload();
  }

  reload(): void {
    this.api.getBrands().subscribe({ next: (b) => this.brands.set(b), error: () => this.error.set('Load failed') });
    this.api.getConversions().subscribe({ next: (c) => this.conversions.set(c) });
  }

  saveBrand(): void {
    if (this.brandForm.invalid) return;
    this.saving.set(true);
    this.api.createBrand(this.brandForm.getRawValue()).subscribe({
      next: () => { this.saving.set(false); this.show.set(null); this.reload(); },
      error: () => { this.saving.set(false); },
    });
  }

  saveConv(): void {
    if (this.convForm.invalid) return;
    this.saving.set(true);
    this.api.createConversion(this.convForm.getRawValue()).subscribe({
      next: () => { this.saving.set(false); this.show.set(null); this.reload(); },
      error: () => { this.saving.set(false); },
    });
  }
}
