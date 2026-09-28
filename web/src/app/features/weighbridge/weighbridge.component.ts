import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe, DecimalPipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ExtendedModulesService } from '../../core/services/extended-modules.service';
import { PartiesService } from '../../core/services/parties.service';
import { ProductsService } from '../../core/services/products.service';
import { Party, Product } from '../../core/models/erp.models';

@Component({
  selector: 'app-weighbridge',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, DatePipe, DecimalPipe],
  templateUrl: './weighbridge.component.html',
})
export class WeighbridgeComponent implements OnInit {
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
      direction: ['In', Validators.required],
      vehicleNo: ['', Validators.required],
      partyId: [''],
      productId: [''],
      firstWeight: [0, Validators.required],
      secondWeight: [0],
      remarks: [''],
    });
  }

  ngOnInit(): void {
    this.partiesApi.getAll().subscribe({ next: (p) => this.parties.set(p) });
    this.productsApi.getAll().subscribe({ next: (p) => this.products.set(p) });
    this.reload();
  }

  reload(): void {
    this.api.getWeighbridge().subscribe({
      next: (rows) => this.items.set(rows),
      error: () => this.error.set('Could not load weighbridge tickets'),
    });
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    const raw = this.form.getRawValue();
    this.api.createWeighbridge({
      ...raw,
      partyId: raw.partyId || null,
      productId: raw.productId || null,
    }).subscribe({
      next: () => { this.saving.set(false); this.showForm.set(false); this.reload(); },
      error: (e) => { this.saving.set(false); this.error.set(e?.error?.message || 'Failed'); },
    });
  }

  complete(id: string): void {
    const second = Number(prompt('Second weight (kg)?') || 0);
    if (second <= 0) return;
    this.api.completeWeighbridge(id, second).subscribe({ next: () => this.reload() });
  }
}
