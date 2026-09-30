import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import {
  AttendanceRow,
  AttendanceSummary,
  Department,
  Designation,
  Employee,
  HrApiService,
  LeaveBalance,
  LeaveRequest,
  LeaveType,
  PayrollComponent,
  PayrollPeriod,
  PayrollRun,
} from '../../core/services/hr-api.service';
import { FinanceApiService, CashBankAccount } from '../../core/services/finance-api.service';
import { AdminApiService } from '../../core/services/admin-api.service';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SearchBoxComponent } from '../../shared/components/search-box/search-box.component';
import {
  DocumentStatusStepperComponent,
  statusLabel,
} from '../../shared/documents/document-status-stepper.component';
import { DocumentActionsComponent } from '../../shared/documents/document-actions.component';
import { bindTabFromRoute, navigateModuleTab } from '../../core/nav/route-tabs';

type HrTab =
  | 'dashboard'
  | 'employees'
  | 'departments'
  | 'designations'
  | 'attendance'
  | 'leave'
  | 'periods'
  | 'payroll'
  | 'runDetail';

const HR_TAB_ROUTE: Record<Exclude<HrTab, 'runDetail'>, string> = {
  dashboard: 'dashboard',
  employees: 'employees',
  departments: 'departments',
  designations: 'designations',
  attendance: 'attendance',
  leave: 'leave',
  periods: 'periods',
  payroll: 'payroll',
};

@Component({
  selector: 'app-hr',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    CurrencyPipe,
    DatePipe,
    PageHeaderComponent,
    SearchBoxComponent,
    DocumentStatusStepperComponent,
    DocumentActionsComponent,
    RouterLink,
    RouterLinkActive,
  ],
  templateUrl: './hr.component.html',
  styleUrl: './hr.component.scss',
})
export class HrComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly hr = inject(HrApiService);
  private readonly finance = inject(FinanceApiService);
  private readonly admin = inject(AdminApiService);
  private readonly fb = inject(FormBuilder);

  readonly tab = signal<HrTab>('dashboard');
  readonly search = signal('');
  readonly loading = signal(true);
  readonly error = signal('');
  readonly saving = signal(false);
  readonly showForm = signal(false);

  readonly departments = signal<Department[]>([]);
  readonly designations = signal<Designation[]>([]);
  readonly components = signal<PayrollComponent[]>([]);
  readonly employees = signal<Employee[]>([]);
  readonly attendance = signal<AttendanceRow[]>([]);
  readonly summary = signal<AttendanceSummary[]>([]);
  readonly leaveTypes = signal<LeaveType[]>([]);
  readonly leaveBalances = signal<LeaveBalance[]>([]);
  readonly leaves = signal<LeaveRequest[]>([]);
  readonly periods = signal<PayrollPeriod[]>([]);
  readonly runs = signal<PayrollRun[]>([]);
  readonly selectedRun = signal<PayrollRun | null>(null);
  readonly cashBanks = signal<CashBankAccount[]>([]);
  readonly branches = signal<{ id: string; code: string; name: string }[]>([]);

  readonly statusLabel = statusLabel;
  readonly today = new Date().toISOString().slice(0, 10);
  readonly year = new Date().getFullYear();
  readonly month = new Date().getMonth() + 1;

  readonly filteredEmployees = computed(() => {
    const q = this.search().toLowerCase().trim();
    return this.employees().filter(
      (e) =>
        !q ||
        e.code.toLowerCase().includes(q) ||
        e.name.toLowerCase().includes(q) ||
        (e.department ?? '').toLowerCase().includes(q),
    );
  });

  readonly dashboard = computed(() => ({
    employees: this.employees().length,
    active: this.employees().filter((e) => e.isActive).length,
    leaveOpen: this.leaves().filter((l) => String(l.status) !== '4' && String(l.status) !== 'Posted').length,
    runs: this.runs().length,
  }));

  empForm = this.fb.nonNullable.group({
    code: ['', Validators.required],
    name: ['', Validators.required],
    departmentId: [''],
    designationId: [''],
    branchId: ['', Validators.required],
    joinDate: [this.today, Validators.required],
    basicSalary: [0, Validators.required],
    phone: [''],
    email: [''],
  });

  attForm = this.fb.nonNullable.group({
    date: [this.today, Validators.required],
    employeeId: ['', Validators.required],
    status: ['Present', Validators.required],
    overtimeHours: [0],
    remarks: [''],
  });

  leaveForm = this.fb.nonNullable.group({
    employeeId: ['', Validators.required],
    leaveTypeId: ['', Validators.required],
    fromDate: [this.today, Validators.required],
    toDate: [this.today, Validators.required],
    remarks: [''],
  });

  periodForm = this.fb.nonNullable.group({
    year: [this.year, Validators.required],
    month: [this.month, Validators.required],
  });

  deptForm = this.fb.nonNullable.group({ code: ['', Validators.required], name: ['', Validators.required] });
  desigForm = this.fb.nonNullable.group({ code: ['', Validators.required], name: ['', Validators.required] });
  payCashBankId = signal('');

  ngOnInit(): void {
    bindTabFromRoute<HrTab>((tab, id) => {
      this.tab.set(tab);
      this.showForm.set(false);
      this.error.set('');
      if (tab === 'runDetail' && id) {
        if (this.selectedRun()?.id !== id) {
          this.hr.getRun(id).subscribe({
            next: (run) => this.selectedRun.set(run),
            error: (e) => this.error.set(e?.error?.message ?? 'Load run failed'),
          });
        }
      } else if (tab !== 'runDetail') {
        this.selectedRun.set(null);
      }
    });
    this.reload();
  }

  setTab(t: Exclude<HrTab, 'runDetail'>): void {
    navigateModuleTab(this.router, 'hr', HR_TAB_ROUTE[t]);
  }

  reload(): void {
    this.loading.set(true);
    this.error.set('');
    Promise.all([
      this.hr.getDepartments().toPromise(),
      this.hr.getDesignations().toPromise(),
      this.hr.getComponents().toPromise(),
      this.hr.getEmployees().toPromise(),
      this.hr.getAttendance().toPromise(),
      this.hr.getAttendanceSummary(this.year, this.month).toPromise(),
      this.hr.getLeaveTypes().toPromise(),
      this.hr.getLeaveBalances().toPromise(),
      this.hr.getLeaves().toPromise(),
      this.hr.getPeriods().toPromise(),
      this.hr.getRuns().toPromise(),
      this.finance.getCashBank().toPromise(),
      this.admin.getBranches().toPromise(),
    ])
      .then(
        ([
          depts,
          desigs,
          comps,
          emps,
          att,
          sum,
          types,
          bals,
          leaves,
          periods,
          runs,
          banks,
          branches,
        ]) => {
          this.departments.set(depts ?? []);
          this.designations.set(desigs ?? []);
          this.components.set(comps ?? []);
          this.employees.set(emps ?? []);
          this.attendance.set(att ?? []);
          this.summary.set(sum ?? []);
          this.leaveTypes.set(types ?? []);
          this.leaveBalances.set(bals ?? []);
          this.leaves.set(leaves ?? []);
          this.periods.set(periods ?? []);
          this.runs.set(runs ?? []);
          this.cashBanks.set(banks ?? []);
          this.branches.set((branches ?? []).map((b: { id: string; code: string; name: string }) => b));
          if (!this.empForm.value.branchId && this.branches().length) {
            this.empForm.patchValue({ branchId: this.branches()[0].id });
          }
          if (!this.payCashBankId() && this.cashBanks().length) {
            const bank = this.cashBanks().find((c) => c.code === 'BANK') ?? this.cashBanks()[0];
            this.payCashBankId.set(bank.id);
          }
        },
      )
      .catch((e) => this.error.set(e?.error?.message ?? e?.message ?? 'Failed to load HR data'))
      .finally(() => this.loading.set(false));
  }

  saveDepartment(): void {
    if (this.deptForm.invalid) return;
    this.saving.set(true);
    this.hr.createDepartment(this.deptForm.getRawValue()).subscribe({
      next: () => {
        this.deptForm.reset({ code: '', name: '' });
        this.reload();
        this.saving.set(false);
      },
      error: (e) => {
        this.error.set(e?.error?.message ?? 'Save failed');
        this.saving.set(false);
      },
    });
  }

  saveDesignation(): void {
    if (this.desigForm.invalid) return;
    this.saving.set(true);
    this.hr.createDesignation(this.desigForm.getRawValue()).subscribe({
      next: () => {
        this.desigForm.reset({ code: '', name: '' });
        this.reload();
        this.saving.set(false);
      },
      error: (e) => {
        this.error.set(e?.error?.message ?? 'Save failed');
        this.saving.set(false);
      },
    });
  }

  saveEmployee(): void {
    if (this.empForm.invalid) return;
    const v = this.empForm.getRawValue();
    const allow = this.components().find((c) => c.code === 'ALLOW');
    const tax = this.components().find((c) => c.code === 'TAX');
    const other = this.components().find((c) => c.code === 'OTHER_DED');
    const profileLines = [
      allow ? { componentId: allow.id, amount: 20000 } : null,
      tax ? { componentId: tax.id, amount: 5000 } : null,
      other ? { componentId: other.id, amount: 2000 } : null,
    ].filter(Boolean);
    this.saving.set(true);
    this.hr
      .createEmployee({
        ...v,
        departmentId: v.departmentId || null,
        designationId: v.designationId || null,
        employmentStatus: 'Active',
        employmentType: 'Permanent',
        isActive: true,
        profileLines,
      })
      .subscribe({
        next: () => {
          this.showForm.set(false);
          this.reload();
          this.saving.set(false);
        },
        error: (e) => {
          this.error.set(e?.error?.message ?? 'Save failed');
          this.saving.set(false);
        },
      });
  }

  saveAttendance(): void {
    if (this.attForm.invalid) return;
    this.saving.set(true);
    this.hr.createAttendance(this.attForm.getRawValue()).subscribe({
      next: () => {
        this.showForm.set(false);
        this.reload();
        this.saving.set(false);
      },
      error: (e) => {
        this.error.set(e?.error?.message ?? 'Save failed');
        this.saving.set(false);
      },
    });
  }

  saveLeave(): void {
    if (this.leaveForm.invalid) return;
    this.saving.set(true);
    this.hr.createLeave(this.leaveForm.getRawValue()).subscribe({
      next: () => {
        this.showForm.set(false);
        this.reload();
        this.saving.set(false);
      },
      error: (e) => {
        this.error.set(e?.error?.message ?? 'Save failed');
        this.saving.set(false);
      },
    });
  }

  leaveAction(id: string, action: string): void {
    this.hr.leaveAction(id, action).subscribe({
      next: () => this.reload(),
      error: (e) => this.error.set(e?.error?.message ?? 'Action failed'),
    });
  }

  savePeriod(): void {
    if (this.periodForm.invalid) return;
    const v = this.periodForm.getRawValue();
    this.saving.set(true);
    this.hr.createPeriod(v.year, v.month).subscribe({
      next: () => {
        this.showForm.set(false);
        this.reload();
        this.saving.set(false);
      },
      error: (e) => {
        this.error.set(e?.error?.message ?? 'Save failed');
        this.saving.set(false);
      },
    });
  }

  createRun(periodId: string): void {
    this.hr.createRun(periodId).subscribe({
      next: (run) => {
        this.reload();
        this.openRun(run.id);
      },
      error: (e) => this.error.set(e?.error?.message ?? 'Create run failed'),
    });
  }

  openRun(id: string): void {
    navigateModuleTab(this.router, 'hr', 'payroll', id);
  }

  calculateRun(): void {
    const run = this.selectedRun();
    if (!run) return;
    this.hr.calculateRun(run.id).subscribe({
      next: (r) => {
        this.selectedRun.set(r);
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.message ?? 'Calculate failed'),
    });
  }

  runAction(action: string): void {
    const run = this.selectedRun();
    if (!run) return;
    this.hr.runAction(run.id, action).subscribe({
      next: (r) => {
        this.selectedRun.set(r);
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.message ?? 'Action failed'),
    });
  }

  payRun(): void {
    const run = this.selectedRun();
    const bankId = this.payCashBankId();
    if (!run || !bankId) return;
    this.hr.createPayment({ payrollRunId: run.id, cashBankAccountId: bankId, post: true }).subscribe({
      next: () => {
        this.openRun(run.id);
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.message ?? 'Payment failed'),
    });
  }
}
