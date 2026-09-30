import { Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  FormArray,
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import {
  AuditLogEntry,
  CustomerPayment,
  DeliveryOrder,
  ReceivableBalance,
  SalesInvoiceDoc,
  SalesOrder,
  SalesService,
} from '../../core/services/sales.service';
import { PartiesService } from '../../core/services/parties.service';
import { ProductsService } from '../../core/services/products.service';
import { WarehousesService, Warehouse } from '../../core/services/warehouses.service';
import { Party, Product } from '../../core/models/erp.models';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SearchBoxComponent } from '../../shared/components/search-box/search-box.component';
import { DocumentStatusStepperComponent, statusLabel } from '../../shared/documents/document-status-stepper.component';
import { DocumentActionsComponent, DocumentAction } from '../../shared/documents/document-actions.component';
import { AuditTimelineComponent, AuditTimelineEntry } from '../../shared/documents/audit-timeline.component';
import { WorkflowActorsComponent } from '../../shared/documents/workflow-actors.component';
import { AuthService } from '../../core/services/auth.service';
import { AdminApiService, DocumentTypeRow } from '../../core/services/admin-api.service';
import { CashBankAccount, FinanceApiService } from '../../core/services/finance-api.service';
import { bindTabFromRoute, navigateModuleTab } from '../../core/nav/route-tabs';

type SalesTab = 'customers' | 'orders' | 'deliveries' | 'invoices' | 'payments';

const SALES_TAB_ROUTE: Record<SalesTab, string> = {
  customers: 'customers',
  orders: 'orders',
  deliveries: 'delivery-orders',
  invoices: 'invoices',
  payments: 'payments',
};

@Component({
  selector: 'app-sales',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    CurrencyPipe,
    DatePipe,
    PageHeaderComponent,
    SearchBoxComponent,
    DocumentStatusStepperComponent,
    DocumentActionsComponent,
    AuditTimelineComponent,
    WorkflowActorsComponent,
    RouterLink,
    RouterLinkActive,
  ],
  templateUrl: './sales.component.html',
  styleUrl: './sales.component.scss',
})
export class SalesComponent implements OnInit {
  private readonly router = inject(Router);
  readonly tab = signal<SalesTab>('orders');
  readonly orders = signal<SalesOrder[]>([]);
  readonly deliveries = signal<DeliveryOrder[]>([]);
  readonly invoices = signal<SalesInvoiceDoc[]>([]);
  readonly payments = signal<CustomerPayment[]>([]);
  readonly cashBanks = signal<CashBankAccount[]>([]);
  readonly parties = signal<Party[]>([]);
  readonly products = signal<Product[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly search = signal('');
  readonly showForm = signal(false);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly saving = signal(false);
  readonly transitioning = signal(false);
  readonly receivable = signal<ReceivableBalance | null>(null);

  readonly selectedOrder = signal<SalesOrder | null>(null);
  readonly selectedDelivery = signal<DeliveryOrder | null>(null);
  readonly selectedInvoice = signal<SalesInvoiceDoc | null>(null);
  readonly selectedPayment = signal<CustomerPayment | null>(null);
  readonly auditEntries = signal<AuditTimelineEntry[]>([]);
  readonly documentTypes = signal<DocumentTypeRow[]>([]);

  readonly statusLabel = statusLabel;

  readonly soFlags = computed(() => this.docFlags('SO'));
  readonly doFlags = computed(() => this.docFlags('DO'));
  readonly siFlags = computed(() => this.docFlags('SI'));
  readonly cpFlags = computed(() => this.docFlags('CP'));

  readonly orderActionAllowed = computed(() => this.actionAllowed('Order'));
  readonly deliveryActionAllowed = computed(() => this.actionAllowed('Delivery'));
  readonly invoiceActionAllowed = computed(() => this.actionAllowed('Invoice'));
  readonly paymentActionAllowed = computed(() => this.actionAllowed('Payment'));

  readonly filteredCustomers = computed(() => {
    const q = this.search().toLowerCase().trim();
    return this.parties().filter(
      (p) => !q || p.code.toLowerCase().includes(q) || p.name.toLowerCase().includes(q),
    );
  });

  readonly filteredOrders = computed(() => {
    const q = this.search().toLowerCase().trim();
    return this.orders().filter(
      (o) =>
        !q ||
        o.orderNo.toLowerCase().includes(q) ||
        (o.partyName ?? '').toLowerCase().includes(q),
    );
  });

  readonly filteredDeliveries = computed(() => {
    const q = this.search().toLowerCase().trim();
    return this.deliveries().filter(
      (d) =>
        !q ||
        d.deliveryNo.toLowerCase().includes(q) ||
        (d.partyName ?? '').toLowerCase().includes(q),
    );
  });

  readonly filteredInvoices = computed(() => {
    const q = this.search().toLowerCase().trim();
    return this.invoices().filter(
      (i) =>
        !q ||
        i.invoiceNo.toLowerCase().includes(q) ||
        (i.partyName ?? '').toLowerCase().includes(q),
    );
  });

  readonly filteredPayments = computed(() => {
    const q = this.search().toLowerCase().trim();
    return this.payments().filter(
      (p) =>
        !q ||
        p.paymentNo.toLowerCase().includes(q) ||
        (p.partyName ?? '').toLowerCase().includes(q),
    );
  });

  readonly approvedOrders = computed(() =>
    this.orders().filter((o) => {
      const s = this.statusLabel(o.status);
      return s === 'Approved' || s === 'Posted';
    }),
  );

  readonly postedDeliveries = computed(() =>
    this.deliveries().filter((d) => this.statusLabel(d.status) === 'Posted'),
  );

  readonly postedInvoices = computed(() =>
    this.invoices().filter((i) => this.statusLabel(i.status) === 'Posted'),
  );

  readonly orderForm;
  readonly deliveryForm;
  readonly invoiceForm;
  readonly paymentForm;
  readonly customerForm;

  constructor(
    private readonly api: SalesService,
    private readonly financeApi: FinanceApiService,
    private readonly partiesApi: PartiesService,
    private readonly productsApi: ProductsService,
    private readonly warehousesApi: WarehousesService,
    private readonly adminApi: AdminApiService,
    private readonly auth: AuthService,
    private readonly fb: FormBuilder,
  ) {
    this.orderForm = this.fb.nonNullable.group({
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      partyId: ['', Validators.required],
      warehouseId: [''],
      tax: [0, [Validators.min(0)]],
      remarks: [''],
      lines: this.fb.array([this.createLine()]),
    });
    this.deliveryForm = this.fb.nonNullable.group({
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      partyId: ['', Validators.required],
      warehouseId: ['', Validators.required],
      salesOrderId: ['', Validators.required],
      remarks: [''],
      lines: this.fb.array([this.createLine()]),
    });
    this.invoiceForm = this.fb.nonNullable.group({
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      partyId: ['', Validators.required],
      deliveryOrderId: [''],
      salesOrderId: [''],
      discount: [0, [Validators.min(0)]],
      tax: [0, [Validators.min(0)]],
      remarks: [''],
      lines: this.fb.array([this.createLine()]),
    });
    this.paymentForm = this.fb.nonNullable.group({
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      partyId: ['', Validators.required],
      salesInvoiceId: ['', Validators.required],
      cashBankAccountId: [''],
      amount: [0, [Validators.required, Validators.min(0.01)]],
      remarks: [''],
    });
    this.customerForm = this.fb.nonNullable.group({
      code: ['', Validators.required],
      name: ['', Validators.required],
      phone: [''],
      email: [''],
      city: [''],
      address: [''],
      creditLimit: [0],
    });
  }

  get orderLines(): FormArray {
    return this.orderForm.get('lines') as FormArray;
  }
  get deliveryLines(): FormArray {
    return this.deliveryForm.get('lines') as FormArray;
  }
  get invoiceLines(): FormArray {
    return this.invoiceForm.get('lines') as FormArray;
  }

  ngOnInit(): void {
    bindTabFromRoute<SalesTab>((tab) => {
      this.tab.set(tab);
      this.showForm.set(false);
      this.error.set('');
      this.clearSelection();
    });
    this.loadAll();
    this.adminApi.getDocumentTypes().subscribe({
      next: (types) => this.documentTypes.set(types),
      error: () => this.documentTypes.set([]),
    });
    this.reloadCustomers();
    this.productsApi.getAll().subscribe({
      next: (data) => this.products.set(data),
      error: () => this.products.set([]),
    });
    this.warehousesApi.getAll().subscribe({
      next: (data) => this.warehouses.set(data),
      error: () => this.warehouses.set([]),
    });
  }

  private docFlags(code: string): { requiresVerification: boolean; requiresApproval: boolean } {
    const t = this.documentTypes().find((d) => d.code.toUpperCase() === code);
    if (!t) {
      if (code === 'SO') return { requiresVerification: false, requiresApproval: true };
      if (code === 'DO') return { requiresVerification: true, requiresApproval: false };
      if (code === 'CP') return { requiresVerification: false, requiresApproval: false };
      return { requiresVerification: true, requiresApproval: true };
    }
    return {
      requiresVerification: !!t.requiresVerification,
      requiresApproval: !!t.requiresApproval,
    };
  }

  private actionAllowed(docKind: string): Partial<Record<DocumentAction, boolean>> {
    return {
      submit: this.auth.hasPermission(`Sales.${docKind}.Submit`) || this.auth.hasPermission(`Sales.${docKind}.Create`),
      verify: this.auth.hasPermission(`Sales.${docKind}.Verify`),
      approve: this.auth.hasPermission(`Sales.${docKind}.Approve`),
      post: this.auth.hasPermission(`Sales.${docKind}.Post`),
      cancel: this.auth.hasPermission(`Sales.${docKind}.Edit`),
    };
  }

  setTab(tab: SalesTab): void {
    navigateModuleTab(this.router, 'sales', SALES_TAB_ROUTE[tab]);
  }

  clearSelection(): void {
    this.selectedOrder.set(null);
    this.selectedDelivery.set(null);
    this.selectedInvoice.set(null);
    this.selectedPayment.set(null);
    this.auditEntries.set([]);
    this.receivable.set(null);
  }

  reloadCustomers(): void {
    this.partiesApi.getAll().subscribe({
      next: (data) =>
        this.parties.set(data.filter((p) => p.partyType === 'Customer' || p.partyType === 'Both')),
      error: () => this.parties.set([]),
    });
  }

  loadAll(): void {
    this.loading.set(true);
    this.error.set('');
    let pending = 5;
    const done = () => {
      pending -= 1;
      if (pending <= 0) this.loading.set(false);
    };
    this.api.getOrders().subscribe({
      next: (d) => { this.orders.set(d); done(); },
      error: () => { this.orders.set([]); this.error.set('Could not load sales data.'); done(); },
    });
    this.api.getDeliveries().subscribe({
      next: (d) => { this.deliveries.set(d); done(); },
      error: () => { this.deliveries.set([]); done(); },
    });
    this.api.getInvoices().subscribe({
      next: (d) => { this.invoices.set(d); done(); },
      error: () => { this.invoices.set([]); done(); },
    });
    this.api.getPayments().subscribe({
      next: (d) => { this.payments.set(d); done(); },
      error: () => { this.payments.set([]); done(); },
    });
    this.financeApi.getCashBank().subscribe({
      next: (d) => { this.cashBanks.set(d); done(); },
      error: () => { this.cashBanks.set([]); done(); },
    });
  }

  createLine() {
    return this.fb.nonNullable.group({
      productId: ['', Validators.required],
      qty: [1, [Validators.required, Validators.min(0.01)]],
      rate: [0, [Validators.required, Validators.min(0)]],
      amount: [0],
    });
  }

  linesOf(which: 'orders' | 'deliveries' | 'invoices'): FormArray {
    if (which === 'orders') return this.orderLines;
    if (which === 'deliveries') return this.deliveryLines;
    return this.invoiceLines;
  }

  addLine(which: 'orders' | 'deliveries' | 'invoices'): void {
    this.linesOf(which).push(this.createLine());
  }

  removeLine(which: 'orders' | 'deliveries' | 'invoices', index: number): void {
    const lines = this.linesOf(which);
    if (lines.length > 1) lines.removeAt(index);
  }

  onProductChange(which: 'orders' | 'deliveries' | 'invoices', index: number): void {
    const line = this.linesOf(which).at(index);
    const product = this.products().find((p) => String(p.id) === String(line.value.productId));
    if (product) {
      const rate = Number(product.salePrice || 0);
      line.patchValue({ rate, amount: rate * Number(line.value.qty || 0) });
    }
  }

  recalcLine(which: 'orders' | 'deliveries' | 'invoices', index: number): void {
    const line = this.linesOf(which).at(index);
    line.patchValue(
      { amount: Number(line.value.qty || 0) * Number(line.value.rate || 0) },
      { emitEvent: false },
    );
  }

  subTotal(which: 'orders' | 'deliveries' | 'invoices'): number {
    return this.linesOf(which).controls.reduce((sum, c) => sum + Number(c.value.amount || 0), 0);
  }

  grandTotal(which: 'orders' | 'deliveries' | 'invoices'): number {
    if (which === 'deliveries') return this.subTotal(which);
    if (which === 'orders') return this.subTotal(which) + Number(this.orderForm.value.tax || 0);
    return (
      this.subTotal(which) -
      Number(this.invoiceForm.value.discount || 0) +
      Number(this.invoiceForm.value.tax || 0)
    );
  }

  newButtonLabel(): string {
    switch (this.tab()) {
      case 'customers': return 'New customer';
      case 'orders': return 'New sales order';
      case 'deliveries': return 'New delivery';
      case 'invoices': return 'New invoice';
      case 'payments': return 'New payment';
    }
  }

  openForm(): void {
    this.clearSelection();
    const today = new Date().toISOString().slice(0, 10);
    const defWh = this.warehouses().find((w) => w.isDefault)?.id ?? '';
    if (this.tab() === 'customers') {
      this.customerForm.reset({ code: '', name: '', phone: '', email: '', city: '', address: '', creditLimit: 0 });
    } else if (this.tab() === 'orders') {
      this.orderForm.reset({ date: today, partyId: '', warehouseId: defWh, tax: 0, remarks: '' });
      this.orderLines.clear();
      this.orderLines.push(this.createLine());
    } else if (this.tab() === 'deliveries') {
      this.deliveryForm.reset({ date: today, partyId: '', warehouseId: defWh, salesOrderId: '', remarks: '' });
      this.deliveryLines.clear();
      this.deliveryLines.push(this.createLine());
    } else if (this.tab() === 'invoices') {
      this.invoiceForm.reset({ date: today, partyId: '', deliveryOrderId: '', salesOrderId: '', discount: 0, tax: 0, remarks: '' });
      this.invoiceLines.clear();
      this.invoiceLines.push(this.createLine());
    } else {
      this.paymentForm.reset({
        date: today,
        partyId: '',
        salesInvoiceId: '',
        cashBankAccountId: this.cashBanks().find((c) => c.isDefault)?.id ?? '',
        amount: 0,
        remarks: '',
      });
    }
    this.showForm.set(true);
  }

  onDeliverySoChange(): void {
    const soId = String(this.deliveryForm.value.salesOrderId || '');
    const so = this.orders().find((o) => o.id === soId);
    if (!so) return;
    this.deliveryForm.patchValue({
      partyId: so.partyId,
      warehouseId: so.warehouseId || this.deliveryForm.value.warehouseId,
    });
    this.deliveryLines.clear();
    for (const l of so.lines) {
      this.deliveryLines.push(
        this.fb.nonNullable.group({
          productId: [l.productId, Validators.required],
          qty: [l.qty, [Validators.required, Validators.min(0.01)]],
          rate: [l.rate, [Validators.required, Validators.min(0)]],
          amount: [l.amount],
        }),
      );
    }
    if (!this.deliveryLines.length) this.deliveryLines.push(this.createLine());
  }

  onInvoiceDoChange(): void {
    const doId = String(this.invoiceForm.value.deliveryOrderId || '');
    const delivery = this.deliveries().find((d) => d.id === doId);
    if (!delivery) return;
    this.invoiceForm.patchValue({
      partyId: delivery.partyId,
      salesOrderId: delivery.salesOrderId,
    });
    this.invoiceLines.clear();
    for (const l of delivery.lines) {
      this.invoiceLines.push(
        this.fb.nonNullable.group({
          productId: [l.productId, Validators.required],
          qty: [l.qty, [Validators.required, Validators.min(0.01)]],
          rate: [l.rate, [Validators.required, Validators.min(0)]],
          amount: [l.amount],
        }),
      );
    }
    if (!this.invoiceLines.length) this.invoiceLines.push(this.createLine());
  }

  onPaymentInvoiceChange(): void {
    const invId = String(this.paymentForm.value.salesInvoiceId || '');
    const inv = this.invoices().find((i) => i.id === invId);
    if (!inv) return;
    const outstanding = Number(inv.grandTotal) - Number(inv.paidAmount || 0);
    this.paymentForm.patchValue({ partyId: inv.partyId, amount: outstanding });
    this.api.getReceivable(inv.partyId).subscribe({
      next: (b) => this.receivable.set(b),
      error: () => this.receivable.set(null),
    });
  }

  save(): void {
    this.error.set('');
    this.saving.set(true);
    if (this.tab() === 'customers') {
      if (this.customerForm.invalid) {
        this.customerForm.markAllAsTouched();
        this.saving.set(false);
        return;
      }
      const raw = this.customerForm.getRawValue();
      this.partiesApi
        .create({
          ...raw,
          partyType: 'Customer',
          openingBalance: 0,
          isActive: true,
        })
        .subscribe({
          next: () => {
            this.reloadCustomers();
            this.saving.set(false);
            this.showForm.set(false);
          },
          error: (e) => {
            this.saving.set(false);
            this.error.set(e?.error?.message || 'Failed to create customer.');
          },
        });
      return;
    }

    if (this.tab() === 'orders') {
      this.orderLines.controls.forEach((_, i) => this.recalcLine('orders', i));
      if (this.orderForm.invalid) {
        this.orderForm.markAllAsTouched();
        this.saving.set(false);
        return;
      }
      const raw = this.orderForm.getRawValue();
      this.api
        .createOrder({
          date: raw.date,
          partyId: raw.partyId,
          warehouseId: raw.warehouseId || null,
          tax: Number(raw.tax || 0),
          remarks: raw.remarks,
          post: false,
          lines: raw.lines.map((l) => ({
            productId: l.productId,
            qty: Number(l.qty),
            rate: Number(l.rate),
            amount: Number(l.qty) * Number(l.rate),
          })),
        })
        .subscribe({
          next: (created) => {
            this.orders.update((list) => [created, ...list]);
            this.saving.set(false);
            this.showForm.set(false);
            this.selectOrder(created);
          },
          error: (e) => {
            this.saving.set(false);
            this.error.set(e?.error?.message || 'Failed to create sales order.');
          },
        });
      return;
    }

    if (this.tab() === 'deliveries') {
      this.deliveryLines.controls.forEach((_, i) => this.recalcLine('deliveries', i));
      if (this.deliveryForm.invalid) {
        this.deliveryForm.markAllAsTouched();
        this.saving.set(false);
        return;
      }
      const raw = this.deliveryForm.getRawValue();
      this.api
        .createDelivery({
          date: raw.date,
          partyId: raw.partyId,
          warehouseId: raw.warehouseId,
          salesOrderId: raw.salesOrderId,
          remarks: raw.remarks,
          post: false,
          lines: raw.lines.map((l) => ({
            productId: l.productId,
            qty: Number(l.qty),
            rate: Number(l.rate),
            amount: Number(l.qty) * Number(l.rate),
          })),
        })
        .subscribe({
          next: (created) => {
            this.deliveries.update((list) => [created, ...list]);
            this.saving.set(false);
            this.showForm.set(false);
            this.selectDelivery(created);
          },
          error: (e) => {
            this.saving.set(false);
            this.error.set(e?.error?.message || 'Failed to create delivery.');
          },
        });
      return;
    }

    if (this.tab() === 'invoices') {
      this.invoiceLines.controls.forEach((_, i) => this.recalcLine('invoices', i));
      if (this.invoiceForm.invalid) {
        this.invoiceForm.markAllAsTouched();
        this.saving.set(false);
        return;
      }
      const raw = this.invoiceForm.getRawValue();
      this.api
        .createInvoice({
          date: raw.date,
          partyId: raw.partyId,
          deliveryOrderId: raw.deliveryOrderId || null,
          salesOrderId: raw.salesOrderId || null,
          discount: Number(raw.discount || 0),
          tax: Number(raw.tax || 0),
          remarks: raw.remarks,
          post: false,
          lines: raw.lines.map((l) => ({
            productId: l.productId,
            qty: Number(l.qty),
            rate: Number(l.rate),
            amount: Number(l.qty) * Number(l.rate),
          })),
        })
        .subscribe({
          next: (created) => {
            this.invoices.update((list) => [created, ...list]);
            this.saving.set(false);
            this.showForm.set(false);
            this.selectInvoice(created);
          },
          error: (e) => {
            this.saving.set(false);
            this.error.set(e?.error?.message || 'Failed to create invoice.');
          },
        });
      return;
    }

    if (this.paymentForm.invalid) {
      this.paymentForm.markAllAsTouched();
      this.saving.set(false);
      return;
    }
    const raw = this.paymentForm.getRawValue();
    this.api
      .createPayment({
        date: raw.date,
        partyId: raw.partyId,
        amount: Number(raw.amount),
        cashBankAccountId: raw.cashBankAccountId || null,
        remarks: raw.remarks,
        post: false,
        applications: [
          {
            salesInvoiceId: raw.salesInvoiceId,
            amountApplied: Number(raw.amount),
          },
        ],
      })
      .subscribe({
        next: (created) => {
          this.payments.update((list) => [created, ...list]);
          this.saving.set(false);
          this.showForm.set(false);
          this.selectPayment(created);
        },
        error: (e) => {
          this.saving.set(false);
          this.error.set(e?.error?.message || 'Failed to create payment.');
        },
      });
  }

  selectOrder(row: SalesOrder): void {
    this.clearSelection();
    this.tab.set('orders');
    this.showForm.set(false);
    this.api.getOrder(row.id).subscribe({
      next: (full) => {
        this.selectedOrder.set(full);
        this.loadAudit('SalesOrder', full.id);
      },
      error: () => this.selectedOrder.set(row),
    });
  }

  selectDelivery(row: DeliveryOrder): void {
    this.clearSelection();
    this.tab.set('deliveries');
    this.showForm.set(false);
    this.api.getDelivery(row.id).subscribe({
      next: (full) => {
        this.selectedDelivery.set(full);
        this.loadAudit('DeliveryOrder', full.id);
      },
      error: () => this.selectedDelivery.set(row),
    });
  }

  selectInvoice(row: SalesInvoiceDoc): void {
    this.clearSelection();
    this.tab.set('invoices');
    this.showForm.set(false);
    this.api.getInvoice(row.id).subscribe({
      next: (full) => {
        this.selectedInvoice.set(full);
        this.loadAudit('SalesInvoice', full.id);
        this.api.getReceivable(full.partyId).subscribe({
          next: (b) => this.receivable.set(b),
          error: () => this.receivable.set(null),
        });
      },
      error: () => this.selectedInvoice.set(row),
    });
  }

  selectPayment(row: CustomerPayment): void {
    this.clearSelection();
    this.tab.set('payments');
    this.showForm.set(false);
    this.api.getPayment(row.id).subscribe({
      next: (full) => {
        this.selectedPayment.set(full);
        this.loadAudit('CustomerPayment', full.id);
      },
      error: () => this.selectedPayment.set(row),
    });
  }

  private loadAudit(
    entity: 'SalesOrder' | 'DeliveryOrder' | 'SalesInvoice' | 'CustomerPayment',
    id: string,
  ): void {
    this.api.getAudit(entity, id).subscribe({
      next: (rows) =>
        this.auditEntries.set(
          rows.map(
            (r: AuditLogEntry): AuditTimelineEntry => ({
              action: r.action,
              oldValues: r.oldValues,
              newValues: r.newValues,
              timestamp: r.timestamp,
              userId: r.userId,
              ipAddress: r.ipAddress,
            }),
          ),
        ),
      error: () => this.auditEntries.set([]),
    });
  }

  runOrderAction(action: DocumentAction): void {
    const row = this.selectedOrder();
    if (!row) return;
    this.transitioning.set(true);
    this.api.transitionOrder(row.id, action).subscribe({
      next: (updated) => {
        this.orders.update((list) => list.map((x) => (x.id === updated.id ? updated : x)));
        this.selectedOrder.set(updated);
        this.loadAudit('SalesOrder', updated.id);
        this.transitioning.set(false);
      },
      error: (e) => {
        this.transitioning.set(false);
        this.error.set(e?.error?.message || `Failed to ${action} sales order.`);
      },
    });
  }

  runDeliveryAction(action: DocumentAction): void {
    const row = this.selectedDelivery();
    if (!row) return;
    this.transitioning.set(true);
    this.api.transitionDelivery(row.id, action).subscribe({
      next: (updated) => {
        this.deliveries.update((list) => list.map((x) => (x.id === updated.id ? updated : x)));
        this.selectedDelivery.set(updated);
        this.loadAudit('DeliveryOrder', updated.id);
        this.transitioning.set(false);
      },
      error: (e) => {
        this.transitioning.set(false);
        this.error.set(e?.error?.message || `Failed to ${action} delivery.`);
      },
    });
  }

  runInvoiceAction(action: DocumentAction): void {
    const row = this.selectedInvoice();
    if (!row) return;
    this.transitioning.set(true);
    this.api.transitionInvoice(row.id, action).subscribe({
      next: (updated) => {
        this.invoices.update((list) => list.map((x) => (x.id === updated.id ? updated : x)));
        this.selectedInvoice.set(updated);
        this.loadAudit('SalesInvoice', updated.id);
        this.api.getReceivable(updated.partyId).subscribe({
          next: (b) => this.receivable.set(b),
          error: () => this.receivable.set(null),
        });
        this.transitioning.set(false);
      },
      error: (e) => {
        this.transitioning.set(false);
        this.error.set(e?.error?.message || `Failed to ${action} invoice.`);
      },
    });
  }

  runPaymentAction(action: DocumentAction): void {
    const row = this.selectedPayment();
    if (!row) return;
    this.transitioning.set(true);
    this.api.transitionPayment(row.id, action).subscribe({
      next: (updated) => {
        this.payments.update((list) => list.map((x) => (x.id === updated.id ? updated : x)));
        this.selectedPayment.set(updated);
        this.loadAudit('CustomerPayment', updated.id);
        this.loadAll();
        this.transitioning.set(false);
      },
      error: (e) => {
        this.transitioning.set(false);
        this.error.set(e?.error?.message || `Failed to ${action} payment.`);
      },
    });
  }
}
