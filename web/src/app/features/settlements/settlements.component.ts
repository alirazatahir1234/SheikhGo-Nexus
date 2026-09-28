import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { PartySettlement, SettlementsService } from '../../core/services/warehouses.service';
import { PartiesService } from '../../core/services/parties.service';
import { Party } from '../../core/models/erp.models';

@Component({
  selector: 'app-settlements',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, CurrencyPipe, DatePipe],
  templateUrl: './settlements.component.html',
  styleUrl: './settlements.component.scss',
})
export class SettlementsComponent implements OnInit {
  readonly items = signal<PartySettlement[]>([]);
  readonly parties = signal<Party[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly form;

  constructor(
    private readonly api: SettlementsService,
    private readonly partiesApi: PartiesService,
    private readonly fb: FormBuilder,
  ) {
    this.form = this.fb.nonNullable.group({
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      partyId: ['', Validators.required],
      settlementType: ['Receipt', Validators.required],
      mode: ['Cash', Validators.required],
      amount: [0, [Validators.required, Validators.min(0.01)]],
      remarks: [''],
    });
  }

  ngOnInit(): void {
    this.partiesApi.getAll().subscribe({
      next: (data) => this.parties.set(data),
      error: () => this.parties.set([]),
    });
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.api.getAll().subscribe({
      next: (data) => {
        this.items.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load receipts / payments.');
      },
    });
  }

  filteredParties(): Party[] {
    const type = this.form.value.settlementType;
    if (type === 'Payment') {
      return this.parties().filter((p) => p.partyType === 'Supplier' || p.partyType === 'Both');
    }
    return this.parties().filter((p) => p.partyType === 'Customer' || p.partyType === 'Both');
  }

  openForm(kind: 'Receipt' | 'Payment'): void {
    this.form.reset({
      date: new Date().toISOString().slice(0, 10),
      partyId: '',
      settlementType: kind,
      mode: 'Cash',
      amount: 0,
      remarks: '',
    });
    this.showForm.set(true);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.api.create(this.form.getRawValue()).subscribe({
      next: (created) => {
        this.items.update((list) => [created, ...list]);
        this.saving.set(false);
        this.showForm.set(false);
        this.error.set('');
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err?.error?.message || 'Failed to post settlement.');
      },
    });
  }
}
