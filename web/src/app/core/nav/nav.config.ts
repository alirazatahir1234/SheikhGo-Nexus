export interface NavItem {
  id: string;
  label: string;
  icon: string;
  route?: string;
  children?: NavItem[];
  /** Any of these permissions grants visibility. Empty = always visible when authenticated. */
  permissions?: string[];
  /** Tenant feature modules required (any-of). Empty = always when perms pass. */
  modules?: string[];
  /** Badge provider key (see NavBadgeService). */
  badgeKey?: string;
  /** Section header when rendering flat/top groups */
  section?: string;
}

/** Curated two-level navigation — single source for sidebar, breadcrumbs, ⌘K. */
export const NAV_TREE: NavItem[] = [
  {
    id: 'overview',
    label: 'Overview',
    icon: 'layout-dashboard',
    section: 'Overview',
    children: [
      { id: 'dashboard', label: 'Dashboard', icon: 'layout-dashboard', route: '/dashboard' },
      { id: 'store', label: 'Online store', icon: 'shopping-bag', route: '/shop' },
      { id: 'ai', label: 'AI Assistant', icon: 'sparkles', route: '/ai', permissions: ['Ai.Chat.View'] },
      { id: 'reports', label: 'Reports', icon: 'bar-chart-3', route: '/reports', permissions: ['Reports.Executive.View', 'Reports.Finance.View', 'Reports.Sales.View', 'Reports.Procurement.View', 'Reports.Inventory.View', 'Finance.Reports.View', 'Inventory.Reports.View'] },
    ],
  },
  {
    id: 'operations',
    label: 'Operations',
    icon: 'factory',
    section: 'Operations',
    children: [
      {
        id: 'production',
        label: 'Production',
        icon: 'factory',
        permissions: ['Production.BOM.View', 'Production.Order.View'],
        modules: ['production'],
        children: [
          { id: 'production-dashboard', label: 'Dashboard', icon: 'layout-dashboard', route: '/production/dashboard' },
          { id: 'production-orders', label: 'Orders', icon: 'clipboard-list', route: '/production/orders' },
          { id: 'production-boms', label: 'BOMs', icon: 'layers', route: '/production/boms' },
          { id: 'production-costing', label: 'Costing', icon: 'calculator', route: '/production/costing' },
        ],
      },
      {
        id: 'inventory',
        label: 'Inventory',
        icon: 'boxes',
        permissions: ['Inventory.Product.View'],
        modules: ['inventory'],
        children: [
          { id: 'inventory-dashboard', label: 'Dashboard', icon: 'layout-dashboard', route: '/inventory/dashboard' },
          { id: 'inventory-stock', label: 'Stock Balance', icon: 'package', route: '/inventory/stock' },
          { id: 'inventory-ledger', label: 'Ledger', icon: 'book-open', route: '/inventory/ledger' },
          { id: 'inventory-transfers', label: 'Transfers', icon: 'arrow-left-right', route: '/inventory/transfers' },
          { id: 'inventory-adjustments', label: 'Adjustments', icon: 'sliders-horizontal', route: '/inventory/adjustments' },
          { id: 'inventory-counts', label: 'Counts', icon: 'list-checks', route: '/inventory/counts' },
        ],
      },
      {
        id: 'quality',
        label: 'Quality',
        icon: 'flask-conical',
        route: '/lab/dashboard',
        permissions: ['Lab.TestType.View', 'Lab.Sample.View', 'Lab.Result.View'],
        modules: ['quality'],
        children: [
          { id: 'lab-dashboard', label: 'Dashboard', icon: 'layout-dashboard', route: '/lab/dashboard' },
          { id: 'lab-types', label: 'Test Types', icon: 'list', route: '/lab/types' },
          { id: 'lab-samples', label: 'Samples', icon: 'test-tube', route: '/lab/samples' },
          { id: 'lab-results', label: 'Results', icon: 'file-check', route: '/lab/results' },
        ],
      },
      {
        id: 'farm',
        label: 'Farm',
        icon: 'wheat',
        permissions: ['Farm.View', 'Flock.View'],
        modules: ['farm'],
        children: [
          { id: 'farm-dashboard', label: 'Dashboard', icon: 'layout-dashboard', route: '/farm/dashboard' },
          { id: 'farm-farms', label: 'Farms', icon: 'warehouse', route: '/farm/farms' },
          { id: 'farm-flocks', label: 'Flocks', icon: 'bird', route: '/farm/flocks' },
          { id: 'farm-feed', label: 'Feed', icon: 'utensils', route: '/farm/feed' },
          { id: 'farm-mortality', label: 'Mortality', icon: 'activity', route: '/farm/mortality' },
          { id: 'farm-expenses', label: 'Expenses', icon: 'wallet', route: '/farm/expenses' },
          { id: 'farm-performance', label: 'Performance', icon: 'trending-up', route: '/farm/performance' },
        ],
      },
      {
        id: 'ops',
        label: 'Operations',
        icon: 'truck',
        permissions: ['Operations.Vehicle.View', 'Operations.GatePass.View'],
        modules: ['operations'],
        children: [
          { id: 'ops-vehicles', label: 'Vehicles', icon: 'truck', route: '/operations/vehicles' },
          { id: 'ops-drivers', label: 'Drivers', icon: 'user', route: '/operations/drivers' },
          { id: 'ops-gate', label: 'Gate Passes', icon: 'door-open', route: '/operations/gate-passes' },
        ],
      },
      {
        id: 'weighbridge',
        label: 'Weighbridge',
        icon: 'scale',
        route: '/weighbridge',
        permissions: ['Weighbridge.Ticket.View'],
        modules: ['weighbridge'],
        badgeKey: 'weighbridgeOpen',
      },
    ],
  },
  {
    id: 'commercial',
    label: 'Commercial',
    icon: 'shopping-cart',
    section: 'Commercial',
    children: [
      {
        id: 'sales',
        label: 'Sales',
        icon: 'shopping-cart',
        permissions: ['Sales.Order.View', 'Sales.Delivery.View', 'Sales.Invoice.View', 'Sales.Customer.View'],
        modules: ['sales'],
        children: [
          { id: 'sales-customers', label: 'Customers', icon: 'users', route: '/sales/customers' },
          { id: 'sales-orders', label: 'Orders', icon: 'file-text', route: '/sales/orders' },
          { id: 'sales-do', label: 'Delivery Orders', icon: 'truck', route: '/sales/delivery-orders' },
          { id: 'sales-invoices', label: 'Invoices', icon: 'receipt', route: '/sales/invoices' },
          { id: 'sales-payments', label: 'Payments', icon: 'banknote', route: '/sales/payments' },
        ],
      },
      {
        id: 'purchase',
        label: 'Purchase',
        icon: 'package-plus',
        permissions: ['Procurement.PurchaseOrder.View', 'Procurement.GoodsReceipt.View', 'Procurement.PurchaseInvoice.View'],
        modules: ['purchase'],
        children: [
          { id: 'purchase-orders', label: 'Orders', icon: 'file-text', route: '/purchase/orders' },
          { id: 'purchase-grn', label: 'Goods Receipts', icon: 'package-check', route: '/purchase/goods-receipts' },
          { id: 'purchase-invoices', label: 'Invoices', icon: 'receipt', route: '/purchase/invoices' },
          { id: 'purchase-payments', label: 'Payments', icon: 'banknote', route: '/purchase/payments' },
        ],
      },
      {
        id: 'pos',
        label: 'POS',
        icon: 'store',
        permissions: ['POS.Sale.View', 'POS.Shift.View'],
        modules: ['pos'],
        children: [
          { id: 'pos-terminal', label: 'Terminal', icon: 'shopping-bag', route: '/pos/terminal' },
          { id: 'pos-sales', label: 'Sales', icon: 'receipt', route: '/pos/sales' },
          { id: 'pos-shifts', label: 'Shifts', icon: 'clock', route: '/pos/shifts' },
          { id: 'pos-reports', label: 'Reports', icon: 'bar-chart-3', route: '/pos/reports' },
        ],
      },
    ],
  },
  {
    id: 'finance-group',
    label: 'Finance',
    icon: 'landmark',
    section: 'Finance',
    children: [
      {
        id: 'finance',
        label: 'Accounting',
        icon: 'landmark',
        permissions: ['Finance.Account.View', 'Finance.Journal.View', 'Finance.Ledger.View', 'Finance.Reports.View'],
        modules: ['finance'],
        children: [
          { id: 'finance-dashboard', label: 'Dashboard', icon: 'layout-dashboard', route: '/finance/dashboard' },
          { id: 'finance-coa', label: 'Chart of Accounts', icon: 'list-tree', route: '/finance/chart-of-accounts' },
          { id: 'finance-journals', label: 'Journals', icon: 'book', route: '/finance/journals' },
          { id: 'finance-gl', label: 'General Ledger', icon: 'book-open', route: '/finance/general-ledger' },
          { id: 'finance-cash', label: 'Cash / Bank', icon: 'wallet', route: '/finance/cash-bank' },
          { id: 'finance-tb', label: 'Trial Balance', icon: 'scale', route: '/finance/trial-balance' },
          { id: 'finance-pl', label: 'P&L', icon: 'trending-up', route: '/finance/profit-loss' },
          { id: 'finance-bs', label: 'Balance Sheet', icon: 'pie-chart', route: '/finance/balance-sheet' },
        ],
      },
    ],
  },
  {
    id: 'people',
    label: 'People',
    icon: 'users',
    section: 'People',
    children: [
      {
        id: 'hr',
        label: 'HR & Payroll',
        icon: 'users',
        permissions: ['HR.Employee.View', 'HR.Attendance.View', 'HR.Leave.View', 'HR.Payroll.View'],
        modules: ['hr'],
        children: [
          { id: 'hr-dashboard', label: 'Dashboard', icon: 'layout-dashboard', route: '/hr/dashboard' },
          { id: 'hr-employees', label: 'Employees', icon: 'user', route: '/hr/employees' },
          { id: 'hr-attendance', label: 'Attendance', icon: 'calendar-check', route: '/hr/attendance' },
          { id: 'hr-leave', label: 'Leave', icon: 'palm-tree', route: '/hr/leave' },
          { id: 'hr-payroll', label: 'Payroll', icon: 'banknote', route: '/hr/payroll' },
        ],
      },
    ],
  },
  {
    id: 'setup',
    label: 'Setup',
    icon: 'settings',
    section: 'Setup',
    children: [
      { id: 'admin-companies', label: 'Companies', icon: 'building-2', route: '/administration/companies', permissions: ['Admin.Company.View'] },
      { id: 'admin-branches', label: 'Branches', icon: 'git-branch', route: '/administration/branches', permissions: ['Admin.Branch.View'] },
      { id: 'admin-users', label: 'Users', icon: 'user-cog', route: '/administration/users', permissions: ['Admin.User.Edit'] },
      { id: 'admin-roles', label: 'Roles', icon: 'shield', route: '/administration/roles', permissions: ['Admin.Role.View'] },
      { id: 'admin-fy', label: 'Financial Years', icon: 'calendar', route: '/administration/financial-years', permissions: ['Admin.FinancialYear.View'] },
      { id: 'admin-audit', label: 'Audit Log', icon: 'scroll-text', route: '/administration/audit', permissions: ['Admin.Audit.View'] },
      { id: 'admin-doctypes', label: 'Document Types', icon: 'file-stack', route: '/administration/document-types', permissions: ['Admin.DocumentType.View'] },
    ],
  },
];

/** Default modules enabled when tenant has no flags stored. */
export const DEFAULT_ENABLED_MODULES = [
  'production',
  'inventory',
  'quality',
  'farm',
  'operations',
  'weighbridge',
  'sales',
  'purchase',
  'pos',
  'finance',
  'hr',
];
