using SoftaxeERP.Domain.Enums;

namespace SoftaxeERP.Application.DTOs;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, UserDto User, CompanyDto Company);
public record UserDto(Guid Id, string Email, string FullName, string Role, bool IsPlatformAdmin);
public record CompanyDto(
    Guid Id, string Name, string ShortName, string? Address, string? City,
    string Country, string? Phone, string? Email, string Currency, string? Ntn,
    IndustryType Industry, decimal DefaultSaleTaxPercent = 0, decimal DefaultPurchaseTaxPercent = 0,
    bool SyncToFbr = false, bool TaxModule = false);

public record DashboardSummaryDto(
    decimal SalesToday,
    decimal PurchasesToday,
    decimal Receivables,
    decimal Payables,
    IReadOnlyList<VoucherListDto> RecentVouchers);

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
    decimal ReorderLevel, bool IsActive, bool IsRawMaterial, bool IsFinishedGood);

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

public record PurchaseInvoiceDto(Guid Id, string InvoiceNo, DateTime Date, Guid PartyId, string? PartyName, DocumentStatus Status, decimal SubTotal, decimal Discount, decimal Tax, decimal GrandTotal, string? Remarks, IReadOnlyList<InvoiceLineDto> Lines);
public record PurchaseInvoiceCreateDto(DateTime Date, Guid PartyId, decimal Discount, decimal Tax, string? Remarks, bool Post, IReadOnlyList<InvoiceLineDto> Lines, Guid? GoodsReceiptId = null, Guid? PurchaseOrderId = null);

public record PurchaseOrderDto(Guid Id, string OrderNo, DateTime Date, Guid PartyId, string? PartyName, Guid? WarehouseId, DocumentStatus Status, decimal SubTotal, decimal Tax, decimal GrandTotal, string? Remarks, IReadOnlyList<InvoiceLineDto> Lines);
public record PurchaseOrderCreateDto(DateTime Date, Guid PartyId, Guid? WarehouseId, decimal Tax, string? Remarks, bool Post, IReadOnlyList<InvoiceLineDto> Lines);

public record GoodsReceiptDto(Guid Id, string ReceiptNo, DateTime Date, Guid PartyId, string? PartyName, Guid WarehouseId, Guid? PurchaseOrderId, DocumentStatus Status, string? Remarks, IReadOnlyList<InvoiceLineDto> Lines);
public record GoodsReceiptCreateDto(DateTime Date, Guid PartyId, Guid WarehouseId, Guid? PurchaseOrderId, string? Remarks, bool Post, IReadOnlyList<InvoiceLineDto> Lines);

public record PayableBalanceDto(Guid PartyId, string? PartyName, decimal Balance);

public record StockDto(Guid ProductId, string Code, string Name, string Unit, decimal StockQty, decimal ReorderLevel, bool LowStock);
public record EmployeeDto(Guid Id, string Code, string Name, string? Designation, string? Department, DateTime? JoinDate, decimal BasicSalary, string? Phone, bool IsActive);
public record EmployeeUpsertDto(string Code, string Name, string? Designation, string? Department, DateTime? JoinDate, decimal BasicSalary, string? Phone, string? Email, string? Address);

public record CompanyUpdateDto(
    string Name, string ShortName, string? Address, string? City, string Country,
    string? Phone, string? Email, string Currency, string? Ntn, IndustryType Industry,
    decimal DefaultSaleTaxPercent = 0, decimal DefaultPurchaseTaxPercent = 0,
    bool SyncToFbr = false, bool TaxModule = false);

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
public record BomLineDto(Guid? Id, Guid RawMaterialId, string? RawMaterialName, decimal Qty);
public record BomDto(Guid Id, string Code, string Name, Guid FinishedProductId, string? FinishedProductName, decimal OutputQty, bool IsActive, IReadOnlyList<BomLineDto> Lines);
public record BomUpsertDto(string Code, string Name, Guid FinishedProductId, decimal OutputQty, IReadOnlyList<BomLineDto> Lines);
public record ProductionOrderDto(Guid Id, string OrderNo, Guid BomId, string? BomName, DateTime Date, decimal BatchQty, decimal OutputQty, DocumentStatus Status, string? Remarks);
public record ProductionCreateDto(Guid BomId, DateTime Date, decimal BatchQty, string? Remarks, bool Post);

// ---- Farm ----
public record FarmFlockDto(Guid Id, string Code, string Name, string BirdType, DateTime PlacementDate, int PlacedQty, int CurrentQty, int MortalityQty, string? Shed, string Status, decimal FeedConsumedKg, decimal TotalExpense);
public record FarmFlockCreateDto(string Code, string Name, string BirdType, DateTime PlacementDate, int PlacedQty, string? Shed, string? Remarks);
public record FarmEventDto(Guid Id, Guid FarmFlockId, DateTime Date, string EventType, decimal Qty, decimal Amount, Guid? ProductId, string? Remarks);
public record FarmEventCreateDto(DateTime Date, string EventType, decimal Qty, decimal Amount, Guid? ProductId, string? Remarks);

// ---- Users / Inventory adjust ----
public record UserCreateDto(string Email, string FullName, string Password, string Role);
public record UserUpdateDto(string FullName, string Role, bool IsActive);
public record StockAdjustmentDto(Guid Id, string AdjustmentNo, DateTime Date, Guid ProductId, string? ProductName, decimal QtyChange, string Reason, string? Remarks);
public record StockAdjustmentCreateDto(DateTime Date, Guid ProductId, decimal QtyChange, string Reason, string? Remarks);

public record WarehouseDto(Guid Id, string Code, string Name, string? Location, bool IsDefault);
public record WarehouseUpsertDto(string Code, string Name, string? Location, bool IsDefault);

public record PartySettlementDto(Guid Id, string DocNo, DateTime Date, Guid PartyId, string? PartyName, string SettlementType, string Mode, decimal Amount, string? Remarks);
public record PartySettlementCreateDto(DateTime Date, Guid PartyId, string SettlementType, string Mode, decimal Amount, string? Remarks);

// ---- Softaxe extensions ----
public record PurchaseContractLineDto(Guid? Id, Guid ProductId, string? ProductName, decimal Qty, decimal ReceivedQty, decimal Rate, string? Unit, string? Remarks);
public record PurchaseContractDto(Guid Id, string ContractNo, DateTime Date, DateTime? DeliveryDate, Guid PartyId, string? PartyName, string Status, string? Remarks, IReadOnlyList<PurchaseContractLineDto> Lines);
public record PurchaseContractCreateDto(DateTime Date, DateTime? DeliveryDate, Guid PartyId, string? Remarks, IReadOnlyList<PurchaseContractLineDto> Lines);

public record LabTestTypeDto(Guid Id, string Code, string Name, string? Unit, bool IsActive);
public record LabTestTypeUpsertDto(string Code, string Name, string? Unit, bool IsActive);
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

public record AttendanceDto(Guid Id, DateTime Date, Guid EmployeeId, string? EmployeeName, string Status, string? Shift, string? Remarks);
public record AttendanceCreateDto(DateTime Date, Guid EmployeeId, string Status, string? Shift, string? Remarks);
public record OvertimeDto(Guid Id, string DocNo, DateTime Date, Guid EmployeeId, string? EmployeeName, decimal Hours, decimal PerHourRate, decimal Amount, string? Remarks);
public record OvertimeCreateDto(DateTime Date, Guid EmployeeId, decimal Hours, decimal PerHourRate, string? Remarks);
public record DayCloseDto(Guid Id, DateTime CloseDate, bool IsClosed, string? Remarks, string? ClosedBy);
public record DayCloseCreateDto(DateTime CloseDate, string? Remarks);

public record EobiDto(Guid Id, string DocNo, DateTime Date, Guid EmployeeId, string? EmployeeName, decimal Amount, bool IsActive, string? Remarks);
public record EobiCreateDto(DateTime Date, Guid EmployeeId, decimal Amount, string? Remarks);

public record WeighbridgeTicketDto(Guid Id, string TicketNo, DateTime Date, string Direction, string? VehicleNo, Guid? PartyId, string? PartyName, Guid? ProductId, string? ProductName, decimal FirstWeight, decimal SecondWeight, decimal NetWeight, string Status, string? Remarks);
public record WeighbridgeTicketCreateDto(DateTime Date, string Direction, string? VehicleNo, Guid? PartyId, Guid? ProductId, decimal FirstWeight, decimal SecondWeight, string? Remarks);
public record WeighbridgeCompleteDto(decimal SecondWeight);

public record PosSaleLineDto(Guid? Id, Guid ProductId, string? ProductName, decimal Qty, decimal Rate, decimal Amount);
public record PosSaleDto(Guid Id, string BillNo, DateTime Date, Guid? PartyId, string? PartyName, string PaymentMode, decimal SubTotal, decimal Discount, decimal Tax, decimal GrandTotal, string Status, string? Remarks, IReadOnlyList<PosSaleLineDto> Lines);
public record PosSaleCreateDto(DateTime Date, Guid? PartyId, string PaymentMode, decimal Discount, decimal Tax, string? Remarks, IReadOnlyList<PosSaleLineDto> Lines);

public record StockTransferLineDto(Guid? Id, Guid ProductId, string? ProductName, decimal Qty);
public record StockTransferDto(Guid Id, string TransferNo, DateTime Date, Guid FromWarehouseId, string? FromWarehouseName, Guid ToWarehouseId, string? ToWarehouseName, string Status, string? Remarks, IReadOnlyList<StockTransferLineDto> Lines);
public record StockTransferCreateDto(DateTime Date, Guid FromWarehouseId, Guid ToWarehouseId, string? Remarks, IReadOnlyList<StockTransferLineDto> Lines);

public record TradeReturnLineDto(Guid? Id, Guid ProductId, string? ProductName, decimal Qty, decimal Rate, decimal Amount);
public record TradeReturnDto(Guid Id, string ReturnNo, DateTime Date, Guid PartyId, string? PartyName, string Kind, DocumentStatus Status, decimal SubTotal, decimal Discount, decimal Tax, decimal GrandTotal, string? Remarks, IReadOnlyList<TradeReturnLineDto> Lines);
public record TradeReturnCreateDto(DateTime Date, Guid PartyId, decimal Discount, decimal Tax, string? Remarks, bool Post, IReadOnlyList<TradeReturnLineDto> Lines);

public record LeaveDto(Guid Id, string DocNo, Guid EmployeeId, string? EmployeeName, DateTime FromDate, DateTime ToDate, string LeaveType, decimal Days, string Status, string? Remarks);
public record LeaveCreateDto(Guid EmployeeId, DateTime FromDate, DateTime ToDate, string LeaveType, string? Remarks);

public record SalarySheetLineDto(Guid Id, Guid EmployeeId, string? EmployeeName, string? Department, int Year, int Month, decimal Basic, decimal Allowances, decimal Deductions, decimal NetSalary, string Status, DateTime? PaidOn);
public record SalarySheetDto(int Year, int Month, int Count, decimal TotalNet, IReadOnlyList<SalarySheetLineDto> Lines);
public record SalaryGenerateDto(int Year, int Month, decimal DefaultAllowancePercent, decimal DefaultDeductionPercent);
public record SalaryPayDto(Guid Id);

