import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import {
  BalanceSheetReport,
  CashBankAccount,
  FinanceApiService,
  FiscalPeriod,
  JournalEntry,
  ProfitLossReport,
  SupplierPayment,
  TrialBalanceReport,
} from '../../core/services/finance-api.service';
import { AccountsService } from '../../core/services/accounts.service';
import { PurchasesService, PurchaseInvoice } from '../../core/services/purchases.service';
import { PartiesService } from '../../core/services/parties.service';
import { Account, Party } from '../../core/models/erp.models';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SearchBoxComponent } from '../../shared/components/search-box/search-box.component';
import { statusLabel } from '../../shared/documents/document-status-stepper.component';
import { bindTabFromRoute, navigateModuleTab } from '../../core/nav/route-tabs';

type FinTab =
  | 'dashboard'
  | 'accounts'
  | 'journals'
  | 'ledger'
  | 'cashbank'
  | 'periods'
  | 'trial'
  | 'pl'
  | 'bs'
  | 'supplierPayments';

const FIN_TAB_ROUTE: Record<FinTab, string> = {
  dashboard: 'dashboard',
  accounts: 'chart-of-accounts',
  journals: 'journals',
  ledger: 'general-ledger',
  cashbank: 'cash-bank',
  periods: 'periods',
  trial: 'trial-balance',
  pl: 'profit-loss',
  bs: 'balance-sheet',
  supplierPayments: 'supplier-payments',
};

@Component({
  selector: 'app-finance',
  standalone: true,
  imports: [FormsModule, ReactiveFormsModule, CurrencyPipe, DatePipe, PageHeaderComponent, SearchBoxComponent, RouterLink, RouterLinkActive],
  templateUrl: './finance.component.html',
  styleUrl: './finance.component.scss',
})
export class FinanceComponent implements OnInit {
  private readonly router = inject(Router);
  readonly tab = signal<FinTab>('dashboard');
  readonly search = signal('');
  readonly loading = signal(true);
  readonly error = signal('');
  readonly saving = signal(false);
  readonly showForm = signal(false);

  readonly accounts = signal<Account[]>([]);
  readonly journals = signal<JournalEntry[]>([]);
  readonly cashBanks = signal<CashBankAccount[]>([]);
  readonly periods = signal<FiscalPeriod[]>([]);
  readonly supplierPayments = signal<SupplierPayment[]>([]);
  readonly parties = signal<Party[]>([]);
  readonly purchaseInvoices = signal<PurchaseInvoice[]>([]);
  readonly trial = signal<TrialBalanceReport | null>(null);
  readonly pl = signal<ProfitLossReport | null>(null);
  readonly bs = signal<BalanceSheetReport | null>(null);
  readonly ledgerAccountId = signal('');
  readonly ledgerLines = signal<{ date: string; voucherNo: string; debit: number; credit: number; balance: number }[]>([]);

  readonly statusLabel = statusLabel;
  readonly today = new Date().toISOString().slice(0, 10);

  readonly filteredAccounts = computed(() => {
    const q = this.search().toLowerCase().trim();
    return this.accounts().filter(
      (a) => !q || a.code.toLowerCase().includes(q) || a.name.toLowerCase().includes(q),
    );
  });

  readonly filteredJournals = computed(() => {
    const q = this.search().toLowerCase().trim();
    return this.journals().filter(
      (j) => !q || j.journalNo.toLowerCase().includes(q) || (j.narration ?? '').toLowerCase().includes(q),
    );
  });

  readonly arApHint = computed(() => {
    const tb = this.trial();
    if (!tb) return 'Load Trial Balance for balance check.';
    const balanced = Math.abs(tb.totalDebit - tb.totalCredit) < 0.01;
    return balanced ? `TB balanced · Dr ${tb.totalDebit} = Cr ${tb.totalCredit}` : `TB OUT OF BALANCE · Dr ${tb.totalDebit} vs Cr ${tb.totalCredit}`;
  });

  readonly openPeriodCount = computed(() => this.periods().filter((p) => !p.isClosed).length);

  readonly journalForm;
  readonly paymentForm;

  constructor(
    private readonly api: FinanceApiService,
    private readonly accountsApi: AccountsService,
    private readonly purchasesApi: PurchasesService,
    private readonly partiesApi: PartiesService,
    private readonly fb: FormBuilder,
  ) {
    this.journalForm = this.fb.nonNullable.group({
      postingDate: [this.today, Validators.required],
      narration: [''],
      lines: this.fb.array([this.createJournalLine(), this.createJournalLine()]),
    });
    this.paymentForm = this.fb.nonNullable.group({
      date: [this.today, Validators.required],
      partyId: ['', Validators.required],
      purchaseInvoiceId: ['', Validators.required],
      cashBankAccountId: [''],
      amount: [0, [Validators.required, Validators.min(0.01)]],
      remarks: [''],
    });
  }

  get journalLines(): FormArray {
    return this.journalForm.get('lines') as FormArray;
  }

  ngOnInit(): void {
    bindTabFromRoute<FinTab>((tab) => this.applyTab(tab));
    this.reload();
    this.partiesApi.getAll().subscribe({
      next: (d) => this.parties.set(d.filter((p) => p.partyType === 'Supplier' || p.partyType === 'Both')),
      error: () => this.parties.set([]),
    });
    this.purchasesApi.getAll().subscribe({
      next: (d) => this.purchaseInvoices.set(d.filter((i) => this.statusLabel(i.status) === 'Posted')),
      error: () => this.purchaseInvoices.set([]),
    });
  }

  createJournalLine() {
    return this.fb.nonNullable.group({
      accountId: ['', Validators.required],
      debit: [0, [Validators.min(0)]],
      credit: [0, [Validators.min(0)]],
      description: [''],
    });
  }

  private applyTab(tab: FinTab): void {
    this.tab.set(tab);
    this.showForm.set(false);
    this.error.set('');
    if (tab === 'trial') this.loadTrial();
    if (tab === 'pl') this.loadPl();
    if (tab === 'bs') this.loadBs();
    if (tab === 'periods') this.loadPeriods();
  }

  setTab(tab: FinTab): void {
    navigateModuleTab(this.router, 'finance', FIN_TAB_ROUTE[tab]);
  }

  reload(): void {
    this.loading.set(true);
    let pending = 4;
    const done = () => {
      pending -= 1;
      if (pending <= 0) this.loading.set(false);
    };
    this.accountsApi.getAll().subscribe({
      next: (d) => { this.accounts.set(d); done(); },
      error: () => { this.accounts.set([]); done(); },
    });
    this.api.getJournals().subscribe({
      next: (d) => { this.journals.set(d); done(); },
      error: () => { this.journals.set([]); done(); },
    });
    this.api.getCashBank().subscribe({
      next: (d) => { this.cashBanks.set(d); done(); },
      error: () => { this.cashBanks.set([]); done(); },
    });
    this.api.getSupplierPayments().subscribe({
      next: (d) => { this.supplierPayments.set(d); done(); },
      error: () => { this.supplierPayments.set([]); done(); },
    });
    this.loadTrial();
    this.loadPeriods();
  }

  loadTrial(): void {
    this.api.trialBalance(this.today).subscribe({
      next: (r) => this.trial.set(r),
      error: () => this.trial.set(null),
    });
  }

  loadPl(): void {
    const from = `${new Date().getFullYear()}-01-01`;
    this.api.profitLoss(from, this.today).subscribe({
      next: (r) => this.pl.set(r),
      error: () => this.pl.set(null),
    });
  }

  loadBs(): void {
    this.api.balanceSheet(this.today).subscribe({
      next: (r) => this.bs.set(r),
      error: () => this.bs.set(null),
    });
  }

  loadPeriods(): void {
    this.api.getPeriods().subscribe({
      next: (d) => this.periods.set(d),
      error: () => this.periods.set([]),
    });
  }

  loadLedger(): void {
    const id = this.ledgerAccountId();
    if (!id) return;
    const from = `${new Date().getFullYear()}-01-01`;
    this.api.ledger(id, from, this.today).subscribe({
      next: (r) => this.ledgerLines.set(r.lines),
      error: () => this.ledgerLines.set([]),
    });
  }

  openJournalForm(): void {
    this.journalForm.reset({ postingDate: this.today, narration: '' });
    this.journalLines.clear();
    this.journalLines.push(this.createJournalLine());
    this.journalLines.push(this.createJournalLine());
    this.showForm.set(true);
  }

  openPaymentForm(): void {
    this.paymentForm.reset({
      date: this.today,
      partyId: '',
      purchaseInvoiceId: '',
      cashBankAccountId: this.cashBanks().find((c) => c.isDefault)?.id ?? '',
      amount: 0,
      remarks: '',
    });
    this.showForm.set(true);
  }

  addJournalLine(): void {
    this.journalLines.push(this.createJournalLine());
  }

  saveJournal(): void {
    if (this.journalForm.invalid) {
      this.journalForm.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    const raw = this.journalForm.getRawValue();
    this.api
      .createJournal({
        postingDate: raw.postingDate,
        narration: raw.narration,
        post: true,
        lines: raw.lines.map((l) => ({
          accountId: l.accountId,
          debit: Number(l.debit || 0),
          credit: Number(l.credit || 0),
          description: l.description,
        })),
      })
      .subscribe({
        next: (j) => {
          this.journals.update((list) => [j, ...list]);
          this.saving.set(false);
          this.showForm.set(false);
          this.loadTrial();
        },
        error: (e) => {
          this.saving.set(false);
          this.error.set(e?.error?.message || 'Failed to create journal.');
        },
      });
  }

  onPaymentInvoiceChange(): void {
    const id = String(this.paymentForm.value.purchaseInvoiceId || '');
    const inv = this.purchaseInvoices().find((i) => i.id === id);
    if (!inv) return;
    this.paymentForm.patchValue({ partyId: inv.partyId, amount: inv.grandTotal });
  }

  savePayment(): void {
    if (this.paymentForm.invalid) {
      this.paymentForm.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    const raw = this.paymentForm.getRawValue();
    this.api
      .createSupplierPayment({
        date: raw.date,
        partyId: raw.partyId,
        amount: Number(raw.amount),
        cashBankAccountId: raw.cashBankAccountId || null,
        remarks: raw.remarks,
        post: true,
        applications: [{ purchaseInvoiceId: raw.purchaseInvoiceId, amountApplied: Number(raw.amount) }],
      })
      .subscribe({
        next: (p) => {
          this.supplierPayments.update((list) => [p, ...list]);
          this.saving.set(false);
          this.showForm.set(false);
          this.loadTrial();
        },
        error: (e) => {
          this.saving.set(false);
          this.error.set(e?.error?.message || 'Failed to create supplier payment.');
        },
      });
  }

  closePeriod(id: string): void {
    this.api.closePeriod(id).subscribe({
      next: () => this.loadPeriods(),
      error: (e) => this.error.set(e?.error?.message || 'Failed to close period.'),
    });
  }

  openPeriod(id: string): void {
    this.api.openPeriod(id).subscribe({
      next: () => this.loadPeriods(),
      error: (e) => this.error.set(e?.error?.message || 'Failed to open period.'),
    });
  }
}
