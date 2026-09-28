# Softaxe ERP → SheikhGo Nexus (Phase 0)

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

## Phase 0 modules (mapped)

| Schema | Tables |
|--------|--------|
| core | Tenant, Company, Branch, FinancialYear, FiscalPeriod, AuditLog |
| identity | User, Role, Permission, UserRole, RolePermission |
| inventory | ProductGroup, ProductCategory, Brand, UnitOfMeasure, Product, Warehouse, WarehouseStock |
| documents | DocumentType, Document, DocumentLine, DocumentNumberSequence, Verification, Approval |
| procurement | Party, SupplierProfile, PurchaseOrder(+Line), GoodsReceipt(+Line), PurchaseInvoice(+Line) |
| finance | Account, PayableBalance (stub) |

**Stock rule:** GRN increases `WarehouseStock`. Purchase Invoice updates `PayableBalance` only (no double stock).

## Demo login

- Email: `admin@softaxe.local`
- Password: `Admin@123`

## Quick start

```bash
docker start sheikh-sqlserver
# DB should already exist; create if needed:
# CREATE DATABASE SheikhGo_Nexus_Dev;

cd src/SoftaxeERP.Api
dotnet ef database update --project ../SoftaxeERP.Infrastructure
dotnet run
```

`Database:ApplyMigrations` defaults to true in Development; set false in Production and run migrations in CI/CD.

## Deferred modules

Farm, POS, Sales invoices, HR, Lab, Weighbridge, etc. are stubbed (API returns empty / “Deferred until after Phase 0”). Do not add them to Phase 0 DbContext.

## Legacy Softaxe knowledge

Offline package: `~/Desktop/Softaxe-ERP-ChatGPT-Package` — reference only.
| POS | Counter sales with stock out |
| Stock transfer | Godown-to-godown STK-TRF |
| Returns | Sale / purchase returns (SR / PR) |
| Leaves | Employee leave entries |
| Salary sheet | Monthly payroll generate + pay |
| EOBI | Employee EOBI deductions |
| Voucher flavors | CP-FREIGHT, JV-RM, DO-Sales, CP-AD, BP-SL, STK-TRF, SR, PR, CP-PAID… |
| Users | Tenant user CRUD + roles |
| Reports | Ledger, trial balance, AR/AP aging |
| Dashboard | KPIs |

## Demo login

```
Email:    admin@softaxe.local
Password: Admin@123
```

## Run API

```bash
docker start sheikh-sqlserver

cd src/SoftaxeERP.Api
dotnet run --launch-profile http
# → http://localhost:5080/swagger
```

## Run Web

```bash
cd web
npm install
npm start -- --host 127.0.0.1 --port 4200
# → http://localhost:4200
```

## Architecture notes

- One **tenant** per company (Softaxe used one DB per client; we use shared DB + `TenantId`).
- Softaxe `Level1–Level5` → cleaned **Account** tree.
- Softaxe `TransMain` / `tblTransVch` → **Voucher** / **VoucherLine** with balanced debit/credit.
- Softaxe farm/feed flags → `CompanyProfile` module toggles + `IndustryType`.
- Demo DB recreates automatically when new module tables are missing (`EnsureCreated` + missing-table check).
