using SheikhGoNexusERP.Domain.Enums;

namespace SheikhGoNexusERP.Application.DTOs;

public record LoginRequest(string Email, string Password);
public record LoginResponse(
    string Token,
    UserDto User,
    CompanyDto Company,
    IReadOnlyList<CompanyBriefDto> Companies,
    BranchDto? DefaultBranch,
    FinancialYearDto? CurrentFinancialYear,
    IReadOnlyList<string> Permissions);
public record UserDto(Guid Id, string Email, string FullName, string Role, bool IsPlatformAdmin);
public record CompanyBriefDto(Guid Id, string Name, string ShortName);
public record CompanyDto(
    Guid Id, string Name, string ShortName, string? Address, string? City,
    string Country, string? Phone, string? Email, string Currency, string? Ntn,
    IndustryType Industry, decimal DefaultSaleTaxPercent = 0, decimal DefaultPurchaseTaxPercent = 0,
    bool SyncToFbr = false, bool TaxModule = false);

public record DashboardSummaryDto(
    decimal SalesToday,
    decimal PurchasesToday,
    decimal InventoryValue,
    decimal Receivables,
    decimal Payables,
    IReadOnlyList<VoucherListDto> RecentVouchers,
    IReadOnlyList<PendingApprovalDto> PendingApprovals,
    IReadOnlyList<RecentDocumentDto> RecentDocuments);

public record PendingApprovalDto(
    Guid Id,
    string DocumentNo,
    string DocumentKind,
    string Status,
    DateTime Date,
    string? PartyName,
    decimal Amount);

public record RecentDocumentDto(
    Guid Id,
    string DocumentNo,
    string DocumentKind,
    string Status,
    DateTime Date,
    string? PartyName,
    decimal Amount);

public record AccountDto(
    Guid Id, string Code, string Name, AccountType AccountType,
    Guid? ParentId, bool IsPostable, decimal Balance, int Level);

public record AccountUpsertDto(
    string Code, string Name, AccountType AccountType,
    Guid? ParentId, bool IsPostable, decimal OpeningBalance);

public record PartyDto(
    Guid Id, string Code, string Name, PartyType PartyType,
    string? Phone, string? Email, string? City, string? Address,
    decimal CreditLimit, decimal OpeningBalance, bool IsActive);

public record PartyUpsertDto(
    string Code, string Name, PartyType PartyType,
    string? Phone, string? Email, string? City, string? Address,
    decimal CreditLimit, decimal OpeningBalance);

public record ProductDto(
    Guid Id, string Code, string Name, string? Category, string Unit,
    decimal SalePrice, decimal PurchasePrice, decimal StockQty,
    decimal ReorderLevel, bool IsActive, bool IsRawMaterial, bool IsFinishedGood,
    decimal AverageCost = 0);

public record ProductUpsertDto(
    string Code, string Name, string? Category, string Unit,
    decimal SalePrice, decimal PurchasePrice, decimal StockQty,
    decimal ReorderLevel, bool IsRawMaterial, bool IsFinishedGood);

public record VoucherLineDto(Guid? Id, Guid AccountId, string? AccountName, decimal Debit, decimal Credit, string? Remarks);
public record VoucherListDto(Guid Id, string VoucherNo, string VoucherType, DateTime Date, string? Narration, decimal TotalDebit, decimal TotalCredit, string Status);
public record VoucherDto(Guid Id, string VoucherNo, VoucherType VoucherType, DateTime Date, string? Narration, DocumentStatus Status, decimal TotalDebit, decimal TotalCredit, IReadOnlyList<VoucherLineDto> Lines);
public record VoucherCreateDto(VoucherType VoucherType, DateTime Date, string? Narration, bool Post, IReadOnlyList<VoucherLineDto> Lines);

public record InvoiceLineDto(Guid? Id, Guid ProductId, string? ProductName, decimal Qty, decimal Rate, decimal Amount);
public record SaleInvoiceDto(Guid Id, string InvoiceNo, DateTime Date, Guid PartyId, string? PartyName, DocumentStatus Status, decimal SubTotal, decimal Discount, decimal Tax, decimal GrandTotal, string? Remarks, IReadOnlyList<InvoiceLineDto> Lines);
public record SaleInvoiceCreateDto(DateTime Date, Guid PartyId, decimal Discount, decimal Tax, string? Remarks, bool Post, IReadOnlyList<InvoiceLineDto> Lines);

public record PurchaseInvoiceDto(Guid Id, string InvoiceNo, DateTime Date, Guid PartyId, string? PartyName, DocumentStatus Status, decimal SubTotal, decimal Discount, decimal Tax, decimal GrandTotal, string? Remarks, IReadOnlyList<InvoiceLineDto> Lines, DocumentWorkflowActorsDto? Workflow = null);
public record PurchaseInvoiceCreateDto(DateTime Date, Guid PartyId, decimal Discount, decimal Tax, string? Remarks, bool Post, IReadOnlyList<InvoiceLineDto> Lines, Guid? GoodsReceiptId = null, Guid? PurchaseOrderId = null);

public record PurchaseOrderDto(Guid Id, string OrderNo, DateTime Date, Guid PartyId, string? PartyName, Guid? WarehouseId, DocumentStatus Status, decimal SubTotal, decimal Tax, decimal GrandTotal, string? Remarks, IReadOnlyList<InvoiceLineDto> Lines, DocumentWorkflowActorsDto? Workflow = null);
public record PurchaseOrderCreateDto(DateTime Date, Guid PartyId, Guid? WarehouseId, decimal Tax, string? Remarks, bool Post, IReadOnlyList<InvoiceLineDto> Lines);

public record GoodsReceiptDto(Guid Id, string ReceiptNo, DateTime Date, Guid PartyId, string? PartyName, Guid WarehouseId, Guid? PurchaseOrderId, DocumentStatus Status, string? Remarks, IReadOnlyList<InvoiceLineDto> Lines, DocumentWorkflowActorsDto? Workflow = null);
public record GoodsReceiptCreateDto(DateTime Date, Guid PartyId, Guid WarehouseId, Guid? PurchaseOrderId, string? Remarks, bool Post, IReadOnlyList<InvoiceLineDto> Lines);

public record PayableBalanceDto(Guid PartyId, string? PartyName, decimal Balance);
public record ReceivableBalanceDto(Guid PartyId, string? PartyName, decimal Balance);

public record SalesOrderDto(Guid Id, string OrderNo, DateTime Date, Guid PartyId, string? PartyName, Guid? WarehouseId, DocumentStatus Status, decimal SubTotal, decimal Tax, decimal GrandTotal, string? Remarks, IReadOnlyList<InvoiceLineDto> Lines, DocumentWorkflowActorsDto? Workflow = null);
public record SalesOrderCreateDto(DateTime Date, Guid PartyId, Guid? WarehouseId, decimal Tax, string? Remarks, bool Post, IReadOnlyList<InvoiceLineDto> Lines);

public record DeliveryOrderDto(Guid Id, string DeliveryNo, DateTime Date, Guid PartyId, string? PartyName, Guid WarehouseId, Guid SalesOrderId, DocumentStatus Status, string? Remarks, IReadOnlyList<InvoiceLineDto> Lines, DocumentWorkflowActorsDto? Workflow = null);
public record DeliveryOrderCreateDto(DateTime Date, Guid PartyId, Guid WarehouseId, Guid SalesOrderId, string? Remarks, bool Post, IReadOnlyList<InvoiceLineDto> Lines);

public record SalesInvoiceDocDto(Guid Id, string InvoiceNo, DateTime Date, Guid PartyId, string? PartyName, Guid? DeliveryOrderId, Guid? SalesOrderId, DocumentStatus Status, decimal SubTotal, decimal Discount, decimal Tax, decimal GrandTotal, decimal PaidAmount, string? Remarks, IReadOnlyList<InvoiceLineDto> Lines, DocumentWorkflowActorsDto? Workflow = null);
public record SalesInvoiceDocCreateDto(DateTime Date, Guid PartyId, decimal Discount, decimal Tax, string? Remarks, bool Post, IReadOnlyList<InvoiceLineDto> Lines, Guid? DeliveryOrderId = null, Guid? SalesOrderId = null);

public record PaymentApplicationDto(Guid? Id, Guid SalesInvoiceId, string? InvoiceNo, decimal AmountApplied);
public record CustomerPaymentDto(Guid Id, string PaymentNo, DateTime Date, Guid PartyId, string? PartyName, decimal Amount, DocumentStatus Status, string? Remarks, IReadOnlyList<PaymentApplicationDto> Applications, DocumentWorkflowActorsDto? Workflow = null);
public record CustomerPaymentCreateDto(DateTime Date, Guid PartyId, decimal Amount, string? Remarks, bool Post, IReadOnlyList<PaymentApplicationDto> Applications, Guid? CashBankAccountId = null);

public record SupplierPaymentApplicationDto(Guid? Id, Guid PurchaseInvoiceId, string? InvoiceNo, decimal AmountApplied);
public record SupplierPaymentDto(Guid Id, string PaymentNo, DateTime Date, Guid PartyId, string? PartyName, Guid? CashBankAccountId, decimal Amount, DocumentStatus Status, string? Remarks, IReadOnlyList<SupplierPaymentApplicationDto> Applications, DocumentWorkflowActorsDto? Workflow = null);
public record SupplierPaymentCreateDto(DateTime Date, Guid PartyId, decimal Amount, string? Remarks, bool Post, IReadOnlyList<SupplierPaymentApplicationDto> Applications, Guid? CashBankAccountId = null);

public record JournalLineDto(Guid? Id, Guid AccountId, string? AccountCode, string? AccountName, decimal Debit, decimal Credit, string? Description, Guid? PartyId = null, Guid? ProductId = null);
public record JournalEntryDto(Guid Id, string JournalNo, DateTime PostingDate, JournalSourceType SourceType, Guid? SourceEntityId, DocumentStatus Status, string? Narration, decimal TotalDebit, decimal TotalCredit, IReadOnlyList<JournalLineDto> Lines);
public record JournalEntryCreateDto(DateTime PostingDate, string? Narration, bool Post, IReadOnlyList<JournalLineDto> Lines);

public record CashBankAccountDto(Guid Id, string Code, string Name, CashBankKind AccountKind, Guid GlAccountId, string? GlAccountCode, bool IsActive, bool IsDefault);
public record CashBankAccountUpsertDto(string Code, string Name, CashBankKind AccountKind, Guid GlAccountId, bool IsDefault);
public record CashBankTxnDto(Guid Id, Guid CashBankAccountId, string? CashBankName, Guid? OppositeCashBankAccountId, Guid? PartyId, string? PartyName, decimal Amount, CashBankTxnType TxnType, DateTime PostingDate, DocumentStatus Status, string? Narration, Guid? JournalEntryId);
public record CashBankTxnCreateDto(Guid CashBankAccountId, Guid? OppositeCashBankAccountId, Guid? PartyId, decimal Amount, CashBankTxnType TxnType, DateTime PostingDate, string? Narration, bool Post);

public record ProfitLossLineDto(string Code, string Name, decimal Amount);
public record ProfitLossReportDto(DateTime From, DateTime To, decimal Revenue, decimal CostOfSales, decimal GrossProfit, decimal Expenses, decimal NetProfit, IReadOnlyList<ProfitLossLineDto> RevenueLines, IReadOnlyList<ProfitLossLineDto> CogsLines, IReadOnlyList<ProfitLossLineDto> ExpenseLines);
public record BalanceSheetLineDto(string Code, string Name, decimal Amount);
public record BalanceSheetReportDto(DateTime AsOf, decimal TotalAssets, decimal TotalLiabilities, decimal TotalEquity, IReadOnlyList<BalanceSheetLineDto> Assets, IReadOnlyList<BalanceSheetLineDto> Liabilities, IReadOnlyList<BalanceSheetLineDto> Equity);

public record StockDto(Guid ProductId, string Code, string Name, string Unit, decimal StockQty, decimal ReorderLevel, bool LowStock, Guid? WarehouseId = null, string? WarehouseCode = null, decimal? AverageCost = null, decimal? Value = null);

public record DepartmentDto(Guid Id, string Code, string Name, bool IsActive);
public record DepartmentUpsertDto(string Code, string Name, bool IsActive = true);
public record DesignationDto(Guid Id, string Code, string Name, bool IsActive);
public record DesignationUpsertDto(string Code, string Name, bool IsActive = true);

public record PayrollProfileLineDto(Guid? Id, Guid ComponentId, string? ComponentCode, string? ComponentName, string ComponentType, decimal Amount);
public record EmployeeDto(
    Guid Id, string Code, string Name, Guid? DepartmentId, string? Department, Guid? DesignationId, string? Designation,
    Guid BranchId, string? BranchName, DateTime? JoinDate, string EmploymentStatus, string EmploymentType,
    decimal BasicSalary, string? Phone, string? Email, bool IsActive, IReadOnlyList<PayrollProfileLineDto> ProfileLines);
public record EmployeeUpsertDto(
    string Code, string Name, Guid? DepartmentId, Guid? DesignationId, Guid BranchId, DateTime? JoinDate,
    string? EmploymentStatus, string? EmploymentType, decimal BasicSalary, string? Phone, string? Email, bool IsActive = true,
    IReadOnlyList<PayrollProfileLineUpsertDto>? ProfileLines = null);
public record PayrollProfileLineUpsertDto(Guid ComponentId, decimal Amount);

public record PayrollComponentDto(Guid Id, string Code, string Name, string ComponentType, bool IsSystem, string? GlMappingKey);

public record GlAccountMappingDto(Guid Id, string MappingKey, Guid AccountId, string? AccountCode, string? AccountName, bool IsActive);
public record GlAccountMappingUpsertDto(string MappingKey, Guid AccountId, bool IsActive = true);

public record CompanyUpdateDto(
    string Name, string ShortName, string? Address, string? City, string Country,
    string? Phone, string? Email, string Currency, string? Ntn, IndustryType Industry,
    decimal DefaultSaleTaxPercent = 0, decimal DefaultPurchaseTaxPercent = 0,
    bool SyncToFbr = false, bool TaxModule = false);

public record BranchDto(Guid Id, Guid CompanyId, string Code, string Name, bool IsDefault);
public record BranchUpsertDto(string Code, string Name, bool IsDefault);
public record RoleDto(Guid Id, string Code, string Name);
public record RoleCreateDto(string Code, string Name);
public record PermissionDto(Guid Id, string Code, string Name, string? Module);
public record RolePermissionsDto(Guid RoleId, IReadOnlyList<string> PermissionCodes);
public record UserRolesDto(IReadOnlyList<Guid> RoleIds);
public record FinancialYearDto(Guid Id, Guid CompanyId, string Code, DateTime StartDate, DateTime EndDate, bool IsCurrent, bool IsClosed);
public record FinancialYearCreateDto(string Code, DateTime StartDate, DateTime EndDate);
public record FiscalPeriodDto(Guid Id, Guid FinancialYearId, string Name, int PeriodNumber, DateTime StartDate, DateTime EndDate, bool IsClosed);
public record FiscalPeriodCreateDto(string Name, int PeriodNumber, DateTime StartDate, DateTime EndDate);
public record AuditLogDto(Guid Id, string EntityName, string EntityId, string Action, string? OldValues, string? NewValues, Guid? UserId, DateTime Timestamp, string? IpAddress = null);
public record DocumentTypeDto(Guid Id, string Code, string Name, bool RequiresVerification, bool RequiresApproval, string? Prefix, bool AffectsInventory, bool AffectsGl);
public record DocumentTypeUpsertDto(string Code, string Name, bool RequiresVerification, bool RequiresApproval, string? Prefix, bool AffectsInventory, bool AffectsGl);

/// <summary>Optional remarks on a document workflow transition (submit/verify/approve/post/cancel).</summary>
public record DocumentActionDto(string? Remarks = null);

/// <summary>Actor stamp for a document workflow step (projected from audit / verification / approval).</summary>
public record DocumentActorDto(Guid? UserId, string? UserName, DateTime? At, string? Remarks = null);

/// <summary>Submitted / Verified / Approved / Posted actors for UI panels (no schema churn).</summary>
public record DocumentWorkflowActorsDto(
    DocumentActorDto? Created,
    DocumentActorDto? Submitted,
    DocumentActorDto? Verified,
    DocumentActorDto? Approved,
    DocumentActorDto? Posted);

/// <summary>Result of a document workflow transition.</summary>
public record DocumentTransitionResultDto(
    Guid Id,
    string DocumentKind,
    string DocumentNo,
    DocumentStatus PreviousStatus,
    DocumentStatus Status,
    string Action);

// ---- Platform / Tenants ----
public static class SoftaxeModuleCatalog
{
    public static readonly string[] All =
    [
        "DASHBOARD", "ACCOUNTS", "SALES", "PURCHASE", "INVENTORY",
        "PRODUCTION", "PAYROLL", "FARM", "TAX", "REPORTS",
        "CONTRACTS", "LAB", "COSTCENTRE", "CHEQUES", "DEMAND", "HRFINANCE",
        "BUDGET", "BRANDS", "ATTENDANCE", "DAYCLOSE", "EOBI", "WEIGHBRIDGE", "POS",
        "STOCKTRANSFER", "RETURNS", "LEAVES", "SALARY"
    ];

    public static readonly string[] Default =
    [
        "DASHBOARD", "ACCOUNTS", "SALES", "PURCHASE", "INVENTORY", "PRODUCTION", "PAYROLL", "REPORTS"
    ];
}

public record TenantListDto(
    Guid Id, string Name, string Code, string Slug, string Status,
    IndustryType Industry, bool IsActive, string? CompanyName, string? City,
    int UserCount, IReadOnlyList<string> Modules, DateTime CreatedAt);

public record TenantDetailDto(
    Guid Id, string Name, string Code, string Slug, string Status,
    IndustryType Industry, bool IsActive, CompanyDto? Company,
    IReadOnlyList<string> Modules, IReadOnlyList<UserDto> Users, DateTime CreatedAt);

public record ProvisionTenantRequest(
    string Name,
    string Code,
    string? Slug,
    IndustryType Industry,
    string AdminFullName,
    string AdminEmail,
    string AdminPassword,
    string? City,
    string? Address,
    string? Phone,
    string Country,
    string Currency,
    string? Ntn,
    IReadOnlyList<string>? Modules);

public record ProvisionTenantResult(Guid TenantId, Guid CompanyId, Guid AdminUserId, string Message);

// ---- Reports ----
public record LedgerLineDto(
    DateTime Date, string VoucherNo, string VoucherType, string? Narration,
    decimal Debit, decimal Credit, decimal Balance);

public record LedgerReportDto(
    Guid AccountId, string AccountCode, string AccountName,
    DateTime From, DateTime To, decimal OpeningBalance, decimal ClosingBalance,
    IReadOnlyList<LedgerLineDto> Lines);

public record TrialBalanceLineDto(
    Guid AccountId, string Code, string Name, string AccountType,
    decimal Debit, decimal Credit, decimal Balance);

public record TrialBalanceReportDto(
    DateTime AsOf, decimal TotalDebit, decimal TotalCredit,
    IReadOnlyList<TrialBalanceLineDto> Lines);

public record AgingBucketDto(string PartyCode, string PartyName, decimal Current, decimal Days1To30, decimal Days31To60, decimal Days61To90, decimal Days90Plus, decimal Total);

public record AgingReportDto(
    string Kind, // AR or AP
    DateTime AsOf,
    decimal GrandTotal,
    IReadOnlyList<AgingBucketDto> Rows);

// ---- Production ----
public record BomLineDto(Guid? Id, Guid ComponentProductId, string? ComponentProductName, decimal QtyPer, Guid? UomId = null, string? UomCode = null, decimal ScrapPercent = 0, int LineNo = 0);
public record BomDto(Guid Id, string Code, string Name, Guid FinishedProductId, string? FinishedProductName, int Version, DateTime EffectiveFrom, DateTime? EffectiveTo, decimal ScrapPercent, bool IsActive, IReadOnlyList<BomLineDto> Lines);
public record BomUpsertDto(string Code, string Name, Guid FinishedProductId, int Version = 1, DateTime? EffectiveFrom = null, DateTime? EffectiveTo = null, decimal ScrapPercent = 0, bool IsActive = true, IReadOnlyList<BomLineDto>? Lines = null);

public record ProductionComponentDto(Guid? Id, Guid ProductId, string? ProductCode, string? ProductName, decimal PlannedQty, decimal ActualQty, decimal UnitCost, decimal TotalCost);
public record ProductionOutputDto(Guid? Id, Guid ProductId, string? ProductCode, string? ProductName, decimal Qty, decimal UnitCost, decimal TotalCost, bool IsByProduct);
public record ProductionVarianceDto(Guid? Id, string Kind, Guid ProductId, string? ProductCode, string? ProductName, decimal Qty, decimal UnitCost, decimal TotalCost, string? Narration);
public record ProductionOrderDto(
    Guid Id, string OrderNo, DateTime OrderDate, Guid BomId, string? BomCode, Guid FinishedProductId, string? FinishedProductName,
    Guid WarehouseId, string? WarehouseName, decimal QtyOrdered, decimal QtyCompleted,
    string Status, DocumentStatus DocumentStatus, decimal MaterialCost, decimal LaborCost, decimal OverheadCost,
    decimal TotalCost, decimal UnitCost, string? Remarks,
    IReadOnlyList<ProductionComponentDto> Components, IReadOnlyList<ProductionOutputDto> Outputs, IReadOnlyList<ProductionVarianceDto> Variances);
public record ProductionOrderCreateDto(Guid BomId, Guid WarehouseId, DateTime? OrderDate, decimal QtyOrdered, decimal LaborCost = 0, decimal OverheadCost = 0, string? Remarks = null);
public record ProductionVarianceCreateDto(string Kind, Guid ProductId, decimal Qty, string? Narration = null);
public record ProductionCostingDto(Guid OrderId, string OrderNo, decimal MaterialCost, decimal LaborCost, decimal OverheadCost, decimal TotalCost, decimal UnitCost, decimal QtyCompleted);

// Legacy aliases kept for older Angular clients
public record ProductionCreateDto(Guid BomId, DateTime Date, decimal BatchQty, string? Remarks, bool Post);

// ---- Farm ----
public record FarmDto(Guid Id, string Code, string Name, string FarmType, Guid? BranchId, bool IsActive, IReadOnlyList<FarmUnitDto> Units);
public record FarmUnitDto(Guid Id, Guid FarmId, string Code, string Name, int? Capacity, bool IsActive);
public record FarmUpsertDto(string Code, string Name, string FarmType = "Poultry", Guid? BranchId = null, bool IsActive = true);
public record FarmUnitUpsertDto(Guid FarmId, string Code, string Name, int? Capacity = null, bool IsActive = true);

public record FlockDto(Guid Id, string BatchNo, Guid FarmId, string? FarmName, Guid? FarmUnitId, string? FarmUnitName, string BirdType, DateTime PlacementDate, int QtyPlaced, int QtyCurrent, int QtyMortality, string Status, string? Remarks);
public record FlockCreateDto(Guid FarmId, Guid? FarmUnitId, string BatchNo, string BirdType, DateTime PlacementDate, int QtyPlaced, string? Remarks = null);

public record MortalityDto(Guid Id, Guid FlockId, DateTime Date, int Qty, string? Reason);
public record MortalityCreateDto(DateTime Date, int Qty, string? Reason = null);

public record FeedIssueLineDto(Guid? Id, Guid ProductId, string? ProductCode, string? ProductName, decimal Qty, decimal UnitCost, decimal TotalCost);
public record FeedIssueDto(Guid Id, string IssueNo, DateTime IssueDate, Guid FarmId, string? FarmName, Guid FlockId, string? FlockBatchNo, Guid WarehouseId, string? WarehouseName, DocumentStatus Status, string? Remarks, IReadOnlyList<FeedIssueLineDto> Lines);
public record FeedIssueCreateDto(Guid FarmId, Guid FlockId, Guid WarehouseId, DateTime? IssueDate, string? Remarks, IReadOnlyList<FeedIssueLineDto> Lines);

public record FarmTreatmentDto(Guid Id, string TreatmentNo, Guid FlockId, string? FlockBatchNo, DateTime Date, string Kind, Guid? ProductId, string? ProductName, Guid? WarehouseId, decimal Qty, string? BatchNo, string? Narration, DocumentStatus Status);
public record FarmTreatmentCreateDto(Guid FlockId, DateTime Date, string Kind, Guid? ProductId, Guid? WarehouseId, decimal Qty, string? BatchNo = null, string? Narration = null, bool Post = false);

public record FarmExpenseDto(Guid Id, string ExpenseNo, DateTime ExpenseDate, Guid FarmId, string? FarmName, Guid? FlockId, string Category, decimal Amount, Guid? CashBankAccountId, DocumentStatus Status, string? Narration);
public record FarmExpenseCreateDto(Guid FarmId, Guid? FlockId, DateTime ExpenseDate, string Category, decimal Amount, Guid? CashBankAccountId, string? Narration = null, bool Post = false);

public record FarmPerformanceDto(Guid FlockId, string BatchNo, int OpeningQty, int Placements, int MortalityQty, int ClosingQty, decimal MortalityPercent, decimal FeedConsumedKg, decimal TotalExpense, decimal? CostPerBird);

// Legacy farm DTOs kept for older Angular shape mapping
public record FarmFlockDto(Guid Id, string Code, string Name, string BirdType, DateTime PlacementDate, int PlacedQty, int CurrentQty, int MortalityQty, string? Shed, string Status, decimal FeedConsumedKg, decimal TotalExpense);
public record FarmFlockCreateDto(string Code, string Name, string BirdType, DateTime PlacementDate, int PlacedQty, string? Shed, string? Remarks);
public record FarmEventDto(Guid Id, Guid FarmFlockId, DateTime Date, string EventType, decimal Qty, decimal Amount, Guid? ProductId, string? Remarks);
public record FarmEventCreateDto(DateTime Date, string EventType, decimal Qty, decimal Amount, Guid? ProductId, string? Remarks);

// ---- Users / Inventory adjust ----
public record UserCreateDto(string Email, string FullName, string Password, string Role);
public record UserUpdateDto(string FullName, string Role, bool IsActive);
public record StockAdjustmentDto(Guid Id, string AdjustmentNo, DateTime Date, Guid ProductId, string? ProductName, decimal QtyChange, string Reason, string? Remarks);
public record StockAdjustmentCreateDto(DateTime Date, Guid ProductId, decimal QtyChange, string Reason, string? Remarks);

public record StockAdjustmentLineDto(Guid? Id, Guid ProductId, string? ProductName, decimal QtyDelta, Guid? WarehouseLocationId, string? LineReason);
public record StockAdjustmentDocDto(Guid Id, string AdjustmentNo, DateTime Date, Guid WarehouseId, string? WarehouseName, DocumentStatus Status, string Reason, string? Remarks, Guid? StockCountId, IReadOnlyList<StockAdjustmentLineDto> Lines, DocumentWorkflowActorsDto? Workflow = null);
public record StockAdjustmentDocCreateDto(DateTime Date, Guid WarehouseId, string Reason, string? Remarks, bool Post, IReadOnlyList<StockAdjustmentLineDto> Lines);

public record StockCountLineDto(Guid? Id, Guid ProductId, string? ProductName, decimal SystemQty, decimal CountedQty, decimal Variance, Guid? WarehouseLocationId);
public record StockCountDto(Guid Id, string CountNo, DateTime Date, Guid WarehouseId, string? WarehouseName, DocumentStatus Status, DateTime? SnapshotAt, string? Remarks, Guid? LinkedAdjustmentId, IReadOnlyList<StockCountLineDto> Lines);
public record StockCountCreateDto(DateTime Date, Guid WarehouseId, string? Remarks, IReadOnlyList<StockCountLineDto> Lines);

public record InventoryLedgerLineDto(DateTime Date, string SourceType, string? DocumentNo, Guid ProductId, string? ProductCode, Guid WarehouseId, string? WarehouseCode, decimal QtyIn, decimal QtyOut, decimal Balance, decimal UnitCost, decimal TotalCost, string? Narration);
public record StockValuationLineDto(Guid ProductId, string Code, string Name, Guid WarehouseId, string WarehouseCode, decimal Qty, decimal AverageCost, decimal Value);
public record StockMovementLineDto(Guid ProductId, string Code, string Name, Guid WarehouseId, string WarehouseCode, decimal Opening, decimal PurchaseIn, decimal TransferIn, decimal TransferOut, decimal SaleOut, decimal Adjustment, decimal Closing);

public record UnitConversionDto(Guid Id, Guid? ProductId, string? ProductName, Guid FromUomId, string? FromUomCode, Guid ToUomId, string? ToUomCode, decimal Factor, bool IsActive);
public record UnitConversionUpsertDto(Guid? ProductId, Guid FromUomId, Guid ToUomId, decimal Factor, bool IsActive = true);

public record WarehouseLocationDto(Guid Id, Guid WarehouseId, string Code, string Name, bool IsActive);
public record WarehouseLocationUpsertDto(Guid WarehouseId, string Code, string Name, bool IsActive = true);

public record WarehouseDto(Guid Id, string Code, string Name, string? Location, bool IsDefault);
public record WarehouseUpsertDto(string Code, string Name, string? Location, bool IsDefault);

public record PartySettlementDto(Guid Id, string DocNo, DateTime Date, Guid PartyId, string? PartyName, string SettlementType, string Mode, decimal Amount, string? Remarks);
public record PartySettlementCreateDto(DateTime Date, Guid PartyId, string SettlementType, string Mode, decimal Amount, string? Remarks);

// ---- Softaxe extensions ----
public record PurchaseContractLineDto(Guid? Id, Guid ProductId, string? ProductName, decimal Qty, decimal ReceivedQty, decimal Rate, string? Unit, string? Remarks);
public record PurchaseContractDto(Guid Id, string ContractNo, DateTime Date, DateTime? DeliveryDate, Guid PartyId, string? PartyName, string Status, string? Remarks, IReadOnlyList<PurchaseContractLineDto> Lines);
public record PurchaseContractCreateDto(DateTime Date, DateTime? DeliveryDate, Guid PartyId, string? Remarks, IReadOnlyList<PurchaseContractLineDto> Lines);

public record LabTestTypeDto(Guid Id, string Code, string Name, string? Unit, decimal? MinValue, decimal? MaxValue, bool IsActive);
public record LabTestTypeUpsertDto(string Code, string Name, string? Unit = null, decimal? MinValue = null, decimal? MaxValue = null, bool IsActive = true);
public record LabSampleDto(Guid Id, string SampleNo, DateTime SampleDate, Guid? ProductId, string? ProductName, Guid? WarehouseId, string? WarehouseName, string SourceType, Guid? SourceEntityId, Guid? FlockId, string Status, string? Remarks, IReadOnlyList<LabTestResultDto> Results);
public record LabSampleCreateDto(DateTime? SampleDate, Guid? ProductId, Guid? WarehouseId, string SourceType = "Inventory", Guid? SourceEntityId = null, Guid? FlockId = null, string? Remarks = null);
public record LabTestResultDto(Guid Id, Guid SampleId, string? SampleNo, Guid TestTypeId, string? TestTypeCode, string? TestTypeName, DateTime TestedAt, decimal Value, string Decision, string Status, string? Narration);
public record LabTestResultCreateDto(Guid SampleId, Guid TestTypeId, decimal Value, DateTime? TestedAt = null, string? Narration = null);
// Legacy lab result shape
public record LabResultDto(Guid Id, string ResultNo, DateTime Date, Guid? PartyId, string? PartyName, Guid? ProductId, string? ProductName, Guid? LabTestTypeId, string? TestName, string? VehicleNo, decimal Bags, decimal Percentage, decimal PartyDeductionKg, decimal StockDeductionKg, string? Remarks);
public record LabResultCreateDto(DateTime Date, Guid? PartyId, Guid? ProductId, Guid? LabTestTypeId, string? VehicleNo, decimal Bags, decimal Percentage, decimal PartyDeductionKg, decimal StockDeductionKg, string? Remarks);

public record CostCentreDto(Guid Id, string Code, string Name, string? Location, bool IsActive, string? Remarks);
public record CostCentreUpsertDto(string Code, string Name, string? Location, bool IsActive, string? Remarks);

public record ChequeBookDto(Guid Id, string BookNo, DateTime Date, string BankName, long ChequeFrom, long ChequeTo, long NextChequeNo, bool IsClosed, string? Remarks);
public record ChequeBookCreateDto(string BookNo, DateTime Date, string BankName, long ChequeFrom, long ChequeTo, string? Remarks);
public record PostDatedChequeDto(Guid Id, string DocNo, DateTime VoucherDate, DateTime ChequeDate, string ChequeNo, Guid? PartyId, string? PartyName, Guid? ChequeBookId, decimal Amount, string Direction, string Status, DateTime? ClearedDate, DateTime? BouncedDate, string? Remarks);
public record PostDatedChequeCreateDto(DateTime VoucherDate, DateTime ChequeDate, string ChequeNo, Guid? PartyId, Guid? ChequeBookId, decimal Amount, string Direction, string? Remarks);
public record PostDatedChequeStatusDto(string Status);

public record FarmShareHolderDto(Guid Id, Guid FarmFlockId, string? FlockName, string HolderName, string? PartyCode, decimal SharePercent, string ShareType, string? Remarks);
public record FarmShareHolderCreateDto(Guid FarmFlockId, string HolderName, string? PartyCode, decimal SharePercent, string ShareType, string? Remarks);

public record SalesAreaDto(Guid Id, string Code, string Name, bool IsActive);
public record SalesAreaUpsertDto(string Code, string Name, bool IsActive);
public record SalesmanDto(Guid Id, string Code, string Name, Guid? SalesAreaId, string? AreaName, string? Phone, bool IsActive);
public record SalesmanUpsertDto(string Code, string Name, Guid? SalesAreaId, string? Phone, bool IsActive);
public record SalesTargetDto(Guid Id, Guid? SalesmanId, string? SalesmanName, Guid? SalesAreaId, string? AreaName, int Year, int Month, decimal TargetAmount, decimal AchievedAmount, string? Remarks);
public record SalesTargetUpsertDto(Guid? SalesmanId, Guid? SalesAreaId, int Year, int Month, decimal TargetAmount, decimal AchievedAmount, string? Remarks);

public record MaterialDemandLineDto(Guid? Id, Guid ProductId, string? ProductName, decimal Qty, string? Remarks);
public record MaterialDemandDto(Guid Id, string DemandNo, DateTime Date, string DemandType, string Status, string? RequestedBy, string? Remarks, IReadOnlyList<MaterialDemandLineDto> Lines);
public record MaterialDemandCreateDto(DateTime Date, string DemandType, string? RequestedBy, string? Remarks, IReadOnlyList<MaterialDemandLineDto> Lines);

public record SalaryAdvanceDto(Guid Id, string DocNo, DateTime Date, Guid EmployeeId, string? EmployeeName, decimal Amount, string? Remarks);
public record SalaryAdvanceCreateDto(DateTime Date, Guid EmployeeId, decimal Amount, string? Remarks);
public record StaffLoanDto(Guid Id, string DocNo, DateTime Date, Guid EmployeeId, string? EmployeeName, decimal LoanAmount, int Months, decimal InstallmentAmount, bool IsActive, string? Remarks);
public record StaffLoanCreateDto(DateTime Date, Guid EmployeeId, decimal LoanAmount, int Months, decimal InstallmentAmount, string? Remarks);

public record BrandDto(Guid Id, string Code, string Name, bool IsActive);
public record BrandUpsertDto(string Code, string Name, bool IsActive);
public record ProductConversionDto(Guid Id, Guid ProductId, string? ProductName, string Unit, decimal Packing, decimal PackingSize, decimal PurchaseRate, decimal MinRate, decimal MaxRate);
public record ProductConversionUpsertDto(Guid ProductId, string Unit, decimal Packing, decimal PackingSize, decimal PurchaseRate, decimal MinRate, decimal MaxRate);

public record BudgetLineDto(Guid? Id, Guid? AccountId, string? AccountName, string Description, decimal Qty, decimal Amount);
public record BudgetDto(Guid Id, string BudgetNo, DateTime Date, int Year, int Month, string Status, string? Remarks, decimal TotalAmount, IReadOnlyList<BudgetLineDto> Lines);
public record BudgetCreateDto(DateTime Date, int Year, int Month, string? Remarks, IReadOnlyList<BudgetLineDto> Lines);

public record AttendanceDto(Guid Id, DateTime Date, Guid EmployeeId, string? EmployeeName, string Status, decimal? OvertimeHours, string? Remarks);
public record AttendanceCreateDto(DateTime Date, Guid EmployeeId, string Status, decimal? OvertimeHours = null, string? Remarks = null);
public record AttendanceSummaryDto(Guid EmployeeId, string? EmployeeName, int Year, int Month, int Present, int Absent, int Late, int HalfDay, int Overtime, decimal OvertimeHours);
public record OvertimeDto(Guid Id, string DocNo, DateTime Date, Guid EmployeeId, string? EmployeeName, decimal Hours, decimal PerHourRate, decimal Amount, string? Remarks);
public record OvertimeCreateDto(DateTime Date, Guid EmployeeId, decimal Hours, decimal PerHourRate, string? Remarks);
public record DayCloseDto(Guid Id, DateTime CloseDate, bool IsClosed, string? Remarks, string? ClosedBy);
public record DayCloseCreateDto(DateTime CloseDate, string? Remarks);

public record EobiDto(Guid Id, string DocNo, DateTime Date, Guid EmployeeId, string? EmployeeName, decimal Amount, bool IsActive, string? Remarks);
public record EobiCreateDto(DateTime Date, Guid EmployeeId, decimal Amount, string? Remarks);

public record WeighbridgeTicketDto(Guid Id, string TicketNo, DateTime Date, string Direction, string? VehicleNo, Guid? VehicleId, Guid? PartyId, string? PartyName, Guid? ProductId, string? ProductName, decimal FirstWeight, decimal? SecondWeight, decimal? NetWeight, string Status, Guid? GatePassId, string? Remarks);
public record WeighbridgeTicketCreateDto(Guid VehicleId, string Direction = "Inward", Guid? ScaleId = null, Guid? PartyId = null, Guid? ProductId = null, string? Remarks = null);
public record WeighbridgeFirstWeightDto(decimal FirstWeight);
public record WeighbridgeCompleteDto(decimal SecondWeight);
public record WeighbridgeScaleDto(Guid Id, string Code, string Name, string? Location, bool IsActive);

public record VehicleTypeDto(Guid Id, string Code, string Name, bool IsActive);
public record VehicleDto(Guid Id, string Code, Guid VehicleTypeId, string? VehicleTypeName, string? Make, string? Model, bool IsActive);
public record DriverDto(Guid Id, string Code, string Name, Guid? EmployeeId, string? LicenseNo, bool IsActive);
public record GatePassDto(Guid Id, string GatePassNo, string Direction, DateTime PassDate, Guid? VehicleId, string? VehicleNo, Guid? WeighmentId, string? SourceDocType, Guid? SourceEntityId, DocumentStatus Status, string? Remarks);

public record PosTerminalDto(Guid Id, string Code, string Name, Guid WarehouseId, string? WarehouseName, Guid? BranchId, bool IsActive);
public record PosShiftDto(Guid Id, string ShiftNo, Guid TerminalId, string? TerminalCode, DateTime OpenedAt, decimal OpeningCash, DateTime? ClosedAt, decimal? ExpectedCash, decimal? ActualCash, decimal? Variance, string Status);
public record PosShiftOpenDto(Guid TerminalId, decimal OpeningCash);
public record PosShiftCloseDto(decimal ActualCash);
public record PosSaleLineDto(Guid? Id, Guid ProductId, string? ProductName, decimal Qty, decimal Rate, decimal Amount);
public record PosPaymentDto(Guid? Id, string Tender, decimal Amount, Guid? CashBankAccountId, Guid? CustomerPaymentId);
public record PosSaleDto(Guid Id, string SaleNo, Guid ShiftId, Guid TerminalId, Guid PartyId, string? PartyName, DateTime SaleDate, decimal SubTotal, decimal GrandTotal, DocumentStatus Status, Guid? SalesOrderId, Guid? DeliveryOrderId, Guid? SalesInvoiceId, string? Remarks, IReadOnlyList<PosSaleLineDto> Lines, IReadOnlyList<PosPaymentDto> Payments);
public record PosSaleCreateDto(Guid ShiftId, DateTime? SaleDate, string? Remarks, IReadOnlyList<PosSaleLineDto> Lines, IReadOnlyList<PosPaymentDto> Payments);

// Legacy aliases
public record PosSaleLegacyCreateDto(DateTime Date, Guid? PartyId, string PaymentMode, decimal Discount, decimal Tax, string? Remarks, IReadOnlyList<PosSaleLineDto> Lines);

public record StockTransferLineDto(Guid? Id, Guid ProductId, string? ProductName, decimal Qty, Guid? FromLocationId = null, Guid? ToLocationId = null);
public record StockTransferDto(Guid Id, string TransferNo, DateTime Date, Guid FromWarehouseId, string? FromWarehouseName, Guid ToWarehouseId, string? ToWarehouseName, DocumentStatus Status, string? Remarks, IReadOnlyList<StockTransferLineDto> Lines, DocumentWorkflowActorsDto? Workflow = null);
public record StockTransferCreateDto(DateTime Date, Guid FromWarehouseId, Guid ToWarehouseId, string? Remarks, bool Post, IReadOnlyList<StockTransferLineDto> Lines);

public record TradeReturnLineDto(Guid? Id, Guid ProductId, string? ProductName, decimal Qty, decimal Rate, decimal Amount);
public record TradeReturnDto(Guid Id, string ReturnNo, DateTime Date, Guid PartyId, string? PartyName, string Kind, DocumentStatus Status, decimal SubTotal, decimal Discount, decimal Tax, decimal GrandTotal, string? Remarks, IReadOnlyList<TradeReturnLineDto> Lines);
public record TradeReturnCreateDto(DateTime Date, Guid PartyId, decimal Discount, decimal Tax, string? Remarks, bool Post, IReadOnlyList<TradeReturnLineDto> Lines);

public record LeaveTypeDto(Guid Id, string Code, string Name, bool Paid, decimal AnnualAllowanceDays, bool IsActive);
public record LeaveBalanceDto(Guid Id, Guid EmployeeId, string? EmployeeName, Guid LeaveTypeId, string? LeaveTypeName, int Year, decimal Entitled, decimal Used, decimal Remaining);
public record LeaveDto(Guid Id, string DocNo, Guid EmployeeId, string? EmployeeName, Guid LeaveTypeId, string? LeaveTypeName, DateTime FromDate, DateTime ToDate, decimal Days, DocumentStatus Status, string? Remarks);
public record LeaveCreateDto(Guid EmployeeId, Guid LeaveTypeId, DateTime FromDate, DateTime ToDate, string? Remarks);

public record PayrollPeriodDto(Guid Id, int Year, int Month, DateTime StartDate, DateTime EndDate, string Status);
public record PayrollPeriodCreateDto(int Year, int Month);

public record PayrollLineDto(Guid? Id, Guid ComponentId, string? ComponentCode, string? ComponentName, string ComponentType, decimal Amount);
public record PayrollEmployeeDto(Guid Id, Guid EmployeeId, string? EmployeeCode, string? EmployeeName, decimal Basic, decimal Gross, decimal Deductions, decimal Net, IReadOnlyList<PayrollLineDto> Lines);
public record PayrollRunDto(Guid Id, string RunNo, Guid PeriodId, int Year, int Month, DateTime RunDate, DocumentStatus Status, decimal GrossTotal, decimal DeductionTotal, decimal NetTotal, string? Remarks, IReadOnlyList<PayrollEmployeeDto> Employees);
public record PayrollRunCreateDto(Guid PeriodId, DateTime? RunDate = null, string? Remarks = null);

public record SalaryPaymentDto(Guid Id, string PaymentNo, Guid PayrollRunId, DateTime PaymentDate, Guid CashBankAccountId, string? CashBankName, decimal Amount, DocumentStatus Status, string? Remarks);
public record SalaryPaymentCreateDto(Guid PayrollRunId, Guid CashBankAccountId, DateTime? PaymentDate = null, string? Remarks = null, bool Post = true);

// Legacy salary sheet DTOs kept for stub compatibility
public record SalarySheetLineDto(Guid Id, Guid EmployeeId, string? EmployeeName, string? Department, int Year, int Month, decimal Basic, decimal Allowances, decimal Deductions, decimal NetSalary, string Status, DateTime? PaidOn);
public record SalarySheetDto(int Year, int Month, int Count, decimal TotalNet, IReadOnlyList<SalarySheetLineDto> Lines);
public record SalaryGenerateDto(int Year, int Month, decimal DefaultAllowancePercent, decimal DefaultDeductionPercent);
public record SalaryPayDto(Guid Id);

// ---- Phase 9 BI ----
public record ReportFilterDto(
    DateTime? From = null,
    DateTime? To = null,
    DateTime? AsOf = null,
    Guid? BranchId = null,
    Guid? FinancialYearId = null,
    Guid? PeriodId = null,
    Guid? AccountId = null,
    Guid? PartyId = null,
    Guid? WarehouseId = null,
    Guid? ProductId = null);

public record ReportKpiDto(string Key, string Label, decimal Value, string? Unit = null, string? DrillDown = null);
public record ReportSectionDto(string Key, string Title, IReadOnlyList<ReportKpiDto> Kpis);
public record ExecutiveDashboardDto(DateTime From, DateTime To, IReadOnlyList<ReportSectionDto> Sections);

public record NamedAmountRowDto(string Code, string Name, decimal Amount, decimal? Qty, string? Extra);
public record NamedReportDto(string Title, DateTime? From, DateTime? To, DateTime? AsOf, decimal GrandTotal, IReadOnlyList<NamedAmountRowDto> Rows, IReadOnlyDictionary<string, decimal>? Totals);

public record CashFlowLineDto(string Category, string Label, decimal Amount);
public record CashFlowReportDto(DateTime From, DateTime To, decimal OpeningCash, decimal Inflows, decimal Outflows, decimal NetChange, decimal ClosingCash, IReadOnlyList<CashFlowLineDto> Lines);

public record StockAgingLineDto(Guid ProductId, string Code, string Name, Guid WarehouseId, string WarehouseCode, decimal Qty, decimal Value, int DaysSinceMovement, string Bucket);
public record WarehouseComparisonLineDto(Guid WarehouseId, string WarehouseCode, int SkuCount, decimal TotalQty, decimal TotalValue);
public record InventoryTurnoverDto(DateTime From, DateTime To, decimal Cogs, decimal AvgInventoryValue, decimal Turnover);

// ---- Phase 10A / 10B AI ----
public record AiChatRequest(string Message, Guid? ConversationId = null);
public record AiChatSourceDto(string Tool, string Label);
public record AiActionFieldDto(string Key, string Label, string? Value, bool Editable = false, string? InputType = null);
public record AiActionPreviewDto(
    Guid Id,
    string ActionType,
    string Status,
    string PermissionRequired,
    string Summary,
    IReadOnlyList<AiActionFieldDto> Fields,
    IReadOnlyList<string> ValidationErrors,
    Guid? ResultDocumentId = null,
    string? ResultDocumentNo = null);
public record AiChatResponse(
    Guid ConversationId,
    string Intent,
    string Answer,
    IReadOnlyList<AiChatSourceDto> Sources,
    bool Denied = false,
    string? DeniedPermission = null,
    string Kind = "Answer",
    AiActionPreviewDto? Action = null,
    AiAgentRunDto? Agent = null);
public record AiConversationListItemDto(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt);
public record AiMessageDto(Guid Id, string Role, string Content, string? Intent, DateTime CreatedAt);
public record AiConversationDetailDto(Guid Id, string Title, IReadOnlyList<AiMessageDto> Messages);
public record AiActionUpdateRequest(
    decimal? Qty = null,
    decimal? Rate = null,
    decimal? Amount = null,
    decimal? Tax = null,
    string? Remarks = null,
    Guid? CompanyId = null,
    Guid? BranchId = null,
    IReadOnlyList<InvoiceLineDto>? Lines = null,
    IReadOnlyList<PaymentApplicationDto>? Applications = null,
    IReadOnlyList<SupplierPaymentApplicationDto>? SupplierApplications = null,
    IReadOnlyList<JournalLineDto>? JournalLines = null,
    Guid? FromWarehouseId = null,
    Guid? ToWarehouseId = null);
public record AiActionConfirmRequest(Guid? CompanyId = null, Guid? BranchId = null);
public record AiActionConfirmResponse(
    Guid ActionId,
    string Status,
    Guid? DocumentId,
    string? DocumentNo,
    string Message);

// ---- Phase 10C AI Agents ----
public record AiAgentStepDto(
    Guid Id,
    int StepNo,
    string ToolName,
    string Status,
    bool RequiresConfirmation,
    Guid? LinkedPendingActionId,
    string? Summary,
    DateTime? ExecutedAt);
public record AiAgentProposedPoDto(
    Guid? PendingActionId,
    Guid SupplierId,
    string SupplierName,
    IReadOnlyList<string> Lines,
    decimal TotalQty);
public record AiAgentRunDto(
    Guid Id,
    string AgentType,
    string Goal,
    string Status,
    string Summary,
    IReadOnlyList<AiAgentStepDto> Steps,
    IReadOnlyList<AiAgentProposedPoDto> ProposedOrders,
    IReadOnlyList<AiActionPreviewDto> PendingActions,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string? FailureReason = null);
public record AiAgentConfirmRequest(Guid? CompanyId = null, Guid? BranchId = null);
public record AiAgentConfirmResponse(
    Guid AgentRunId,
    string Status,
    IReadOnlyList<AiActionConfirmResponse> Results,
    string Message);

// ---- Phase 10D Predictive ----
public record ForecastHorizonPointDto(int Days, decimal ProjectedOnHand);
public record InventoryDemandForecastLineDto(
    Guid ProductId,
    string Code,
    string Name,
    decimal OnHand,
    decimal ReorderLevel,
    decimal AvgDailyUsage,
    int LeadTimeDays,
    decimal SafetyProxy,
    decimal OnOrder,
    IReadOnlyList<ForecastHorizonPointDto> Horizons,
    int? StockOutRiskDays,
    decimal RecommendedOrderQty,
    decimal? AnomalyPct,
    string Confidence,
    int DataDays,
    string? Message);
public record InventoryDemandForecastReportDto(
    DateTime AsOf,
    Guid? CompanyId,
    Guid? BranchId,
    IReadOnlyList<InventoryDemandForecastLineDto> Lines,
    Guid? PredictionId = null);

