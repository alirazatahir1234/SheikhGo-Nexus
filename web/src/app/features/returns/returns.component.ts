import { Component, OnInit, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ExtendedModulesService } from '../../core/services/extended-modules.service';
import { ProductsService } from '../../core/services/products.service';
import { PartiesService } from '../../core/services/parties.service';
import { Party, Product } from '../../core/models/erp.models';

@Component({
  selector: 'app-returns',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, DatePipe, CurrencyPipe],
  templateUrl: './returns.component.html',
})
export class ReturnsComponent implements OnInit {
  readonly tab = signal<'sale' | 'purchase'>('sale');
  readonly saleReturns = signal<any[]>([]);
  readonly purchaseReturns = signal<any[]>([]);
  readonly products = signal<Product[]>([]);
  readonly parties = signal<Party[]>([]);
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly form;

  constructor(
    private readonly api: ExtendedModulesService,
    private readonly productsApi: ProductsService,
    private readonly partiesApi: PartiesService,
    private readonly fb: FormBuilder,
  ) {
    this.form = this.fb.nonNullable.group({
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      partyId: ['', Validators.required],
      discount: [0],
      tax: [0],
      remarks: [''],
      post: [true],
      lines: this.fb.array([this.line()]),
    });
  }

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  line() {
    return this.fb.nonNullable.group({
      productId: ['', Validators.required],
      qty: [1, Validators.required],
      rate: [0, Validators.required],
      amount: [0],
    });
  }

  ngOnInit(): void {
    this.productsApi.getAll().subscribe({ next: (p) => this.products.set(p) });
    this.partiesApi.getAll().subscribe({ next: (p) => this.parties.set(p) });
    this.reload();
  }

  reload(): void {
    this.api.getSaleReturns().subscribe({ next: (r) => this.saleReturns.set(r) });
    this.api.getPurchaseReturns().subscribe({ next: (r) => this.purchaseReturns.set(r) });
  }

  openForm(): void {
    this.form.reset({
      date: new Date().toISOString().slice(0, 10),
      partyId: '',
      discount: 0,
      tax: 0,
      remarks: '',
      post: true,
    });
    this.lines.clear();
    this.lines.push(this.line());
    this.showForm.set(true);
  }

  addLine(): void {
    this.lines.push(this.line());
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    const raw = this.form.getRawValue();
    const payload = {
      ...raw,
      lines: raw.lines.map((l: any) => ({
        ...l,
        amount: Number(l.qty) * Number(l.rate),
      })),
    };
    const req =
      this.tab() === 'sale'
        ? this.api.createSaleReturn(payload)
        : this.api.createPurchaseReturn(payload);
    req.subscribe({
      next: (d) => {
        if (this.tab() === 'sale') this.saleReturns.update((l) => [d, ...l]);
        else this.purchaseReturns.update((l) => [d, ...l]);
        this.saving.set(false);
        this.showForm.set(false);
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Failed');
      },
    });
  }
}
