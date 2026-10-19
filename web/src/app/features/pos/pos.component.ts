import {
  Component,
  ElementRef,
  HostListener,
  OnInit,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import {
  PosApiService,
  PosDailySalesReport,
  PosOwnerProfitReport,
  PosSale,
  PosSaleLine,
  PosShift,
  PosShiftReconciliation,
  PosTerminal,
} from '../../core/services/pos-api.service';
import { ProductsService } from '../../core/services/products.service';
import { Product } from '../../core/models/erp.models';
import { statusLabel } from '../../shared/documents/document-status-stepper.component';
import { bindTabFromRoute, navigateModuleTab } from '../../core/nav/route-tabs';

type PosTab = 'terminal' | 'sales' | 'shifts' | 'reports';
type TenderMode = 'Cash' | 'Card' | 'Other';

interface CartLine {
  productId: string;
  productName: string;
  unit: string;
  sellByWeight: boolean;
  qty: number;
  rate: number;
  stockQty?: number;
}

interface HeldSale {
  id: string;
  heldAt: string;
  cart: CartLine[];
  discount: number;
  tenderMode: TenderMode;
  cashAmount: number;
  cardAmount: number;
}

const POS_TAB_ROUTE: Record<PosTab, string> = {
  terminal: 'terminal',
  sales: 'sales',
  shifts: 'shifts',
  reports: 'reports',
};

const HOLD_LIST_KEY = 'nexus.pos.heldSales';
const LEGACY_HOLD_KEY = 'nexus.pos.heldSale';
const SHOP_NAME = 'SheikhGo Sweets';
const BRANCH_LABEL = 'Main Branch';

@Component({
  selector: 'app-pos',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe, DatePipe, DecimalPipe, RouterLink, RouterLinkActive],
  templateUrl: './pos.component.html',
  styleUrl: './pos.component.scss',
})
export class PosComponent implements OnInit {
  private readonly router = inject(Router);
  @ViewChild('barcodeInput') barcodeInput?: ElementRef<HTMLInputElement>;

  readonly tab = signal<PosTab>('terminal');
  readonly terminals = signal<PosTerminal[]>([]);
  readonly shifts = signal<PosShift[]>([]);
  readonly sales = signal<PosSale[]>([]);
  readonly products = signal<Product[]>([]);
  readonly cart = signal<CartLine[]>([]);
  readonly barcode = signal('');
  readonly searchQ = signal('');
  readonly category = signal('All');
  readonly discount = signal(0);
  readonly discountPct = signal(false);
  readonly cashAmount = signal(0);
  readonly cardAmount = signal(0);
  readonly cashReceived = signal(0);
  readonly tenderMode = signal<TenderMode>('Cash');
  readonly showOpen = signal(false);
  readonly showClose = signal(false);
  readonly showHolds = signal(false);
  readonly showPay = signal(false);
  readonly showProduct = signal(false);
  readonly showReturn = signal(false);
  readonly productDraft = signal<Product | null>(null);
  readonly productQty = signal(1);
  readonly returnSaleDraft = signal<PosSale | null>(null);
  readonly returnReason = signal('Customer return');
  readonly returnLineIds = signal<Set<string>>(new Set());
  readonly heldSales = signal<HeldSale[]>([]);
  readonly closeActualCash = signal(0);
  readonly recon = signal<PosShiftReconciliation | null>(null);
  readonly saving = signal(false);
  readonly scanning = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  readonly lastSale = signal<PosSale | null>(null);
  readonly lastChange = signal(0);
  readonly dailyReport = signal<PosDailySalesReport | null>(null);
  readonly profitReport = signal<PosOwnerProfitReport | null>(null);
  readonly reportDate = signal(new Date().toISOString().slice(0, 10));
  readonly statusLabel = statusLabel;
  readonly today = new Date().toISOString().slice(0, 10);
  readonly shopName = SHOP_NAME;
  readonly branchLabel = BRANCH_LABEL;
  readonly nowLabel = new Date().toLocaleString();

  readonly openShift = computed(() => this.shifts().find((s) => s.status === 'Open') ?? null);
  readonly activeTerminal = computed(() => {
    const shift = this.openShift();
    if (!shift) return this.terminals()[0] ?? null;
    return this.terminals().find((t) => t.id === shift.terminalId) ?? null;
  });
  readonly terminalWarehouseId = computed(() => this.activeTerminal()?.warehouseId ?? null);

  readonly categories = computed(() => {
    const set = new Set<string>();
    for (const p of this.products()) {
      if (p.category?.trim()) set.add(p.category.trim());
    }
    return ['All', ...Array.from(set).sort()];
  });

  readonly catalog = computed(() => {
    const q = (this.searchQ() || this.barcode()).toLowerCase().trim();
    const cat = this.category();
    return this.products()
      .filter((p) => (cat === 'All' ? true : (p.category || '') === cat))
      .filter((p) => {
        if (!q) return true;
        return (
          p.name.toLowerCase().includes(q) ||
          p.code.toLowerCase().includes(q) ||
          (p.barcode || '').toLowerCase().includes(q)
        );
      })
      .slice(0, 48);
  });

  readonly cartCount = computed(() => this.cart().length);
  readonly subtotal = computed(
    () => Math.round(this.cart().reduce((s, l) => s + l.qty * l.rate, 0) * 100) / 100,
  );
  readonly discountAmount = computed(() => {
    const raw = Number(this.discount()) || 0;
    if (this.discountPct()) {
      return Math.round(Math.min(100, Math.max(0, raw)) * this.subtotal() * 0.01 * 100) / 100;
    }
    return Math.min(this.subtotal(), Math.max(0, raw));
  });
  readonly grandTotal = computed(
    () => Math.round(Math.max(0, this.subtotal() - this.discountAmount()) * 100) / 100,
  );
  readonly changeDue = computed(() => {
    if (this.tenderMode() !== 'Cash') return 0;
    return Math.max(0, Math.round((this.cashReceived() - this.grandTotal()) * 100) / 100);
  });
  readonly productDraftTotal = computed(() => {
    const p = this.productDraft();
    if (!p) return 0;
    return Math.round(this.productQty() * p.salePrice * 100) / 100;
  });
  readonly cashQuickAmounts = computed(() => {
    const t = this.grandTotal();
    const bumps = [0, 100, 175, 500, 1000, 2000].map((b) => Math.ceil((t + b) / 50) * 50);
    return Array.from(new Set([t, ...bumps].filter((n) => n >= t))).slice(0, 5);
  });

  readonly openForm;

  constructor(
    private readonly api: PosApiService,
    private readonly productsApi: ProductsService,
    private readonly fb: FormBuilder,
  ) {
    this.openForm = this.fb.nonNullable.group({
      terminalId: ['', Validators.required],
      openingCash: [0, [Validators.required, Validators.min(0)]],
    });
  }

  ngOnInit(): void {
    bindTabFromRoute<PosTab>((tab) => {
      this.tab.set(tab);
      this.error.set('');
      if (tab === 'reports') this.loadReports();
      if (tab === 'terminal') setTimeout(() => this.focusBarcode(), 50);
    });
    this.productsApi.getAll().subscribe({ next: (p) => this.products.set(p.filter((x) => x.isActive !== false)) });
    this.reload();
    this.loadHeldSales();
    this.migrateLegacyHold();
  }

  private modalOpen(): boolean {
    return (
      this.showOpen() ||
      this.showClose() ||
      this.showHolds() ||
      this.showPay() ||
      this.showProduct() ||
      this.showReturn()
    );
  }

  readonly returnReasons = [
    'Customer return',
    'Wrong item',
    'Quality issue',
    'Damaged',
    'Other',
  ];

  reload(): void {
    this.api.getTerminals().subscribe({
      next: (t) => {
        this.terminals.set(t);
        if (!this.openForm.value.terminalId && t[0]) {
          this.openForm.patchValue({ terminalId: t[0].id });
        }
      },
    });
    this.api.getShifts().subscribe({ next: (s) => this.shifts.set(s) });
    this.api.getSales().subscribe({
      next: (rows) => this.sales.set(rows),
      error: () => this.error.set('Could not load POS sales'),
    });
  }

  setTab(t: PosTab): void {
    navigateModuleTab(this.router, 'pos', POS_TAB_ROUTE[t]);
  }

  focusBarcode(): void {
    this.barcodeInput?.nativeElement?.focus();
    this.barcodeInput?.nativeElement?.select();
  }

  @HostListener('window:focus')
  onWindowFocus(): void {
    if (this.tab() !== 'terminal' || this.modalOpen()) return;
    setTimeout(() => this.focusBarcode(), 30);
  }

  @HostListener('document:keydown', ['$event'])
  onDocumentKeydown(ev: KeyboardEvent): void {
    if (ev.key === 'F2') {
      ev.preventDefault();
      this.focusBarcode();
      return;
    }
    if (ev.key === 'F5' && this.tab() === 'terminal' && !this.modalOpen()) {
      ev.preventDefault();
      this.openPayment();
      return;
    }
    if (this.tab() !== 'terminal' || this.modalOpen() || this.saving()) return;
    const t = ev.target as HTMLElement | null;
    if (t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA' || t.tagName === 'SELECT' || t.isContentEditable)) {
      return;
    }
    if (ev.ctrlKey || ev.metaKey || ev.altKey) return;
    if (ev.key === 'Enter') {
      ev.preventDefault();
      this.focusBarcode();
      const code = this.barcode().trim();
      if (code) this.scanBarcode(code);
      return;
    }
    if (ev.key.length === 1) {
      ev.preventDefault();
      this.focusBarcode();
      this.barcode.update((v) => v + ev.key);
    }
  }

  onScanKeydown(ev: KeyboardEvent): void {
    if (ev.key !== 'Enter') return;
    ev.preventDefault();
    const code = this.barcode().trim() || this.searchQ().trim();
    if (!code) return;
    // Prefer exact barcode lookup; fall back to first catalog hit
    this.scanBarcode(code);
  }

  onScanInput(value: string): void {
    this.barcode.set(value);
    this.searchQ.set(value);
  }

  scanBarcode(code: string): void {
    if (this.scanning()) return;
    this.scanning.set(true);
    this.error.set('');
    this.productsApi.getByBarcode(code, this.terminalWarehouseId()).subscribe({
      next: (p) => {
        this.scanning.set(false);
        this.barcode.set('');
        this.searchQ.set('');
        if (p.sellByWeight) this.openProductDetail(p, 0.25);
        else this.addProduct(p, 1);
      },
      error: () => {
        this.scanning.set(false);
        const hit = this.catalog().find(
          (p) =>
            p.code.toLowerCase() === code.toLowerCase() ||
            p.name.toLowerCase() === code.toLowerCase(),
        );
        if (hit) {
          this.barcode.set('');
          this.searchQ.set('');
          if (hit.sellByWeight) this.openProductDetail(hit, 0.25);
          else this.addProduct(hit, 1);
          return;
        }
        this.error.set(`Unknown barcode: ${code}`);
        this.barcode.set('');
        this.focusBarcode();
      },
    });
  }

  selectCategory(cat: string): void {
    this.category.set(cat);
  }

  openProductDetail(p: Product, qty?: number): void {
    this.productDraft.set(p);
    this.productQty.set(qty ?? (p.sellByWeight ? 0.5 : 1));
    this.showProduct.set(true);
  }

  bumpProductQty(delta: number): void {
    const p = this.productDraft();
    const step = p?.sellByWeight ? 0.05 : 1;
    const next = Math.round((this.productQty() + delta * step) * 1000) / 1000;
    this.productQty.set(Math.max(p?.sellByWeight ? 0.001 : 1, next));
  }

  setProductQty(v: number): void {
    const p = this.productDraft();
    const min = p?.sellByWeight ? 0.001 : 1;
    this.productQty.set(Math.max(min, Number(v) || min));
  }

  confirmProductDetail(): void {
    const p = this.productDraft();
    if (!p) return;
    this.addProduct(p, this.productQty());
    this.showProduct.set(false);
    this.productDraft.set(null);
    setTimeout(() => this.focusBarcode(), 30);
  }

  cancelProductDetail(): void {
    this.showProduct.set(false);
    this.productDraft.set(null);
    setTimeout(() => this.focusBarcode(), 30);
  }

  addProduct(p: Product, qty = 1): void {
    const stock = Number(p.stockQty);
    if (Number.isFinite(stock) && stock <= 0) {
      this.error.set(`No stock for ${p.name}.`);
    } else if (Number.isFinite(stock) && stock < qty) {
      this.notice.set(`Low stock: ${p.name} has ${stock} ${p.unit}.`);
    }
    this.cart.update((lines) => {
      const idx = lines.findIndex((l) => l.productId === p.id);
      if (idx >= 0 && !p.sellByWeight) {
        const copy = [...lines];
        copy[idx] = { ...copy[idx], qty: copy[idx].qty + qty, stockQty: stock };
        return copy;
      }
      if (idx >= 0 && p.sellByWeight) {
        const copy = [...lines];
        copy[idx] = {
          ...copy[idx],
          qty: Math.round((copy[idx].qty + qty) * 1000) / 1000,
          stockQty: stock,
        };
        return copy;
      }
      return [
        ...lines,
        {
          productId: p.id,
          productName: p.name,
          unit: p.unit,
          sellByWeight: !!p.sellByWeight,
          qty,
          rate: p.salePrice,
          stockQty: stock,
        },
      ];
    });
    this.syncTenderToTotal();
    if (!(Number.isFinite(stock) && stock < qty)) this.notice.set(`Added ${p.name}`);
    this.focusBarcode();
  }

  bumpLine(i: number, delta: number): void {
    this.cart.update((lines) => {
      const copy = [...lines];
      const line = copy[i];
      const step = line.sellByWeight ? 0.05 : 1;
      const next = Math.round((line.qty + delta * step) * 1000) / 1000;
      const min = line.sellByWeight ? 0.001 : 1;
      if (next < min) return lines.filter((_, idx) => idx !== i);
      copy[i] = { ...line, qty: next };
      return copy;
    });
    this.syncTenderToTotal();
  }

  setQty(i: number, qty: number): void {
    this.cart.update((lines) => {
      const copy = [...lines];
      const min = copy[i].sellByWeight ? 0.001 : 1;
      copy[i] = { ...copy[i], qty: Math.max(min, Number(qty) || min) };
      return copy;
    });
    this.syncTenderToTotal();
  }

  removeLine(i: number): void {
    this.cart.update((lines) => lines.filter((_, idx) => idx !== i));
    this.syncTenderToTotal();
  }

  setDiscount(v: number): void {
    this.discount.set(Math.max(0, Number(v) || 0));
    this.syncTenderToTotal();
  }

  toggleDiscountMode(): void {
    this.discountPct.update((v) => !v);
    this.syncTenderToTotal();
  }

  setTenderMode(mode: TenderMode): void {
    this.tenderMode.set(mode);
    this.syncTenderToTotal();
  }

  syncTenderToTotal(): void {
    const total = this.grandTotal();
    const mode = this.tenderMode();
    if (mode === 'Cash') {
      this.cashAmount.set(total);
      this.cardAmount.set(0);
      if (this.cashReceived() < total) this.cashReceived.set(total);
    } else if (mode === 'Card' || mode === 'Other') {
      this.cashAmount.set(0);
      this.cardAmount.set(total);
      this.cashReceived.set(0);
    }
  }

  /* —— Hold sales (multi) —— */
  private loadHeldSales(): void {
    try {
      const raw = localStorage.getItem(HOLD_LIST_KEY);
      if (!raw) return;
      const list = JSON.parse(raw) as HeldSale[];
      if (Array.isArray(list)) this.heldSales.set(list);
    } catch {
      /* ignore */
    }
  }

  private persistHeldSales(): void {
    localStorage.setItem(HOLD_LIST_KEY, JSON.stringify(this.heldSales()));
  }

  private migrateLegacyHold(): void {
    try {
      const raw = localStorage.getItem(LEGACY_HOLD_KEY);
      if (!raw) return;
      const data = JSON.parse(raw);
      if (Array.isArray(data.cart) && data.cart.length) {
        const held: HeldSale = {
          id: `HLD-${Date.now().toString().slice(-6)}`,
          heldAt: new Date().toISOString(),
          cart: data.cart,
          discount: Number(data.discount) || 0,
          tenderMode: data.tenderMode || 'Cash',
          cashAmount: Number(data.cashAmount) || 0,
          cardAmount: Number(data.cardAmount) || 0,
        };
        this.heldSales.update((list) => [held, ...list]);
        this.persistHeldSales();
      }
      localStorage.removeItem(LEGACY_HOLD_KEY);
    } catch {
      /* ignore */
    }
  }

  holdSale(): void {
    if (!this.cart().length) return;
    const held: HeldSale = {
      id: `HLD-${Date.now().toString().slice(-6)}`,
      heldAt: new Date().toISOString(),
      cart: this.cart(),
      discount: this.discount(),
      tenderMode: this.tenderMode(),
      cashAmount: this.cashAmount(),
      cardAmount: this.cardAmount(),
    };
    this.heldSales.update((list) => [held, ...list]);
    this.persistHeldSales();
    this.cart.set([]);
    this.discount.set(0);
    this.syncTenderToTotal();
    this.notice.set(`Held ${held.id}`);
    this.focusBarcode();
  }

  openHolds(): void {
    this.loadHeldSales();
    this.showHolds.set(true);
  }

  loadHeld(h: HeldSale): void {
    if (this.cart().length && !confirm('Replace current cart with held sale?')) return;
    this.cart.set(h.cart);
    this.discount.set(h.discount);
    this.tenderMode.set(h.tenderMode);
    this.cashAmount.set(h.cashAmount);
    this.cardAmount.set(h.cardAmount);
    this.heldSales.update((list) => list.filter((x) => x.id !== h.id));
    this.persistHeldSales();
    this.showHolds.set(false);
    this.notice.set(`Loaded ${h.id}`);
    this.syncTenderToTotal();
    setTimeout(() => this.focusBarcode(), 30);
  }

  deleteHeld(h: HeldSale): void {
    this.heldSales.update((list) => list.filter((x) => x.id !== h.id));
    this.persistHeldSales();
  }

  heldTotal(h: HeldSale): number {
    const sub = h.cart.reduce((s, l) => s + l.qty * l.rate, 0);
    return Math.round(Math.max(0, sub - (h.discount || 0)) * 100) / 100;
  }

  /* —— Payment —— */
  openPayment(): void {
    if (this.saving() || this.showPay()) return;
    if (!this.openShift()) {
      this.error.set('Open a shift before selling.');
      this.showOpen.set(true);
      return;
    }
    if (!this.cart().length) {
      this.error.set('Cart is empty.');
      return;
    }
    this.syncTenderToTotal();
    this.cashReceived.set(this.grandTotal());
    this.showPay.set(true);
  }

  setCashReceived(v: number): void {
    this.cashReceived.set(Math.max(0, Number(v) || 0));
  }

  confirmPayment(): void {
    if (this.saving()) return;
    const total = this.grandTotal();
    const mode = this.tenderMode();
    if (mode === 'Cash') {
      if (this.cashReceived() + 0.001 < total) {
        this.error.set('Cash received is less than total.');
        return;
      }
      this.cashAmount.set(total);
      this.cardAmount.set(0);
      this.lastChange.set(this.changeDue());
    } else {
      this.cashAmount.set(0);
      this.cardAmount.set(total);
      this.lastChange.set(0);
    }
    this.showPay.set(false);
    this.completeSale();
  }

  cancelPay(): void {
    this.showPay.set(false);
    setTimeout(() => this.focusBarcode(), 30);
  }

  completeSale(): void {
    if (this.saving()) return;
    const shift = this.openShift();
    if (!shift) {
      this.error.set('Open a shift before selling.');
      return;
    }
    if (!this.cart().length) {
      this.error.set('Cart is empty.');
      return;
    }
    const total = this.grandTotal();
    const cash = Number(this.cashAmount()) || 0;
    const card = Number(this.cardAmount()) || 0;
    if (Math.abs(cash + card - total) > 0.01) {
      this.error.set('Cash + card must equal total.');
      return;
    }
    const lines = this.cart().map((l) => ({
      productId: l.productId,
      productName: l.productName,
      qty: l.qty,
      rate: l.rate,
      amount: Math.round(l.qty * l.rate * 100) / 100,
    }));
    const tenderLabel = this.tenderMode() === 'Other' ? 'Card' : this.tenderMode();
    const payments = [
      ...(cash > 0 ? [{ tender: 'Cash', amount: cash }] : []),
      ...(card > 0 ? [{ tender: tenderLabel, amount: card }] : []),
    ];
    const idempotencyKey =
      typeof crypto !== 'undefined' && 'randomUUID' in crypto
        ? crypto.randomUUID()
        : `${Date.now()}-${Math.random().toString(36).slice(2)}`;

    this.saving.set(true);
    this.error.set('');
    this.api
      .createSale({
        shiftId: shift.id,
        saleDate: this.today,
        discount: this.discountAmount(),
        idempotencyKey,
        lines,
        payments,
      })
      .subscribe({
        next: (sale) => {
          this.saving.set(false);
          this.lastSale.set(sale);
          this.cart.set([]);
          this.discount.set(0);
          this.syncTenderToTotal();
          this.reload();
          this.notice.set(`Sale ${sale.saleNo} posted.`);
          setTimeout(() => this.printReceipt(sale, this.lastChange()), 100);
          this.focusBarcode();
        },
        error: (e) => {
          this.saving.set(false);
          this.error.set(e?.error?.message || 'Sale failed');
          this.focusBarcode();
        },
      });
  }

  printReceipt(sale?: PosSale | null, change = 0): void {
    const s = sale ?? this.lastSale();
    if (!s) return;
    const w = window.open('', '_blank', 'width=320,height=640');
    if (!w) {
      this.error.set('Allow pop-ups for this site to print 80mm receipts.');
      this.focusBarcode();
      return;
    }
    const esc = (v: string) =>
      v.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    const pkr = (n: number) =>
      `Rs. ${Number(n || 0).toLocaleString('en-PK', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;
    const qtyFmt = (n: number) =>
      Number(n || 0).toLocaleString('en-PK', { minimumFractionDigits: 3, maximumFractionDigits: 3 });
    const term = this.activeTerminal();
    const shift = this.openShift();
    const lines = (s.lines || [])
      .map(
        (l) =>
          `<tr><td>${esc(l.productName || '')}</td><td class="n">${qtyFmt(l.qty)}</td><td class="n">${pkr(l.rate)}</td><td class="n">${pkr(l.amount)}</td></tr>`,
      )
      .join('');
    const pays = (s.payments || [])
      .map((p) => `<div class="row"><span>${esc(p.tender)}</span><span>${pkr(p.amount)}</span></div>`)
      .join('');
    const changeRow =
      change > 0 ? `<div class="row"><span>Change</span><span>${pkr(change)}</span></div>` : '';
    w.document.write(`<!doctype html><html><head><title>${esc(s.saleNo)}</title>
<style>
  @page { size: 80mm auto; margin: 3mm; }
  body { font-family: "Courier New", ui-monospace, Menlo, monospace; font-size: 12px; width: 74mm; margin: 0 auto; color: #111; }
  h1 { font-size: 15px; text-align: center; margin: 0 0 2px; }
  .muted { text-align: center; color: #333; margin-bottom: 8px; font-size: 11px; line-height: 1.35; }
  table { width: 100%; border-collapse: collapse; }
  th, td { padding: 2px 0; vertical-align: top; }
  th { border-bottom: 1px dashed #999; text-align: left; font-size: 10px; }
  .n { text-align: right; white-space: nowrap; }
  .row { display: flex; justify-content: space-between; margin-top: 2px; gap: 8px; }
  .tot { border-top: 1px dashed #999; margin-top: 6px; padding-top: 6px; font-weight: 700; font-size: 13px; }
  .thanks { text-align: center; margin-top: 10px; font-size: 11px; }
</style></head><body>
  <h1>${esc(SHOP_NAME)}</h1>
  <div class="muted">${esc(BRANCH_LABEL)}<br>${esc(s.saleNo)} · ${esc(term?.code || '')}<br>${esc(new Date(s.saleDate).toLocaleString())}<br>Cashier / ${esc(shift?.terminalCode || 'POS')}</div>
  <table><thead><tr><th>Item</th><th class="n">Qty</th><th class="n">Rate</th><th class="n">Amt</th></tr></thead>
  <tbody>${lines}</tbody></table>
  <div class="row"><span>Subtotal</span><span>${pkr(s.subTotal ?? 0)}</span></div>
  <div class="row"><span>Discount</span><span>${pkr(s.discount ?? 0)}</span></div>
  <div class="row tot"><span>TOTAL</span><span>${pkr(s.grandTotal ?? 0)}</span></div>
  ${pays}${changeRow}
  <div class="thanks">Thank you — visit again</div>
  <script>window.onload=()=>{try{window.focus();window.print();}catch(e){}setTimeout(()=>{try{window.close();}catch(e){}},500)}</script>
</body></html>`);
    w.document.close();
  }

  openReturn(sale: PosSale): void {
    if (sale.saleReturnId) return;
    this.returnSaleDraft.set(sale);
    this.returnReason.set('Customer return');
    const ids = new Set((sale.lines || []).map((l) => l.productId));
    this.returnLineIds.set(ids);
    this.showReturn.set(true);
  }

  toggleReturnLine(productId: string): void {
    this.returnLineIds.update((set) => {
      const next = new Set(set);
      if (next.has(productId)) next.delete(productId);
      else next.add(productId);
      return next;
    });
  }

  cancelReturn(): void {
    this.showReturn.set(false);
    this.returnSaleDraft.set(null);
  }

  confirmReturn(): void {
    const sale = this.returnSaleDraft();
    if (!sale) return;
    const selected = (sale.lines || []).filter((l) => this.returnLineIds().has(l.productId));
    if (!selected.length) {
      this.error.set('Select at least one line to return.');
      return;
    }
    const lines: PosSaleLine[] = selected.map((l) => ({
      productId: l.productId,
      productName: l.productName,
      qty: l.qty,
      rate: l.rate,
      amount: l.amount,
    }));
    const full = selected.length === (sale.lines || []).length;
    this.api
      .returnSale(sale.id, {
        remarks: this.returnReason(),
        lines: full ? undefined : lines,
      })
      .subscribe({
        next: () => {
          this.showReturn.set(false);
          this.returnSaleDraft.set(null);
          this.notice.set(`Returned ${sale.saleNo}`);
          this.reload();
        },
        error: (e) => this.error.set(e?.error?.message || 'Return failed'),
      });
  }

  loadReports(): void {
    const d = this.reportDate();
    this.api.dailySales(d).subscribe({
      next: (r) => this.dailyReport.set(r),
      error: () => this.error.set('Could not load daily sales'),
    });
    this.api.ownerProfit(d).subscribe({
      next: (r) => this.profitReport.set(r),
      error: () => undefined,
    });
  }

  cancelOpen(): void {
    this.showOpen.set(false);
    setTimeout(() => this.focusBarcode(), 30);
  }

  cancelClose(): void {
    this.showClose.set(false);
    setTimeout(() => this.focusBarcode(), 30);
  }

  saveOpen(): void {
    if (this.openForm.invalid) return;
    this.saving.set(true);
    this.api.openShift(this.openForm.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.showOpen.set(false);
        this.reload();
        setTimeout(() => this.focusBarcode(), 50);
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(e?.error?.message || 'Failed');
      },
    });
  }

  openCloseDialog(): void {
    const s = this.openShift();
    if (!s) return;
    this.recon.set(null);
    this.api.shiftReconciliation(s.id).subscribe({
      next: (r) => {
        this.recon.set(r);
        this.closeActualCash.set(r.expectedCash);
        this.showClose.set(true);
      },
      error: () => {
        this.closeActualCash.set(s.openingCash);
        this.showClose.set(true);
      },
    });
  }

  confirmClose(): void {
    const s = this.openShift();
    if (!s) return;
    this.api.closeShift(s.id, Number(this.closeActualCash()) || 0).subscribe({
      next: (closed) => {
        this.showClose.set(false);
        this.recon.set(null);
        this.reload();
        this.notice.set(
          `Shift closed. Expected ${closed.expectedCash ?? 0}, variance ${closed.variance ?? 0}`,
        );
        setTimeout(() => this.focusBarcode(), 50);
      },
      error: (e) => this.error.set(e?.error?.message || 'Failed'),
    });
  }

  lineAmount(l: CartLine): number {
    return Math.round(l.qty * l.rate * 100) / 100;
  }

  initials(name: string): string {
    return (name || '?')
      .split(/\s+/)
      .slice(0, 2)
      .map((w) => w[0]?.toUpperCase() || '')
      .join('');
  }

  fmtQty(n: number): string {
    return Number(n || 0).toLocaleString('en-PK', {
      minimumFractionDigits: 3,
      maximumFractionDigits: 3,
    });
  }
}
