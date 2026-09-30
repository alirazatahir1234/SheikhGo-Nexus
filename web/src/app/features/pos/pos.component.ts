import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import {
  PosApiService,
  PosSale,
  PosShift,
  PosTerminal,
} from '../../core/services/pos-api.service';
import { ProductsService } from '../../core/services/products.service';
import { Product } from '../../core/models/erp.models';
import { statusLabel } from '../../shared/documents/document-status-stepper.component';
import { bindTabFromRoute, navigateModuleTab } from '../../core/nav/route-tabs';

type PosTab = 'shifts' | 'sales';

const POS_TAB_ROUTE: Record<PosTab, string> = {
  sales: 'sales',
  shifts: 'shifts',
};

@Component({
  selector: 'app-pos',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, CurrencyPipe, DatePipe, RouterLink, RouterLinkActive],
  templateUrl: './pos.component.html',
  styleUrl: './pos.component.scss',
})
export class PosComponent implements OnInit {
  private readonly router = inject(Router);
  readonly tab = signal<PosTab>('sales');
  readonly terminals = signal<PosTerminal[]>([]);
  readonly shifts = signal<PosShift[]>([]);
  readonly sales = signal<PosSale[]>([]);
  readonly products = signal<Product[]>([]);
  readonly showForm = signal(false);
  readonly showOpen = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly statusLabel = statusLabel;
  readonly today = new Date().toISOString().slice(0, 10);

  readonly openShift = computed(() => this.shifts().find((s) => s.status === 'Open') ?? null);

  readonly openForm;
  readonly saleForm;

  constructor(
    private readonly api: PosApiService,
    private readonly productsApi: ProductsService,
    private readonly fb: FormBuilder,
  ) {
    this.openForm = this.fb.nonNullable.group({
      terminalId: ['', Validators.required],
      openingCash: [10000, [Validators.required, Validators.min(0)]],
    });
    this.saleForm = this.fb.nonNullable.group({
      saleDate: [this.today],
      remarks: [''],
      cashAmount: [0, Validators.required],
      cardAmount: [0, Validators.required],
      lines: this.fb.array([this.line()]),
    });
  }

  get lines(): FormArray {
    return this.saleForm.get('lines') as FormArray;
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
    bindTabFromRoute<PosTab>((tab) => {
      this.tab.set(tab);
      this.showForm.set(false);
      this.error.set('');
    });
    this.productsApi.getAll().subscribe({ next: (p) => this.products.set(p) });
    this.reload();
  }

  reload(): void {
    this.api.getTerminals().subscribe({ next: (t) => this.terminals.set(t) });
    this.api.getShifts().subscribe({ next: (s) => this.shifts.set(s) });
    this.api.getSales().subscribe({
      next: (rows) => this.sales.set(rows),
      error: () => this.error.set('Could not load POS sales'),
    });
  }

  setTab(t: PosTab): void {
    navigateModuleTab(this.router, 'pos', POS_TAB_ROUTE[t]);
  }

  openSaleForm(): void {
    if (!this.openShift()) {
      this.error.set('Open a shift before selling.');
      return;
    }
    this.saleForm.reset({
      saleDate: this.today,
      remarks: '',
      cashAmount: 1200,
      cardAmount: 800,
    });
    this.lines.clear();
    this.lines.push(this.line());
    this.showForm.set(true);
  }

  addLine(): void {
    this.lines.push(this.line());
  }

  onProduct(i: number): void {
    const line = this.lines.at(i);
    const p = this.products().find((x) => String(x.id) === String(line.value.productId));
    if (p) {
      line.patchValue({ rate: p.salePrice, amount: p.salePrice * Number(line.value.qty || 0) });
    }
  }

  recalc(i: number): void {
    const line = this.lines.at(i);
    line.patchValue(
      { amount: Number(line.value.qty || 0) * Number(line.value.rate || 0) },
      { emitEvent: false },
    );
  }

  saveOpen(): void {
    if (this.openForm.invalid) return;
    this.saving.set(true);
    this.api.openShift(this.openForm.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.showOpen.set(false);
        this.reload();
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Failed');
      },
    });
  }

  closeShift(): void {
    const s = this.openShift();
    if (!s) return;
    const actual = Number(prompt('Actual cash in drawer?', String(s.openingCash)) || -1);
    if (actual < 0) return;
    this.api.closeShift(s.id, actual).subscribe({
      next: () => this.reload(),
      error: (e) => this.error.set(e?.error?.message || 'Failed'),
    });
  }

  saveSale(): void {
    this.lines.controls.forEach((_, i) => this.recalc(i));
    const shift = this.openShift();
    if (!shift || this.saleForm.invalid) return;
    const raw = this.saleForm.getRawValue();
    const lines = raw.lines.map((l) => ({
      productId: l.productId,
      qty: Number(l.qty),
      rate: Number(l.rate),
      amount: Number(l.qty) * Number(l.rate),
    }));
    const payments = [
      ...(Number(raw.cashAmount) > 0
        ? [{ tender: 'Cash', amount: Number(raw.cashAmount) }]
        : []),
      ...(Number(raw.cardAmount) > 0
        ? [{ tender: 'Card', amount: Number(raw.cardAmount) }]
        : []),
    ];
    this.saving.set(true);
    this.api
      .createSale({
        shiftId: shift.id,
        saleDate: raw.saleDate,
        remarks: raw.remarks,
        lines,
        payments,
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
}
