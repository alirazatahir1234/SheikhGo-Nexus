using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoftaxeERP.Application.Interfaces;
using SoftaxeERP.Infrastructure.Persistence;
using SoftaxeERP.Infrastructure.Services;

namespace SoftaxeERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        // Phase 0 services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IPartyService, PartyService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IDocumentWorkflowService, DocumentWorkflowService>();
        services.AddScoped<ITenantPlatformService, TenantPlatformService>();

        // Deferred module stubs
        services.AddScoped<IVoucherService, VoucherService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IProductionService, ProductionService>();
        services.AddScoped<IFarmService, FarmService>();
        services.AddScoped<IStockAdjustmentService, StockAdjustmentService>();
        services.AddScoped<IPartySettlementService, PartySettlementService>();
        services.AddScoped<IPurchaseContractService, PurchaseContractService>();
        services.AddScoped<ILabService, LabService>();
        services.AddScoped<ICostCentreService, CostCentreService>();
        services.AddScoped<IChequeService, ChequeService>();
        services.AddScoped<IFarmShareHolderService, FarmShareHolderService>();
        services.AddScoped<ISalesForceService, SalesForceService>();
        services.AddScoped<IMaterialDemandService, MaterialDemandService>();
        services.AddScoped<IHrFinanceService, HrFinanceService>();
        services.AddScoped<IWeighbridgeService, WeighbridgeService>();
        services.AddScoped<IPosService, PosService>();
        services.AddScoped<IProductConversionService, ProductConversionService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IDayCloseService, DayCloseService>();
        services.AddScoped<IStockTransferService, StockTransferService>();
        services.AddScoped<ITradeReturnService, TradeReturnService>();
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<ISalarySheetService, SalarySheetService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
