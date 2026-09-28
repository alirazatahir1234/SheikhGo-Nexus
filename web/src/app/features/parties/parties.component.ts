import { Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { PartiesService } from '../../core/services/parties.service';
import { Party, PartyType } from '../../core/models/erp.models';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SearchBoxComponent } from '../../shared/components/search-box/search-box.component';

@Component({
  selector: 'app-parties',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe, PageHeaderComponent, SearchBoxComponent],
  templateUrl: './parties.component.html',
})
export class PartiesComponent implements OnInit {
  readonly parties = signal<Party[]>([]);
  readonly search = signal('');
  readonly showForm = signal(false);
  readonly editingId = signal<string | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly saving = signal(false);
  readonly partyTypes: PartyType[] = ['Customer', 'Supplier', 'Both'];

  readonly filtered = computed(() => {
    const q = this.search().toLowerCase().trim();
    return this.parties().filter(
      (p) =>
        !q ||
        p.code.toLowerCase().includes(q) ||
        p.name.toLowerCase().includes(q) ||
        p.city.toLowerCase().includes(q) ||
        p.partyType.toLowerCase().includes(q),
    );
  });

  readonly form;

  constructor(
    private readonly api: PartiesService,
    private readonly fb: FormBuilder,
  ) {
    this.form = this.fb.nonNullable.group({
      code: ['', Validators.required],
      name: ['', Validators.required],
      partyType: ['Customer' as PartyType, Validators.required],
      phone: [''],
      email: ['', Validators.email],
      city: [''],
      address: [''],
      creditLimit: [0, [Validators.min(0)]],
      openingBalance: [0],
    });
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getAll().subscribe({
      next: (data) => {
        this.parties.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.parties.set([]);
        this.loading.set(false);
        this.error.set('Could not load parties.');
      },
    });
  }

  openCreate(): void {
    this.editingId.set(null);
    this.form.reset({
      code: '',
      name: '',
      partyType: 'Customer',
      phone: '',
      email: '',
      city: '',
      address: '',
      creditLimit: 0,
      openingBalance: 0,
    });
    this.showForm.set(true);
  }

  openEdit(party: Party): void {
    this.editingId.set(party.id);
    this.form.patchValue(party);
    this.showForm.set(true);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    const payload = this.form.getRawValue();
    const id = this.editingId();
    const req = id ? this.api.update(id, payload) : this.api.create(payload);
    req.subscribe({
      next: (saved) => {
        if (id) {
          this.parties.update((list) => list.map((p) => (p.id === id ? saved : p)));
        } else {
          this.parties.update((list) => [...list, saved]);
        }
        this.saving.set(false);
        this.showForm.set(false);
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Failed to save party.');
      },
    });
  }

  remove(party: Party): void {
    if (!confirm(`Delete ${party.name}?`)) return;
    this.api.delete(party.id).subscribe({
      next: () => this.parties.update((list) => list.filter((p) => p.id !== party.id)),
      error: () => this.error.set('Failed to delete party.'),
    });
  }
}
