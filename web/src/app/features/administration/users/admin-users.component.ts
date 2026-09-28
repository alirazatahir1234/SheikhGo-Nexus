import { Component, OnInit, signal } from '@angular/core';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { UsersService } from '../../../core/services/users.service';
import { AdminApiService, AdminRole } from '../../../core/services/admin-api.service';
import { User } from '../../../core/models/auth.model';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [PageHeaderComponent],
  template: `
    <app-page-header title="Users" subtitle="Assign Nexus roles to tenant users." />

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
              <th>Name</th>
              <th>Email</th>
              <th>Current role</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (u of users(); track u.id) {
              <tr>
                <td>{{ u.fullName }}</td>
                <td>{{ u.email }}</td>
                <td>{{ u.role }}</td>
                <td class="actions">
                  <button type="button" class="btn btn--ghost btn--sm" (click)="openAssign(u)">
                    Assign roles
                  </button>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="4" class="empty">{{ loading() ? 'Loading…' : 'No users.' }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </section>

    @if (selectedUser(); as user) {
      <section class="panel form-panel" style="margin-top: 1.25rem">
        <h2>Roles for {{ user.fullName }}</h2>
        <div style="display: grid; gap: 0.5rem; padding: 0 1.15rem">
          @for (r of roles(); track r.id) {
            <label class="field checkbox">
              <input
                type="checkbox"
                [checked]="selectedRoleIds().has(r.id)"
                (change)="toggleRole(r.id, $event)"
              />
              <span>{{ r.name }} <span class="muted">({{ r.code }})</span></span>
            </label>
          } @empty {
            <p class="muted">No roles available.</p>
          }
        </div>
        <div class="form-actions" style="padding: 0 1.15rem">
          <button type="button" class="btn btn--ghost" (click)="selectedUser.set(null)">Cancel</button>
          <button type="button" class="btn btn--primary" [disabled]="saving()" (click)="saveRoles()">
            {{ saving() ? 'Saving…' : 'Save roles' }}
          </button>
        </div>
      </section>
    }
  `,
})
export class AdminUsersComponent implements OnInit {
  readonly users = signal<User[]>([]);
  readonly roles = signal<AdminRole[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly success = signal('');
  readonly selectedUser = signal<User | null>(null);
  readonly selectedRoleIds = signal<Set<string>>(new Set());

  constructor(
    private readonly usersApi: UsersService,
    private readonly adminApi: AdminApiService,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.usersApi.getAll().subscribe({
      next: (rows) => {
        this.users.set(rows);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load users.');
      },
    });
    this.adminApi.getRoles().subscribe({
      next: (rows) => this.roles.set(rows),
      error: () => this.error.set('Could not load roles.'),
    });
  }

  openAssign(u: User): void {
    this.selectedUser.set(u);
    this.success.set('');
    const match = this.roles().find((r) => r.code === u.role || r.name === u.role);
    this.selectedRoleIds.set(match ? new Set([match.id]) : new Set());
  }

  toggleRole(roleId: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selectedRoleIds.update((set) => {
      const next = new Set(set);
      if (checked) next.add(roleId);
      else next.delete(roleId);
      return next;
    });
  }

  saveRoles(): void {
    const user = this.selectedUser();
    if (!user) return;
    this.saving.set(true);
    this.adminApi.setUserRoles(user.id, [...this.selectedRoleIds()]).subscribe({
      next: () => {
        this.saving.set(false);
        this.selectedUser.set(null);
        this.error.set('');
        this.success.set(`Roles updated for ${user.fullName}.`);
        this.load();
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Failed to assign roles.');
      },
    });
  }
}
