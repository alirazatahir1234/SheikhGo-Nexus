import { Component, OnInit, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ExtendedModulesService } from '../../core/services/extended-modules.service';
import { PartiesService } from '../../core/services/parties.service';
import { ProductsService } from '../../core/services/products.service';
import { Party, Product } from '../../core/models/erp.models';

@Component({
  selector: 'app-pos',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, CurrencyPipe, DatePipe],
  templateUrl: './pos.component.html',
})
export class PosComponent implements OnInit {
  readonly items = signal<any[]>([]);
  readonly parties = signal<Party[]>([]);
  readonly products = signal<Product[]>([]);
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly form;

  constructor(
    private readonly api: ExtendedModulesService,
    private readonly partiesApi: PartiesService,
    private readonly productsApi: ProductsService,
    private readonly fb: FormBuilder,
  ) {
    this.form = this.fb.nonNullable.group({
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      partyId: [''],
      paymentMode: ['Cash', Validators.required],
      discount: [0],
      tax: [0],
      remarks: [''],
      lines: this.fb.array([this.line()]),
    });
  }

  get lines(): FormArray { return this.form.get('lines') as FormArray; }
  line() {
    return this.fb.nonNullable.group({
      productId: ['', Validators.required],
      qty: [1, Validators.required],
      rate: [0, Validators.required],
      amount: [0],
    });
  }

  ngOnInit(): void {
    this.partiesApi.getAll().subscribe({
      next: (p) => this.parties.set(p.filter((x) => x.partyType === 'Customer' || x.partyType === 'Both')),
    });
    this.productsApi.getAll().subscribe({ next: (p) => this.products.set(p) });
    this.reload();
  }

  reload(): void {
    this.api.getPos().subscribe({
      next: (rows) => this.items.set(rows),
      error: () => this.error.set('Could not load POS bills'),
    });
  }

  openForm(): void {
    this.form.reset({
      date: new Date().toISOString().slice(0, 10),
      partyId: '',
      paymentMode: 'Cash',
      discount: 0,
      tax: 0,
      remarks: '',
    });
    this.lines.clear();
    this.lines.push(this.line());
    this.showForm.set(true);
  }

  addLine(): void { this.lines.push(this.line()); }

  onProduct(i: number): void {
    const line = this.lines.at(i);
    const p = this.products().find((x) => String(x.id) === String(line.value.productId));
    if (p) line.patchValue({ rate: p.salePrice, amount: p.salePrice * Number(line.value.qty || 0) });
  }

  recalc(i: number): void {
    const line = this.lines.at(i);
    line.patchValue({ amount: Number(line.value.qty || 0) * Number(line.value.rate || 0) }, { emitEvent: false });
  }

  save(): void {
    this.lines.controls.forEach((_, i) => this.recalc(i));
    if (this.form.invalid) return;
    this.saving.set(true);
    const raw = this.form.getRawValue();
    this.api.createPos({
      ...raw,
      partyId: raw.partyId || null,
      lines: raw.lines.map((l) => ({
        productId: l.productId,
        qty: l.qty,
        rate: l.rate,
        amount: Number(l.qty) * Number(l.rate),
      })),
    }).subscribe({
      next: () => { this.saving.set(false); this.showForm.set(false); this.reload(); },
      error: (e) => { this.saving.set(false); this.error.set(e?.error?.message || 'Failed'); },
    });
  }
}
