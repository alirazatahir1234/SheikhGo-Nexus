import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { AdminApiService, DocumentTypeRow } from '../../../core/services/admin-api.service';

@Component({
  selector: 'app-admin-document-types',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent],
  template: `
    <app-page-header title="Document types" subtitle="Voucher prefixes and workflow flags." />

    @if (error()) {
      <div class="alert alert--danger">{{ error() }}</div>
    }
    @if (success()) {
      <div class="alert alert--success">{{ success() }}</div>
    }

    <section class="panel">
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Code</th>
              <th>Name</th>
              <th>Prefix</th>
              <th>Verify</th>
              <th>Approve</th>
              <th>Inventory</th>
              <th>GL</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (d of types(); track d.id) {
              <tr>
                <td>{{ d.code }}</td>
                <td>{{ d.name }}</td>
                <td>{{ d.prefix || '—' }}</td>
                <td>{{ d.requiresVerification ? 'Yes' : '—' }}</td>
                <td>{{ d.requiresApproval ? 'Yes' : '—' }}</td>
                <td>{{ d.affectsInventory ? 'Yes' : '—' }}</td>
                <td>{{ d.affectsGl ? 'Yes' : '—' }}</td>
                <td class="actions">
                  <button type="button" class="btn btn--ghost btn--sm" (click)="openEdit(d)">Edit</button>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="8" class="empty">{{ loading() ? 'Loading…' : 'No document types.' }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </section>

    @if (editing(); as doc) {
      <section class="panel form-panel" style="margin-top: 1.25rem">
        <h2>Edit {{ doc.code }}</h2>
        <form class="form-grid" [formGroup]="form" (ngSubmit)="save()">
          <label class="field"><span>Name</span><input formControlName="name" /></label>
          <label class="field"><span>Prefix</span><input formControlName="prefix" /></label>
          <label class="field checkbox">
            <input type="checkbox" formControlName="requiresVerification" />
            <span>Requires verification</span>
          </label>
          <label class="field checkbox">
            <input type="checkbox" formControlName="requiresApproval" />
            <span>Requires approval</span>
          </label>
          <label class="field checkbox">
            <input type="checkbox" formControlName="affectsInventory" />
            <span>Affects inventory</span>
          </label>
          <label class="field checkbox">
            <input type="checkbox" formControlName="affectsGl" />
            <span>Affects GL</span>
          </label>
          <div class="form-actions span-2">
            <button type="button" class="btn btn--ghost" (click)="editing.set(null)">Cancel</button>
            <button type="submit" class="btn btn--primary" [disabled]="saving()">
              {{ saving() ? 'Saving…' : 'Save' }}
            </button>
          </div>
        </form>
      </section>
    }
  `,
})
export class AdminDocumentTypesComponent implements OnInit {
  readonly types = signal<DocumentTypeRow[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly success = signal('');
  readonly editing = signal<DocumentTypeRow | null>(null);
  readonly form;

  constructor(
    private readonly api: AdminApiService,
    private readonly fb: FormBuilder,
  ) {
    this.form = this.fb.nonNullable.group({
      id: [''],
      code: ['', Validators.required],
      name: ['', Validators.required],
      prefix: [''],
      requiresVerification: [false],
      requiresApproval: [false],
      affectsInventory: [false],
      affectsGl: [false],
    });
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getDocumentTypes().subscribe({
      next: (rows) => {
        this.types.set(rows);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load document types.');
      },
    });
  }

  openEdit(d: DocumentTypeRow): void {
    this.editing.set(d);
    this.success.set('');
    this.form.reset({
      id: d.id,
      code: d.code,
      name: d.name,
      prefix: d.prefix ?? '',
      requiresVerification: d.requiresVerification,
      requiresApproval: d.requiresApproval,
      affectsInventory: d.affectsInventory,
      affectsGl: d.affectsGl,
    });
  }

  save(): void {
    if (this.form.invalid) return;
    const body = this.form.getRawValue() as DocumentTypeRow;
    this.saving.set(true);
    this.api.updateDocumentType(body.id, body).subscribe({
      next: () => {
        this.saving.set(false);
        this.editing.set(null);
        this.error.set('');
        this.success.set('Document type updated.');
        this.load();
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Failed to update document type.');
      },
    });
  }
}
