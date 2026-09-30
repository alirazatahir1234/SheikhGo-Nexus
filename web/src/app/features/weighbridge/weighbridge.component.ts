import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe, DecimalPipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { WeighbridgeApiService, Weighment } from '../../core/services/weighbridge-api.service';
import { OperationsApiService, Vehicle } from '../../core/services/operations-api.service';
import { PartiesService } from '../../core/services/parties.service';
import { ProductsService } from '../../core/services/products.service';
import { Party, Product } from '../../core/models/erp.models';

@Component({
  selector: 'app-weighbridge',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, DatePipe, DecimalPipe],
  templateUrl: './weighbridge.component.html',
  styleUrl: './weighbridge.component.scss',
})
export class WeighbridgeComponent implements OnInit {
  readonly items = signal<Weighment[]>([]);
  readonly vehicles = signal<Vehicle[]>([]);
  readonly parties = signal<Party[]>([]);
  readonly products = signal<Product[]>([]);
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly form;

  constructor(
    private readonly api: WeighbridgeApiService,
    private readonly ops: OperationsApiService,
    private readonly partiesApi: PartiesService,
    private readonly productsApi: ProductsService,
    private readonly fb: FormBuilder,
  ) {
    this.form = this.fb.nonNullable.group({
      vehicleId: ['', Validators.required],
      direction: ['Inward', Validators.required],
      partyId: [''],
      productId: [''],
      remarks: [''],
    });
  }

  ngOnInit(): void {
    this.ops.getVehicles().subscribe({ next: (v) => this.vehicles.set(v) });
    this.partiesApi.getAll().subscribe({ next: (p) => this.parties.set(p) });
    this.productsApi.getAll().subscribe({ next: (p) => this.products.set(p) });
    this.reload();
  }

  reload(): void {
    this.api.getAll().subscribe({
      next: (rows) => this.items.set(rows),
      error: () => this.error.set('Could not load weighments'),
    });
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    const raw = this.form.getRawValue();
    this.api
      .create({
        vehicleId: raw.vehicleId,
        direction: raw.direction,
        partyId: raw.partyId || null,
        productId: raw.productId || null,
        remarks: raw.remarks,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.showForm.set(false);
          this.reload();
        },
        error: (e) => {
          this.saving.set(false);
          this.error.set(e?.error?.message || 'Failed');
        },
      });
  }

  first(id: string): void {
    const w = Number(prompt('First weight (kg)?') || 0);
    if (w <= 0) return;
    this.api.recordFirst(id, w).subscribe({
      next: () => this.reload(),
      error: (e) => this.error.set(e?.error?.message || 'Failed'),
    });
  }

  complete(id: string): void {
    const second = Number(prompt('Second weight (kg)?') || 0);
    if (second <= 0) return;
    this.api.complete(id, second).subscribe({
      next: () => this.reload(),
      error: (e) => this.error.set(e?.error?.message || 'Failed'),
    });
  }
}
