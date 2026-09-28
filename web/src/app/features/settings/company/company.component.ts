import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CompanyService } from '../../../core/services/company.service';
import { Company } from '../../../core/models/erp.models';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-company-settings',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent],
  templateUrl: './company.component.html',
})
export class CompanySettingsComponent implements OnInit {
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly success = signal('');
  readonly form;

  constructor(
    private readonly api: CompanyService,
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
      industry: [1],
      defaultSaleTaxPercent: [0],
      defaultPurchaseTaxPercent: [0],
      syncToFbr: [false],
      taxModule: [false],
    });
  }

  ngOnInit(): void {
    this.api.get().subscribe({
      next: (company) => {
        this.form.patchValue(company);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load company profile. You can still edit and save when API is up.');
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
    this.api.update(this.form.getRawValue() as Partial<Company>).subscribe({
      next: (saved) => {
        this.form.patchValue(saved);
        this.saving.set(false);
        this.success.set('Company profile saved.');
        this.error.set('');
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Failed to save company profile.');
      },
    });
  }
}
