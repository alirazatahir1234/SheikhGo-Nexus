import { Component, OnInit, computed, signal } from '@angular/core';
import {
  FormArray,
  FormBuilder,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { PurchasesService } from '../../core/services/purchases.service';
import { PartiesService } from '../../core/services/parties.service';
import { ProductsService } from '../../core/services/products.service';
import { CompanyService } from '../../core/services/company.service';
import { Party, Product, PurchaseInvoice } from '../../core/models/erp.models';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SearchBoxComponent } from '../../shared/components/search-box/search-box.component';

@Component({
  selector: 'app-purchase',
  standalone: true,
  imports: [FormsModule, ReactiveFormsModule, CurrencyPipe, DatePipe, PageHeaderComponent, SearchBoxComponent],
  templateUrl: './purchase.component.html',
})
export class PurchaseComponent implements OnInit {
  readonly invoices = signal<PurchaseInvoice[]>([]);
  readonly parties = signal<Party[]>([]);
  readonly products = signal<Product[]>([]);
  readonly search = signal('');
  readonly showForm = signal(false);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly saving = signal(false);
  readonly taxEnabled = signal(false);
  readonly taxPercent = signal(0);

  readonly filtered = computed(() => {
    const q = this.search().toLowerCase().trim();
    return this.invoices().filter(
      (i) =>
        !q ||
        i.invoiceNo.toLowerCase().includes(q) ||
        (i.partyName ?? '').toLowerCase().includes(q),
    );
  });

  readonly form;

  constructor(
    private readonly api: PurchasesService,
    private readonly partiesApi: PartiesService,
    private readonly productsApi: ProductsService,
    private readonly companyApi: CompanyService,
    private readonly fb: FormBuilder,
  ) {
    this.form = this.fb.nonNullable.group({
      invoiceNo: ['', Validators.required],
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      partyId: ['', Validators.required],
      discount: [0, [Validators.min(0)]],
      tax: [0, [Validators.min(0)]],
      status: ['Posted'],
      lines: this.fb.array([this.createLine()]),
    });
  }

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  ngOnInit(): void {
    this.load();
    this.partiesApi.getAll().subscribe({
      next: (data) =>
        this.parties.set(data.filter((p) => p.partyType === 'Supplier' || p.partyType === 'Both')),
      error: () => this.parties.set([]),
    });
    this.productsApi.getAll().subscribe({
      next: (data) => this.products.set(data),
      error: () => this.products.set([]),
    });
    this.companyApi.get().subscribe({
      next: (c) => {
        this.taxEnabled.set(!!c.taxModule);
        this.taxPercent.set(Number(c.defaultPurchaseTaxPercent || 0));
      },
    });
  }

  applyDefaultTax(): void {
    if (!this.taxEnabled()) return;
    const taxable = Math.max(0, this.subTotal() - Number(this.form.value.discount || 0));
    const tax = Math.round(((taxable * this.taxPercent()) / 100) * 100) / 100;
    this.form.patchValue({ tax }, { emitEvent: false });
  }

  createLine() {
    return this.fb.nonNullable.group({
      productId: ['', Validators.required],
      qty: [1, [Validators.required, Validators.min(0.01)]],
      rate: [0, [Validators.required, Validators.min(0)]],
      amount: [0],
    });
  }

  addLine(): void {
    this.lines.push(this.createLine());
  }

  removeLine(index: number): void {
    if (this.lines.length > 1) this.lines.removeAt(index);
  }

  onProductChange(index: number): void {
    const line = this.lines.at(index);
    const product = this.products().find((p) => String(p.id) === String(line.value.productId));
    if (product) {
      line.patchValue({
        rate: product.purchasePrice,
        amount: product.purchasePrice * Number(line.value.qty || 0),
      });
    }
  }

  recalcLine(index: number): void {
    const line = this.lines.at(index);
    line.patchValue(
      { amount: Number(line.value.qty || 0) * Number(line.value.rate || 0) },
      { emitEvent: false },
    );
    this.applyDefaultTax();
  }

  subTotal(): number {
    return this.lines.controls.reduce((sum, c) => sum + Number(c.value.amount || 0), 0);
  }

  grandTotal(): number {
    return this.subTotal() - Number(this.form.value.discount || 0) + Number(this.form.value.tax || 0);
  }

  load(): void {
    this.loading.set(true);
    this.api.getAll().subscribe({
      next: (data) => {
        this.invoices.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.invoices.set([]);
        this.loading.set(false);
        this.error.set('Could not load purchase invoices.');
      },
    });
  }

  openForm(): void {
    this.form.reset({
      invoiceNo: '',
      date: new Date().toISOString().slice(0, 10),
      partyId: '',
      discount: 0,
      tax: 0,
      status: 'Posted',
    });
    this.lines.clear();
    this.lines.push(this.createLine());
    this.showForm.set(true);
    this.applyDefaultTax();
  }

  save(): void {
    this.lines.controls.forEach((_, i) => this.recalcLine(i));
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    const raw = this.form.getRawValue();
    const party = this.parties().find((p) => String(p.id) === String(raw.partyId));
    const productMap = new Map(this.products().map((p) => [p.id, p.name]));
    const payload = {
      ...raw,
      partyName: party?.name,
      lines: raw.lines.map((l) => ({
        ...l,
        productName: productMap.get(String(l.productId)),
        amount: Number(l.qty) * Number(l.rate),
      })),
      subTotal: this.subTotal(),
      grandTotal: this.grandTotal(),
    };
    this.api.create(payload).subscribe({
      next: (created) => {
        this.invoices.update((list) => [created, ...list]);
        this.saving.set(false);
        this.showForm.set(false);
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Failed to create purchase invoice.');
      },
    });
  }
}
