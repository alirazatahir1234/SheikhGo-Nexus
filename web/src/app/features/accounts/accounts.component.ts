import { Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { AccountsService } from '../../core/services/accounts.service';
import { Account, AccountType } from '../../core/models/erp.models';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SearchBoxComponent } from '../../shared/components/search-box/search-box.component';

@Component({
  selector: 'app-accounts',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe, PageHeaderComponent, SearchBoxComponent],
  templateUrl: './accounts.component.html',
})
export class AccountsComponent implements OnInit {
  readonly accounts = signal<Account[]>([]);
  readonly search = signal('');
  readonly showForm = signal(false);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly saving = signal(false);

  readonly accountTypes: AccountType[] = ['Asset', 'Liability', 'Equity', 'Revenue', 'Expense'];

  readonly filtered = computed(() => {
    const q = this.search().toLowerCase().trim();
    const list = this.accounts();
    if (!q) return list;
    return list.filter(
      (a) =>
        a.code.toLowerCase().includes(q) ||
        a.name.toLowerCase().includes(q) ||
        a.accountType.toLowerCase().includes(q),
    );
  });

  readonly form;

  constructor(
    private readonly api: AccountsService,
    private readonly fb: FormBuilder,
  ) {
    this.form = this.fb.nonNullable.group({
      code: ['', Validators.required],
      name: ['', Validators.required],
      accountType: ['Asset' as AccountType, Validators.required],
      parentId: [null as string | null],
      isPostable: [true],
      balance: [0],
    });
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getAll().subscribe({
      next: (data) => {
        this.accounts.set(data);
        this.loading.set(false);
        this.error.set('');
      },
      error: () => {
        this.accounts.set([]);
        this.loading.set(false);
        this.error.set('Could not load accounts. API may be offline.');
      },
    });
  }

  openForm(): void {
    this.form.reset({
      code: '',
      name: '',
      accountType: 'Asset',
      parentId: null,
      isPostable: true,
      balance: 0,
    });
    this.showForm.set(true);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.api.create(this.form.getRawValue()).subscribe({
      next: (created) => {
        this.accounts.update((list) => [...list, created]);
        this.saving.set(false);
        this.showForm.set(false);
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Failed to create account.');
      },
    });
  }
}
