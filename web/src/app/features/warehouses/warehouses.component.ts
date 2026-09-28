import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { Warehouse, WarehousesService } from '../../core/services/warehouses.service';

@Component({
  selector: 'app-warehouses',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent],
  templateUrl: './warehouses.component.html',
  styleUrl: './warehouses.component.scss',
})
export class WarehousesComponent implements OnInit {
  readonly items = signal<Warehouse[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly form;

  constructor(
    private readonly api: WarehousesService,
    private readonly fb: FormBuilder,
  ) {
    this.form = this.fb.nonNullable.group({
      code: ['', Validators.required],
      name: ['', Validators.required],
      location: [''],
      isDefault: [false],
    });
  }

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.api.getAll().subscribe({
      next: (data) => {
        this.items.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load warehouses.');
      },
    });
  }

  openForm(): void {
    this.form.reset({ code: '', name: '', location: '', isDefault: false });
    this.showForm.set(true);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.api.create(this.form.getRawValue()).subscribe({
      next: (created) => {
        this.items.update((list) => [...list, created]);
        this.saving.set(false);
        this.showForm.set(false);
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Failed to save warehouse.');
      },
    });
  }
}
