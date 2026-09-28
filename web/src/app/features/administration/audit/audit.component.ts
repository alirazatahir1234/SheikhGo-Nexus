import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { AdminApiService, AuditLogRow } from '../../../core/services/admin-api.service';

@Component({
  selector: 'app-admin-audit',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, DatePipe],
  template: `
    <app-page-header title="Audit log" subtitle="Filter entity change history.">
      <button type="button" class="btn btn--primary" (click)="load()">Refresh</button>
    </app-page-header>

    @if (error()) {
      <div class="alert alert--danger">{{ error() }}</div>
    }

    <section class="panel form-panel">
      <form class="form-grid" [formGroup]="filterForm" (ngSubmit)="load()">
        <label class="field"><span>Entity name</span><input formControlName="entityName" placeholder="Product" /></label>
        <label class="field"><span>Entity ID</span><input formControlName="entityId" placeholder="guid" /></label>
        <div class="form-actions span-2">
          <button type="button" class="btn btn--ghost" (click)="clearFilters()">Clear</button>
          <button type="submit" class="btn btn--primary" [disabled]="loading()">
            {{ loading() ? 'Loading…' : 'Filter' }}
          </button>
        </div>
      </form>
    </section>

    <section class="panel">
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>When</th>
              <th>Entity</th>
              <th>ID</th>
              <th>Action</th>
              <th>User</th>
              <th>IP</th>
            </tr>
          </thead>
          <tbody>
            @for (row of rows(); track row.id) {
              <tr>
                <td>{{ row.timestamp | date: 'short' }}</td>
                <td>{{ row.entityName }}</td>
                <td style="font-family: var(--font-mono, monospace); font-size: 0.8rem">{{ row.entityId }}</td>
                <td><span class="badge">{{ row.action }}</span></td>
                <td>{{ row.userId || '—' }}</td>
                <td>{{ row.ipAddress || '—' }}</td>
              </tr>
            } @empty {
              <tr>
                <td colspan="6" class="empty">{{ loading() ? 'Loading…' : 'No audit rows.' }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </section>
  `,
})
export class AdminAuditComponent implements OnInit {
  readonly rows = signal<AuditLogRow[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly filterForm;

  constructor(
    private readonly api: AdminApiService,
    private readonly fb: FormBuilder,
  ) {
    this.filterForm = this.fb.nonNullable.group({
      entityName: [''],
      entityId: [''],
    });
  }

  ngOnInit(): void {
    this.load();
  }

  clearFilters(): void {
    this.filterForm.reset({ entityName: '', entityId: '' });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    const { entityName, entityId } = this.filterForm.getRawValue();
    this.api
      .getAuditLogs({
        entityName: entityName.trim() || undefined,
        entityId: entityId.trim() || undefined,
        take: 100,
      })
      .subscribe({
        next: (data) => {
          this.rows.set(data);
          this.loading.set(false);
          this.error.set('');
        },
        error: () => {
          this.loading.set(false);
          this.error.set('Could not load audit logs.');
        },
      });
  }
}
