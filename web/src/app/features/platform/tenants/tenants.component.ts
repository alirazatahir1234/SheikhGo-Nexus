import { Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { AuthService } from '../../../core/services/auth.service';
import { ProvisionTenantRequest, TenantListItem, TenantsService } from '../../../core/services/tenants.service';

const INDUSTRIES = [
  { value: 1, label: 'Feed Mill' },
  { value: 2, label: 'Poultry Farm' },
  { value: 3, label: 'Flour Mill' },
  { value: 4, label: 'Distribution' },
  { value: 5, label: 'Packing' },
  { value: 6, label: 'Trading' },
  { value: 99, label: 'Other' },
];

@Component({
  selector: 'app-tenants',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, DatePipe],
  templateUrl: './tenants.component.html',
  styleUrl: './tenants.component.scss',
})
export class TenantsComponent implements OnInit {
  readonly tenants = signal<TenantListItem[]>([]);
  readonly catalog = signal<string[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly success = signal('');
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly industries = INDUSTRIES;

  readonly isPlatformAdmin = computed(() => !!this.auth.user()?.isPlatformAdmin || this.auth.user()?.role === 'PlatformAdmin');

  readonly form;

  constructor(
    private readonly api: TenantsService,
    private readonly auth: AuthService,
    private readonly fb: FormBuilder,
  ) {
    this.form = this.fb.nonNullable.group({
      name: ['', Validators.required],
      code: ['', Validators.required],
      industry: [1, Validators.required],
      adminFullName: ['', Validators.required],
      adminEmail: ['', Validators.required],
      adminPassword: ['Admin@123', [Validators.required, Validators.minLength(6)]],
      city: [''],
      country: ['Pakistan'],
      currency: ['PKR'],
      phone: [''],
      ntn: [''],
    });
  }

  ngOnInit(): void {
    if (!this.isPlatformAdmin()) {
      this.loading.set(false);
      this.error.set('Platform admin access required.');
      return;
    }
    this.api.catalog().subscribe({ next: (c: string[]) => this.catalog.set(c) });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getAll().subscribe({
      next: (rows: TenantListItem[]) => {
        this.tenants.set(rows);
        this.loading.set(false);
        this.error.set('');
      },
      error: (err: any) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Could not load tenants.');
      },
    });
  }

  industryLabel(value: number | string): string {
    const n = typeof value === 'string' ? Number(value) : value;
    return INDUSTRIES.find((i) => i.value === n)?.label ?? String(value);
  }

  openForm(): void {
    this.success.set('');
    this.showForm.set(true);
  }

  provision(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    const v = this.form.getRawValue();
    const body: ProvisionTenantRequest = {
      name: v.name,
      code: v.code,
      industry: Number(v.industry),
      adminFullName: v.adminFullName,
      adminEmail: v.adminEmail,
      adminPassword: v.adminPassword,
      city: v.city || undefined,
      country: v.country,
      currency: v.currency,
      phone: v.phone || undefined,
      ntn: v.ntn || undefined,
      modules: this.catalog().length ? this.catalog() : undefined,
    };
    this.api.provision(body).subscribe({
      next: (res: { message?: string }) => {
        this.saving.set(false);
        this.showForm.set(false);
        this.success.set(res.message || 'Tenant provisioned.');
        this.form.reset({
          name: '',
          code: '',
          industry: 1,
          adminFullName: '',
          adminEmail: '',
          adminPassword: 'Admin@123',
          city: '',
          country: 'Pakistan',
          currency: 'PKR',
          phone: '',
          ntn: '',
        });
        this.load();
      },
      error: (err: any) => {
        this.saving.set(false);
        this.error.set(err?.error?.message || 'Provisioning failed.');
      },
    });
  }

  toggleActive(row: TenantListItem): void {
    this.api.setActive(row.id, !row.isActive).subscribe({
      next: () => this.load(),
      error: (err: any) => this.error.set(err?.error?.message || 'Update failed.'),
    });
  }
}
