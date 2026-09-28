import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ExtendedModulesService } from '../../core/services/extended-modules.service';

@Component({
  selector: 'app-day-close',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, DatePipe],
  templateUrl: './day-close.component.html',
})
export class DayCloseComponent implements OnInit {
  readonly items = signal<any[]>([]);
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly form;
  constructor(private readonly api: ExtendedModulesService, private readonly fb: FormBuilder) {
    this.form = this.fb.nonNullable.group({
      closeDate: [new Date().toISOString().slice(0, 10), Validators.required],
      remarks: [''],
    });
  }
  ngOnInit(): void { this.reload(); }
  reload(): void {
    this.api.getDayCloses().subscribe({ next: (d) => this.items.set(d), error: () => this.error.set('Load failed') });
  }
  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.api.closeDay(this.form.getRawValue()).subscribe({
      next: () => { this.saving.set(false); this.showForm.set(false); this.reload(); },
      error: (e) => { this.saving.set(false); this.error.set(e?.error?.message || 'Failed'); },
    });
  }
}
