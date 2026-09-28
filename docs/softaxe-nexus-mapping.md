# Softaxe → SheikhGo Nexus database mapping

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

### Finance (Phase 0 stub)

| Softaxe | Business meaning | Nexus entity | Nexus table | Notes |
|---------|------------------|--------------|-------------|-------|
| `Level1`–`Level5` CoA | Chart of accounts | Account | `finance.Accounts` | Clean tree |
| Softaxe AP / party position | Payable balance stub | PayableBalance | `finance.PayableBalances` | Updated by PI; full GL later |

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

## Deferred domains (map only — no Phase 0 tables)

### Sales

| Softaxe | Meaning | Future Nexus |
|---------|---------|--------------|
| `tblDOMain` / `tblDODetail` | Delivery order | Sales.DeliveryOrder |
| `TblSale` / `SP*` vouchers | Sales invoice | Sales.SalesInvoice |
| `tblsaleman` / `tblArea` / targets | Sales force | Sales.Salesman / Area / Target |
| `SR` | Sales return | Sales.SalesReturn |

### Production

| Softaxe | Meaning | Future Nexus |
|---------|---------|--------------|
| `TBLBOM` / `TblBomMain` / Detail | BOM | Production.Bom |
| `TblProductionEntry` / `MakeProduction` | Production entry | Production.ProductionOrder |
| `MakeConsumption` / `SC-RAW` | Consumption | Production.Consumption |

### HR & Payroll

| Softaxe | Meaning | Future Nexus |
|---------|---------|--------------|
| `tblEmployeeSetup` | Employee | HR.Employee |
| Attendance / OT / leave tables | Attendance | HR.Attendance / Leave / OT |
| `tblSALARY` / loans / advances | Payroll | HR.Salary / Loan / Advance |
| `tblEOBI` / `tableEOBID` | EOBI | HR.EobiEntry |

### Farm

| Softaxe | Meaning | Future Nexus |
|---------|---------|--------------|
| `tblFarm` / `FarmShareHolder` | Farm | Farm.Farm / Shareholder |
| `TblFlock` / `TblFlockPL` / `tblBird` | Flock | Farm.Flock / Bird |
| `tblFeedBrand` | Feed brand | Farm.FeedBrand |

### Quality

| Softaxe | Meaning | Future Nexus |
|---------|---------|--------------|
| `TblLabTestTypes` / `TblLabResults` | Lab QC | Quality.LabTestType / LabResult |

### Operations

| Softaxe | Meaning | Future Nexus |
|---------|---------|--------------|
| `TblWBSettings` / `TblWeighBridgeCameras` | Weighbridge | Operations.Weighbridge* |
| Gate pass print procs | Gate | Operations.GatePass |

### POS

| Softaxe | Meaning | Future Nexus |
|---------|---------|--------------|
| SoftaxeErpPos DB (111 tables) | POS product | POS.* (separate experience) |

---

## Naming rule

Do **not** preserve Softaxe names as Nexus architecture (`LEVEL5`, `TBLTRANS`, `TBLGROUP`). Softaxe informs **meaning**; Nexus uses clear entities above.
