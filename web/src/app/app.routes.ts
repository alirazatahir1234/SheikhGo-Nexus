import { Routes } from '@angular/router';
import { authGuard, guestGuard, permissionGuard } from './core/guards/auth.guard';

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
          ],
          permissionMode: 'any',
        },
        loadComponent: () =>
          import('./features/purchase/purchase.component').then((m) => m.PurchaseComponent),
      },
      {
        path: 'inventory',
        canActivate: [permissionGuard],
        data: { permission: 'Inventory.Product.View' },
        loadComponent: () =>
          import('./features/inventory/inventory.component').then((m) => m.InventoryComponent),
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
        loadComponent: () =>
          import('./features/lab/lab.component').then((m) => m.LabComponent),
      },
      {
        path: 'cost-centres',
        loadComponent: () =>
          import('./features/cost-centres/cost-centres.component').then((m) => m.CostCentresComponent),
      },
      {
        path: 'cheques',
        loadComponent: () =>
          import('./features/cheques/cheques.component').then((m) => m.ChequesComponent),
      },
      {
        path: 'salesforce',
        loadComponent: () =>
          import('./features/salesforce/salesforce.component').then((m) => m.SalesforceComponent),
      },
      {
        path: 'demands',
        loadComponent: () =>
          import('./features/demands/demands.component').then((m) => m.DemandsComponent),
      },
      {
        path: 'hr-finance',
        loadComponent: () =>
          import('./features/hr-finance/hr-finance.component').then((m) => m.HrFinanceComponent),
      },
      {
        path: 'brands',
        loadComponent: () =>
          import('./features/brands/brands.component').then((m) => m.BrandsComponent),
      },
      {
        path: 'budgets',
        loadComponent: () =>
          import('./features/budgets/budgets.component').then((m) => m.BudgetsComponent),
      },
      {
        path: 'attendance',
        loadComponent: () =>
          import('./features/attendance/attendance.component').then((m) => m.AttendanceComponent),
      },
      {
        path: 'day-close',
        loadComponent: () =>
          import('./features/day-close/day-close.component').then((m) => m.DayCloseComponent),
      },
      {
        path: 'weighbridge',
        loadComponent: () =>
          import('./features/weighbridge/weighbridge.component').then((m) => m.WeighbridgeComponent),
      },
      {
        path: 'pos',
        loadComponent: () =>
          import('./features/pos/pos.component').then((m) => m.PosComponent),
      },
      {
        path: 'stock-transfer',
        loadComponent: () =>
          import('./features/stock-transfer/stock-transfer.component').then((m) => m.StockTransferComponent),
      },
      {
        path: 'returns',
        loadComponent: () =>
          import('./features/returns/returns.component').then((m) => m.ReturnsComponent),
      },
      {
        path: 'leaves',
        loadComponent: () =>
          import('./features/leaves/leaves.component').then((m) => m.LeavesComponent),
      },
      {
        path: 'production',
        loadComponent: () =>
          import('./features/production/production.component').then((m) => m.ProductionComponent),
      },
      {
        path: 'farm',
        loadComponent: () =>
          import('./features/farm/farm.component').then((m) => m.FarmComponent),
      },
      {
        path: 'payroll',
        loadComponent: () =>
          import('./features/payroll/payroll.component').then((m) => m.PayrollComponent),
      },
      {
        path: 'users',
        loadComponent: () =>
          import('./features/users/users.component').then((m) => m.UsersComponent),
      },
      {
        path: 'reports',
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
