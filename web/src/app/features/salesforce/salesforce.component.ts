import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ExtendedModulesService } from '../../core/services/extended-modules.service';

@Component({
  selector: 'app-salesforce',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, CurrencyPipe],
  templateUrl: './salesforce.component.html',
  styleUrl: './salesforce.component.scss',
})
export class SalesforceComponent implements OnInit {
  readonly areas = signal<any[]>([]);
  readonly salesmen = signal<any[]>([]);
  readonly targets = signal<any[]>([]);
  readonly tab = signal<'salesmen' | 'areas' | 'targets'>('salesmen');
  readonly show = signal<'area' | 'salesman' | 'target' | null>(null);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly areaForm;
  readonly salesmanForm;
  readonly targetForm;

  constructor(private readonly api: ExtendedModulesService, private readonly fb: FormBuilder) {
    this.areaForm = this.fb.nonNullable.group({ code: ['', Validators.required], name: ['', Validators.required], isActive: [true] });
    this.salesmanForm = this.fb.nonNullable.group({
      code: ['', Validators.required], name: ['', Validators.required], salesAreaId: [''], phone: [''], isActive: [true],
    });
    const now = new Date();
    this.targetForm = this.fb.nonNullable.group({
      salesmanId: [''], salesAreaId: [''], year: [now.getFullYear()], month: [now.getMonth() + 1],
      targetAmount: [0], achievedAmount: [0], remarks: [''],
    });
  }

  ngOnInit(): void { this.reload(); }

  reload(): void {
    this.api.getAreas().subscribe({ next: (a) => this.areas.set(a) });
    this.api.getSalesmen().subscribe({ next: (s) => this.salesmen.set(s), error: () => this.error.set('Load failed') });
    this.api.getTargets().subscribe({ next: (t) => this.targets.set(t) });
  }

  saveArea(): void {
    if (this.areaForm.invalid) return;
    this.saving.set(true);
    this.api.createArea(this.areaForm.getRawValue()).subscribe({
      next: () => { this.saving.set(false); this.show.set(null); this.reload(); },
      error: () => { this.saving.set(false); },
    });
  }

  saveSalesman(): void {
    if (this.salesmanForm.invalid) return;
    this.saving.set(true);
    const raw = this.salesmanForm.getRawValue();
    this.api.createSalesman({ ...raw, salesAreaId: raw.salesAreaId || null }).subscribe({
      next: () => { this.saving.set(false); this.show.set(null); this.reload(); },
      error: () => { this.saving.set(false); },
    });
  }

  saveTarget(): void {
    this.saving.set(true);
    const raw = this.targetForm.getRawValue();
    this.api.createTarget({
      ...raw,
      salesmanId: raw.salesmanId || null,
      salesAreaId: raw.salesAreaId || null,
    }).subscribe({
      next: () => { this.saving.set(false); this.show.set(null); this.reload(); },
      error: () => { this.saving.set(false); },
    });
  }
}
