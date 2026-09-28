import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { UsersService } from '../../core/services/users.service';
import { User } from '../../core/models/auth.model';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss',
})
export class UsersComponent implements OnInit {
  readonly users = signal<User[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly roles = ['Admin', 'Accountant', 'Sales', 'Purchase', 'HR', 'Store', 'User', 'PlatformAdmin'];
  readonly form;

  constructor(
    private readonly api: UsersService,
    private readonly fb: FormBuilder,
  ) {
    this.form = this.fb.nonNullable.group({
      email: ['', Validators.required],
      fullName: ['', Validators.required],
      password: ['Admin@123', [Validators.required, Validators.minLength(6)]],
      role: ['User', Validators.required],
    });
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getAll().subscribe({
      next: (rows) => {
        this.users.set(rows);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load users');
      },
    });
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.api.create(this.form.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.showForm.set(false);
        this.load();
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Create user failed');
      },
    });
  }
}
