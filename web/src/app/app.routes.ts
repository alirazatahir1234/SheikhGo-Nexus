import { Routes } from '@angular/router';
import { authGuard, guestGuard, permissionGuard } from './core/guards/auth.guard';
import { tabChildren } from './core/nav/route-tabs';

const productionLoad = () =>
  import('./features/production/production.component') as Promise<Record<string, import('@angular/core').Type<unknown>>>;
const financeLoad = () =>
  import('./features/finance/finance.component') as Promise<Record<string, import('@angular/core').Type<unknown>>>;
const salesLoad = () =>
  import('./features/sales/sales.component') as Promise<Record<string, import('@angular/core').Type<unknown>>>;
const purchaseLoad = () =>
  import('./features/purchase/purchase.component') as Promise<Record<string, import('@angular/core').Type<unknown>>>;
const inventoryLoad = () =>
  import('./features/inventory/inventory.component') as Promise<Record<string, import('@angular/core').Type<unknown>>>;
const hrLoad = () =>
  import('./features/hr/hr.component') as Promise<Record<string, import('@angular/core').Type<unknown>>>;
const farmLoad = () =>
  import('./features/farm/farm.component') as Promise<Record<string, import('@angular/core').Type<unknown>>>;
const labLoad = () =>
  import('./features/lab/lab.component') as Promise<Record<string, import('@angular/core').Type<unknown>>>;
const operationsLoad = () =>
  import('./features/operations/operations.component') as Promise<Record<string, import('@angular/core').Type<unknown>>>;
const posLoad = () =>
  import('./features/pos/pos.component') as Promise<Record<string, import('@angular/core').Type<unknown>>>;
const brandsLoad = () =>
  import('./features/brands/brands.component') as Promise<Record<string, import('@angular/core').Type<unknown>>>;
const chequesLoad = () =>
  import('./features/cheques/cheques.component') as Promise<Record<string, import('@angular/core').Type<unknown>>>;
const returnsLoad = () =>
  import('./features/returns/returns.component') as Promise<Record<string, import('@angular/core').Type<unknown>>>;
const salesforceLoad = () =>
  import('./features/salesforce/salesforce.component') as Promise<Record<string, import('@angular/core').Type<unknown>>>;

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./layout/shell/shell.component').then((m) => m.ShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'store',
        loadComponent: () =>
          import('./features/storefront/storefront.component').then((m) => m.StorefrontComponent),
      },
      {
        path: 'ai',
        canActivate: [permissionGuard],
        data: { permission: 'Ai.Chat.View' },
        loadComponent: () => import('./features/ai/ai.component').then((m) => m.AiComponent),
      },
      {
        path: 'coming-soon/:domain',
        loadComponent: () =>
          import('./features/coming-soon/coming-soon.component').then((m) => m.ComingSoonComponent),
      },
      {
        path: 'administration/companies',
        canActivate: [permissionGuard],
        data: { permission: 'Admin.Company.View' },
        loadComponent: () =>
          import('./features/administration/companies/companies.component').then(
            (m) => m.AdminCompaniesComponent,
          ),
      },
      {
        path: 'administration/branches',
        canActivate: [permissionGuard],
        data: { permission: 'Admin.Branch.View' },
        loadComponent: () =>
          import('./features/administration/branches/branches.component').then(
            (m) => m.AdminBranchesComponent,
          ),
      },
      {
        path: 'administration/users',
        canActivate: [permissionGuard],
        data: { permission: 'Admin.User.Edit' },
        loadComponent: () =>
          import('./features/administration/users/admin-users.component').then(
            (m) => m.AdminUsersComponent,
          ),
      },
      {
        path: 'administration/roles',
        canActivate: [permissionGuard],
        data: { permission: 'Admin.Role.View' },
        loadComponent: () =>
          import('./features/administration/roles/roles.component').then(
            (m) => m.AdminRolesComponent,
          ),
      },
      {
        path: 'administration/financial-years',
        canActivate: [permissionGuard],
        data: { permission: 'Admin.FinancialYear.View' },
        loadComponent: () =>
          import('./features/administration/financial-years/financial-years.component').then(
            (m) => m.AdminFinancialYearsComponent,
          ),
      },
      {
        path: 'administration/audit',
        canActivate: [permissionGuard],
        data: { permission: 'Admin.Audit.View' },
        loadComponent: () =>
          import('./features/administration/audit/audit.component').then(
            (m) => m.AdminAuditComponent,
          ),
      },
      {
        path: 'administration/document-types',
        canActivate: [permissionGuard],
        data: { permission: 'Admin.DocumentType.View' },
        loadComponent: () =>
          import('./features/administration/document-types/document-types.component').then(
            (m) => m.AdminDocumentTypesComponent,
          ),
      },
      {
        path: 'purchase',
        canActivate: [permissionGuard],
        data: {
          permissions: [
            'Procurement.PurchaseOrder.View',
            'Procurement.GoodsReceipt.View',
            'Procurement.PurchaseInvoice.View',
            'Finance.SupplierPayment.View',
          ],
          permissionMode: 'any',
        },
        children: tabChildren(
          purchaseLoad,
          'PurchaseComponent',
          [
            { path: 'orders', tab: 'orders' },
            { path: 'orders/:id', tab: 'orders' },
            { path: 'goods-receipts', tab: 'receipts' },
            { path: 'goods-receipts/:id', tab: 'receipts' },
            { path: 'invoices', tab: 'invoices' },
            { path: 'invoices/:id', tab: 'invoices' },
            { path: 'payments', tab: 'payments' },
          ],
          'orders',
        ),
      },
      {
        path: 'sales',
        canActivate: [permissionGuard],
        data: {
          permissions: [
            'Sales.Customer.View',
            'Sales.Order.View',
            'Sales.Delivery.View',
            'Sales.Invoice.View',
            'Sales.Payment.View',
          ],
          permissionMode: 'any',
        },
        children: tabChildren(
          salesLoad,
          'SalesComponent',
          [
            { path: 'customers', tab: 'customers' },
            { path: 'orders', tab: 'orders' },
            { path: 'orders/:id', tab: 'orders' },
            { path: 'delivery-orders', tab: 'deliveries' },
            { path: 'delivery-orders/:id', tab: 'deliveries' },
            { path: 'invoices', tab: 'invoices' },
            { path: 'invoices/:id', tab: 'invoices' },
            { path: 'payments', tab: 'payments' },
          ],
          'orders',
        ),
      },
      {
        path: 'finance',
        canActivate: [permissionGuard],
        data: {
          permissions: [
            'Finance.Account.View',
            'Finance.Journal.View',
            'Finance.Ledger.View',
            'Finance.Reports.View',
            'Finance.CashBank.View',
            'Finance.Period.View',
            'Finance.SupplierPayment.View',
          ],
          permissionMode: 'any',
        },
        children: tabChildren(
          financeLoad,
          'FinanceComponent',
          [
            { path: 'dashboard', tab: 'dashboard' },
            { path: 'chart-of-accounts', tab: 'accounts' },
            { path: 'journals', tab: 'journals' },
            { path: 'general-ledger', tab: 'ledger' },
            { path: 'cash-bank', tab: 'cashbank' },
            { path: 'periods', tab: 'periods' },
            { path: 'trial-balance', tab: 'trial' },
            { path: 'profit-loss', tab: 'pl' },
            { path: 'balance-sheet', tab: 'bs' },
            { path: 'supplier-payments', tab: 'supplierPayments' },
          ],
          'dashboard',
        ),
      },
      {
        path: 'inventory',
        canActivate: [permissionGuard],
        data: {
          permissions: [
            'Inventory.Product.View',
            'Inventory.Warehouse.View',
            'Inventory.Transfer.View',
            'Inventory.Adjustment.View',
            'Inventory.StockCount.View',
            'Inventory.Reports.View',
          ],
          permissionMode: 'any',
        },
        children: tabChildren(
          inventoryLoad,
          'InventoryComponent',
          [
            { path: 'dashboard', tab: 'dashboard' },
            { path: 'stock', tab: 'stock' },
            { path: 'ledger', tab: 'ledger' },
            { path: 'transfers', tab: 'transfers' },
            { path: 'transfers/:id', tab: 'transfers' },
            { path: 'adjustments', tab: 'adjustments' },
            { path: 'adjustments/:id', tab: 'adjustments' },
            { path: 'counts', tab: 'counts' },
            { path: 'counts/:id', tab: 'counts' },
            { path: 'valuation', tab: 'valuation' },
          ],
          'stock',
        ),
      },
      {
        path: 'hr',
        canActivate: [permissionGuard],
        data: {
          permissions: [
            'HR.Employee.View',
            'HR.Attendance.View',
            'HR.Leave.View',
            'HR.Payroll.View',
          ],
          permissionMode: 'any',
        },
        children: tabChildren(
          hrLoad,
          'HrComponent',
          [
            { path: 'dashboard', tab: 'dashboard' },
            { path: 'employees', tab: 'employees' },
            { path: 'departments', tab: 'departments' },
            { path: 'designations', tab: 'designations' },
            { path: 'attendance', tab: 'attendance' },
            { path: 'leave', tab: 'leave' },
            { path: 'periods', tab: 'periods' },
            { path: 'payroll', tab: 'payroll' },
            { path: 'payroll/:id', tab: 'runDetail' },
          ],
          'dashboard',
        ),
      },
      {
        path: 'parties',
        loadComponent: () =>
          import('./features/parties/parties.component').then((m) => m.PartiesComponent),
      },
      {
        path: 'products',
        loadComponent: () =>
          import('./features/products/products.component').then((m) => m.ProductsComponent),
      },
      {
        path: 'warehouses',
        loadComponent: () =>
          import('./features/warehouses/warehouses.component').then((m) => m.WarehousesComponent),
      },
      {
        path: 'settlements',
        loadComponent: () =>
          import('./features/settlements/settlements.component').then((m) => m.SettlementsComponent),
      },
      {
        path: 'contracts',
        loadComponent: () =>
          import('./features/contracts/contracts.component').then((m) => m.ContractsComponent),
      },
      {
        path: 'lab',
        canActivate: [permissionGuard],
        data: {
          permissions: ['Lab.TestType.View', 'Lab.Sample.View', 'Lab.Result.View'],
          permissionMode: 'any',
        },
        children: tabChildren(
          labLoad,
          'LabComponent',
          [
            { path: 'dashboard', tab: 'dashboard' },
            { path: 'types', tab: 'types' },
            { path: 'samples', tab: 'samples' },
            { path: 'samples/:id', tab: 'samples' },
            { path: 'results', tab: 'results' },
          ],
          'dashboard',
        ),
      },
      { path: 'quality', redirectTo: 'lab', pathMatch: 'full' },
      {
        path: 'cost-centres',
        loadComponent: () =>
          import('./features/cost-centres/cost-centres.component').then((m) => m.CostCentresComponent),
      },
      {
        path: 'cheques',
        children: tabChildren(
          chequesLoad,
          'ChequesComponent',
          [
            { path: 'pdc', tab: 'pdc' },
            { path: 'books', tab: 'books' },
          ],
          'pdc',
        ),
      },
      {
        path: 'salesforce',
        children: tabChildren(
          salesforceLoad,
          'SalesforceComponent',
          [
            { path: 'salesmen', tab: 'salesmen' },
            { path: 'areas', tab: 'areas' },
            { path: 'targets', tab: 'targets' },
          ],
          'salesmen',
        ),
      },
      {
        path: 'demands',
        loadComponent: () =>
          import('./features/demands/demands.component').then((m) => m.DemandsComponent),
      },
      { path: 'hr-finance', redirectTo: 'hr', pathMatch: 'full' },
      {
        path: 'brands',
        children: tabChildren(
          brandsLoad,
          'BrandsComponent',
          [
            { path: 'list', tab: 'brands' },
            { path: 'conversions', tab: 'conversions' },
          ],
          'list',
        ),
      },
      {
        path: 'budgets',
        loadComponent: () =>
          import('./features/budgets/budgets.component').then((m) => m.BudgetsComponent),
      },
      { path: 'attendance', redirectTo: 'hr/attendance', pathMatch: 'full' },
      {
        path: 'day-close',
        loadComponent: () =>
          import('./features/day-close/day-close.component').then((m) => m.DayCloseComponent),
      },
      {
        path: 'weighbridge',
        canActivate: [permissionGuard],
        data: { permissions: ['Weighbridge.Ticket.View'], permissionMode: 'any' },
        loadComponent: () =>
          import('./features/weighbridge/weighbridge.component').then((m) => m.WeighbridgeComponent),
      },
      {
        path: 'operations',
        canActivate: [permissionGuard],
        data: {
          permissions: ['Operations.Vehicle.View', 'Operations.GatePass.View'],
          permissionMode: 'any',
        },
        children: tabChildren(
          operationsLoad,
          'OperationsComponent',
          [
            { path: 'vehicles', tab: 'vehicles' },
            { path: 'drivers', tab: 'drivers' },
            { path: 'gate-passes', tab: 'gatepasses' },
          ],
          'vehicles',
        ),
      },
      {
        path: 'pos',
        canActivate: [permissionGuard],
        data: { permissions: ['POS.Sale.View', 'POS.Shift.View'], permissionMode: 'any' },
        children: tabChildren(
          posLoad,
          'PosComponent',
          [
            { path: 'terminal', tab: 'terminal' },
            { path: 'sales', tab: 'sales' },
            { path: 'shifts', tab: 'shifts' },
            { path: 'reports', tab: 'reports' },
          ],
          'terminal',
        ),
      },
      {
        path: 'stock-transfer',
        loadComponent: () =>
          import('./features/stock-transfer/stock-transfer.component').then((m) => m.StockTransferComponent),
      },
      {
        path: 'returns',
        children: tabChildren(
          returnsLoad,
          'ReturnsComponent',
          [
            { path: 'sale', tab: 'sale' },
            { path: 'purchase', tab: 'purchase' },
          ],
          'sale',
        ),
      },
      { path: 'leaves', redirectTo: 'hr/leave', pathMatch: 'full' },
      {
        path: 'production',
        canActivate: [permissionGuard],
        data: {
          permissions: ['Production.BOM.View', 'Production.Order.View'],
          permissionMode: 'any',
        },
        children: tabChildren(
          productionLoad,
          'ProductionComponent',
          [
            { path: 'dashboard', tab: 'dashboard' },
            { path: 'boms', tab: 'boms' },
            { path: 'boms/:id', tab: 'boms' },
            { path: 'orders', tab: 'orders' },
            { path: 'orders/:id', tab: 'detail' },
            { path: 'costing', tab: 'costing' },
          ],
          'dashboard',
        ),
      },
      {
        path: 'farm',
        canActivate: [permissionGuard],
        data: {
          permissions: ['Farm.View', 'Flock.View'],
          permissionMode: 'any',
        },
        children: tabChildren(
          farmLoad,
          'FarmComponent',
          [
            { path: 'dashboard', tab: 'dashboard' },
            { path: 'farms', tab: 'farms' },
            { path: 'flocks', tab: 'flocks' },
            { path: 'flocks/:id', tab: 'flocks' },
            { path: 'feed', tab: 'feed' },
            { path: 'mortality', tab: 'mortality' },
            { path: 'expenses', tab: 'expenses' },
            { path: 'performance', tab: 'performance' },
          ],
          'dashboard',
        ),
      },
      { path: 'payroll', redirectTo: 'hr/payroll', pathMatch: 'full' },
      {
        path: 'users',
        loadComponent: () =>
          import('./features/users/users.component').then((m) => m.UsersComponent),
      },
      {
        path: 'reports',
        canActivate: [permissionGuard],
        data: {
          permissions: [
            'Reports.Executive.View',
            'Reports.Finance.View',
            'Reports.Sales.View',
            'Reports.Procurement.View',
            'Reports.Inventory.View',
            'Reports.HR.View',
            'Reports.Production.View',
            'Reports.Farm.View',
            'Reports.Quality.View',
            'Reports.Operations.View',
            'Reports.POS.View',
            'Finance.Reports.View',
            'Inventory.Reports.View',
          ],
          permissionMode: 'any',
        },
        loadComponent: () =>
          import('./features/reports/reports.component').then((m) => m.ReportsComponent),
      },
      {
        path: 'platform/tenants',
        loadComponent: () =>
          import('./features/platform/tenants/tenants.component').then((m) => m.TenantsComponent),
      },
      {
        path: 'settings/company',
        loadComponent: () =>
          import('./features/settings/company/company.component').then(
            (m) => m.CompanySettingsComponent,
          ),
      },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
