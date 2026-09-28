import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { AdminApiService } from '../../../core/services/admin-api.service';

@Component({
  selector: 'app-admin-companies',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent],
  template: `
    <app-page-header title="Company" subtitle="Tenant company profile and tax defaults." />

    @if (error()) {
      <div class="alert alert--danger">{{ error() }}</div>
    }
    @if (success()) {
      <div class="alert alert--success">{{ success() }}</div>
    }

    <section class="panel form-panel">
      @if (loading()) {
        <p class="muted" style="padding: 1rem 1.15rem">Loading company…</p>
      } @else {
        <form class="form-grid" [formGroup]="form" (ngSubmit)="save()">
          <label class="field"><span>Name</span><input formControlName="name" /></label>
          <label class="field"><span>Short name</span><input formControlName="shortName" /></label>
          <label class="field span-2"><span>Address</span><input formControlName="address" /></label>
          <label class="field"><span>City</span><input formControlName="city" /></label>
          <label class="field"><span>Country</span><input formControlName="country" /></label>
          <label class="field"><span>Phone</span><input formControlName="phone" /></label>
          <label class="field"><span>Email</span><input type="email" formControlName="email" /></label>
          <label class="field"><span>Currency</span><input formControlName="currency" /></label>
          <label class="field"><span>NTN</span><input formControlName="ntn" /></label>
          <label class="field"><span>Industry</span><input formControlName="industry" /></label>
          <label class="field"
            ><span>Default sale tax %</span
            ><input type="number" step="0.01" formControlName="defaultSaleTaxPercent"
          /></label>
          <label class="field"
            ><span>Default purchase tax %</span
            ><input type="number" step="0.01" formControlName="defaultPurchaseTaxPercent"
          /></label>
          <div class="form-actions span-2">
            <button type="submit" class="btn btn--primary" [disabled]="saving()">
              {{ saving() ? 'Saving…' : 'Save company' }}
            </button>
          </div>
        </form>
      }
    </section>
  `,
})
export class AdminCompaniesComponent implements OnInit {
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly success = signal('');
  readonly form;

  constructor(
    private readonly api: AdminApiService,
    private readonly fb: FormBuilder,
  ) {
    this.form = this.fb.nonNullable.group({
      id: [''],
      name: ['', Validators.required],
      shortName: ['', Validators.required],
      address: [''],
      city: [''],
      country: ['Pakistan'],
      phone: [''],
      email: [''],
      currency: ['PKR', Validators.required],
      ntn: [''],
      industry: [0 as number | string],
      defaultSaleTaxPercent: [0],
      defaultPurchaseTaxPercent: [0],
    });
  }

  ngOnInit(): void {
    this.api.getCompany().subscribe({
      next: (c) => {
        this.form.patchValue(c);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load company.');
      },
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.success.set('');
    this.api.updateCompany(this.form.getRawValue()).subscribe({
      next: (saved) => {
        this.form.patchValue(saved);
        this.saving.set(false);
        this.error.set('');
        this.success.set('Company saved.');
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Failed to save company.');
      },
    });
  }
}
