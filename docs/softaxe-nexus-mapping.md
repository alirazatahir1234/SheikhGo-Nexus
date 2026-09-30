# Softaxe → SheikhGo-Nexus-ERP database mapping

**Canonical schema contract for Phase 0+.** Softaxe objects are **read-only knowledge**. Nexus tables are created only on `SheikhGo_Nexus_Dev` (never Softaxe live).

Dual-DB policy: [database-policy.md](database-policy.md) · Softaxe facts: [source-db.md](source-db.md)

Columns: Softaxe → Business meaning → Nexus entity → Nexus table → Workflow / notes

---

## Phase 0 — implement now

### Core

| Softaxe | Business meaning | Nexus entity | Nexus table | Notes |
|---------|------------------|--------------|-------------|-------|
| *(none / app)* | Multi-tenant root | Tenant | `core.Tenants` | Softaxe = one DB per client |
| `Company` | Legal company | Company | `core.Companies` | Config flags → company settings later |
| `Location` | Branch / site | Branch | `core.Branches` | Softaxe LocId scope |
| `tblfinyear` | Financial year | FinancialYear | `core.FinancialYears` | |
| *(periods)* | FY periods | FiscalPeriod | `core.FiscalPeriods` | Derived / new |
| `TblLOG` / `LOGG` | Audit trail | AuditLog | `core.AuditLogs` | Richer: Old/New JSON, IP |

### Identity

| Softaxe | Business meaning | Nexus entity | Nexus table | Notes |
|---------|------------------|--------------|-------------|-------|
| `Users` | Login user | User | `identity.Users` | |
| Softaxe role/menu | Role | Role | `identity.Roles` | Softaxe often string/`Admin` |
| `menu` / `childmenu` / `TblUserVchTypes` | Permission | Permission | `identity.Permissions` | |
| | User↔Role | UserRole | `identity.UserRoles` | |
| | Role↔Permission | RolePermission | `identity.RolePermissions` | |

### Inventory

| Softaxe | Business meaning | Nexus entity | Nexus table | Notes |
|---------|------------------|--------------|-------------|-------|
| `TblGroup` / `TBLGROUP` | Product group | ProductGroup | `inventory.ProductGroups` | |
| Softaxe category patterns | Product category | ProductCategory | `inventory.ProductCategories` | |
| `TblBrand` | Brand | Brand | `inventory.Brands` | |
| `tblUOM` | Unit of measure | UnitOfMeasure | `inventory.UnitsOfMeasure` | |
| `Level5` (item leaf) / product usage | Product | Product | `inventory.Products` | Level5 also parties/accounts — split in Nexus |
| `TBLGODOWNS` | Warehouse | Warehouse | `inventory.Warehouses` | Under Branch |
| `TBLTRANS` balances / stock SPs | On-hand stock | WarehouseStock | `inventory.WarehouseStocks` | Unique Product+Warehouse |

### Documents (Document Engine)

| Softaxe | Business meaning | Nexus entity | Nexus table | Notes |
|---------|------------------|--------------|-------------|-------|
| `tblvchtypes` / `TblTypes` | Document type + workflow flags | DocumentType | `documents.DocumentTypes` | RequiresVerification / RequiresApproval |
| `TransMain` | Document header | Document | `documents.Documents` | Cmp+Loc+Fin+VchType+VchNo → clean Id |
| `tblTransVch` / `TblSubTrans` | Document lines | DocumentLine | `documents.DocumentLines` | |
| `TBLVCNO` / series | Number sequence | DocumentNumberSequence | `documents.DocumentNumberSequences` | Company+Branch+Type+FY+Prefix |
| Verify flags | Verification step | Verification | `documents.Verifications` | |
| Approval flags | Approval step | Approval | `documents.Approvals` | |
| `Aprove` / status bits | Status | *(enum on Document)* | — | Draft→Submitted→Verified→Approved→Posted |

### Procurement

| Softaxe | Business meaning | Nexus entity | Nexus table | Notes |
|---------|------------------|--------------|-------------|-------|
| `Level5` party / party lists | Party master | Party | `procurement.Parties` | Customer+Supplier capable |
| Softaxe supplier usage | Supplier profile | SupplierProfile | `procurement.SupplierProfiles` | PartyId FK |
| Softaxe PO / contract patterns | Purchase order | PurchaseOrder / Line | `procurement.PurchaseOrders` | Links Document |
| Gate / inward / GRN patterns | Goods receipt | GoodsReceipt / Line | `procurement.GoodsReceipts` | **Increases WarehouseStock** |
| `PI` / `PI1` / `PI2` / `RP-RAW` | Purchase invoice | PurchaseInvoice / Line | `procurement.PurchaseInvoices` | **AP only — no stock** |

### Finance (Phase 0 stub → superseded by Phase 3)

| Softaxe | Business meaning | Nexus entity | Nexus table | Notes |
|---------|------------------|--------------|-------------|-------|
| `Level1`–`Level5` CoA | Chart of accounts | Account | `finance.Accounts` | Clean tree; codes preserved in Phase 3 |
| Softaxe AP / party position | Payable balance stub | PayableBalance | `finance.PayableBalances` | Still updated by PI/SPAY; journals also posted |

---

## Phase 0 vertical slice workflow

```text
Party + SupplierProfile
        ↓
PurchaseOrder (+ Document)
        ↓
GoodsReceipt  →  WarehouseStock +1
        ↓
PurchaseInvoice  →  PayableBalance +1
        ↓
(later) Payment → Cash/Bank → GL
```

---

## Phase 2 — Sales (implemented)

| Softaxe | Business meaning | Nexus entity | Nexus table | Notes |
|---------|------------------|--------------|-------------|-------|
| Softaxe customer usage | Customer profile | CustomerProfile | `procurement.CustomerProfiles` | PartyId FK; PartyType Customer/Both |
| `tblbooking` | Sales order | SalesOrder / Line | `sales.SalesOrders` | Document Engine SO |
| `tblDOMain` / `tblDODetail` | Delivery order | DeliveryOrder / Line | `sales.DeliveryOrders` | **Decreases WarehouseStock** on Post |
| `SP*` vouchers | Sales invoice | SalesInvoice / Line | `sales.SalesInvoices` | **AR only — no stock** |
| `CR` / `BR` receipts | Customer payment | CustomerPayment / Application | `sales.CustomerPayments` | Reduces ReceivableBalance |
| Party position / AgingCR | Receivable stub | ReceivableBalance | `finance.ReceivableBalances` | Updated by SI/CP; full GL later |

```text
Party + CustomerProfile
        ↓
SalesOrder (+ Document) → Approve
        ↓
DeliveryOrder  →  WarehouseStock −1
        ↓
SalesInvoice  →  ReceivableBalance +1
        ↓
CustomerPayment  →  ReceivableBalance −1
```

---

## Phase 3 — Finance & GL (implemented)

| Softaxe | Business meaning | Nexus entity | Nexus table | Notes |
|---------|------------------|--------------|-------------|-------|
| `Level1`–`Level5` | Chart of accounts | Account | `finance.Accounts` | Codes: 1100 AR, 1200 Inv, 1300 Cash, 1400 Bank, 2100 AP, 4100 Sales, 5100 COGS |
| `TransMain` / `tblTransVch` | Journal voucher | JournalEntry / Line | `finance.JournalEntries` | Posted via `AccountingPostingService`; balanced Dr=Cr |
| Cash/Bank books | Cash & bank accounts | CashBankAccount / Txn | `finance.CashBankAccounts` | Linked to GL 1300/1400 |
| Supplier payment patterns | Supplier payment | SupplierPayment / Application | `finance.SupplierPayments` | Reduces AP; Cash/Bank credit |
| `trg_CalculateWeightedAverage` | Inventory valuation | Product.AverageCost | `inventory.Products` | Weighted average perpetual — see [inventory-valuation.md](inventory-valuation.md) |
| Fiscal periods | Period control | FiscalPeriod | `core.FiscalPeriods` | Closed period rejects journal post |
| TB / GL / P&L / BS | Financial reports | *(queries)* | — | From posted journal lines |

```text
Domain Post (PI / SI / CP / SPAY / DO-COGS / Manual JV)
        ↓
AccountingPostingService (period open + balanced + idempotent)
        ↓
Document (JV/SPAY/CP) + JournalEntry + Lines
        ↓
AP/AR stubs · Product.AverageCost · CashBankTransaction
        ↓
Trial Balance / GL / P&L / Balance Sheet
```

Default GL map on post:

| Event | Journal |
|-------|---------|
| Purchase Invoice | Dr Inventory 1200 / Cr AP 2100; update AverageCost |
| Supplier Payment | Dr AP 2100 / Cr Cash or Bank |
| Delivery Order | Qty −; Dr COGS 5100 / Cr Inventory 1200 @ pre-issue avg |
| Sales Invoice | Dr AR 1100 / Cr Sales 4100 |
| Customer Payment | Dr Cash or Bank / Cr AR 1100 |
| GRN | Qty only — **no** GL |

---

## Phase 4 — Advanced Inventory (implemented)

| Softaxe | Business meaning | Nexus entity | Nexus table | Notes |
|---------|------------------|--------------|-------------|-------|
| `tblUOM` + packing | Unit conversion | UnitConversion | `inventory.UnitConversions` | Base UOM on Product; Factor To=From×Factor |
| `TBLGODOWNS` bins | Warehouse location | WarehouseLocation | `inventory.WarehouseLocations` | Optional on txn lines |
| `TBLTRANS` movements | Inventory ledger | InventoryTransaction | `inventory.InventoryTransactions` | Source of truth; IPS only |
| Stock transfer | Inter-warehouse move | StockTransfer / Line | `inventory.StockTransfers` | Atomic Out+In via IPS |
| Stock adjustment | Qty correction | StockAdjustment / Line | `inventory.StockAdjustments` | Reason + audit; GL via APS 5600 |
| Physical count | Cycle count | StockCount / Line | `inventory.StockCounts` | Snapshot → variance → linked SA |

Central `InventoryPostingService` posts GRN/DO/Transfer/Adjustment. `WarehouseStock` is projection only.

---

## Phase 5 — HR & Payroll (implemented)

| Softaxe | Business meaning | Nexus entity | Nexus table | Notes |
|---------|------------------|--------------|-------------|-------|
| `tblEmployeeSetup` | Employee master | Employee | `hr.Employees` | Branch FK; BasicSalary + PayrollProfile |
| Dept / Desig | Org structure | Department / Designation | `hr.Departments` / `Designations` | |
| Attendance | Daily mark | Attendance | `hr.Attendances` | Present/Absent/Late/HalfDay/Overtime |
| Leave | Leave request | LeaveRequest / Balance / Type | `hr.Leave*` | Doc type `LV`; Approve → balance |
| `tblSALARY` | Payroll run | PayrollRun / Employee / Line | `hr.Payroll*` | Doc `PR`; Calculated status |
| Salary payment | Net pay disbursement | SalaryPayment | `hr.SalaryPayments` | Doc `HPAY` |
| CoA mapping | Configurable GL | GlAccountMapping | `finance.GlAccountMappings` | PayrollExpense/SalaryPayable/Tax/Other |

GL: Dr 5200 Gross / Cr 2200 Net + Cr 2210 Tax + Cr 2220 Other; payment Dr 2200 / Cr Bank.

---

## Deferred domains (map only — no Phase 0/2/3/4/5 tables)

### Sales (remaining)

| Softaxe | Meaning | Future Nexus |
|---------|---------|--------------|
| `tblsaleman` / `tblArea` / targets | Sales force | Sales.Salesman / Area / Target |
| `SR` | Sales return | Sales.SalesReturn |

### Production (Phase 6 — implemented on `SheikhGo_Nexus_Dev`)

| Softaxe | Meaning | Nexus |
|---------|---------|-------|
| `TBLBOM` / `TblBomMain` / Detail | BOM | `production.BillOfMaterials` / `BillOfMaterialLines` |
| `TblProductionEntry` / `MakeProduction` | Manufacturing order | `production.ProductionOrders` (doc type **MO**) |
| `MakeConsumption` / `SC-RAW` | RM consumption / FG output | Via `InventoryPostingService` (`ProductionConsumption` / `ProductionOutput`) + APS WIP (`1210`) ↔ Inventory (`1200`) |

**MVP limits:** material-driven weighted-average FG cost (labor/overhead amounts optional, no HR auto-link); single warehouse per order; no multi-step partial backflush; one Inventory CoA (1200) for RM+FG.

### HR & Payroll (remaining)

| Softaxe | Meaning | Future Nexus |
|---------|---------|--------------|
| loans / advances | Staff finance | HR.Loan / Advance (deferred) |
| `tblEOBI` / `tableEOBID` | EOBI | HR.EobiEntry (deferred) |

### Farm (Phase 7 — implemented on `SheikhGo_Nexus_Dev`)

| Softaxe | Meaning | Nexus |
|---------|---------|-------|
| `tblFarm` | Farm | `farm.Farms` / `FarmUnits` |
| `TblFlock` / `TblFlockPL` | Flock batch | `farm.Flocks` + mortality/feed/treatments/expenses |
| `tblFeedBrand` | Feed | Inventory product + `FeedIssue` via IPS |

**MVP limits:** shareholders deferred; no flock transfer; poultry metrics only.

### Quality (Phase 7 — implemented)

| Softaxe | Meaning | Nexus |
|---------|---------|-------|
| `TblLabTestTypes` / `TblLabResults` | Lab QC | `quality.LabTestTypes` / `LabSamples` / `LabTestResults` |

PASS/FAIL/HOLD on approve; FAIL/HOLD sets `WarehouseStock.QualityStatus` when sample linked to product+warehouse; IPS outbound rejects Hold/Rejected.

### Operations (Phase 8 — implemented on `SheikhGo_Nexus_Dev`)

| Softaxe | Meaning | Nexus |
|---------|---------|-------|
| Vehicle / fleet masters | Vehicles & drivers | `operations.VehicleTypes` / `Vehicles` / `Drivers` / `VehicleAssignments` |
| Gate pass print procs | Gate pass | `operations.GatePasses` (+ lines); doc type `GP` |
| `TblWBSettings` / weigh tickets | Weighbridge | `weighbridge.WeighbridgeScales` / `Weighments`; doc type `WBT` |

**Weighbridge rules:** Net = Gross − Tare computed only on server; Complete creates linked Gate Pass. No auto GRN/DO from net weight in MVP. No camera/hardware.

### POS (Phase 8 — implemented)

| Softaxe | Meaning | Nexus |
|---------|---------|-------|
| SoftaxeErpPos (separate product) | Counter sales | `pos.PosTerminals` / `PosShifts` / `PosSales` / lines / payments; doc type `POS` |

POS orchestrates existing **SO → DO (IPS+COGS) → SI (AR) → CP** for walk-in `CUS-POS`. Cash drawer: Expected = Opening + Cash tenders. No SoftaxeErpPos SP parity; refunds/void of posted sales deferred.

**MVP limits:** no camera/hardware; WB does not auto-create GRN/DO qty from net; single warehouse per terminal; Cash/Card/Other tenders only.

### BI / Reports (Phase 9 — implemented on `SheikhGo_Nexus_Dev`)

| Softaxe | Meaning | Nexus |
|---------|---------|-------|
| Softaxe report SPs / Crystal | Operational & financial BI | `/api/reports/**` + `/api/dashboard/executive` query services over posted Nexus tables |

**Source of truth:** posted documents, `JournalEntry` / `JournalEntryLines`, `InventoryTransactions` / `WarehouseStocks` (Qty × AverageCost). No SQL views/cubes; Softaxe report SPs are **not** ported.

**Phase 9 limits:** no salesperson analytics (`SalesmanId` absent on sales docs); Cash Flow is operational cash/bank summary (not full IAS 7); no scheduled/email delivery; no Softaxe report parity; export CSV always, Excel/PDF for named domain exports.

### AI / Nexus Intelligence (Phase 10A — implemented on `SheikhGo_Nexus_Dev`)

| Softaxe | Meaning | Nexus |
|---------|---------|-------|
| *(none)* | Controlled AI assistant | `/api/ai/chat|query|conversations` + `ai.Conversations` / `ai.Messages` |

**Flow:** JWT → RBAC (`Ai.Chat.View` / `Ai.Chat.Query` + domain `Reports.*`) → rules-first intent → read-only tools over existing report services → LLM provider (Null / Ollama / OpenAI) → answer. **No** LLM SQL, **no** Softaxe, **no** write actions in 10A.

**10A limits:** eight tools only (sales, AR, inventory, purchases, AP, TB, P&L, BS); no HR salary Q&A; no autonomous PO/SI; default provider `Null` (template from tool JSON).

### AI Actions (Phase 10B — implemented on `SheikhGo_Nexus_Dev`)

| Softaxe | Meaning | Nexus |
|---------|---------|-------|
| *(none)* | Human-confirmed draft actions | `/api/ai/chat` → pending action → `/api/ai/actions/{id}/confirm` |

**Flow:** JWT → RBAC (`Ai.Action.Propose` / `Ai.Action.Confirm` + domain `*.Create`) → rules-first action intent → entity resolve → `ai.PendingActions` draft → **human confirm** → existing create services with `Post: false` → Document Engine numbering → audit (`AiActionPropose` / `AiActionConfirm`).

**Draft types:** Purchase Order, Sales Order, Stock Transfer, Customer Payment, Supplier Payment, Journal Entry.

**10B limits:** no auto-post/approve; no delete; no Softaxe; no elevated AI permissions; cross-company/branch overrides rejected; payments need posted outstanding invoices for FIFO apply.

### AI Agents (Phase 10C — implemented on `SheikhGo_Nexus_Dev`)

| Softaxe | Meaning | Nexus |
|---------|---------|-------|
| *(none)* | Controlled multi-step agents | `/api/ai/agents` + chat `Kind=AgentWaitingConfirm` |

**Flow:** JWT → `Ai.Agent.Run` → Inventory Replenishment steps (read low stock → resolve suppliers → prepare `AiPendingAction` PO drafts) → human **Confirm All** → existing Purchase create (`Post: false`) → Document Engine → audit (`AiAgentStart` / `AiAgentStep` / `AiAgentConfirm`).

**10C limits:** only Inventory Replenishment agent executed; Sales/Finance/Reporting agent types reserved but not available; no auto-post/approve; agent uses current user permissions only; Softaxe untouched.

### Predictive Intelligence (Phase 10D — implemented on `SheikhGo_Nexus_Dev`)

| Softaxe | Meaning | Nexus |
|---------|---------|-------|
| *(none)* | Inventory demand & stock-out forecast | `/api/reports/inventory/demand-forecast` + AI intent `InventoryDemandForecast` + `ai.Predictions` audit |

**Flow:** JWT → RBAC (`Reports.Predictive.View` **or** inventory report view; AI also needs `Ai.Chat.Query`) → `InventoryForecastService` deterministic stats over `InventoryTransactions` (Delivery / ProductionConsumption / FarmFeedIssue / FarmMedication) + PO/GRN lead-time median → optional Null/Ollama/OpenAI **explanation only** of FACTS JSON. **No** document create/post/approve; **no** auto-start of 10C replenishment.

**Model:** trailing 28-day SMA of daily demand; horizons 7/14/21/30; stock-out day vs SafetyProxy; reorder qty recommendation; confidence High/Medium/Low/InsufficientData; anomaly ±30% last-7 vs prior-21.

**10D limits:** inventory demand/stock-out only (no cash-flow / AR risk / revenue / production-batch forecasts); no ML training pipelines; Softaxe untouched; LLM never invents numbers.

---

## Naming rule

Do **not** preserve Softaxe names as Nexus architecture (`LEVEL5`, `TBLTRANS`, `TBLGROUP`). Softaxe informs **meaning**; Nexus uses clear entities above.
