using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Entities;
using SheikhGoNexusERP.Domain.Enums;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

internal static class Deferred
{
    public const string Message = "Deferred until after Phase 0";
    public static InvalidOperationException Mutate() => new(Message);
}

// ─── Platform (Phase 0 — fully implemented) ─────────────────────────────────

public class TenantPlatformService : ITenantPlatformService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public TenantPlatformService(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public IReadOnlyList<string> GetModuleCatalog() => SoftaxeModuleCatalog.All;

    public async Task<IReadOnlyList<TenantListDto>> GetAllAsync(CancellationToken ct = default)
    {
        var tenants = await _db.Tenants.IgnoreQueryFilters().AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);
        var companies = await _db.Companies.IgnoreQueryFilters().Where(c => !c.IsDeleted).ToListAsync(ct);
        var userCounts = await _db.Users.IgnoreQueryFilters()
            .Where(u => !u.IsDeleted)
            .GroupBy(u => u.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return tenants.Select(t =>
        {
            var company = companies.FirstOrDefault(c => c.TenantId == t.Id);
            var users = userCounts.FirstOrDefault(u => u.TenantId == t.Id)?.Count ?? 0;
            return new TenantListDto(
                t.Id, t.Name, t.Code, t.Slug, t.Status, t.Industry, t.IsActive,
                company?.Name, company?.City, users,
                TenantBootstrap.DeserializeModules(t.EnabledModulesJson),
                t.CreatedAt);
        }).ToList();
    }

    public async Task<TenantDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var t = await _db.Tenants.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return null;

        var company = await _db.Companies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == id && !c.IsDeleted, ct);
        var users = await _db.Users.IgnoreQueryFilters()
            .Where(u => u.TenantId == id && !u.IsDeleted)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderBy(u => u.FullName)
            .Select(u => new UserDto(
                u.Id, u.Email, u.FullName,
                u.UserRoles.Select(r => r.Role!.Code).FirstOrDefault() ?? "User",
                u.IsPlatformAdmin))
            .ToListAsync(ct);

        CompanyDto? companyDto = company is null
            ? null
            : new CompanyDto(company.Id, company.Name, company.ShortName, company.Address, company.City,
                company.Country, company.Phone, company.Email, company.Currency, company.Ntn, company.Industry,
                company.DefaultSaleTaxPercent, company.DefaultPurchaseTaxPercent);

        return new TenantDetailDto(
            t.Id, t.Name, t.Code, t.Slug, t.Status, t.Industry, t.IsActive,
            companyDto, TenantBootstrap.DeserializeModules(t.EnabledModulesJson), users, t.CreatedAt);
    }

    public async Task<ProvisionTenantResult> ProvisionAsync(ProvisionTenantRequest request, CancellationToken ct = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Code == code && !t.IsDeleted, ct))
            throw new InvalidOperationException($"Tenant code '{code}' already exists.");

        var email = request.AdminEmail.Trim().ToLowerInvariant();
        if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email && !u.IsDeleted, ct))
            throw new InvalidOperationException($"Admin email '{email}' is already registered.");

        var tenantId = Guid.NewGuid();
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? TenantBootstrap.ToSlug(request.Name)
            : TenantBootstrap.ToSlug(request.Slug!);
        var modules = (request.Modules?.Count > 0 ? request.Modules : SoftaxeModuleCatalog.Default).ToList();

        var previousTenant = _tenant.TenantId;
        var previousUser = _tenant.UserId;
        _tenant.Set(tenantId, null);

        try
        {
            var tenant = new Tenant
            {
                Id = tenantId,
                TenantId = tenantId,
                Name = request.Name.Trim(),
                Code = code,
                Slug = slug,
                Status = "Active",
                Industry = request.Industry,
                IsActive = true,
                EnabledModulesJson = TenantBootstrap.SerializeModules(modules)
            };
            _db.Tenants.Add(tenant);

            var company = new Company
            {
                TenantId = tenantId,
                Name = request.Name.Trim(),
                ShortName = code.Length <= 6 ? code : code[..6],
                Address = request.Address,
                City = request.City,
                Country = string.IsNullOrWhiteSpace(request.Country) ? "Pakistan" : request.Country,
                Phone = request.Phone,
                Email = email,
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "PKR" : request.Currency,
                Ntn = request.Ntn,
                Industry = request.Industry,
                SaleModule = modules.Contains("SALES", StringComparer.OrdinalIgnoreCase),
                PurchaseModule = modules.Contains("PURCHASE", StringComparer.OrdinalIgnoreCase),
                InventoryModule = modules.Contains("INVENTORY", StringComparer.OrdinalIgnoreCase),
                AccountsModule = modules.Contains("ACCOUNTS", StringComparer.OrdinalIgnoreCase)
            };
            _db.Companies.Add(company);
            await _db.SaveChangesAsync(ct);

            var branch = new Branch
            {
                TenantId = tenantId,
                CompanyId = company.Id,
                Code = "MAIN",
                Name = "Main Branch",
                IsDefault = true
            };
            _db.Branches.Add(branch);

            var adminRole = new Role { TenantId = tenantId, Code = "Admin", Name = "Administrator" };
            _db.Roles.Add(adminRole);

            var admin = new User
            {
                TenantId = tenantId,
                Email = email,
                FullName = request.AdminFullName.Trim(),
                IsPlatformAdmin = false,
                PasswordHash = AuthService.HashPassword(request.AdminPassword)
            };
            _db.Users.Add(admin);
            await _db.SaveChangesAsync(ct);

            _db.UserRoles.Add(new UserRole { TenantId = tenantId, UserId = admin.Id, RoleId = adminRole.Id });
            _db.Accounts.AddRange(TenantBootstrap.DefaultChartOfAccounts(tenantId));
            _db.Warehouses.Add(new Warehouse
            {
                TenantId = tenantId,
                BranchId = branch.Id,
                Code = "MAIN",
                Name = "Main Warehouse",
                IsDefault = true
            });

            await _db.SaveChangesAsync(ct);

            return new ProvisionTenantResult(
                tenantId, company.Id, admin.Id,
                $"Tenant '{tenant.Name}' provisioned with admin {admin.Email}.");
        }
        finally
        {
            _tenant.Set(previousTenant, previousUser);
        }
    }

    public async Task<TenantDetailDto?> UpdateModulesAsync(Guid id, IReadOnlyList<string> modules, CancellationToken ct = default)
    {
        var t = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (t is null) return null;

        var normalized = modules
            .Where(m => SoftaxeModuleCatalog.All.Contains(m, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        t.EnabledModulesJson = TenantBootstrap.SerializeModules(normalized);

        var company = await _db.Companies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == id && !c.IsDeleted, ct);
        if (company is not null)
        {
            company.SaleModule = normalized.Contains("SALES", StringComparer.OrdinalIgnoreCase);
            company.PurchaseModule = normalized.Contains("PURCHASE", StringComparer.OrdinalIgnoreCase);
            company.InventoryModule = normalized.Contains("INVENTORY", StringComparer.OrdinalIgnoreCase);
            company.AccountsModule = normalized.Contains("ACCOUNTS", StringComparer.OrdinalIgnoreCase);
        }

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<bool> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var t = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (t is null) return false;
        t.IsActive = isActive;
        t.Status = isActive ? "Active" : "Suspended";
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ─── Deferred module stubs ──────────────────────────────────────────────────

public class VoucherService : IVoucherService
{
    public Task<IReadOnlyList<VoucherListDto>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<VoucherListDto>>(Array.Empty<VoucherListDto>());

    public Task<VoucherDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult<VoucherDto?>(null);

    public Task<VoucherDto> CreateAsync(VoucherCreateDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();
}

public class SaleService : ISaleService
{
    public Task<IReadOnlyList<SaleInvoiceDto>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SaleInvoiceDto>>(Array.Empty<SaleInvoiceDto>());

    public Task<SaleInvoiceDto> CreateAsync(SaleInvoiceCreateDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();
}

public class PartySettlementService : IPartySettlementService
{
    public Task<IReadOnlyList<PartySettlementDto>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<PartySettlementDto>>(Array.Empty<PartySettlementDto>());

    public Task<PartySettlementDto> CreateAsync(PartySettlementCreateDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();
}

public class PurchaseContractService : IPurchaseContractService
{
    public Task<IReadOnlyList<PurchaseContractDto>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<PurchaseContractDto>>(Array.Empty<PurchaseContractDto>());

    public Task<PurchaseContractDto> CreateAsync(PurchaseContractCreateDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();

    public Task<bool> CloseAsync(Guid id, CancellationToken ct = default) =>
        throw Deferred.Mutate();
}

public class CostCentreService : ICostCentreService
{
    public Task<IReadOnlyList<CostCentreDto>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<CostCentreDto>>(Array.Empty<CostCentreDto>());

    public Task<CostCentreDto> CreateAsync(CostCentreUpsertDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();
}

public class ChequeService : IChequeService
{
    public Task<IReadOnlyList<ChequeBookDto>> GetBooksAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ChequeBookDto>>(Array.Empty<ChequeBookDto>());

    public Task<ChequeBookDto> CreateBookAsync(ChequeBookCreateDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();

    public Task<IReadOnlyList<PostDatedChequeDto>> GetChequesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<PostDatedChequeDto>>(Array.Empty<PostDatedChequeDto>());

    public Task<PostDatedChequeDto> CreateChequeAsync(PostDatedChequeCreateDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();

    public Task<PostDatedChequeDto?> UpdateStatusAsync(Guid id, PostDatedChequeStatusDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();
}

public class FarmShareHolderService : IFarmShareHolderService
{
    public Task<IReadOnlyList<FarmShareHolderDto>> GetAllAsync(Guid? flockId = null, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<FarmShareHolderDto>>(Array.Empty<FarmShareHolderDto>());

    public Task<FarmShareHolderDto> CreateAsync(FarmShareHolderCreateDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();
}

public class SalesForceService : ISalesForceService
{
    public Task<IReadOnlyList<SalesAreaDto>> GetAreasAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SalesAreaDto>>(Array.Empty<SalesAreaDto>());

    public Task<SalesAreaDto> CreateAreaAsync(SalesAreaUpsertDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();

    public Task<IReadOnlyList<SalesmanDto>> GetSalesmenAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SalesmanDto>>(Array.Empty<SalesmanDto>());

    public Task<SalesmanDto> CreateSalesmanAsync(SalesmanUpsertDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();

    public Task<IReadOnlyList<SalesTargetDto>> GetTargetsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SalesTargetDto>>(Array.Empty<SalesTargetDto>());

    public Task<SalesTargetDto> CreateTargetAsync(SalesTargetUpsertDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();
}

public class MaterialDemandService : IMaterialDemandService
{
    public Task<IReadOnlyList<MaterialDemandDto>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MaterialDemandDto>>(Array.Empty<MaterialDemandDto>());

    public Task<MaterialDemandDto> CreateAsync(MaterialDemandCreateDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();
}

public class HrFinanceService : IHrFinanceService
{
    public Task<IReadOnlyList<SalaryAdvanceDto>> GetAdvancesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SalaryAdvanceDto>>(Array.Empty<SalaryAdvanceDto>());

    public Task<SalaryAdvanceDto> CreateAdvanceAsync(SalaryAdvanceCreateDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();

    public Task<IReadOnlyList<StaffLoanDto>> GetLoansAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<StaffLoanDto>>(Array.Empty<StaffLoanDto>());

    public Task<StaffLoanDto> CreateLoanAsync(StaffLoanCreateDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();

    public Task<IReadOnlyList<EobiDto>> GetEobiAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<EobiDto>>(Array.Empty<EobiDto>());

    public Task<EobiDto> CreateEobiAsync(EobiCreateDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();
}

public class ProductConversionService : IProductConversionService
{
    public Task<IReadOnlyList<ProductConversionDto>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ProductConversionDto>>(Array.Empty<ProductConversionDto>());

    public Task<ProductConversionDto> CreateAsync(ProductConversionUpsertDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();
}

public class BudgetService : IBudgetService
{
    public Task<IReadOnlyList<BudgetDto>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<BudgetDto>>(Array.Empty<BudgetDto>());

    public Task<BudgetDto> CreateAsync(BudgetCreateDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();

    public Task<bool> ApproveAsync(Guid id, CancellationToken ct = default) =>
        throw Deferred.Mutate();
}

public class DayCloseService : IDayCloseService
{
    public Task<IReadOnlyList<DayCloseDto>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<DayCloseDto>>(Array.Empty<DayCloseDto>());

    public Task<DayCloseDto> CloseAsync(DayCloseCreateDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();

    public Task<bool> IsDateClosedAsync(DateTime date, CancellationToken ct = default) =>
        Task.FromResult(false);
}

public class TradeReturnService : ITradeReturnService
{
    public Task<IReadOnlyList<TradeReturnDto>> GetSaleReturnsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<TradeReturnDto>>(Array.Empty<TradeReturnDto>());

    public Task<TradeReturnDto> CreateSaleReturnAsync(TradeReturnCreateDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();

    public Task<IReadOnlyList<TradeReturnDto>> GetPurchaseReturnsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<TradeReturnDto>>(Array.Empty<TradeReturnDto>());

    public Task<TradeReturnDto> CreatePurchaseReturnAsync(TradeReturnCreateDto dto, CancellationToken ct = default) =>
        throw Deferred.Mutate();
}

// LeaveService / SalarySheetService / EmployeeService / AttendanceService → HrServices.cs (Phase 5)
// ReportService moved to FinanceReportService.cs (Phase 3 GL reports)
