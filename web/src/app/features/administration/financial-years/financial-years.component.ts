import { Component, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import {
  AdminApiService,
  FinancialYear,
  FiscalPeriod,
} from '../../../core/services/admin-api.service';

@Component({
  selector: 'app-admin-financial-years',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, DatePipe],
  template: `
    <app-page-header title="Financial years" subtitle="Fiscal years and monthly periods.">
      <button type="button" class="btn btn--primary" (click)="showYearForm.set(true)">+ Year</button>
    </app-page-header>

    @if (error()) {
      <div class="alert alert--danger">{{ error() }}</div>
    }
    @if (success()) {
      <div class="alert alert--success">{{ success() }}</div>
    }

    @if (showYearForm()) {
      <section class="panel form-panel">
        <h2>New financial year</h2>
        <form class="form-grid" [formGroup]="yearForm" (ngSubmit)="createYear()">
          <label class="field"><span>Code</span><input formControlName="code" placeholder="FY2026" /></label>
          <label class="field"><span>Start date</span><input type="date" formControlName="startDate" /></label>
          <label class="field"><span>End date</span><input type="date" formControlName="endDate" /></label>
          <div class="form-actions span-2">
            <button type="button" class="btn btn--ghost" (click)="showYearForm.set(false)">Cancel</button>
            <button type="submit" class="btn btn--primary" [disabled]="saving()">
              {{ saving() ? 'Saving…' : 'Create' }}
            </button>
          </div>
        </form>
      </section>
    }

    <section class="panel">
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Code</th>
              <th>Start</th>
              <th>End</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (y of years(); track y.id) {
              <tr
                (click)="selectYear(y)"
                [style.background]="selectedYear()?.id === y.id ? 'rgba(20,184,166,0.08)' : null"
                style="cursor: pointer"
              >
                <td>{{ y.code }}</td>
                <td>{{ y.startDate | date: 'mediumDate' }}</td>
                <td>{{ y.endDate | date: 'mediumDate' }}</td>
                <td>
                  @if (y.isCurrent) {
                    <span class="badge">Current</span>
                  }
                  @if (y.isClosed) {
                    Closed
                  }
                  @if (!y.isCurrent && !y.isClosed) {
                    —
                  }
                </td>
                <td class="actions">
                  @if (!y.isCurrent && !y.isClosed) {
                    <button
                      type="button"
                      class="btn btn--ghost btn--sm"
                      (click)="setCurrent(y, $event)"
                    >
                      Set current
                    </button>
                  }
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="5" class="empty">{{ loading() ? 'Loading…' : 'No financial years.' }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </section>

    @if (selectedYear(); as year) {
      <section class="panel form-panel" style="margin-top: 1.25rem">
        <h2>Fiscal periods — {{ year.code }}</h2>
        <form class="form-grid" [formGroup]="periodForm" (ngSubmit)="createPeriod()" style="margin-bottom: 1rem">
          <label class="field"><span>Name</span><input formControlName="name" placeholder="Period 1" /></label>
          <label class="field"
            ><span>Number</span><input type="number" formControlName="periodNumber"
          /></label>
          <label class="field"><span>Start</span><input type="date" formControlName="startDate" /></label>
          <label class="field"><span>End</span><input type="date" formControlName="endDate" /></label>
          <div class="form-actions span-2">
            <button type="submit" class="btn btn--primary" [disabled]="savingPeriod()">
              {{ savingPeriod() ? 'Saving…' : 'Add period' }}
            </button>
          </div>
        </form>
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>#</th>
                <th>Name</th>
                <th>Start</th>
                <th>End</th>
                <th>Closed</th>
              </tr>
            </thead>
            <tbody>
              @for (p of periods(); track p.id) {
                <tr>
                  <td>{{ p.periodNumber }}</td>
                  <td>{{ p.name }}</td>
                  <td>{{ p.startDate | date: 'mediumDate' }}</td>
                  <td>{{ p.endDate | date: 'mediumDate' }}</td>
                  <td>{{ p.isClosed ? 'Yes' : '—' }}</td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="5" class="empty">No periods for this year.</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </section>
    }
  `,
})
export class AdminFinancialYearsComponent implements OnInit {
  readonly years = signal<FinancialYear[]>([]);
  readonly periods = signal<FiscalPeriod[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly savingPeriod = signal(false);
  readonly error = signal('');
  readonly success = signal('');
  readonly showYearForm = signal(false);
  readonly selectedYear = signal<FinancialYear | null>(null);
  readonly yearForm;
  readonly periodForm;

  constructor(
    private readonly api: AdminApiService,
    private readonly fb: FormBuilder,
  ) {
    this.yearForm = this.fb.nonNullable.group({
      code: ['', Validators.required],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
    });
    this.periodForm = this.fb.nonNullable.group({
      name: ['', Validators.required],
      periodNumber: [1, Validators.required],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
    });
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getFinancialYears().subscribe({
      next: (rows) => {
        this.years.set(rows);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load financial years.');
      },
    });
  }

  createYear(): void {
    if (this.yearForm.invalid) return;
    this.saving.set(true);
    this.api.createFinancialYear(this.yearForm.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.showYearForm.set(false);
        this.yearForm.reset({ code: '', startDate: '', endDate: '' });
        this.success.set('Financial year created.');
        this.load();
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Failed to create year.');
      },
    });
  }

  setCurrent(y: FinancialYear, event: Event): void {
    event.stopPropagation();
    this.api.setCurrentFinancialYear(y.id).subscribe({
      next: () => {
        this.success.set(`${y.code} set as current.`);
        this.load();
      },
      error: (e) => this.error.set(e?.error?.message || 'Failed to set current year.'),
    });
  }

  selectYear(y: FinancialYear): void {
    this.selectedYear.set(y);
    this.api.getFiscalPeriods(y.id).subscribe({
      next: (rows) => this.periods.set(rows),
      error: () => {
        this.periods.set([]);
        this.error.set('Could not load fiscal periods.');
      },
    });
  }

  createPeriod(): void {
    const year = this.selectedYear();
    if (!year || this.periodForm.invalid) return;
    this.savingPeriod.set(true);
    const raw = this.periodForm.getRawValue();
    this.api
      .createFiscalPeriod(year.id, {
        name: raw.name,
        periodNumber: Number(raw.periodNumber),
        startDate: raw.startDate,
        endDate: raw.endDate,
      })
      .subscribe({
        next: () => {
          this.savingPeriod.set(false);
          this.periodForm.reset({ name: '', periodNumber: raw.periodNumber + 1, startDate: '', endDate: '' });
          this.selectYear(year);
          this.success.set('Period added.');
        },
        error: (e) => {
          this.savingPeriod.set(false);
          this.error.set(e?.error?.message || 'Failed to create period.');
        },
      });
  }
}
