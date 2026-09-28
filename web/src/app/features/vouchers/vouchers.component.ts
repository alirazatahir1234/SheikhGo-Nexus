import { Component, OnInit, computed, signal } from '@angular/core';
import {
  FormArray,
  FormBuilder,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { VouchersService } from '../../core/services/vouchers.service';
import { AccountsService } from '../../core/services/accounts.service';
import { Account, Voucher, VoucherType } from '../../core/models/erp.models';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SearchBoxComponent } from '../../shared/components/search-box/search-box.component';

@Component({
  selector: 'app-vouchers',
  standalone: true,
  imports: [FormsModule, ReactiveFormsModule, CurrencyPipe, DatePipe, PageHeaderComponent, SearchBoxComponent],
  templateUrl: './vouchers.component.html',
})
export class VouchersComponent implements OnInit {
  readonly vouchers = signal<Voucher[]>([]);
  readonly accounts = signal<Account[]>([]);
  readonly search = signal('');
  readonly showForm = signal(false);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly saving = signal(false);
  readonly voucherTypes: VoucherType[] = [
    'JV', 'CP', 'CR', 'BP', 'BR',
    'CpFreight', 'CpAd', 'BpAd', 'CpSl', 'BpSl', 'DoSales', 'JvRm', 'JvAdj',
    'StkTrf', 'Sr', 'Pr', 'CpPaid',
  ];

  readonly filtered = computed(() => {
    const q = this.search().toLowerCase().trim();
    return this.vouchers().filter(
      (v) =>
        !q ||
        v.voucherNo.toLowerCase().includes(q) ||
        v.voucherType.toLowerCase().includes(q) ||
        v.narration.toLowerCase().includes(q),
    );
  });

  readonly form;

  constructor(
    private readonly api: VouchersService,
    private readonly accountsApi: AccountsService,
    private readonly fb: FormBuilder,
  ) {
    this.form = this.fb.nonNullable.group({
      voucherNo: ['', Validators.required],
      voucherType: ['JV' as VoucherType, Validators.required],
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      narration: ['', Validators.required],
      status: ['Posted'],
      lines: this.fb.array([this.createLine(), this.createLine()]),
    });
  }

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  ngOnInit(): void {
    this.load();
    this.accountsApi.getAll().subscribe({
      next: (data) => this.accounts.set(data.filter((a) => a.isPostable)),
      error: () => this.accounts.set([]),
    });
  }

  createLine() {
    return this.fb.nonNullable.group({
      accountId: ['', Validators.required],
      debit: [0, [Validators.min(0)]],
      credit: [0, [Validators.min(0)]],
      remarks: [''],
    });
  }

  addLine(): void {
    this.lines.push(this.createLine());
  }

  removeLine(index: number): void {
    if (this.lines.length > 2) this.lines.removeAt(index);
  }

  load(): void {
    this.loading.set(true);
    this.api.getAll().subscribe({
      next: (data) => {
        this.vouchers.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.vouchers.set([]);
        this.loading.set(false);
        this.error.set('Could not load vouchers.');
      },
    });
  }

  openForm(): void {
    this.form.reset({
      voucherNo: '',
      voucherType: 'JV',
      date: new Date().toISOString().slice(0, 10),
      narration: '',
      status: 'Posted',
    });
    this.lines.clear();
    this.lines.push(this.createLine());
    this.lines.push(this.createLine());
    this.showForm.set(true);
  }

  totalDebit(): number {
    return this.lines.controls.reduce((sum, c) => sum + Number(c.value.debit || 0), 0);
  }

  totalCredit(): number {
    return this.lines.controls.reduce((sum, c) => sum + Number(c.value.credit || 0), 0);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (Math.abs(this.totalDebit() - this.totalCredit()) > 0.01) {
      this.error.set('Debit and credit totals must balance.');
      return;
    }
    this.saving.set(true);
    const raw = this.form.getRawValue();
    const accountMap = new Map(this.accounts().map((a) => [a.id, a.name]));
    const payload = {
      ...raw,
      lines: raw.lines.map((l) => ({
        ...l,
        accountName: accountMap.get(String(l.accountId)),
      })),
      totalDebit: this.totalDebit(),
      totalCredit: this.totalCredit(),
    };
    this.api.create(payload).subscribe({
      next: (created) => {
        this.vouchers.update((list) => [created, ...list]);
        this.saving.set(false);
        this.showForm.set(false);
        this.error.set('');
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Failed to create voucher.');
      },
    });
  }
}
