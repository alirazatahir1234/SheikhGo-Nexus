# SheikhGo-Nexus-ERP

Modern multi-tenant ERP (SheikhGo-Nexus-ERP). Softaxe is **read-only** business reference only.

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

- **Shell:** SheikhGo-Nexus-ERP login, domain sidebar, company/branch/FY session context, breadcrumb
- **Session:** Login returns companies, default branch, current FY, and permission codes; topbar switcher persists `nexus_*` keys
- **RBAC:** API `PermissionService` enforces workflow; Angular `permissionGuard` + sidebar/action gating; `Procurement.*.Verify` seeded and enforced separately from Approve
- **Administration APIs + UI:** Companies, Branches, Users/Roles/Permissions, Financial Years / Fiscal Periods, Audit Log (incl. IP), Document Types
- **Document Engine:** DocumentType-driven lifecycle (PO skips verify; GRN skips approve; PI uses full chain) with Verification/Approval + AuditLog
- **Procurement UX:** shared document components (header, lines, totals, status stepper, document-actions, verification panel, workflow actors, audit timeline) on PO / GRN / PI
- **Dashboard:** Purchases / Inventory / Payables / Receivables KPIs, pending approvals, recent documents
- **Partial GRN:** quantities cannot exceed remaining PO line qty; multiple GRNs per PO allowed
- **Permission seed:** Inventory.*, Procurement.*.View|Create|Edit|Submit|Verify|Approve|Post, Admin.* (Admin role gets all)
- **Audit IP:** `AuditLog.IpAddress` populated from `X-Forwarded-For` / remote IP via `ITenantContext.ClientIp`
- **Workflow actors:** Get-by-id DTOs expose Created/Submitted/Verified/Approved/Posted (projected; no Document column churn)

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

Sales slice, full GL/cash/bank, Farm/POS/HR entities in DbContext.

## Demo login

```
Email:    admin@softaxe.local
Password: Admin@123
```

## Quick start

```bash
docker start sheikh-sqlserver
# CREATE DATABASE SheikhGo_Nexus_Dev;  # if needed

cd src/SheikhGo-Nexus-ERP.Api
dotnet ef database update --project ../SheikhGo-Nexus-ERP.Infrastructure
dotnet run --launch-profile http
# → http://localhost:5080/swagger

cd web
npm install
npm start -- --host 127.0.0.1 --port 4200
# → http://localhost:4200
```

`Database:ApplyMigrations` defaults to true in Development; set false in Production and run migrations in CI/CD.

## Phase 9 (complete) — BI + Advanced Reporting

- **SoT:** posted docs + `JournalEntryLines` + `InventoryTransactions` / `WarehouseStocks` (Qty × AverageCost). No Softaxe report SPs / cubes.
- **API:** `/api/dashboard/executive`, `/api/reports/finance|sales|procurement|inventory|hr|production|farm|quality|operations|pos/**`, export `format=csv|xlsx|pdf`
- **RBAC:** `Reports.*.View` (+ aliases `Finance.Reports.View` / `Inventory.Reports.View`)
- **UI:** `/reports` BI shell (domain nav, filters, export/print); dashboard executive KPIs with drill-downs
- **Limits:** no salesperson analytics; cash flow = operational cash/bank summary (not IAS 7); no scheduled delivery
- **Verify:** `python3 scripts/phase9-acceptance.py` (API on `:5080`)

## Phase 10A (complete) — Nexus Intelligence Foundation (read-only)

- **Gateway:** `/api/ai/chat`, `/api/ai/query`, `/api/ai/conversations`
- **Tools:** sales, AR, inventory valuation, purchases, AP, TB, P&L, BS via existing report services (no SQL from LLM)
- **Providers:** `Ai:Provider` = `Null` | `Ollama` | `OpenAI` (default Null)
- **RBAC:** `Ai.Chat.View` + `Ai.Chat.Query` plus domain `Reports.*`
- **UI:** `/ai` assistant chat
- **Verify:** `python3 scripts/phase10a-acceptance.py`
- **Not in 10A:** write actions, agents, Softaxe, HR salary tools

## Phase 10B (complete) — AI Actions + Human Approval

- **Rule:** AI prepares drafts only; **Confirm & Create** calls existing Nexus create APIs with `Post: false`
- **Actions:** PO, SO, Stock Transfer, Customer Payment, Supplier Payment, Journal Entry
- **APIs:** `PATCH/POST /api/ai/actions/{id}`, `/confirm`, `/cancel` + chat `kind=DraftAction`
- **RBAC:** `Ai.Action.Propose` + `Ai.Action.Confirm` + domain `*.Create` (no elevation)
- **UI:** `/ai` action card (edit qty/rate/amount, Confirm, Cancel)
- **Verify:** `python3 scripts/phase10b-acceptance.py`
- **Not in 10B:** autonomous agents, auto-post/approve, Softaxe

## Phase 10C (complete) — AI Agents + Controlled Automation

- **First agent:** Inventory Replenishment (low stock → suppliers → PO drafts → Confirm All)
- **Trail:** `ai.AgentRuns` / `ai.AgentSteps`; writes still via 10B `AiPendingAction` (`Post: false`)
- **APIs:** `GET/POST /api/ai/agents/{id}`, `/confirm`, `/cancel` + chat `Kind=AgentWaitingConfirm`
- **RBAC:** `Ai.Agent.Run` / `Confirm` / `Cancel` + inventory report view + `Procurement.PurchaseOrder.Create`
- **UI:** `/ai` agent plan card (steps, proposed POs, Confirm All)
- **Verify:** `python3 scripts/phase10c-acceptance.py`
- **Not in 10C:** other agents, predictive (10D), auto-post/approve, Softaxe

## Phase 10D (complete) — Predictive Intelligence (Inventory Demand & Stock-out)

- **Engine:** deterministic SMA over demand `QtyOut` + PO/GRN lead-time proxies (no LLM numbers)
- **Surfaces:** `GET /api/reports/inventory/demand-forecast`, AI chat intent `InventoryDemandForecast`, Reports UI **Demand forecast**
- **Audit:** `ai.Predictions` + audit action `AiPrediction`
- **RBAC:** `Reports.Predictive.View` or inventory report view (+ `Ai.Chat.Query` for chat)
- **Rule:** recommendations only — never creates/posts POs; user may separately ask 10C replenishment
- **Verify:** `python3 scripts/phase10d-acceptance.py`
- **Not in 10D:** cash-flow / AR / sales revenue / production forecasts; auto 10C; Softaxe; trained ML

## Architecture notes

- One **tenant** per company (Softaxe used one DB per client; we use shared DB + `TenantId`).
- Softaxe package on disk is **knowledge only** — never a runtime dependency.
- Numbering: Company + Branch + DocumentType + FinancialYear via `DocumentNumberSequence`.
