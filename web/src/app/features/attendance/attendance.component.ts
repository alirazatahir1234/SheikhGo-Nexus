import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ExtendedModulesService } from '../../core/services/extended-modules.service';
import { EmployeesService } from '../../core/services/employees.service';

@Component({
  selector: 'app-attendance',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, CurrencyPipe, DatePipe],
  templateUrl: './attendance.component.html',
  styleUrl: './attendance.component.scss',
})
export class AttendanceComponent implements OnInit {
  readonly attendance = signal<any[]>([]);
  readonly overtime = signal<any[]>([]);
  readonly employees = signal<any[]>([]);
  readonly tab = signal<'attendance' | 'overtime'>('attendance');
  readonly show = signal<'att' | 'ot' | null>(null);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly attForm;
  readonly otForm;

  constructor(
    private readonly api: ExtendedModulesService,
    private readonly employeesApi: EmployeesService,
    private readonly fb: FormBuilder,
  ) {
    this.attForm = this.fb.nonNullable.group({
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      employeeId: ['', Validators.required],
      status: ['Present', Validators.required],
      shift: ['MORNING'],
      remarks: [''],
    });
    this.otForm = this.fb.nonNullable.group({
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      employeeId: ['', Validators.required],
      hours: [2, Validators.required],
      perHourRate: [500, Validators.required],
      remarks: [''],
    });
  }

  ngOnInit(): void {
    this.employeesApi.getAll().subscribe({ next: (e) => this.employees.set(e) });
    this.reload();
  }

  reload(): void {
    this.api.getAttendance().subscribe({ next: (a) => this.attendance.set(a), error: () => this.error.set('Load failed') });
    this.api.getOvertime().subscribe({ next: (o) => this.overtime.set(o) });
  }

  saveAtt(): void {
    if (this.attForm.invalid) return;
    this.saving.set(true);
    this.api.createAttendance(this.attForm.getRawValue()).subscribe({
      next: () => { this.saving.set(false); this.show.set(null); this.reload(); },
      error: () => { this.saving.set(false); },
    });
  }

  saveOt(): void {
    if (this.otForm.invalid) return;
    this.saving.set(true);
    this.api.createOvertime(this.otForm.getRawValue()).subscribe({
      next: () => { this.saving.set(false); this.show.set(null); this.reload(); },
      error: (e) => { this.saving.set(false); this.error.set(e?.error?.message || 'Failed'); },
    });
  }
}
