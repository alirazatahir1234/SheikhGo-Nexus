using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Infrastructure.Options;
using SheikhGoNexusERP.Infrastructure.Persistence;
using SheikhGoNexusERP.Infrastructure.Services;

namespace SheikhGoNexusERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AiOptions>(config.GetSection(AiOptions.SectionName));
        services.AddHttpClient<OllamaLlmProvider>();
        services.AddHttpClient<OpenAiLlmProvider>();

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IPartyService, PartyService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IInventoryService, InventoryReportsService>();
        services.AddScoped<IInventoryForecastService, InventoryForecastService>();
        services.AddScoped<IInventoryPostingService, InventoryPostingService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IWarehouseLocationService, WarehouseLocationService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<ISalesService, SalesService>();
        services.AddScoped<IAccountingPostingService, AccountingPostingService>();
        services.AddScoped<IFinanceService, FinanceService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IDocumentWorkflowService, DocumentWorkflowService>();
        services.AddScoped<ITenantPlatformService, TenantPlatformService>();
        services.AddScoped<IStockTransferService, StockTransferService>();
        services.AddScoped<IStockAdjustmentService, StockAdjustmentService>();
        services.AddScoped<IStockCountService, StockCountService>();
        services.AddScoped<IUnitConversionService, UnitConversionService>();

        services.AddScoped<IHrOrgService, HrOrgService>();
        services.AddScoped<IGlMappingService, GlMappingService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<ISalarySheetService, SalarySheetService>();

        services.AddScoped<IVoucherService, VoucherService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IProductionService, ProductionService>();
        services.AddScoped<IFarmService, FarmService>();
        services.AddScoped<IPartySettlementService, PartySettlementService>();
        services.AddScoped<IPurchaseContractService, PurchaseContractService>();
        services.AddScoped<ILabService, LabService>();
        services.AddScoped<ICostCentreService, CostCentreService>();
        services.AddScoped<IChequeService, ChequeService>();
        services.AddScoped<IFarmShareHolderService, FarmShareHolderService>();
        services.AddScoped<ISalesForceService, SalesForceService>();
        services.AddScoped<IMaterialDemandService, MaterialDemandService>();
        services.AddScoped<IHrFinanceService, HrFinanceService>();
        services.AddScoped<IOperationsService, OperationsService>();
        services.AddScoped<IWeighbridgeService, WeighbridgeService>();
        services.AddScoped<IPosService, PosService>();
        services.AddScoped<IProductConversionService, ProductConversionService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IDayCloseService, DayCloseService>();
        services.AddScoped<ITradeReturnService, TradeReturnService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IBiReportService, BiReportService>();

        services.AddSingleton<NullLlmProvider>();
        services.AddScoped<IAiIntentResolver, AiIntentResolver>();
        services.AddScoped<IAiToolEngine, AiToolEngine>();
        services.AddScoped<IAiEntityResolver, AiEntityResolver>();
        services.AddScoped<IAiActionPlanner, AiActionPlanner>();
        services.AddScoped<IAiActionExecutor, AiActionExecutor>();
        services.AddScoped<IAiAgentOrchestrator, AiAgentOrchestrator>();
        services.AddScoped<ILlmProvider, ConfigurableLlmProvider>();
        services.AddScoped<IAiAssistantService, AiAssistantService>();

        return services;
    }
}
