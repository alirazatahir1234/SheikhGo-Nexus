import { Component, OnInit, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ExtendedModulesService } from '../../core/services/extended-modules.service';
import { AccountsService } from '../../core/services/accounts.service';

@Component({
  selector: 'app-budgets',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, CurrencyPipe, DatePipe],
  templateUrl: './budgets.component.html',
})
export class BudgetsComponent implements OnInit {
  readonly items = signal<any[]>([]);
  readonly accounts = signal<any[]>([]);
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly form;
  constructor(
    private readonly api: ExtendedModulesService,
    private readonly accountsApi: AccountsService,
    private readonly fb: FormBuilder,
  ) {
    const now = new Date();
    this.form = this.fb.nonNullable.group({
      date: [now.toISOString().slice(0, 10), Validators.required],
      year: [now.getFullYear()],
      month: [now.getMonth() + 1],
      remarks: [''],
      lines: this.fb.array([this.line()]),
    });
  }
  get lines(): FormArray { return this.form.get('lines') as FormArray; }
  line() {
    return this.fb.nonNullable.group({
      accountId: [''], description: ['', Validators.required], qty: [1], amount: [0, Validators.required],
    });
  }
  ngOnInit(): void {
    this.accountsApi.getAll().subscribe({ next: (a) => this.accounts.set(a) });
    this.reload();
  }
  reload(): void {
    this.api.getBudgets().subscribe({ next: (b) => this.items.set(b), error: () => this.error.set('Load failed') });
  }
  openForm(): void {
    const now = new Date();
    this.form.reset({ date: now.toISOString().slice(0, 10), year: now.getFullYear(), month: now.getMonth() + 1, remarks: '' });
    this.lines.clear(); this.lines.push(this.line()); this.showForm.set(true);
  }
  addLine(): void { this.lines.push(this.line()); }
  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    const raw = this.form.getRawValue();
    this.api.createBudget({
      ...raw,
      lines: raw.lines.map((l: any) => ({ ...l, accountId: l.accountId || null })),
    }).subscribe({
      next: () => { this.saving.set(false); this.showForm.set(false); this.reload(); },
      error: (e) => { this.saving.set(false); this.error.set(e?.error?.message || 'Failed'); },
    });
  }
  approve(id: string): void { this.api.approveBudget(id).subscribe({ next: () => this.reload() }); }
}
