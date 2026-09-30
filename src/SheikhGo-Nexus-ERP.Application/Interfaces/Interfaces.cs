using SoftaxeERP.Application.DTOs;

namespace SoftaxeERP.Application.Interfaces;

public interface ITenantContext
{
    Guid TenantId { get; }
    Guid? UserId { get; }
    string? ClientIp { get; }
    void Set(Guid tenantId, Guid? userId, string? clientIp = null);
}

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default);
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
    Task<IReadOnlyList<StockDto>> GetStockAsync(CancellationToken ct = default);
}

public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeDto>> GetAllAsync(CancellationToken ct = default);
    Task<EmployeeDto> CreateAsync(EmployeeUpsertDto dto, CancellationToken ct = default);
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
}

public interface IProductionService
{
    Task<IReadOnlyList<BomDto>> GetBomsAsync(CancellationToken ct = default);
    Task<BomDto> CreateBomAsync(BomUpsertDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<ProductionOrderDto>> GetOrdersAsync(CancellationToken ct = default);
    Task<ProductionOrderDto> CreateOrderAsync(ProductionCreateDto dto, CancellationToken ct = default);
}

public interface IFarmService
{
    Task<IReadOnlyList<FarmFlockDto>> GetFlocksAsync(CancellationToken ct = default);
    Task<FarmFlockDto> CreateFlockAsync(FarmFlockCreateDto dto, CancellationToken ct = default);
    Task<FarmEventDto> AddEventAsync(Guid flockId, FarmEventCreateDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<FarmEventDto>> GetEventsAsync(Guid flockId, CancellationToken ct = default);
    Task<bool> CloseFlockAsync(Guid flockId, CancellationToken ct = default);
}

public interface IUserAdminService
{
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken ct = default);
    Task<UserDto> CreateAsync(UserCreateDto dto, CancellationToken ct = default);
    Task<UserDto?> UpdateAsync(Guid id, UserUpdateDto dto, CancellationToken ct = default);
}

public interface IStockAdjustmentService
{
    Task<IReadOnlyList<StockAdjustmentDto>> GetAllAsync(CancellationToken ct = default);
    Task<StockAdjustmentDto> CreateAsync(StockAdjustmentCreateDto dto, CancellationToken ct = default);
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
    Task<IReadOnlyList<LabResultDto>> GetResultsAsync(CancellationToken ct = default);
    Task<LabResultDto> CreateResultAsync(LabResultCreateDto dto, CancellationToken ct = default);
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
    Task<IReadOnlyList<WeighbridgeTicketDto>> GetAllAsync(CancellationToken ct = default);
    Task<WeighbridgeTicketDto> CreateAsync(WeighbridgeTicketCreateDto dto, CancellationToken ct = default);
    Task<WeighbridgeTicketDto?> CompleteAsync(Guid id, WeighbridgeCompleteDto dto, CancellationToken ct = default);
}

public interface IPosService
{
    Task<IReadOnlyList<PosSaleDto>> GetAllAsync(CancellationToken ct = default);
    Task<PosSaleDto> CreateAsync(PosSaleCreateDto dto, CancellationToken ct = default);
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
    Task<StockTransferDto> CreateAsync(StockTransferCreateDto dto, CancellationToken ct = default);
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
    Task<IReadOnlyList<LeaveDto>> GetAllAsync(CancellationToken ct = default);
    Task<LeaveDto> CreateAsync(LeaveCreateDto dto, CancellationToken ct = default);
}

public interface ISalarySheetService
{
    Task<SalarySheetDto> GetAsync(int year, int month, CancellationToken ct = default);
    Task<SalarySheetDto> GenerateAsync(SalaryGenerateDto dto, CancellationToken ct = default);
    Task<SalarySheetLineDto?> MarkPaidAsync(Guid id, CancellationToken ct = default);
}
