import { Component, OnInit, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ExtendedModulesService } from '../../core/services/extended-modules.service';
import { ProductsService } from '../../core/services/products.service';
import { Product } from '../../core/models/erp.models';

@Component({
  selector: 'app-demands',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, DatePipe],
  templateUrl: './demands.component.html',
})
export class DemandsComponent implements OnInit {
  readonly items = signal<any[]>([]);
  readonly products = signal<Product[]>([]);
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly form;

  constructor(
    private readonly api: ExtendedModulesService,
    private readonly productsApi: ProductsService,
    private readonly fb: FormBuilder,
  ) {
    this.form = this.fb.nonNullable.group({
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      demandType: ['Demand', Validators.required],
      requestedBy: [''],
      remarks: [''],
      lines: this.fb.array([this.line()]),
    });
  }

  get lines(): FormArray { return this.form.get('lines') as FormArray; }
  line() {
    return this.fb.nonNullable.group({
      productId: ['', Validators.required],
      qty: [1, Validators.required],
      remarks: [''],
    });
  }

  ngOnInit(): void {
    this.productsApi.getAll().subscribe({ next: (p) => this.products.set(p) });
    this.api.getDemands().subscribe({
      next: (rows) => this.items.set(rows),
      error: () => this.error.set('Could not load demands'),
    });
  }

  openForm(): void {
    this.form.reset({ date: new Date().toISOString().slice(0, 10), demandType: 'Demand', requestedBy: '', remarks: '' });
    this.lines.clear();
    this.lines.push(this.line());
    this.showForm.set(true);
  }

  addLine(): void { this.lines.push(this.line()); }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.api.createDemand(this.form.getRawValue()).subscribe({
      next: (d) => { this.items.update((l) => [d, ...l]); this.saving.set(false); this.showForm.set(false); },
      error: (e) => { this.saving.set(false); this.error.set(e?.error?.message || 'Failed'); },
    });
  }
}
