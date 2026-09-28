export type AccountType = 'Asset' | 'Liability' | 'Equity' | 'Revenue' | 'Expense';
export type PartyType = 'Customer' | 'Supplier' | 'Both';

export interface Product {
  id: string;
  code: string;
  name: string;
  category: string;
  unit: string;
  salePrice: number;
  purchasePrice: number;
  stockQty: number;
  reorderLevel: number;
  isActive?: boolean;
  isRawMaterial?: boolean;
  isFinishedGood?: boolean;
}

export interface Account {
  id: string;
  code: string;
  name: string;
  accountType: AccountType;
  parentId?: string | null;
  isPostable: boolean;
  balance: number;
  level?: number;
}

export interface Party {
  id: string;
  code: string;
  name: string;
  partyType: PartyType;
  phone: string;
  email: string;
  city: string;
  address: string;
  creditLimit: number;
  openingBalance: number;
  isActive?: boolean;
}

export type VoucherType =
  | 'JV'
  | 'CP'
  | 'CR'
  | 'BP'
  | 'BR'
  | 'CpFreight'
  | 'CpAd'
  | 'BpAd'
  | 'CpSl'
  | 'BpSl'
  | 'DoSales'
  | 'JvRm'
  | 'JvAdj'
  | 'StkTrf'
  | 'Sr'
  | 'Pr'
  | 'CpPaid';

export interface VoucherLine {
  accountId: string;
  accountName?: string;
  debit: number;
  credit: number;
  remarks?: string;
}

export interface Voucher {
  id: string;
  voucherNo: string;
  voucherType: VoucherType;
  date: string;
  narration: string;
  lines: VoucherLine[];
  totalDebit: number;
  totalCredit: number;
  status: string;
}

export interface InvoiceLine {
  productId: string;
  productName?: string;
  qty: number;
  rate: number;
  amount: number;
}

export interface SaleInvoice {
  id: string;
  invoiceNo: string;
  date: string;
  partyId: string;
  partyName?: string;
  lines: InvoiceLine[];
  subTotal: number;
  discount: number;
  tax: number;
  grandTotal: number;
  status: string;
}

export interface PurchaseInvoice {
  id: string;
  invoiceNo: string;
  date: string;
  partyId: string;
  partyName?: string;
  lines: InvoiceLine[];
  subTotal: number;
  discount: number;
  tax: number;
  grandTotal: number;
  status: string;
}

export interface StockItem {
  productId: string;
  code: string;
  name: string;
  category: string;
  unit: string;
  stockQty: number;
  reorderLevel: number;
  avgCost?: number;
  stockValue?: number;
}

export interface Employee {
  id: string;
  code: string;
  name: string;
  designation: string;
  department: string;
  joinDate: string;
  basicSalary: number;
  phone: string;
}

export interface Company {
  id: string;
  name: string;
  shortName: string;
  address: string;
  city: string;
  country: string;
  phone: string;
  email: string;
  currency: string;
  ntn: string;
  industry?: number;
  defaultSaleTaxPercent?: number;
  defaultPurchaseTaxPercent?: number;
  syncToFbr?: boolean;
  taxModule?: boolean;
}

export interface StockAdjustment {
  id: string;
  adjustmentNo: string;
  date: string;
  productId: string;
  productName?: string;
  qtyChange: number;
  reason: string;
  remarks?: string;
}

export interface RecentVoucher {
  id: string;
  voucherNo: string;
  voucherType: string;
  date: string;
  narration: string;
  totalDebit: number;
  status: string;
}

export interface DashboardSummary {
  salesToday: number;
  purchasesToday: number;
  inventoryValue?: number;
  receivables: number;
  payables: number;
  recentVouchers: RecentVoucher[];
  pendingApprovals?: PendingApprovalItem[];
  recentDocuments?: RecentDocumentItem[];
}

export interface PendingApprovalItem {
  id: string;
  documentNo: string;
  documentKind: string;
  status: string;
  date: string;
  partyName?: string;
  amount: number;
}

export interface RecentDocumentItem {
  id: string;
  documentNo: string;
  documentKind: string;
  status: string;
  date: string;
  partyName?: string;
  amount: number;
}
