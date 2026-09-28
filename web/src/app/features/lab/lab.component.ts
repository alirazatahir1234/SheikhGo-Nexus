import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe, DecimalPipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ExtendedModulesService } from '../../core/services/extended-modules.service';
import { PartiesService } from '../../core/services/parties.service';
import { ProductsService } from '../../core/services/products.service';
import { Party, Product } from '../../core/models/erp.models';

@Component({
  selector: 'app-lab',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, DatePipe, DecimalPipe],
  templateUrl: './lab.component.html',
  styleUrl: './lab.component.scss',
})
export class LabComponent implements OnInit {
  readonly tests = signal<any[]>([]);
  readonly results = signal<any[]>([]);
  readonly parties = signal<Party[]>([]);
  readonly products = signal<Product[]>([]);
  readonly tab = signal<'results' | 'tests'>('results');
  readonly showResult = signal(false);
  readonly showTest = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly resultForm;
  readonly testForm;

  constructor(
    private readonly api: ExtendedModulesService,
    private readonly partiesApi: PartiesService,
    private readonly productsApi: ProductsService,
    private readonly fb: FormBuilder,
  ) {
    this.resultForm = this.fb.nonNullable.group({
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      partyId: [''],
      productId: [''],
      labTestTypeId: [''],
      vehicleNo: [''],
      bags: [0],
      percentage: [0],
      partyDeductionKg: [0],
      stockDeductionKg: [0],
      remarks: [''],
    });
    this.testForm = this.fb.nonNullable.group({
      code: ['', Validators.required],
      name: ['', Validators.required],
      unit: ['%'],
      isActive: [true],
    });
  }

  ngOnInit(): void {
    this.partiesApi.getAll().subscribe({ next: (p) => this.parties.set(p) });
    this.productsApi.getAll().subscribe({ next: (p) => this.products.set(p) });
    this.reload();
  }

  reload(): void {
    this.api.getLabTests().subscribe({ next: (t) => this.tests.set(t) });
    this.api.getLabResults().subscribe({
      next: (r) => this.results.set(r),
      error: () => this.error.set('Could not load lab results'),
    });
  }

  saveTest(): void {
    if (this.testForm.invalid) return;
    this.saving.set(true);
    this.api.upsertLabTest(this.testForm.getRawValue()).subscribe({
      next: () => { this.saving.set(false); this.showTest.set(false); this.reload(); },
      error: () => { this.saving.set(false); this.error.set('Failed to save test type'); },
    });
  }

  saveResult(): void {
    if (this.resultForm.invalid) return;
    this.saving.set(true);
    const raw = this.resultForm.getRawValue();
    this.api.createLabResult({
      ...raw,
      partyId: raw.partyId || null,
      productId: raw.productId || null,
      labTestTypeId: raw.labTestTypeId || null,
    }).subscribe({
      next: (r) => {
        this.results.update((list) => [r, ...list]);
        this.saving.set(false);
        this.showResult.set(false);
      },
      error: () => { this.saving.set(false); this.error.set('Failed to save lab result'); },
    });
  }
}
