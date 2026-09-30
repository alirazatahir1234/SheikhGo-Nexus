using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Domain.Entities;
using SheikhGoNexusERP.Domain.Enums;

namespace SheikhGoNexusERP.Application.Interfaces;

public interface ITenantContext
{
    Guid TenantId { get; }
    Guid? UserId { get; }
    string? ClientIp { get; }
    Guid? CompanyId { get; }
    Guid? BranchId { get; }
    Guid? FinancialYearId { get; }
    void Set(Guid tenantId, Guid? userId, string? clientIp = null, Guid? companyId = null, Guid? branchId = null, Guid? financialYearId = null);
}

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default);
    Task<ExecutiveDashboardDto> GetExecutiveAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
}

public interface IAccountService
{
    Task<IReadOnlyList<AccountDto>> GetAllAsync(CancellationToken ct = default);
    Task<AccountDto> CreateAsync(AccountUpsertDto dto, CancellationToken ct = default);
}

public interface IPartyService
{
    Task<IReadOnlyList<PartyDto>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<PartyDto> CreateAsync(PartyUpsertDto dto, CancellationToken ct = default);
    Task<PartyDto?> UpdateAsync(Guid id, PartyUpsertDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(ProductUpsertDto dto, CancellationToken ct = default);
    Task<ProductDto?> UpdateAsync(Guid id, ProductUpsertDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IVoucherService
{
    Task<IReadOnlyList<VoucherListDto>> GetAllAsync(CancellationToken ct = default);
    Task<VoucherDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<VoucherDto> CreateAsync(VoucherCreateDto dto, CancellationToken ct = default);
}

public interface ISaleService
{
    Task<IReadOnlyList<SaleInvoiceDto>> GetAllAsync(CancellationToken ct = default);
    Task<SaleInvoiceDto> CreateAsync(SaleInvoiceCreateDto dto, CancellationToken ct = default);
}

public interface ISalesService
{
    Task<IReadOnlyList<SalesOrderDto>> GetOrdersAsync(CancellationToken ct = default);
    Task<SalesOrderDto?> GetOrderByIdAsync(Guid id, CancellationToken ct = default);
    Task<SalesOrderDto> CreateOrderAsync(SalesOrderCreateDto dto, CancellationToken ct = default);
    Task<SalesOrderDto?> UpdateOrderAsync(Guid id, SalesOrderCreateDto dto, CancellationToken ct = default);
    Task<SalesOrderDto?> TransitionOrderAsync(Guid id, string action, CancellationToken ct = default);

    Task<IReadOnlyList<DeliveryOrderDto>> GetDeliveriesAsync(CancellationToken ct = default);
    Task<DeliveryOrderDto?> GetDeliveryByIdAsync(Guid id, CancellationToken ct = default);
    Task<DeliveryOrderDto> CreateDeliveryAsync(DeliveryOrderCreateDto dto, CancellationToken ct = default);
    Task<DeliveryOrderDto?> TransitionDeliveryAsync(Guid id, string action, CancellationToken ct = default);

    Task<IReadOnlyList<SalesInvoiceDocDto>> GetInvoicesAsync(CancellationToken ct = default);
    Task<SalesInvoiceDocDto?> GetInvoiceByIdAsync(Guid id, CancellationToken ct = default);
    Task<SalesInvoiceDocDto> CreateInvoiceAsync(SalesInvoiceDocCreateDto dto, CancellationToken ct = default);
    Task<SalesInvoiceDocDto?> TransitionInvoiceAsync(Guid id, string action, CancellationToken ct = default);

    Task<IReadOnlyList<CustomerPaymentDto>> GetPaymentsAsync(CancellationToken ct = default);
    Task<CustomerPaymentDto?> GetPaymentByIdAsync(Guid id, CancellationToken ct = default);
    Task<CustomerPaymentDto> CreatePaymentAsync(CustomerPaymentCreateDto dto, CancellationToken ct = default);
    Task<CustomerPaymentDto?> TransitionPaymentAsync(Guid id, string action, CancellationToken ct = default);

    Task<ReceivableBalanceDto?> GetReceivableAsync(Guid partyId, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLogDto>> GetAuditAsync(string entityName, Guid id, CancellationToken ct = default);
}

public interface IPurchaseService
{
    // Legacy PI list/create (PI posts AP only — never stock)
    Task<IReadOnlyList<PurchaseInvoiceDto>> GetAllAsync(CancellationToken ct = default);
    Task<PurchaseInvoiceDto?> GetInvoiceByIdAsync(Guid id, CancellationToken ct = default);
    Task<PurchaseInvoiceDto> CreateAsync(PurchaseInvoiceCreateDto dto, CancellationToken ct = default);

    // Phase 0 vertical slice
    Task<IReadOnlyList<PurchaseOrderDto>> GetOrdersAsync(CancellationToken ct = default);
    Task<PurchaseOrderDto?> GetOrderByIdAsync(Guid id, CancellationToken ct = default);
    Task<PurchaseOrderDto> CreateOrderAsync(PurchaseOrderCreateDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<GoodsReceiptDto>> GetReceiptsAsync(CancellationToken ct = default);
    Task<GoodsReceiptDto?> GetReceiptByIdAsync(Guid id, CancellationToken ct = default);
    Task<GoodsReceiptDto> CreateReceiptAsync(GoodsReceiptCreateDto dto, CancellationToken ct = default);
    Task<PayableBalanceDto?> GetPayableAsync(Guid partyId, CancellationToken ct = default);
    Task<PurchaseOrderDto?> UpdateOrderAsync(Guid id, PurchaseOrderCreateDto dto, CancellationToken ct = default);
    Task<PurchaseOrderDto?> TransitionOrderAsync(Guid id, string action, CancellationToken ct = default);
    Task<GoodsReceiptDto?> TransitionReceiptAsync(Guid id, string action, CancellationToken ct = default);
    Task<PurchaseInvoiceDto?> TransitionInvoiceAsync(Guid id, string action, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLogDto>> GetAuditAsync(string entityName, Guid id, CancellationToken ct = default);
}

public interface IDocumentWorkflowService
{
    Task<PurchaseOrderDto?> TransitionOrderAsync(Guid id, string action, string? remarks = null, CancellationToken ct = default);
    Task<GoodsReceiptDto?> TransitionReceiptAsync(Guid id, string action, string? remarks = null, CancellationToken ct = default);
    Task<PurchaseInvoiceDto?> TransitionInvoiceAsync(Guid id, string action, string? remarks = null, CancellationToken ct = default);

    Task<SalesOrderDto?> TransitionSalesOrderAsync(Guid id, string action, string? remarks = null, CancellationToken ct = default);
    Task<DeliveryOrderDto?> TransitionDeliveryOrderAsync(Guid id, string action, string? remarks = null, CancellationToken ct = default);
    Task<SalesInvoiceDocDto?> TransitionSalesInvoiceAsync(Guid id, string action, string? remarks = null, CancellationToken ct = default);
    Task<CustomerPaymentDto?> TransitionCustomerPaymentAsync(Guid id, string action, string? remarks = null, CancellationToken ct = default);
    Task<SupplierPaymentDto?> TransitionSupplierPaymentAsync(Guid id, string action, string? remarks = null, CancellationToken ct = default);
}

public interface IAccountingPostingService
{
    Task PostSalesInvoiceAsync(SalesInvoice invoice, CancellationToken ct = default);
    Task PostCustomerPaymentAsync(CustomerPayment payment, CancellationToken ct = default);
    Task PostPurchaseInvoiceAsync(PurchaseInvoice invoice, CancellationToken ct = default);
    Task PostSupplierPaymentAsync(SupplierPayment payment, CancellationToken ct = default);
    Task PostDeliveryCogsAsync(DeliveryOrder delivery, CancellationToken ct = default);
    Task PostStockAdjustmentAsync(StockAdjustment adjustment, CancellationToken ct = default);
    Task PostPayrollRunAsync(PayrollRun run, CancellationToken ct = default);
    Task PostSalaryPaymentAsync(SalaryPayment payment, CancellationToken ct = default);
    Task PostProductionIssueAsync(ProductionOrder order, CancellationToken ct = default);
    Task PostProductionReceiptAsync(ProductionOrder order, CancellationToken ct = default);
    Task PostFarmExpenseAsync(FarmExpense expense, CancellationToken ct = default);
    Task<JournalEntryDto> CreateManualJournalAsync(JournalEntryCreateDto dto, CancellationToken ct = default);
    Task EnsurePeriodOpenAsync(DateTime postingDate, Guid companyId, Guid financialYearId, CancellationToken ct = default);
}

public interface IFinanceService
{
    Task<IReadOnlyList<JournalEntryDto>> GetJournalsAsync(CancellationToken ct = default);
    Task<JournalEntryDto?> GetJournalByIdAsync(Guid id, CancellationToken ct = default);
    Task<JournalEntryDto> CreateJournalAsync(JournalEntryCreateDto dto, CancellationToken ct = default);
    Task<JournalEntryDto?> PostJournalAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<CashBankAccountDto>> GetCashBankAccountsAsync(CancellationToken ct = default);
    Task<CashBankAccountDto> CreateCashBankAccountAsync(CashBankAccountUpsertDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<CashBankTxnDto>> GetCashBankTransactionsAsync(CancellationToken ct = default);
    Task<CashBankTxnDto> CreateCashBankTransactionAsync(CashBankTxnCreateDto dto, CancellationToken ct = default);

    Task<IReadOnlyList<SupplierPaymentDto>> GetSupplierPaymentsAsync(CancellationToken ct = default);
    Task<SupplierPaymentDto?> GetSupplierPaymentByIdAsync(Guid id, CancellationToken ct = default);
    Task<SupplierPaymentDto> CreateSupplierPaymentAsync(SupplierPaymentCreateDto dto, CancellationToken ct = default);
    Task<SupplierPaymentDto?> TransitionSupplierPaymentAsync(Guid id, string action, CancellationToken ct = default);

    Task<FiscalPeriodDto?> ClosePeriodAsync(Guid periodId, CancellationToken ct = default);
    Task<FiscalPeriodDto?> OpenPeriodAsync(Guid periodId, CancellationToken ct = default);
    Task EnsureMonthlyPeriodsAsync(CancellationToken ct = default);
}

public interface IPermissionService
{
    Task EnsureAsync(string permissionCode, CancellationToken ct = default);
    Task<bool> HasAsync(string permissionCode, CancellationToken ct = default);
}

public interface IAuditService
{
    Task WriteAsync(string entityName, Guid entityId, string action, string? oldValues = null, string? newValues = null, CancellationToken ct = default);
}

public interface IAdminService
{
    Task<CompanyDto?> GetCompanyAsync(CancellationToken ct = default);
    Task<CompanyDto?> UpdateCompanyAsync(CompanyUpdateDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<BranchDto>> GetBranchesAsync(CancellationToken ct = default);
    Task<BranchDto> CreateBranchAsync(BranchUpsertDto dto, CancellationToken ct = default);
    Task<BranchDto?> UpdateBranchAsync(Guid id, BranchUpsertDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default);
    Task<RoleDto> CreateRoleAsync(RoleCreateDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken ct = default);
    Task<RolePermissionsDto?> GetRolePermissionsAsync(Guid roleId, CancellationToken ct = default);
    Task<RolePermissionsDto?> SetRolePermissionsAsync(Guid roleId, IReadOnlyList<string> codes, CancellationToken ct = default);
    Task<bool> SetUserRolesAsync(Guid userId, IReadOnlyList<Guid> roleIds, CancellationToken ct = default);
    Task<IReadOnlyList<FinancialYearDto>> GetFinancialYearsAsync(CancellationToken ct = default);
    Task<FinancialYearDto> CreateFinancialYearAsync(FinancialYearCreateDto dto, CancellationToken ct = default);
    Task<FinancialYearDto?> SetCurrentFinancialYearAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<FiscalPeriodDto>> GetFiscalPeriodsAsync(Guid yearId, CancellationToken ct = default);
    Task<FiscalPeriodDto> CreateFiscalPeriodAsync(Guid yearId, FiscalPeriodCreateDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLogDto>> GetAuditLogsAsync(string? entityName, string? entityId, int take, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentTypeDto>> GetDocumentTypesAsync(CancellationToken ct = default);
    Task<DocumentTypeDto?> UpdateDocumentTypeAsync(Guid id, DocumentTypeUpsertDto dto, CancellationToken ct = default);
}

public interface IInventoryService
{
    Task<IReadOnlyList<StockDto>> GetStockAsync(Guid? warehouseId = null, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryLedgerLineDto>> GetLedgerAsync(Guid? productId, Guid? warehouseId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<StockValuationLineDto>> GetValuationAsync(Guid? warehouseId = null, CancellationToken ct = default);
    Task<IReadOnlyList<StockMovementLineDto>> GetMovementAsync(Guid? productId, Guid? warehouseId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<NamedAmountRowDto>> GetSlowMovingAsync(DateTime from, DateTime to, decimal maxQty = 5, CancellationToken ct = default);
    Task<IReadOnlyList<NamedAmountRowDto>> GetFastMovingAsync(DateTime from, DateTime to, decimal minQty = 20, CancellationToken ct = default);
    Task<IReadOnlyList<StockAgingLineDto>> GetStockAgingAsync(DateTime? asOf = null, CancellationToken ct = default);
    Task<IReadOnlyList<WarehouseComparisonLineDto>> GetWarehouseComparisonAsync(CancellationToken ct = default);
    Task<IReadOnlyList<NamedAmountRowDto>> GetAdjustmentAnalysisAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<InventoryTurnoverDto> GetTurnoverAsync(DateTime from, DateTime to, CancellationToken ct = default);
}

public interface IInventoryForecastService
{
    Task<InventoryDemandForecastReportDto> GetDemandForecastAsync(
        Guid? productId = null,
        Guid? warehouseId = null,
        DateTime? asOf = null,
        Guid? companyId = null,
        Guid? branchId = null,
        CancellationToken ct = default);
}

public interface IInventoryPostingService
{
    Task PostGoodsReceiptAsync(GoodsReceipt grn, CancellationToken ct = default);
    Task PostDeliveryIssueAsync(DeliveryOrder delivery, CancellationToken ct = default);
    Task PostTransferAsync(StockTransfer transfer, CancellationToken ct = default);
    Task PostAdjustmentAsync(StockAdjustment adjustment, CancellationToken ct = default);
    Task PostProductionConsumptionAsync(ProductionOrder order, CancellationToken ct = default);
    Task PostProductionOutputAsync(ProductionOrder order, CancellationToken ct = default);
    Task PostFarmFeedIssueAsync(FeedIssue issue, CancellationToken ct = default);
    Task PostFarmMedicationAsync(FarmTreatment treatment, CancellationToken ct = default);
}

public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeDto>> GetAllAsync(CancellationToken ct = default);
    Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EmployeeDto> CreateAsync(EmployeeUpsertDto dto, CancellationToken ct = default);
    Task<EmployeeDto?> UpdateAsync(Guid id, EmployeeUpsertDto dto, CancellationToken ct = default);
}

public interface IHrOrgService
{
    Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken ct = default);
    Task<DepartmentDto> CreateDepartmentAsync(DepartmentUpsertDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<DesignationDto>> GetDesignationsAsync(CancellationToken ct = default);
    Task<DesignationDto> CreateDesignationAsync(DesignationUpsertDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<PayrollComponentDto>> GetComponentsAsync(CancellationToken ct = default);
}

public interface IGlMappingService
{
    Task<IReadOnlyList<GlAccountMappingDto>> GetAllAsync(CancellationToken ct = default);
    Task<GlAccountMappingDto> UpsertAsync(GlAccountMappingUpsertDto dto, CancellationToken ct = default);
}

public interface ICompanyService
{
    Task<CompanyDto?> GetAsync(CancellationToken ct = default);
    Task<CompanyDto?> UpdateAsync(CompanyUpdateDto dto, CancellationToken ct = default);
}

public interface ITenantPlatformService
{
    Task<IReadOnlyList<TenantListDto>> GetAllAsync(CancellationToken ct = default);
    Task<TenantDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ProvisionTenantResult> ProvisionAsync(ProvisionTenantRequest request, CancellationToken ct = default);
    Task<TenantDetailDto?> UpdateModulesAsync(Guid id, IReadOnlyList<string> modules, CancellationToken ct = default);
    Task<bool> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);
    IReadOnlyList<string> GetModuleCatalog();
}

public interface IReportService
{
    Task<LedgerReportDto> GetLedgerAsync(Guid accountId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<TrialBalanceReportDto> GetTrialBalanceAsync(DateTime asOf, CancellationToken ct = default);
    Task<AgingReportDto> GetReceivablesAgingAsync(DateTime? asOf = null, CancellationToken ct = default);
    Task<AgingReportDto> GetPayablesAgingAsync(DateTime? asOf = null, CancellationToken ct = default);
    Task<ProfitLossReportDto> GetProfitLossAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<BalanceSheetReportDto> GetBalanceSheetAsync(DateTime asOf, CancellationToken ct = default);
    Task<CashFlowReportDto> GetCashFlowAsync(DateTime from, DateTime to, CancellationToken ct = default);
}

public interface IBiReportService
{
    Task EnsureAsync(string permission, CancellationToken ct = default);

    Task<NamedReportDto> SalesSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> SalesByCustomerAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> SalesByProductAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> SalesByCategoryAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> SalesByBranchAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> SalesTrendAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> CollectionsAsync(DateTime from, DateTime to, CancellationToken ct = default);

    Task<NamedReportDto> PurchaseSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> PurchaseBySupplierAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> PurchaseByProductAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> SupplierOutstandingAsync(DateTime? asOf = null, CancellationToken ct = default);
    Task<NamedReportDto> PurchasePriceAnalysisAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> GrnVsInvoiceAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> PurchaseTrendAsync(DateTime from, DateTime to, CancellationToken ct = default);

    Task<NamedReportDto> HrEmployeeSummaryAsync(CancellationToken ct = default);
    Task<NamedReportDto> HrAttendanceAsync(int year, int month, CancellationToken ct = default);
    Task<NamedReportDto> HrLeaveAsync(int? year = null, CancellationToken ct = default);
    Task<NamedReportDto> HrPayrollAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> HrDepartmentCostAsync(CancellationToken ct = default);
    Task<NamedReportDto> HrOvertimeAsync(DateTime from, DateTime to, CancellationToken ct = default);

    Task<NamedReportDto> ProductionSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> ProductionOrderStatusAsync(CancellationToken ct = default);
    Task<NamedReportDto> ProductionConsumptionAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> ProductionCostAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> ProductionYieldAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> ProductionFgAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> ProductionWipAsync(CancellationToken ct = default);

    Task<NamedReportDto> FarmFlockSummaryAsync(CancellationToken ct = default);
    Task<NamedReportDto> FarmMortalityAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> FarmFeedAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> FarmExpensesAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> FarmPerformanceAsync(CancellationToken ct = default);

    Task<NamedReportDto> QualitySamplesAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> QualityResultsAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> QualityPassFailAsync(DateTime from, DateTime to, CancellationToken ct = default);

    Task<NamedReportDto> OpsWeighmentSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> OpsVehicleMovementAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> OpsGatePassAsync(DateTime from, DateTime to, CancellationToken ct = default);

    Task<NamedReportDto> PosSalesAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> PosPaymentsAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> PosShiftsAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<NamedReportDto> PosProductsAsync(DateTime from, DateTime to, CancellationToken ct = default);
}

public interface IProductionService
{
    Task<IReadOnlyList<BomDto>> GetBomsAsync(CancellationToken ct = default);
    Task<BomDto?> GetBomByIdAsync(Guid id, CancellationToken ct = default);
    Task<BomDto> CreateBomAsync(BomUpsertDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<ProductionOrderDto>> GetOrdersAsync(CancellationToken ct = default);
    Task<ProductionOrderDto?> GetOrderByIdAsync(Guid id, CancellationToken ct = default);
    Task<ProductionOrderDto> CreateOrderAsync(ProductionOrderCreateDto dto, CancellationToken ct = default);
    Task<ProductionOrderDto?> TransitionAsync(Guid id, string action, CancellationToken ct = default);
    Task<ProductionOrderDto?> AddVarianceAsync(Guid id, ProductionVarianceCreateDto dto, CancellationToken ct = default);
    Task<ProductionCostingDto?> GetCostingAsync(Guid orderId, CancellationToken ct = default);
}

public interface IFarmService
{
    Task<IReadOnlyList<FarmDto>> GetFarmsAsync(CancellationToken ct = default);
    Task<FarmDto> CreateFarmAsync(FarmUpsertDto dto, CancellationToken ct = default);
    Task<FarmUnitDto> CreateUnitAsync(FarmUnitUpsertDto dto, CancellationToken ct = default);

    Task<IReadOnlyList<FlockDto>> GetFlocksAsync(CancellationToken ct = default);
    Task<FlockDto?> GetFlockByIdAsync(Guid id, CancellationToken ct = default);
    Task<FlockDto> CreateFlockAsync(FlockCreateDto dto, CancellationToken ct = default);
    Task<bool> CloseFlockAsync(Guid flockId, CancellationToken ct = default);

    Task<MortalityDto> AddMortalityAsync(Guid flockId, MortalityCreateDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<MortalityDto>> GetMortalitiesAsync(Guid flockId, CancellationToken ct = default);

    Task<IReadOnlyList<FeedIssueDto>> GetFeedIssuesAsync(CancellationToken ct = default);
    Task<FeedIssueDto> CreateFeedIssueAsync(FeedIssueCreateDto dto, CancellationToken ct = default);
    Task<FeedIssueDto?> TransitionFeedIssueAsync(Guid id, string action, CancellationToken ct = default);

    Task<IReadOnlyList<FarmTreatmentDto>> GetTreatmentsAsync(Guid? flockId = null, CancellationToken ct = default);
    Task<FarmTreatmentDto> CreateTreatmentAsync(FarmTreatmentCreateDto dto, CancellationToken ct = default);

    Task<IReadOnlyList<FarmExpenseDto>> GetExpensesAsync(CancellationToken ct = default);
    Task<FarmExpenseDto> CreateExpenseAsync(FarmExpenseCreateDto dto, CancellationToken ct = default);
    Task<FarmExpenseDto?> TransitionExpenseAsync(Guid id, string action, CancellationToken ct = default);

    Task<FarmPerformanceDto?> GetPerformanceAsync(Guid flockId, CancellationToken ct = default);

    // Legacy compatibility
    Task<FarmEventDto> AddEventAsync(Guid flockId, FarmEventCreateDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<FarmEventDto>> GetEventsAsync(Guid flockId, CancellationToken ct = default);
}

public interface IUserAdminService
{
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken ct = default);
    Task<UserDto> CreateAsync(UserCreateDto dto, CancellationToken ct = default);
    Task<UserDto?> UpdateAsync(Guid id, UserUpdateDto dto, CancellationToken ct = default);
}

public interface IStockAdjustmentService
{
    Task<IReadOnlyList<StockAdjustmentDocDto>> GetAllAsync(CancellationToken ct = default);
    Task<StockAdjustmentDocDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<StockAdjustmentDocDto> CreateAsync(StockAdjustmentDocCreateDto dto, CancellationToken ct = default);
    Task<StockAdjustmentDocDto?> TransitionAsync(Guid id, string action, CancellationToken ct = default);
}

public interface IStockCountService
{
    Task<IReadOnlyList<StockCountDto>> GetAllAsync(CancellationToken ct = default);
    Task<StockCountDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<StockCountDto> CreateAsync(StockCountCreateDto dto, CancellationToken ct = default);
    Task<StockCountDto?> SnapshotAsync(Guid id, CancellationToken ct = default);
    Task<StockCountDto?> TransitionAsync(Guid id, string action, CancellationToken ct = default);
}

public interface IWarehouseService
{
    Task<IReadOnlyList<WarehouseDto>> GetAllAsync(CancellationToken ct = default);
    Task<WarehouseDto> CreateAsync(WarehouseUpsertDto dto, CancellationToken ct = default);
    Task<WarehouseDto?> UpdateAsync(Guid id, WarehouseUpsertDto dto, CancellationToken ct = default);
}

public interface IPartySettlementService
{
    Task<IReadOnlyList<PartySettlementDto>> GetAllAsync(CancellationToken ct = default);
    Task<PartySettlementDto> CreateAsync(PartySettlementCreateDto dto, CancellationToken ct = default);
}

public interface IPurchaseContractService
{
    Task<IReadOnlyList<PurchaseContractDto>> GetAllAsync(CancellationToken ct = default);
    Task<PurchaseContractDto> CreateAsync(PurchaseContractCreateDto dto, CancellationToken ct = default);
    Task<bool> CloseAsync(Guid id, CancellationToken ct = default);
}

public interface ILabService
{
    Task<IReadOnlyList<LabTestTypeDto>> GetTestTypesAsync(CancellationToken ct = default);
    Task<LabTestTypeDto> UpsertTestTypeAsync(LabTestTypeUpsertDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<LabSampleDto>> GetSamplesAsync(CancellationToken ct = default);
    Task<LabSampleDto> CreateSampleAsync(LabSampleCreateDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<LabTestResultDto>> GetResultsAsync(CancellationToken ct = default);
    Task<LabTestResultDto> CreateResultAsync(LabTestResultCreateDto dto, CancellationToken ct = default);
    Task<LabTestResultDto?> ApproveResultAsync(Guid id, CancellationToken ct = default);
}

public interface ICostCentreService
{
    Task<IReadOnlyList<CostCentreDto>> GetAllAsync(CancellationToken ct = default);
    Task<CostCentreDto> CreateAsync(CostCentreUpsertDto dto, CancellationToken ct = default);
}

public interface IChequeService
{
    Task<IReadOnlyList<ChequeBookDto>> GetBooksAsync(CancellationToken ct = default);
    Task<ChequeBookDto> CreateBookAsync(ChequeBookCreateDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<PostDatedChequeDto>> GetChequesAsync(CancellationToken ct = default);
    Task<PostDatedChequeDto> CreateChequeAsync(PostDatedChequeCreateDto dto, CancellationToken ct = default);
    Task<PostDatedChequeDto?> UpdateStatusAsync(Guid id, PostDatedChequeStatusDto dto, CancellationToken ct = default);
}

public interface IFarmShareHolderService
{
    Task<IReadOnlyList<FarmShareHolderDto>> GetAllAsync(Guid? flockId = null, CancellationToken ct = default);
    Task<FarmShareHolderDto> CreateAsync(FarmShareHolderCreateDto dto, CancellationToken ct = default);
}

public interface ISalesForceService
{
    Task<IReadOnlyList<SalesAreaDto>> GetAreasAsync(CancellationToken ct = default);
    Task<SalesAreaDto> CreateAreaAsync(SalesAreaUpsertDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<SalesmanDto>> GetSalesmenAsync(CancellationToken ct = default);
    Task<SalesmanDto> CreateSalesmanAsync(SalesmanUpsertDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<SalesTargetDto>> GetTargetsAsync(CancellationToken ct = default);
    Task<SalesTargetDto> CreateTargetAsync(SalesTargetUpsertDto dto, CancellationToken ct = default);
}

public interface IMaterialDemandService
{
    Task<IReadOnlyList<MaterialDemandDto>> GetAllAsync(CancellationToken ct = default);
    Task<MaterialDemandDto> CreateAsync(MaterialDemandCreateDto dto, CancellationToken ct = default);
}

public interface IHrFinanceService
{
    Task<IReadOnlyList<SalaryAdvanceDto>> GetAdvancesAsync(CancellationToken ct = default);
    Task<SalaryAdvanceDto> CreateAdvanceAsync(SalaryAdvanceCreateDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<StaffLoanDto>> GetLoansAsync(CancellationToken ct = default);
    Task<StaffLoanDto> CreateLoanAsync(StaffLoanCreateDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<EobiDto>> GetEobiAsync(CancellationToken ct = default);
    Task<EobiDto> CreateEobiAsync(EobiCreateDto dto, CancellationToken ct = default);
}

public interface IWeighbridgeService
{
    Task<IReadOnlyList<WeighbridgeScaleDto>> GetScalesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<WeighbridgeTicketDto>> GetAllAsync(CancellationToken ct = default);
    Task<WeighbridgeTicketDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<WeighbridgeTicketDto> CreateAsync(WeighbridgeTicketCreateDto dto, CancellationToken ct = default);
    Task<WeighbridgeTicketDto?> RecordFirstWeightAsync(Guid id, WeighbridgeFirstWeightDto dto, CancellationToken ct = default);
    Task<WeighbridgeTicketDto?> CompleteAsync(Guid id, WeighbridgeCompleteDto dto, CancellationToken ct = default);
}

public interface IPosService
{
    Task<IReadOnlyList<PosTerminalDto>> GetTerminalsAsync(CancellationToken ct = default);
    Task<PosTerminalDto> CreateTerminalAsync(PosTerminalDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<PosShiftDto>> GetShiftsAsync(CancellationToken ct = default);
    Task<PosShiftDto> OpenShiftAsync(PosShiftOpenDto dto, CancellationToken ct = default);
    Task<PosShiftDto?> CloseShiftAsync(Guid id, PosShiftCloseDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<PosSaleDto>> GetAllAsync(CancellationToken ct = default);
    Task<PosSaleDto> CreateAsync(PosSaleCreateDto dto, CancellationToken ct = default);
}

public interface IOperationsService
{
    Task<IReadOnlyList<VehicleTypeDto>> GetVehicleTypesAsync(CancellationToken ct = default);
    Task<VehicleTypeDto> CreateVehicleTypeAsync(VehicleTypeDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<VehicleDto>> GetVehiclesAsync(CancellationToken ct = default);
    Task<VehicleDto> CreateVehicleAsync(VehicleDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<DriverDto>> GetDriversAsync(CancellationToken ct = default);
    Task<DriverDto> CreateDriverAsync(DriverDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<GatePassDto>> GetGatePassesAsync(CancellationToken ct = default);
    Task<GatePassDto> CreateGatePassAsync(GatePassDto dto, CancellationToken ct = default);
    Task<GatePassDto?> TransitionGatePassAsync(Guid id, string action, CancellationToken ct = default);
}

public interface IBrandService
{
    Task<IReadOnlyList<BrandDto>> GetAllAsync(CancellationToken ct = default);
    Task<BrandDto> CreateAsync(BrandUpsertDto dto, CancellationToken ct = default);
}

public interface IProductConversionService
{
    Task<IReadOnlyList<ProductConversionDto>> GetAllAsync(CancellationToken ct = default);
    Task<ProductConversionDto> CreateAsync(ProductConversionUpsertDto dto, CancellationToken ct = default);
}

public interface IBudgetService
{
    Task<IReadOnlyList<BudgetDto>> GetAllAsync(CancellationToken ct = default);
    Task<BudgetDto> CreateAsync(BudgetCreateDto dto, CancellationToken ct = default);
    Task<bool> ApproveAsync(Guid id, CancellationToken ct = default);
}

public interface IAttendanceService
{
    Task<IReadOnlyList<AttendanceDto>> GetAttendanceAsync(CancellationToken ct = default);
    Task<AttendanceDto> CreateAttendanceAsync(AttendanceCreateDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceSummaryDto>> GetSummaryAsync(int year, int month, CancellationToken ct = default);
    Task<IReadOnlyList<OvertimeDto>> GetOvertimeAsync(CancellationToken ct = default);
    Task<OvertimeDto> CreateOvertimeAsync(OvertimeCreateDto dto, CancellationToken ct = default);
}

public interface IDayCloseService
{
    Task<IReadOnlyList<DayCloseDto>> GetAllAsync(CancellationToken ct = default);
    Task<DayCloseDto> CloseAsync(DayCloseCreateDto dto, CancellationToken ct = default);
    Task<bool> IsDateClosedAsync(DateTime date, CancellationToken ct = default);
}

public interface IStockTransferService
{
    Task<IReadOnlyList<StockTransferDto>> GetAllAsync(CancellationToken ct = default);
    Task<StockTransferDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<StockTransferDto> CreateAsync(StockTransferCreateDto dto, CancellationToken ct = default);
    Task<StockTransferDto?> TransitionAsync(Guid id, string action, CancellationToken ct = default);
}

public interface IUnitConversionService
{
    Task<IReadOnlyList<UnitConversionDto>> GetAllAsync(CancellationToken ct = default);
    Task<UnitConversionDto> CreateAsync(UnitConversionUpsertDto dto, CancellationToken ct = default);
}

public interface IWarehouseLocationService
{
    Task<IReadOnlyList<WarehouseLocationDto>> GetByWarehouseAsync(Guid warehouseId, CancellationToken ct = default);
    Task<WarehouseLocationDto> CreateAsync(WarehouseLocationUpsertDto dto, CancellationToken ct = default);
}

public interface ITradeReturnService
{
    Task<IReadOnlyList<TradeReturnDto>> GetSaleReturnsAsync(CancellationToken ct = default);
    Task<TradeReturnDto> CreateSaleReturnAsync(TradeReturnCreateDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<TradeReturnDto>> GetPurchaseReturnsAsync(CancellationToken ct = default);
    Task<TradeReturnDto> CreatePurchaseReturnAsync(TradeReturnCreateDto dto, CancellationToken ct = default);
}

public interface ILeaveService
{
    Task<IReadOnlyList<LeaveTypeDto>> GetTypesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LeaveBalanceDto>> GetBalancesAsync(Guid? employeeId = null, CancellationToken ct = default);
    Task<IReadOnlyList<LeaveDto>> GetAllAsync(CancellationToken ct = default);
    Task<LeaveDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<LeaveDto> CreateAsync(LeaveCreateDto dto, CancellationToken ct = default);
    Task<LeaveDto?> TransitionAsync(Guid id, string action, CancellationToken ct = default);
}

public interface IPayrollService
{
    Task<IReadOnlyList<PayrollPeriodDto>> GetPeriodsAsync(CancellationToken ct = default);
    Task<PayrollPeriodDto> CreatePeriodAsync(PayrollPeriodCreateDto dto, CancellationToken ct = default);
    Task<PayrollPeriodDto?> ClosePeriodAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PayrollRunDto>> GetRunsAsync(CancellationToken ct = default);
    Task<PayrollRunDto?> GetRunByIdAsync(Guid id, CancellationToken ct = default);
    Task<PayrollRunDto> CreateRunAsync(PayrollRunCreateDto dto, CancellationToken ct = default);
    Task<PayrollRunDto?> CalculateAsync(Guid id, CancellationToken ct = default);
    Task<PayrollRunDto?> TransitionAsync(Guid id, string action, CancellationToken ct = default);
    Task<SalaryPaymentDto> CreatePaymentAsync(SalaryPaymentCreateDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<SalaryPaymentDto>> GetPaymentsAsync(Guid? runId = null, CancellationToken ct = default);
}

public interface ISalarySheetService
{
    Task<SalarySheetDto> GetAsync(int year, int month, CancellationToken ct = default);
    Task<SalarySheetDto> GenerateAsync(SalaryGenerateDto dto, CancellationToken ct = default);
    Task<SalarySheetLineDto?> MarkPaidAsync(Guid id, CancellationToken ct = default);
}

// ---- Phase 10A / 10B / 10C AI ----
public interface IAiAssistantService
{
    Task<AiChatResponse> ChatAsync(AiChatRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AiConversationListItemDto>> ListConversationsAsync(CancellationToken ct = default);
    Task<AiConversationDetailDto?> GetConversationAsync(Guid id, CancellationToken ct = default);
    Task<AiActionPreviewDto?> GetActionAsync(Guid id, CancellationToken ct = default);
    Task<AiActionPreviewDto> UpdateActionAsync(Guid id, AiActionUpdateRequest request, CancellationToken ct = default);
    Task<AiActionConfirmResponse> ConfirmActionAsync(Guid id, AiActionConfirmRequest? request = null, CancellationToken ct = default);
    Task<AiActionPreviewDto> CancelActionAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AiAgentRunDto>> ListAgentRunsAsync(CancellationToken ct = default);
    Task<AiAgentRunDto?> GetAgentRunAsync(Guid id, CancellationToken ct = default);
    Task<AiAgentConfirmResponse> ConfirmAgentRunAsync(Guid id, AiAgentConfirmRequest? request = null, CancellationToken ct = default);
    Task<AiAgentRunDto> CancelAgentRunAsync(Guid id, CancellationToken ct = default);
}

public interface IAiIntentResolver
{
    AiIntentResolution Resolve(string message);
}

public record AiActionSlots(
    decimal? Qty = null,
    decimal? Amount = null,
    decimal? Rate = null,
    string? PartyHint = null,
    string? ProductHint = null,
    string? FromWarehouseHint = null,
    string? ToWarehouseHint = null,
    string? DebitAccountHint = null,
    string? CreditAccountHint = null,
    string? Narration = null,
    Guid? ForcedCompanyId = null,
    Guid? ForcedBranchId = null);

public record AiIntentResolution(
    string Intent,
    DateTime From,
    DateTime To,
    DateTime AsOf,
    decimal? AmountThreshold,
    string? Reason,
    AiActionSlots? ActionSlots = null);

public interface IAiToolEngine
{
    Task<AiToolResult> ExecuteAsync(AiIntentResolution intent, CancellationToken ct = default);
}

public record AiToolResult(
    string ToolName,
    string Label,
    object? Data,
    bool Denied = false,
    string? DeniedPermission = null,
    string? Error = null);

public interface IAiEntityResolver
{
    Task<Party> ResolvePartyAsync(string hint, PartyType? type, CancellationToken ct = default);
    Task<Product> ResolveProductAsync(string hint, CancellationToken ct = default);
    Task<Warehouse> ResolveWarehouseAsync(string? hint, bool preferDefault, CancellationToken ct = default);
    Task<Account> ResolveAccountAsync(string hint, CancellationToken ct = default);
}

public interface IAiActionPlanner
{
    Task<AiActionPreviewDto> ProposeAsync(Guid conversationId, AiIntentResolution resolution, string rawMessage, CancellationToken ct = default);
    Task<AiActionPreviewDto> ProposePayloadAsync(
        Guid conversationId,
        string actionType,
        string permissionRequired,
        object payload,
        Guid? agentRunId = null,
        Guid? agentStepId = null,
        CancellationToken ct = default);
}

public interface IAiActionExecutor
{
    Task<AiActionPreviewDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<AiActionPreviewDto> UpdateAsync(Guid id, AiActionUpdateRequest request, CancellationToken ct = default);
    Task<AiActionConfirmResponse> ConfirmAsync(Guid id, AiActionConfirmRequest? request, CancellationToken ct = default);
    Task<AiActionPreviewDto> CancelAsync(Guid id, CancellationToken ct = default);
}

public interface IAiAgentOrchestrator
{
    Task<AiAgentRunDto> StartAsync(Guid conversationId, AiIntentResolution resolution, string goal, CancellationToken ct = default);
    Task<IReadOnlyList<AiAgentRunDto>> ListAsync(CancellationToken ct = default);
    Task<AiAgentRunDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<AiAgentConfirmResponse> ConfirmAsync(Guid id, AiAgentConfirmRequest? request, CancellationToken ct = default);
    Task<AiAgentRunDto> CancelAsync(Guid id, CancellationToken ct = default);
}

public static class AiActionTypes
{
    public const string PurchaseOrderDraft = "PurchaseOrderDraft";
    public const string SalesOrderDraft = "SalesOrderDraft";
    public const string StockTransferDraft = "StockTransferDraft";
    public const string CustomerPaymentDraft = "CustomerPaymentDraft";
    public const string SupplierPaymentDraft = "SupplierPaymentDraft";
    public const string JournalEntryDraft = "JournalEntryDraft";
}

public static class AiActionStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";
    public const string Failed = "Failed";
}

public static class AiAgentTypes
{
    public const string InventoryReplenishment = "InventoryReplenishment";
    public const string Procurement = "Procurement";
    public const string Sales = "Sales";
    public const string Finance = "Finance";
    public const string InventoryAssistant = "InventoryAssistant";
    public const string Reporting = "Reporting";
}

public static class AiAgentRunStatuses
{
    public const string Planning = "Planning";
    public const string Running = "Running";
    public const string WaitingConfirmation = "WaitingConfirmation";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string Failed = "Failed";
}

public static class AiAgentStepStatuses
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
    public const string WaitingConfirm = "WaitingConfirm";
    public const string Skipped = "Skipped";
}

public interface ILlmProvider
{
    Task<string> CompleteAsync(IReadOnlyList<LlmChatMessage> messages, CancellationToken ct = default);
}

public record LlmChatMessage(string Role, string Content);
