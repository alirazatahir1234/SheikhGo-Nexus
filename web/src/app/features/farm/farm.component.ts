import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { FarmEvent, FarmFlock, FarmService } from '../../core/services/farm.service';
import { ExtendedModulesService } from '../../core/services/extended-modules.service';
import { ProductsService } from '../../core/services/products.service';
import { Product } from '../../core/models/erp.models';

@Component({
  selector: 'app-farm',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, DatePipe, CurrencyPipe],
  templateUrl: './farm.component.html',
  styleUrl: './farm.component.scss',
})
export class FarmComponent implements OnInit {
  readonly flocks = signal<FarmFlock[]>([]);
  readonly events = signal<FarmEvent[]>([]);
  readonly shareholders = signal<any[]>([]);
  readonly products = signal<Product[]>([]);
  readonly selectedId = signal<string | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly showFlock = signal(false);
  readonly showEvent = signal(false);
  readonly showShare = signal(false);
  readonly saving = signal(false);

  readonly flockForm;
  readonly eventForm;
  readonly shareForm;

  constructor(
    private readonly api: FarmService,
    private readonly ext: ExtendedModulesService,
    private readonly productsApi: ProductsService,
    private readonly fb: FormBuilder,
  ) {
    this.flockForm = this.fb.nonNullable.group({
      code: ['', Validators.required],
      name: ['', Validators.required],
      birdType: ['Broiler', Validators.required],
      placementDate: [new Date().toISOString().slice(0, 10), Validators.required],
      placedQty: [1000, Validators.required],
      shed: [''],
      remarks: [''],
    });
    this.eventForm = this.fb.nonNullable.group({
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      eventType: ['Feed', Validators.required],
      qty: [0, Validators.required],
      amount: [0],
      productId: [''],
      remarks: [''],
    });
    this.shareForm = this.fb.nonNullable.group({
      holderName: ['', Validators.required],
      partyCode: [''],
      sharePercent: [0, Validators.required],
      shareType: ['A'],
      remarks: [''],
    });
  }

  ngOnInit(): void {
    this.productsApi.getAll().subscribe({ next: (p) => this.products.set(p) });
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.api.getFlocks().subscribe({
      next: (rows) => {
        this.flocks.set(rows);
        this.loading.set(false);
        if (!this.selectedId() && rows.length) this.select(rows[0].id);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load flocks');
      },
    });
  }

  select(id: string): void {
    this.selectedId.set(id);
    this.api.getEvents(id).subscribe({ next: (e) => this.events.set(e) });
    this.ext.getShareholders(id).subscribe({ next: (s) => this.shareholders.set(s) });
  }

  saveShare(): void {
    const id = this.selectedId();
    if (!id || this.shareForm.invalid) return;
    this.saving.set(true);
    this.ext.createShareholder({ farmFlockId: id, ...this.shareForm.getRawValue() }).subscribe({
      next: () => {
        this.saving.set(false);
        this.showShare.set(false);
        this.select(id);
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Failed to save shareholder');
      },
    });
  }

  saveFlock(): void {
    if (this.flockForm.invalid) return;
    this.saving.set(true);
    this.api.createFlock(this.flockForm.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.showFlock.set(false);
        this.reload();
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Create flock failed');
      },
    });
  }

  saveEvent(): void {
    const id = this.selectedId();
    if (!id || this.eventForm.invalid) return;
    const v = this.eventForm.getRawValue();
    this.saving.set(true);
    this.api
      .addEvent(id, {
        ...v,
        productId: v.productId || null,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.showEvent.set(false);
          this.reload();
          this.select(id);
        },
        error: (e) => {
          this.saving.set(false);
          this.error.set(e?.error?.message || 'Event failed');
        },
      });
  }

  closeSelected(): void {
    const id = this.selectedId();
    if (!id) return;
    this.api.close(id).subscribe({ next: () => this.reload() });
  }
}
