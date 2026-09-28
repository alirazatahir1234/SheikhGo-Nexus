import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ExtendedModulesService } from '../../core/services/extended-modules.service';
import { EmployeesService } from '../../core/services/employees.service';

@Component({
  selector: 'app-hr-finance',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, CurrencyPipe, DatePipe],
  templateUrl: './hr-finance.component.html',
  styleUrl: './hr-finance.component.scss',
})
export class HrFinanceComponent implements OnInit {
  readonly advances = signal<any[]>([]);
  readonly loans = signal<any[]>([]);
  readonly eobi = signal<any[]>([]);
  readonly employees = signal<any[]>([]);
  readonly tab = signal<'advances' | 'loans' | 'eobi'>('advances');
  readonly show = signal<'advance' | 'loan' | 'eobi' | null>(null);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly advanceForm;
  readonly loanForm;
  readonly eobiForm;

  constructor(
    private readonly api: ExtendedModulesService,
    private readonly employeesApi: EmployeesService,
    private readonly fb: FormBuilder,
  ) {
    this.advanceForm = this.fb.nonNullable.group({
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      employeeId: ['', Validators.required],
      amount: [0, Validators.required],
      remarks: [''],
    });
    this.loanForm = this.fb.nonNullable.group({
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      employeeId: ['', Validators.required],
      loanAmount: [0, Validators.required],
      months: [6, Validators.required],
      installmentAmount: [0],
      remarks: [''],
    });
    this.eobiForm = this.fb.nonNullable.group({
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      employeeId: ['', Validators.required],
      amount: [0, Validators.required],
      remarks: [''],
    });
  }

  ngOnInit(): void {
    this.employeesApi.getAll().subscribe({ next: (e) => this.employees.set(e) });
    this.reload();
  }

  reload(): void {
    this.api.getAdvances().subscribe({ next: (a) => this.advances.set(a), error: () => this.error.set('Load failed') });
    this.api.getLoans().subscribe({ next: (l) => this.loans.set(l) });
    this.api.getEobi().subscribe({ next: (e) => this.eobi.set(e) });
  }

  saveAdvance(): void {
    if (this.advanceForm.invalid) return;
    this.saving.set(true);
    this.api.createAdvance(this.advanceForm.getRawValue()).subscribe({
      next: () => { this.saving.set(false); this.show.set(null); this.reload(); },
      error: (e) => { this.saving.set(false); this.error.set(e?.error?.message || 'Failed'); },
    });
  }

  saveLoan(): void {
    if (this.loanForm.invalid) return;
    this.saving.set(true);
    this.api.createLoan(this.loanForm.getRawValue()).subscribe({
      next: () => { this.saving.set(false); this.show.set(null); this.reload(); },
      error: (e) => { this.saving.set(false); this.error.set(e?.error?.message || 'Failed'); },
    });
  }

  saveEobi(): void {
    if (this.eobiForm.invalid) return;
    this.saving.set(true);
    this.api.createEobi(this.eobiForm.getRawValue()).subscribe({
      next: () => { this.saving.set(false); this.show.set(null); this.reload(); },
      error: (e) => { this.saving.set(false); this.error.set(e?.error?.message || 'Failed'); },
    });
  }
}
