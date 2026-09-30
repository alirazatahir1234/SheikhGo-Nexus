import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ExtendedModulesService } from '../../core/services/extended-modules.service';
import { PartiesService } from '../../core/services/parties.service';
import { Party } from '../../core/models/erp.models';
import { bindTabFromRoute, navigateModuleTab } from '../../core/nav/route-tabs';

type ChequesTab = 'pdc' | 'books';

const CHEQUES_TAB_ROUTE: Record<ChequesTab, string> = {
  pdc: 'pdc',
  books: 'books',
};

@Component({
  selector: 'app-cheques',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, CurrencyPipe, DatePipe, RouterLink, RouterLinkActive],
  templateUrl: './cheques.component.html',
  styleUrl: './cheques.component.scss',
})
export class ChequesComponent implements OnInit {
  private readonly router = inject(Router);
  readonly books = signal<any[]>([]);
  readonly pdcs = signal<any[]>([]);
  readonly parties = signal<Party[]>([]);
  readonly tab = signal<ChequesTab>('pdc');
  readonly showBook = signal(false);
  readonly showPdc = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly bookForm;
  readonly pdcForm;

  constructor(
    private readonly api: ExtendedModulesService,
    private readonly partiesApi: PartiesService,
    private readonly fb: FormBuilder,
  ) {
    this.bookForm = this.fb.nonNullable.group({
      bookNo: ['', Validators.required],
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      bankName: ['', Validators.required],
      chequeFrom: [1, Validators.required],
      chequeTo: [100, Validators.required],
      remarks: [''],
    });
    this.pdcForm = this.fb.nonNullable.group({
      voucherDate: [new Date().toISOString().slice(0, 10), Validators.required],
      chequeDate: [new Date().toISOString().slice(0, 10), Validators.required],
      chequeNo: ['', Validators.required],
      partyId: [''],
      chequeBookId: [''],
      amount: [0, Validators.required],
      direction: ['Issued', Validators.required],
      remarks: [''],
    });
  }

  ngOnInit(): void {
    bindTabFromRoute<ChequesTab>((tab) => this.tab.set(tab));
    this.partiesApi.getAll().subscribe({ next: (p) => this.parties.set(p) });
    this.reload();
  }

  setTab(tab: ChequesTab): void {
    navigateModuleTab(this.router, 'cheques', CHEQUES_TAB_ROUTE[tab]);
  }

  openBookForm(): void {
    navigateModuleTab(this.router, 'cheques', 'books');
    this.showBook.set(true);
  }

  openPdcForm(): void {
    navigateModuleTab(this.router, 'cheques', 'pdc');
    this.showPdc.set(true);
  }

  reload(): void {
    this.api.getChequeBooks().subscribe({ next: (b) => this.books.set(b) });
    this.api.getPdcs().subscribe({
      next: (p) => this.pdcs.set(p),
      error: () => this.error.set('Could not load cheques'),
    });
  }

  saveBook(): void {
    if (this.bookForm.invalid) return;
    this.saving.set(true);
    this.api.createChequeBook(this.bookForm.getRawValue()).subscribe({
      next: (b) => { this.books.update((l) => [b, ...l]); this.saving.set(false); this.showBook.set(false); },
      error: (e) => { this.saving.set(false); this.error.set(e?.error?.message || 'Failed'); },
    });
  }

  savePdc(): void {
    if (this.pdcForm.invalid) return;
    this.saving.set(true);
    const raw = this.pdcForm.getRawValue();
    this.api.createPdc({
      ...raw,
      partyId: raw.partyId || null,
      chequeBookId: raw.chequeBookId || null,
    }).subscribe({
      next: (p) => { this.pdcs.update((l) => [p, ...l]); this.saving.set(false); this.showPdc.set(false); this.reload(); },
      error: (e) => { this.saving.set(false); this.error.set(e?.error?.message || 'Failed'); },
    });
  }

  setStatus(id: string, status: string): void {
    this.api.updatePdcStatus(id, status).subscribe({ next: () => this.reload() });
  }
}
