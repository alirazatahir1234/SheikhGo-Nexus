# Softaxe ERP → SheikhGo Nexus

Modern multi-tenant ERP inspired by Softaxe. Product database is **SheikhGo Nexus**, not Softaxe live.

## Dual-DB rule (mandatory)

```text
🔴 Softaxe live / PremierFoodTest = READ ONLY reference — never modify
🟢 SheikhGo_Nexus_Dev = only writable product database in Development
❌ No Softaxe stored procedure runtime
```

Development allowlist: connection `Database` must be `SheikhGo_Nexus_Dev` (enforced by `DatabaseSafetyGuard`). Prefer SQL login with no Softaxe write rights.

## Stack

- **Backend:** .NET 8, Clean Architecture, EF Core migrations, SQL Server, JWT
- **Frontend:** Angular 21 (`/web`) with `/api` proxy → `http://localhost:5080`
- **Database:** Docker `sheikh-sqlserver` → **`SheikhGo_Nexus_Dev`**
- **Schema map:** [docs/softaxe-nexus-mapping.md](docs/softaxe-nexus-mapping.md)
- **Git tags:** `phase-0-complete` · `phase-1-complete`

## Phase 0 (complete)

| Schema | Tables |
|--------|--------|
| core | Tenant, Company, Branch, FinancialYear, FiscalPeriod, AuditLog |
| identity | User, Role, Permission, UserRole, RolePermission |
| inventory | ProductGroup, ProductCategory, Brand, UnitOfMeasure, Product, Warehouse, WarehouseStock |
| documents | DocumentType, Document, DocumentLine, DocumentNumberSequence, Verification, Approval |
| procurement | Party, SupplierProfile, PurchaseOrder(+Line), GoodsReceipt(+Line), PurchaseInvoice(+Line) |
| finance | Account, PayableBalance (stub) |

**Stock rule:** GRN Post increases `WarehouseStock`. Purchase Invoice Post updates `PayableBalance` only (no double stock).

## Phase 1 (complete) — Nexus Core + Document Engine UI

- **Shell:** SheikhGo Nexus login, domain sidebar, company/branch/FY session context, breadcrumb
- **Session:** Login returns companies, default branch, current FY, and permission codes; topbar switcher persists `nexus_*` keys
- **RBAC:** API `PermissionService` enforces workflow; Angular `permissionGuard` + sidebar/action gating
- **Administration APIs + UI:** Companies, Branches, Users/Roles/Permissions, Financial Years / Fiscal Periods, Audit Log, Document Types
- **Document Engine:** DocumentType-driven lifecycle (PO skips verify; GRN skips approve; PI uses full chain) with Verification/Approval + AuditLog
- **Procurement UX:** shared document components (header, lines, totals, status stepper, document-actions, verification panel, audit timeline) on PO / GRN / PI
- **Dashboard:** Purchases / Inventory / Payables / Receivables KPIs, pending approvals, recent documents
- **Partial GRN:** quantities cannot exceed remaining PO line qty; multiple GRNs per PO allowed
- **Permission seed:** Inventory.*, Procurement.*.View|Create|Edit|Submit|Approve|Post, Admin.* (Admin role gets all)

### Key API routes

```text
GET/PUT  /api/admin/companies
GET/POST/PUT /api/admin/branches
GET/POST /api/admin/roles , GET /api/admin/permissions
PUT /api/admin/roles/{id}/permissions , PUT /api/admin/users/{id}/roles
GET/POST /api/admin/financial-years , PUT .../set-current
GET /api/admin/audit-logs , GET/PUT /api/admin/document-types

POST /api/purchases/orders/{id}/{submit|verify|approve|post|cancel}
POST /api/purchases/receipts/{id}/{submit|verify|approve|post|cancel}
POST /api/purchases/invoices/{id}/{submit|verify|approve|post|cancel}
GET  /api/purchases/audit/{entityName}/{id}
```

### Explicit non-goals (still deferred)

Sales slice, full GL/cash/bank, Farm/POS/HR entities in DbContext, SoftaxeERP project rename.

## Demo login

```
Email:    admin@softaxe.local
Password: Admin@123
```

## Quick start

```bash
docker start sheikh-sqlserver
# CREATE DATABASE SheikhGo_Nexus_Dev;  # if needed

cd src/SoftaxeERP.Api
dotnet ef database update --project ../SoftaxeERP.Infrastructure
dotnet run --launch-profile http
# → http://localhost:5080/swagger

cd web
npm install
npm start -- --host 127.0.0.1 --port 4200
# → http://localhost:4200
```

`Database:ApplyMigrations` defaults to true in Development; set false in Production and run migrations in CI/CD.

## Architecture notes

- One **tenant** per company (Softaxe used one DB per client; we use shared DB + `TenantId`).
- Softaxe package on disk is **knowledge only** — never a runtime dependency.
- Numbering: Company + Branch + DocumentType + FinancialYear via `DocumentNumberSequence`.
