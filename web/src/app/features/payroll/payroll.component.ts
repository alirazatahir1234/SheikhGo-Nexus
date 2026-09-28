import { Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { EmployeesService } from '../../core/services/employees.service';
import { ExtendedModulesService } from '../../core/services/extended-modules.service';
import { Employee } from '../../core/models/erp.models';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SearchBoxComponent } from '../../shared/components/search-box/search-box.component';

@Component({
  selector: 'app-payroll',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe, DatePipe, PageHeaderComponent, SearchBoxComponent],
  templateUrl: './payroll.component.html',
})
export class PayrollComponent implements OnInit {
  readonly tab = signal<'employees' | 'salary'>('employees');
  readonly employees = signal<Employee[]>([]);
  readonly salarySheet = signal<any | null>(null);
  readonly search = signal('');
  readonly showForm = signal(false);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly saving = signal(false);

  readonly filtered = computed(() => {
    const q = this.search().toLowerCase().trim();
    return this.employees().filter(
      (e) =>
        !q ||
        e.code.toLowerCase().includes(q) ||
        e.name.toLowerCase().includes(q) ||
        e.department.toLowerCase().includes(q) ||
        e.designation.toLowerCase().includes(q),
    );
  });

  readonly form;
  readonly salaryForm;

  constructor(
    private readonly api: EmployeesService,
    private readonly modules: ExtendedModulesService,
    private readonly fb: FormBuilder,
  ) {
    this.form = this.fb.nonNullable.group({
      code: ['', Validators.required],
      name: ['', Validators.required],
      designation: ['', Validators.required],
      department: ['', Validators.required],
      joinDate: [new Date().toISOString().slice(0, 10), Validators.required],
      basicSalary: [0, [Validators.required, Validators.min(0)]],
      phone: [''],
    });
    const now = new Date();
    this.salaryForm = this.fb.nonNullable.group({
      year: [now.getFullYear(), Validators.required],
      month: [now.getMonth() + 1, Validators.required],
      defaultAllowancePercent: [0],
      defaultDeductionPercent: [0],
    });
  }

  ngOnInit(): void {
    this.load();
    this.loadSalary();
  }

  load(): void {
    this.loading.set(true);
    this.api.getAll().subscribe({
      next: (data) => {
        this.employees.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.employees.set([]);
        this.loading.set(false);
        this.error.set('Could not load employees.');
      },
    });
  }

  loadSalary(): void {
    const { year, month } = this.salaryForm.getRawValue();
    this.modules.getSalarySheet(year, month).subscribe({
      next: (s) => this.salarySheet.set(s),
      error: () => this.salarySheet.set(null),
    });
  }

  openForm(): void {
    this.form.reset({
      code: '',
      name: '',
      designation: '',
      department: '',
      joinDate: new Date().toISOString().slice(0, 10),
      basicSalary: 0,
      phone: '',
    });
    this.showForm.set(true);
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.api.create(this.form.getRawValue()).subscribe({
      next: (e) => {
        this.employees.update((l) => [e, ...l]);
        this.saving.set(false);
        this.showForm.set(false);
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Failed to save employee.');
      },
    });
  }

  generateSalary(): void {
    if (this.salaryForm.invalid) return;
    this.saving.set(true);
    this.modules.generateSalary(this.salaryForm.getRawValue()).subscribe({
      next: (s) => {
        this.salarySheet.set(s);
        this.saving.set(false);
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Failed to generate salary sheet');
      },
    });
  }

  pay(id: string): void {
    this.modules.paySalary(id).subscribe({
      next: () => this.loadSalary(),
      error: (e) => this.error.set(e?.error?.message || 'Pay failed'),
    });
  }
}
