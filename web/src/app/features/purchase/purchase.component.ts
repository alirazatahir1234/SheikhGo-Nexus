import { Component, OnInit, computed, signal } from '@angular/core';
import {
  FormArray,
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import {
  AuditLogEntry,
  GoodsReceipt,
  PayableBalance,
  PurchaseInvoice,
  PurchaseOrder,
  PurchasesService,
} from '../../core/services/purchases.service';
import { PartiesService } from '../../core/services/parties.service';
import { ProductsService } from '../../core/services/products.service';
import { CompanyService } from '../../core/services/company.service';
import { WarehousesService, Warehouse } from '../../core/services/warehouses.service';
import { Party, Product } from '../../core/models/erp.models';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SearchBoxComponent } from '../../shared/components/search-box/search-box.component';
import { DocumentStatusStepperComponent, statusLabel } from '../../shared/documents/document-status-stepper.component';
import { DocumentHeaderComponent } from '../../shared/documents/document-header.component';
import { DocumentLinesComponent } from '../../shared/documents/document-lines.component';
import { DocumentTotalsComponent } from '../../shared/documents/document-totals.component';
import { VerificationPanelComponent } from '../../shared/documents/verification-panel.component';
import { DocumentActionsComponent, DocumentAction } from '../../shared/documents/document-actions.component';
import { AuditTimelineComponent, AuditTimelineEntry } from '../../shared/documents/audit-timeline.component';
import { AuthService } from '../../core/services/auth.service';
import { AdminApiService, DocumentTypeRow } from '../../core/services/admin-api.service';

type ProcTab = 'orders' | 'receipts' | 'invoices';

@Component({
  selector: 'app-purchase',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    CurrencyPipe,
    DatePipe,
    PageHeaderComponent,
    SearchBoxComponent,
    DocumentStatusStepperComponent,
    DocumentHeaderComponent,
    DocumentLinesComponent,
    DocumentTotalsComponent,
    VerificationPanelComponent,
    DocumentActionsComponent,
    AuditTimelineComponent,
  ],
  templateUrl: './purchase.component.html',
  styleUrl: './purchase.component.scss',
})
export class PurchaseComponent implements OnInit {
  readonly tab = signal<ProcTab>('orders');
  readonly orders = signal<PurchaseOrder[]>([]);
  readonly receipts = signal<GoodsReceipt[]>([]);
  readonly invoices = signal<PurchaseInvoice[]>([]);
  readonly parties = signal<Party[]>([]);
  readonly products = signal<Product[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly search = signal('');
  readonly showForm = signal(false);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly saving = signal(false);
  readonly transitioning = signal(false);
  readonly taxEnabled = signal(false);
  readonly taxPercent = signal(0);
  readonly payable = signal<PayableBalance | null>(null);

  readonly selectedOrder = signal<PurchaseOrder | null>(null);
  readonly selectedReceipt = signal<GoodsReceipt | null>(null);
  readonly selectedInvoice = signal<PurchaseInvoice | null>(null);
  readonly auditEntries = signal<AuditTimelineEntry[]>([]);
  readonly documentTypes = signal<DocumentTypeRow[]>([]);

  readonly statusLabel = statusLabel;

  readonly poFlags = computed(() => this.docFlags('PO'));
  readonly grnFlags = computed(() => this.docFlags('GRN'));
  readonly piFlags = computed(() => this.docFlags('PI'));

  readonly orderActionAllowed = computed(() => this.actionAllowed('PurchaseOrder'));
  readonly receiptActionAllowed = computed(() => this.actionAllowed('GoodsReceipt'));
  readonly invoiceActionAllowed = computed(() => this.actionAllowed('PurchaseInvoice'));

  private docFlags(code: string): { requiresVerification: boolean; requiresApproval: boolean } {
    const t = this.documentTypes().find((d) => d.code.toUpperCase() === code);
    if (!t) {
      if (code === 'PO') return { requiresVerification: false, requiresApproval: true };
      if (code === 'GRN') return { requiresVerification: true, requiresApproval: false };
      return { requiresVerification: true, requiresApproval: true };
    }
    return {
      requiresVerification: !!t.requiresVerification,
      requiresApproval: !!t.requiresApproval,
    };
  }

  private actionAllowed(docKind: string): Partial<Record<DocumentAction, boolean>> {
    return {
      submit: this.auth.hasPermission(`Procurement.${docKind}.Submit`),
      verify: this.auth.hasPermission(`Procurement.${docKind}.Approve`),
      approve: this.auth.hasPermission(`Procurement.${docKind}.Approve`),
      post: this.auth.hasPermission(`Procurement.${docKind}.Post`),
      cancel: this.auth.hasPermission(`Procurement.${docKind}.Edit`),
    };
  }

  readonly filteredOrders = computed(() => {
    const q = this.search().toLowerCase().trim();
    return this.orders().filter(
      (o) =>
        !q ||
        o.orderNo.toLowerCase().includes(q) ||
        (o.partyName ?? '').toLowerCase().includes(q),
    );
  });

  readonly filteredReceipts = computed(() => {
    const q = this.search().toLowerCase().trim();
    return this.receipts().filter(
      (r) =>
        !q ||
        r.receiptNo.toLowerCase().includes(q) ||
        (r.partyName ?? '').toLowerCase().includes(q),
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

  readonly orderForm;
  readonly receiptForm;
  readonly invoiceForm;

  constructor(
    private readonly api: PurchasesService,
    private readonly partiesApi: PartiesService,
    private readonly productsApi: ProductsService,
    private readonly companyApi: CompanyService,
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

    this.receiptForm = this.fb.nonNullable.group({
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      partyId: ['', Validators.required],
      warehouseId: ['', Validators.required],
      purchaseOrderId: [''],
      remarks: [''],
      lines: this.fb.array([this.createLine()]),
    });

    this.invoiceForm = this.fb.nonNullable.group({
      date: [new Date().toISOString().slice(0, 10), Validators.required],
      partyId: ['', Validators.required],
      purchaseOrderId: [''],
      goodsReceiptId: [''],
      discount: [0, [Validators.min(0)]],
      tax: [0, [Validators.min(0)]],
      remarks: [''],
      lines: this.fb.array([this.createLine()]),
    });
  }

  get orderLines(): FormArray {
    return this.orderForm.get('lines') as FormArray;
  }
  get receiptLines(): FormArray {
    return this.receiptForm.get('lines') as FormArray;
  }
  get invoiceLines(): FormArray {
    return this.invoiceForm.get('lines') as FormArray;
  }

  ngOnInit(): void {
    this.loadAll();
    this.adminApi.getDocumentTypes().subscribe({
      next: (types) => this.documentTypes.set(types),
      error: () => this.documentTypes.set([]),
    });
    this.partiesApi.getAll().subscribe({
      next: (data) =>
        this.parties.set(data.filter((p) => p.partyType === 'Supplier' || p.partyType === 'Both')),
      error: () => this.parties.set([]),
    });
    this.productsApi.getAll().subscribe({
      next: (data) => this.products.set(data),
      error: () => this.products.set([]),
    });
    this.warehousesApi.getAll().subscribe({
      next: (data) => this.warehouses.set(data),
      error: () => this.warehouses.set([]),
    });
    this.companyApi.get().subscribe({
      next: (c) => {
        this.taxEnabled.set(!!c.taxModule);
        this.taxPercent.set(Number(c.defaultPurchaseTaxPercent || 0));
      },
    });
  }

  setTab(tab: ProcTab): void {
    this.tab.set(tab);
    this.showForm.set(false);
    this.clearSelection();
    this.error.set('');
  }

  clearSelection(): void {
    this.selectedOrder.set(null);
    this.selectedReceipt.set(null);
    this.selectedInvoice.set(null);
    this.auditEntries.set([]);
  }

  loadAll(): void {
    this.loading.set(true);
    this.error.set('');
    let pending = 3;
    const done = () => {
      pending -= 1;
      if (pending <= 0) this.loading.set(false);
    };
    this.api.getOrders().subscribe({
      next: (d) => {
        this.orders.set(d);
        done();
      },
      error: () => {
        this.orders.set([]);
        this.error.set('Could not load purchase orders.');
        done();
      },
    });
    this.api.getReceipts().subscribe({
      next: (d) => {
        this.receipts.set(d);
        done();
      },
      error: () => {
        this.receipts.set([]);
        done();
      },
    });
    this.api.getAll().subscribe({
      next: (d) => {
        this.invoices.set(d);
        done();
      },
      error: () => {
        this.invoices.set([]);
        done();
      },
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

  addLine(which: ProcTab): void {
    this.linesOf(which).push(this.createLine());
  }

  removeLine(which: ProcTab, index: number): void {
    const lines = this.linesOf(which);
    if (lines.length > 1) lines.removeAt(index);
  }

  linesOf(which: ProcTab): FormArray {
    if (which === 'orders') return this.orderLines;
    if (which === 'receipts') return this.receiptLines;
    return this.invoiceLines;
  }

  onProductChange(which: ProcTab, index: number): void {
    const line = this.linesOf(which).at(index);
    const product = this.products().find((p) => String(p.id) === String(line.value.productId));
    if (product) {
      const rate = Number(product.purchasePrice || 0);
      line.patchValue({ rate, amount: rate * Number(line.value.qty || 0) });
      this.applyDefaultTax(which);
    }
  }

  recalcLine(which: ProcTab, index: number): void {
    const line = this.linesOf(which).at(index);
    line.patchValue(
      { amount: Number(line.value.qty || 0) * Number(line.value.rate || 0) },
      { emitEvent: false },
    );
    this.applyDefaultTax(which);
  }

  subTotal(which: ProcTab): number {
    return this.linesOf(which).controls.reduce((sum, c) => sum + Number(c.value.amount || 0), 0);
  }

  applyDefaultTax(which: ProcTab): void {
    if (!this.taxEnabled() || which === 'receipts') return;
    const discount = which === 'invoices' ? Number(this.invoiceForm.value.discount || 0) : 0;
    const taxable = Math.max(0, this.subTotal(which) - discount);
    const tax = Math.round(((taxable * this.taxPercent()) / 100) * 100) / 100;
    if (which === 'orders') this.orderForm.patchValue({ tax }, { emitEvent: false });
    else this.invoiceForm.patchValue({ tax }, { emitEvent: false });
  }

  grandTotal(which: ProcTab): number {
    if (which === 'receipts') return this.subTotal(which);
    if (which === 'orders') return this.subTotal(which) + Number(this.orderForm.value.tax || 0);
    return (
      this.subTotal(which) -
      Number(this.invoiceForm.value.discount || 0) +
      Number(this.invoiceForm.value.tax || 0)
    );
  }

  openForm(): void {
    this.clearSelection();
    const today = new Date().toISOString().slice(0, 10);
    if (this.tab() === 'orders') {
      this.orderForm.reset({ date: today, partyId: '', warehouseId: '', tax: 0, remarks: '' });
      this.orderLines.clear();
      this.orderLines.push(this.createLine());
      this.applyDefaultTax('orders');
    } else if (this.tab() === 'receipts') {
      const defWh = this.warehouses().find((w) => w.isDefault)?.id ?? '';
      this.receiptForm.reset({
        date: today,
        partyId: '',
        warehouseId: defWh,
        purchaseOrderId: '',
        remarks: '',
      });
      this.receiptLines.clear();
      this.receiptLines.push(this.createLine());
    } else {
      this.invoiceForm.reset({
        date: today,
        partyId: '',
        purchaseOrderId: '',
        goodsReceiptId: '',
        discount: 0,
        tax: 0,
        remarks: '',
      });
      this.invoiceLines.clear();
      this.invoiceLines.push(this.createLine());
      this.payable.set(null);
      this.applyDefaultTax('invoices');
    }
    this.showForm.set(true);
  }

  onInvoicePartyChange(): void {
    const partyId = String(this.invoiceForm.value.partyId || '');
    if (!partyId) {
      this.payable.set(null);
      return;
    }
    this.api.getPayables(partyId).subscribe({
      next: (b) => this.payable.set(b),
      error: () => this.payable.set(null),
    });
  }

  onReceiptPoChange(): void {
    const poId = String(this.receiptForm.value.purchaseOrderId || '');
    if (!poId) return;
    const po = this.orders().find((o) => o.id === poId);
    if (!po) return;
    this.receiptForm.patchValue({
      partyId: po.partyId,
      warehouseId: po.warehouseId || this.receiptForm.value.warehouseId,
    });
    this.receiptLines.clear();
    for (const l of po.lines) {
      this.receiptLines.push(
        this.fb.nonNullable.group({
          productId: [l.productId, Validators.required],
          qty: [l.qty, [Validators.required, Validators.min(0.01)]],
          rate: [l.rate, [Validators.required, Validators.min(0)]],
          amount: [l.amount],
        }),
      );
    }
    if (!this.receiptLines.length) this.receiptLines.push(this.createLine());
  }

  validateGrnAgainstPo(): string | null {
    const poId = String(this.receiptForm.value.purchaseOrderId || '');
    if (!poId) return null;
    const po = this.orders().find((o) => o.id === poId);
    if (!po) return 'Selected purchase order was not found.';
    for (const c of this.receiptLines.controls) {
      const productId = String(c.value.productId);
      const qty = Number(c.value.qty || 0);
      const poLine = po.lines.find((l) => String(l.productId) === productId);
      if (!poLine) return 'A GRN product is not on the selected purchase order.';
      if (qty > Number(poLine.qty)) {
        return `Qty for ${poLine.productName || productId} exceeds PO qty (${poLine.qty}).`;
      }
    }
    return null;
  }

  mapLines(which: ProcTab) {
    const productMap = new Map(this.products().map((p) => [String(p.id), p.name]));
    return this.linesOf(which).getRawValue().map((l) => ({
      productId: String(l.productId),
      productName: productMap.get(String(l.productId)),
      qty: Number(l.qty),
      rate: Number(l.rate),
      amount: Number(l.qty) * Number(l.rate),
    }));
  }

  save(): void {
    this.error.set('');
    const tab = this.tab();
    if (tab === 'orders') {
      this.orderLines.controls.forEach((_, i) => this.recalcLine('orders', i));
      if (this.orderForm.invalid) {
        this.orderForm.markAllAsTouched();
        return;
      }
      this.saving.set(true);
      const raw = this.orderForm.getRawValue();
      this.api
        .createOrder({
          date: raw.date,
          partyId: String(raw.partyId),
          warehouseId: raw.warehouseId || null,
          tax: Number(raw.tax || 0),
          remarks: raw.remarks || undefined,
          post: false,
          lines: this.mapLines('orders'),
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
            this.error.set(e?.error?.message || 'Failed to create purchase order.');
          },
        });
      return;
    }

    if (tab === 'receipts') {
      this.receiptLines.controls.forEach((_, i) => this.recalcLine('receipts', i));
      if (this.receiptForm.invalid) {
        this.receiptForm.markAllAsTouched();
        return;
      }
      const qtyErr = this.validateGrnAgainstPo();
      if (qtyErr) {
        this.error.set(qtyErr);
        return;
      }
      this.saving.set(true);
      const raw = this.receiptForm.getRawValue();
      this.api
        .createReceipt({
          date: raw.date,
          partyId: String(raw.partyId),
          warehouseId: String(raw.warehouseId),
          purchaseOrderId: raw.purchaseOrderId || null,
          remarks: raw.remarks || undefined,
          post: false,
          lines: this.mapLines('receipts'),
        })
        .subscribe({
          next: (created) => {
            this.receipts.update((list) => [created, ...list]);
            this.saving.set(false);
            this.showForm.set(false);
            this.selectReceipt(created);
          },
          error: (e) => {
            this.saving.set(false);
            this.error.set(e?.error?.message || 'Failed to create goods receipt.');
          },
        });
      return;
    }

    this.invoiceLines.controls.forEach((_, i) => this.recalcLine('invoices', i));
    if (this.invoiceForm.invalid) {
      this.invoiceForm.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    const raw = this.invoiceForm.getRawValue();
    this.api
      .create({
        date: raw.date,
        partyId: String(raw.partyId),
        discount: Number(raw.discount || 0),
        tax: Number(raw.tax || 0),
        remarks: raw.remarks || undefined,
        post: false,
        goodsReceiptId: raw.goodsReceiptId || null,
        purchaseOrderId: raw.purchaseOrderId || null,
        lines: this.mapLines('invoices'),
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
          this.error.set(e?.error?.message || 'Failed to create purchase invoice.');
        },
      });
  }

  selectOrder(order: PurchaseOrder): void {
    this.showForm.set(false);
    this.selectedOrder.set(order);
    this.selectedReceipt.set(null);
    this.selectedInvoice.set(null);
    this.loadAudit('PurchaseOrder', order.id);
  }

  selectReceipt(receipt: GoodsReceipt): void {
    this.showForm.set(false);
    this.selectedReceipt.set(receipt);
    this.selectedOrder.set(null);
    this.selectedInvoice.set(null);
    this.loadAudit('GoodsReceipt', receipt.id);
  }

  selectInvoice(invoice: PurchaseInvoice): void {
    this.showForm.set(false);
    this.selectedInvoice.set(invoice);
    this.selectedOrder.set(null);
    this.selectedReceipt.set(null);
    this.loadAudit('PurchaseInvoice', invoice.id);
  }

  loadAudit(entity: 'PurchaseOrder' | 'GoodsReceipt' | 'PurchaseInvoice', id: string): void {
    this.api.getAudit(entity, id).subscribe({
      next: (rows: AuditLogEntry[]) =>
        this.auditEntries.set(
          rows.map((r) => ({
            action: r.action,
            timestamp: r.timestamp,
            oldValues: r.oldValues,
            newValues: r.newValues,
            userId: r.userId,
          })),
        ),
      error: () => this.auditEntries.set([]),
    });
  }

  onApproval(action: DocumentAction): void {
    this.error.set('');
    this.transitioning.set(true);
    const finishErr = (e: { error?: { message?: string } }) => {
      this.transitioning.set(false);
      this.error.set(e?.error?.message || `Failed to ${action} document.`);
    };

    if (this.selectedOrder()) {
      const id = this.selectedOrder()!.id;
      this.api.transitionOrder(id, action).subscribe({
        next: (updated) => {
          this.orders.update((list) => list.map((o) => (o.id === id ? updated : o)));
          this.selectedOrder.set(updated);
          this.transitioning.set(false);
          this.loadAudit('PurchaseOrder', id);
        },
        error: finishErr,
      });
      return;
    }

    if (this.selectedReceipt()) {
      const id = this.selectedReceipt()!.id;
      this.api.transitionReceipt(id, action).subscribe({
        next: (updated) => {
          this.receipts.update((list) => list.map((r) => (r.id === id ? updated : r)));
          this.selectedReceipt.set(updated);
          this.transitioning.set(false);
          this.loadAudit('GoodsReceipt', id);
        },
        error: finishErr,
      });
      return;
    }

    if (this.selectedInvoice()) {
      const id = this.selectedInvoice()!.id;
      this.api.transitionInvoice(id, action).subscribe({
        next: (updated) => {
          this.invoices.update((list) => list.map((i) => (i.id === id ? updated : i)));
          this.selectedInvoice.set(updated);
          this.transitioning.set(false);
          this.loadAudit('PurchaseInvoice', id);
        },
        error: finishErr,
      });
    }
  }

  receiptSubTotal(r: GoodsReceipt): number {
    return (r.lines ?? []).reduce((s, l) => s + Number(l.amount || 0), 0);
  }

  newButtonLabel(): string {
    if (this.tab() === 'orders') return 'New PO';
    if (this.tab() === 'receipts') return 'New GRN';
    return 'New invoice';
  }

  poNo(purchaseOrderId?: string | null): string {
    if (!purchaseOrderId) return '—';
    return this.orders().find((o) => o.id === purchaseOrderId)?.orderNo ?? 'Linked';
  }
}
