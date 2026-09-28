import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import {
  AdminApiService,
  AdminPermission,
  AdminRole,
} from '../../../core/services/admin-api.service';

@Component({
  selector: 'app-admin-roles',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent],
  template: `
    <app-page-header title="Roles & permissions" subtitle="Define roles and assign permission codes.">
      <button type="button" class="btn btn--primary" (click)="showCreate.set(true)">+ Role</button>
    </app-page-header>

    @if (error()) {
      <div class="alert alert--danger">{{ error() }}</div>
    }
    @if (success()) {
      <div class="alert alert--success">{{ success() }}</div>
    }

    @if (showCreate()) {
      <section class="panel form-panel">
        <h2>New role</h2>
        <form class="form-grid" [formGroup]="createForm" (ngSubmit)="createRole()">
          <label class="field"><span>Code</span><input formControlName="code" /></label>
          <label class="field"><span>Name</span><input formControlName="name" /></label>
          <div class="form-actions span-2">
            <button type="button" class="btn btn--ghost" (click)="showCreate.set(false)">Cancel</button>
            <button type="submit" class="btn btn--primary" [disabled]="saving()">
              {{ saving() ? 'Saving…' : 'Create' }}
            </button>
          </div>
        </form>
      </section>
    }

    <div style="display: grid; gap: 1.25rem; grid-template-columns: minmax(0, 1fr) minmax(0, 1.4fr)">
      <section class="panel">
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Code</th>
                <th>Name</th>
              </tr>
            </thead>
            <tbody>
              @for (r of roles(); track r.id) {
                <tr
                  (click)="selectRole(r)"
                  [style.background]="selectedRole()?.id === r.id ? 'rgba(20,184,166,0.08)' : null"
                  style="cursor: pointer"
                >
                  <td>{{ r.code }}</td>
                  <td>{{ r.name }}</td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="2" class="empty">{{ loading() ? 'Loading…' : 'No roles.' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </section>

      <section class="panel form-panel">
        @if (selectedRole(); as role) {
          <h2>Permissions — {{ role.name }}</h2>
          <div style="display: grid; gap: 0.45rem; padding: 0 1.15rem; max-height: 28rem; overflow: auto">
            @for (p of permissions(); track p.id) {
              <label class="field checkbox">
                <input
                  type="checkbox"
                  [checked]="selectedCodes().has(p.code)"
                  (change)="togglePerm(p.code, $event)"
                />
                <span
                  >{{ p.name }} <span class="muted">({{ p.code }}{{ p.module ? ' · ' + p.module : '' }})</span></span
                >
              </label>
            } @empty {
              <p class="muted">No permissions loaded.</p>
            }
          </div>
          <div class="form-actions" style="padding: 0 1.15rem">
            <button type="button" class="btn btn--primary" [disabled]="savingPerms()" (click)="savePermissions()">
              {{ savingPerms() ? 'Saving…' : 'Save permissions' }}
            </button>
          </div>
        } @else {
          <p class="muted" style="padding: 1rem 1.15rem">Select a role to edit permissions.</p>
        }
      </section>
    </div>
  `,
})
export class AdminRolesComponent implements OnInit {
  readonly roles = signal<AdminRole[]>([]);
  readonly permissions = signal<AdminPermission[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly savingPerms = signal(false);
  readonly error = signal('');
  readonly success = signal('');
  readonly showCreate = signal(false);
  readonly selectedRole = signal<AdminRole | null>(null);
  readonly selectedCodes = signal<Set<string>>(new Set());
  readonly createForm;

  constructor(
    private readonly api: AdminApiService,
    private readonly fb: FormBuilder,
  ) {
    this.createForm = this.fb.nonNullable.group({
      code: ['', Validators.required],
      name: ['', Validators.required],
    });
  }

  ngOnInit(): void {
    this.loadRoles();
    this.api.getPermissions().subscribe({
      next: (rows) => this.permissions.set(rows),
      error: () => this.error.set('Could not load permissions.'),
    });
  }

  loadRoles(): void {
    this.loading.set(true);
    this.api.getRoles().subscribe({
      next: (rows) => {
        this.roles.set(rows);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load roles.');
      },
    });
  }

  createRole(): void {
    if (this.createForm.invalid) return;
    this.saving.set(true);
    this.api.createRole(this.createForm.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.showCreate.set(false);
        this.createForm.reset({ code: '', name: '' });
        this.success.set('Role created.');
        this.loadRoles();
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Failed to create role.');
      },
    });
  }

  selectRole(r: AdminRole): void {
    this.selectedRole.set(r);
    this.success.set('');
    this.api.getRolePermissions(r.id).subscribe({
      next: (rp) => this.selectedCodes.set(new Set(rp.permissionCodes ?? [])),
      error: () => {
        this.selectedCodes.set(new Set());
        this.error.set('Could not load role permissions.');
      },
    });
  }

  togglePerm(code: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selectedCodes.update((set) => {
      const next = new Set(set);
      if (checked) next.add(code);
      else next.delete(code);
      return next;
    });
  }

  savePermissions(): void {
    const role = this.selectedRole();
    if (!role) return;
    this.savingPerms.set(true);
    this.api.setRolePermissions(role.id, [...this.selectedCodes()]).subscribe({
      next: () => {
        this.savingPerms.set(false);
        this.error.set('');
        this.success.set(`Permissions saved for ${role.name}.`);
      },
      error: (e) => {
        this.savingPerms.set(false);
        this.error.set(e?.error?.message || 'Failed to save permissions.');
      },
    });
  }
}
