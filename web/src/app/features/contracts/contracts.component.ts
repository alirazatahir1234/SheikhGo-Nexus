import { Component, OnInit, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ExtendedModulesService } from '../../core/services/extended-modules.service';
import { PartiesService } from '../../core/services/parties.service';
import { ProductsService } from '../../core/services/products.service';
import { Party, Product } from '../../core/models/erp.models';

@Component({
  selector: 'app-contracts',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, DatePipe],
  templateUrl: './contracts.component.html',
  styleUrl: './contracts.component.scss',
})
export class ContractsComponent implements OnInit {
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
      deliveryDate: [''],
      partyId: ['', Validators.required],
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
      unit: ['Bag'],
      remarks: [''],
    });
  }

  ngOnInit(): void {
    this.partiesApi.getAll().subscribe({
      next: (p) => this.parties.set(p.filter((x) => x.partyType === 'Supplier' || x.partyType === 'Both')),
    });
    this.productsApi.getAll().subscribe({ next: (p) => this.products.set(p) });
    this.reload();
  }

  reload(): void {
    this.api.getContracts().subscribe({
      next: (rows) => this.items.set(rows),
      error: () => this.error.set('Could not load contracts'),
    });
  }

  openForm(): void {
    this.form.reset({ date: new Date().toISOString().slice(0, 10), deliveryDate: '', partyId: '', remarks: '' });
    this.lines.clear();
    this.lines.push(this.line());
    this.showForm.set(true);
  }

  addLine(): void { this.lines.push(this.line()); }

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    const raw = this.form.getRawValue();
    this.api.createContract({
      ...raw,
      deliveryDate: raw.deliveryDate || null,
      lines: raw.lines,
    }).subscribe({
      next: (c) => {
        this.items.update((list) => [c, ...list]);
        this.saving.set(false);
        this.showForm.set(false);
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Failed to save contract');
      },
    });
  }

  close(id: string): void {
    this.api.closeContract(id).subscribe({ next: () => this.reload() });
  }
}
