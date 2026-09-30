import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ExtendedModulesService } from '../../core/services/extended-modules.service';
import { ProductsService } from '../../core/services/products.service';
import { Product } from '../../core/models/erp.models';
import { bindTabFromRoute, navigateModuleTab } from '../../core/nav/route-tabs';

type BrandsTab = 'brands' | 'conversions';

const BRANDS_TAB_ROUTE: Record<BrandsTab, string> = {
  brands: 'list',
  conversions: 'conversions',
};

@Component({
  selector: 'app-brands',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, CurrencyPipe, RouterLink, RouterLinkActive],
  templateUrl: './brands.component.html',
  styleUrl: './brands.component.scss',
})
export class BrandsComponent implements OnInit {
  private readonly router = inject(Router);
  readonly brands = signal<any[]>([]);
  readonly conversions = signal<any[]>([]);
  readonly products = signal<Product[]>([]);
  readonly tab = signal<BrandsTab>('brands');
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
    bindTabFromRoute<BrandsTab>((tab) => this.tab.set(tab));
    this.productsApi.getAll().subscribe({ next: (p) => this.products.set(p) });
    this.reload();
  }

  setTab(tab: BrandsTab): void {
    navigateModuleTab(this.router, 'brands', BRANDS_TAB_ROUTE[tab]);
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
