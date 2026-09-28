import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ExtendedModulesService } from '../../core/services/extended-modules.service';
import { EmployeesService } from '../../core/services/employees.service';
import { Employee } from '../../core/models/erp.models';

@Component({
  selector: 'app-leaves',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, DatePipe],
  templateUrl: './leaves.component.html',
})
export class LeavesComponent implements OnInit {
  readonly items = signal<any[]>([]);
  readonly employees = signal<Employee[]>([]);
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly form;

  constructor(
    private readonly api: ExtendedModulesService,
    private readonly employeesApi: EmployeesService,
    private readonly fb: FormBuilder,
  ) {
    this.form = this.fb.nonNullable.group({
      employeeId: ['', Validators.required],
      fromDate: [new Date().toISOString().slice(0, 10), Validators.required],
      toDate: [new Date().toISOString().slice(0, 10), Validators.required],
      leaveType: ['Casual', Validators.required],
      remarks: [''],
    });
  }

  ngOnInit(): void {
    this.employeesApi.getAll().subscribe({ next: (e) => this.employees.set(e) });
    this.api.getLeaves().subscribe({
      next: (rows) => this.items.set(rows),
      error: () => this.error.set('Could not load leaves'),
    });
  }

  openForm(): void {
    this.form.reset({
      employeeId: '',
      fromDate: new Date().toISOString().slice(0, 10),
      toDate: new Date().toISOString().slice(0, 10),
      leaveType: 'Casual',
      remarks: '',
    });
    this.showForm.set(true);
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.api.createLeave(this.form.getRawValue()).subscribe({
      next: (d) => {
        this.items.update((l) => [d, ...l]);
        this.saving.set(false);
        this.showForm.set(false);
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Failed');
      },
    });
  }
}
