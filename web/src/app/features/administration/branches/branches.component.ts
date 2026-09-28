import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { AdminApiService, AdminBranch } from '../../../core/services/admin-api.service';

@Component({
  selector: 'app-admin-branches',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent],
  template: `
    <app-page-header title="Branches" subtitle="Company branches and default location.">
      <button type="button" class="btn btn--primary" (click)="openCreate()">+ Branch</button>
    </app-page-header>

    @if (error()) {
      <div class="alert alert--danger">{{ error() }}</div>
    }

    @if (showForm()) {
      <section class="panel form-panel">
        <h2>{{ editingId() ? 'Edit branch' : 'New branch' }}</h2>
        <form class="form-grid" [formGroup]="form" (ngSubmit)="save()">
          <label class="field"><span>Code</span><input formControlName="code" /></label>
          <label class="field"><span>Name</span><input formControlName="name" /></label>
          <label class="field checkbox">
            <input type="checkbox" formControlName="isDefault" />
            <span>Default branch</span>
          </label>
          <div class="form-actions span-2">
            <button type="button" class="btn btn--ghost" (click)="showForm.set(false)">Cancel</button>
            <button type="submit" class="btn btn--primary" [disabled]="saving()">
              {{ saving() ? 'Saving…' : 'Save' }}
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
              <th>Name</th>
              <th>Default</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (b of branches(); track b.id) {
              <tr>
                <td>{{ b.code }}</td>
                <td>{{ b.name }}</td>
                <td>{{ b.isDefault ? 'Yes' : '—' }}</td>
                <td class="actions">
                  <button type="button" class="btn btn--ghost btn--sm" (click)="openEdit(b)">Edit</button>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="4" class="empty">{{ loading() ? 'Loading…' : 'No branches yet.' }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </section>
  `,
})
export class AdminBranchesComponent implements OnInit {
  readonly branches = signal<AdminBranch[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly showForm = signal(false);
  readonly editingId = signal<string | null>(null);
  readonly form;

  constructor(
    private readonly api: AdminApiService,
    private readonly fb: FormBuilder,
  ) {
    this.form = this.fb.nonNullable.group({
      code: ['', Validators.required],
      name: ['', Validators.required],
      isDefault: [false],
    });
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getBranches().subscribe({
      next: (rows) => {
        this.branches.set(rows);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load branches.');
      },
    });
  }

  openCreate(): void {
    this.editingId.set(null);
    this.form.reset({ code: '', name: '', isDefault: false });
    this.showForm.set(true);
  }

  openEdit(b: AdminBranch): void {
    this.editingId.set(b.id);
    this.form.reset({ code: b.code, name: b.name, isDefault: b.isDefault });
    this.showForm.set(true);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    const body = this.form.getRawValue();
    const id = this.editingId();
    const req = id ? this.api.updateBranch(id, body) : this.api.createBranch(body);
    req.subscribe({
      next: () => {
        this.saving.set(false);
        this.showForm.set(false);
        this.error.set('');
        this.load();
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Failed to save branch.');
      },
    });
  }
}
