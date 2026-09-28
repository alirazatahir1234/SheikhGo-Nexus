import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ExtendedModulesService } from '../../core/services/extended-modules.service';

@Component({
  selector: 'app-cost-centres',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent],
  templateUrl: './cost-centres.component.html',
})
export class CostCentresComponent implements OnInit {
  readonly items = signal<any[]>([]);
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly form;

  constructor(private readonly api: ExtendedModulesService, private readonly fb: FormBuilder) {
    this.form = this.fb.nonNullable.group({
      code: ['', Validators.required],
      name: ['', Validators.required],
      location: [''],
      isActive: [true],
      remarks: [''],
    });
  }

  ngOnInit(): void {
    this.api.getCostCentres().subscribe({
      next: (rows) => this.items.set(rows),
      error: () => this.error.set('Could not load cost centres'),
    });
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.api.createCostCentre(this.form.getRawValue()).subscribe({
      next: (c) => {
        this.items.update((list) => [...list, c]);
        this.saving.set(false);
        this.showForm.set(false);
      },
      error: () => { this.saving.set(false); this.error.set('Failed to save'); },
    });
  }
}
