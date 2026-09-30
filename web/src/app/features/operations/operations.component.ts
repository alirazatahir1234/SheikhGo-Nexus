import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import {
  Driver,
  GatePass,
  OperationsApiService,
  Vehicle,
  VehicleType,
} from '../../core/services/operations-api.service';
import { statusLabel } from '../../shared/documents/document-status-stepper.component';
import { bindTabFromRoute, navigateModuleTab } from '../../core/nav/route-tabs';

type OpsTab = 'vehicles' | 'drivers' | 'gatepasses';

const OPS_TAB_ROUTE: Record<OpsTab, string> = {
  vehicles: 'vehicles',
  drivers: 'drivers',
  gatepasses: 'gate-passes',
};

@Component({
  selector: 'app-operations',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, DatePipe, RouterLink, RouterLinkActive],
  templateUrl: './operations.component.html',
  styleUrl: './operations.component.scss',
})
export class OperationsComponent implements OnInit {
  private readonly router = inject(Router);
  readonly tab = signal<OpsTab>('vehicles');
  readonly types = signal<VehicleType[]>([]);
  readonly vehicles = signal<Vehicle[]>([]);
  readonly drivers = signal<Driver[]>([]);
  readonly gatePasses = signal<GatePass[]>([]);
  readonly error = signal('');
  readonly saving = signal(false);
  readonly showForm = signal(false);
  readonly statusLabel = statusLabel;
  readonly today = new Date().toISOString().slice(0, 10);

  readonly vehicleForm;
  readonly driverForm;
  readonly gateForm;

  constructor(
    private readonly api: OperationsApiService,
    private readonly fb: FormBuilder,
  ) {
    this.vehicleForm = this.fb.nonNullable.group({
      code: ['', Validators.required],
      vehicleTypeId: ['', Validators.required],
      make: [''],
      model: [''],
      isActive: [true],
    });
    this.driverForm = this.fb.nonNullable.group({
      code: ['', Validators.required],
      name: ['', Validators.required],
      licenseNo: [''],
      isActive: [true],
    });
    this.gateForm = this.fb.nonNullable.group({
      direction: ['Outward', Validators.required],
      passDate: [this.today, Validators.required],
      vehicleId: [''],
      remarks: [''],
    });
  }

  ngOnInit(): void {
    bindTabFromRoute<OpsTab>((tab) => {
      this.tab.set(tab);
      this.showForm.set(false);
      this.error.set('');
    });
    this.reload();
  }

  setTab(t: OpsTab): void {
    navigateModuleTab(this.router, 'operations', OPS_TAB_ROUTE[t]);
  }

  reload(): void {
    this.api.getVehicleTypes().subscribe({ next: (r) => this.types.set(r) });
    this.api.getVehicles().subscribe({
      next: (r) => this.vehicles.set(r),
      error: () => this.error.set('Could not load vehicles'),
    });
    this.api.getDrivers().subscribe({ next: (r) => this.drivers.set(r) });
    this.api.getGatePasses().subscribe({ next: (r) => this.gatePasses.set(r) });
  }

  saveVehicle(): void {
    if (this.vehicleForm.invalid) return;
    this.saving.set(true);
    const raw = this.vehicleForm.getRawValue();
    this.api
      .createVehicle({
        id: '00000000-0000-0000-0000-000000000000',
        ...raw,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.showForm.set(false);
          this.reload();
        },
        error: (e) => {
          this.saving.set(false);
          this.error.set(e?.error?.message || 'Failed');
        },
      });
  }

  saveDriver(): void {
    if (this.driverForm.invalid) return;
    this.saving.set(true);
    const raw = this.driverForm.getRawValue();
    this.api
      .createDriver({
        id: '00000000-0000-0000-0000-000000000000',
        ...raw,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.showForm.set(false);
          this.reload();
        },
        error: (e) => {
          this.saving.set(false);
          this.error.set(e?.error?.message || 'Failed');
        },
      });
  }

  saveGate(): void {
    if (this.gateForm.invalid) return;
    this.saving.set(true);
    const raw = this.gateForm.getRawValue();
    this.api
      .createGatePass({
        id: '00000000-0000-0000-0000-000000000000',
        gatePassNo: '',
        direction: raw.direction,
        passDate: raw.passDate,
        vehicleId: raw.vehicleId || undefined,
        status: 'Draft',
        remarks: raw.remarks,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.showForm.set(false);
          this.reload();
        },
        error: (e) => {
          this.saving.set(false);
          this.error.set(e?.error?.message || 'Failed');
        },
      });
  }

  transition(id: string, action: string): void {
    this.api.transitionGatePass(id, action).subscribe({
      next: () => this.reload(),
      error: (e) => this.error.set(e?.error?.message || 'Failed'),
    });
  }
}
