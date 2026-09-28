import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { AccountsService } from '../../core/services/accounts.service';
import { Account } from '../../core/models/erp.models';
import {
  AgingReport,
  LedgerReport,
  ReportsService,
  TrialBalanceReport,
} from '../../core/services/reports.service';

type ReportTab = 'ledger' | 'trial' | 'ar' | 'ap';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, CurrencyPipe, DatePipe],
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss',
})
export class ReportsComponent implements OnInit {
  readonly tab = signal<ReportTab>('ledger');
  readonly accounts = signal<Account[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');

  readonly ledger = signal<LedgerReport | null>(null);
  readonly trial = signal<TrialBalanceReport | null>(null);
  readonly aging = signal<AgingReport | null>(null);

  readonly ledgerForm;
  readonly asOfForm;

  constructor(
    private readonly reports: ReportsService,
    private readonly accountsApi: AccountsService,
    private readonly fb: FormBuilder,
  ) {
    const today = new Date().toISOString().slice(0, 10);
    const monthStart = new Date(new Date().getFullYear(), new Date().getMonth(), 1)
      .toISOString()
      .slice(0, 10);
    this.ledgerForm = this.fb.nonNullable.group({
      accountId: ['', Validators.required],
      from: [monthStart, Validators.required],
      to: [today, Validators.required],
    });
    this.asOfForm = this.fb.nonNullable.group({
      asOf: [today, Validators.required],
    });
  }

  ngOnInit(): void {
    this.accountsApi.getAll().subscribe({
      next: (rows) => this.accounts.set(rows.filter((a) => a.isPostable)),
      error: () => this.error.set('Could not load accounts.'),
    });
  }

  setTab(t: ReportTab): void {
    this.tab.set(t);
    this.error.set('');
    this.ledger.set(null);
    this.trial.set(null);
    this.aging.set(null);
  }

  runLedger(): void {
    if (this.ledgerForm.invalid) {
      this.ledgerForm.markAllAsTouched();
      return;
    }
    const v = this.ledgerForm.getRawValue();
    this.loading.set(true);
    this.reports.ledger(v.accountId, v.from, v.to).subscribe({
      next: (r) => {
        this.ledger.set(r);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Ledger failed.');
      },
    });
  }

  runTrial(): void {
    this.loading.set(true);
    this.reports.trialBalance(this.asOfForm.getRawValue().asOf).subscribe({
      next: (r) => {
        this.trial.set(r);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Trial balance failed.');
      },
    });
  }

  runAging(kind: 'ar' | 'ap'): void {
    this.loading.set(true);
    const asOf = this.asOfForm.getRawValue().asOf;
    const req = kind === 'ar' ? this.reports.receivablesAging(asOf) : this.reports.payablesAging(asOf);
    req.subscribe({
      next: (r) => {
        this.aging.set(r);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Aging report failed.');
      },
    });
  }
}
