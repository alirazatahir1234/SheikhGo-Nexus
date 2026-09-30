using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Entities;
using SheikhGoNexusERP.Domain.Enums;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

public static class DocumentNumbering
{
    public static async Task<string> NextAsync(
        AppDbContext db, Guid companyId, Guid? branchId, Guid documentTypeId,
        Guid financialYearId, string prefix, int year, CancellationToken ct)
    {
        var seq = await db.DocumentNumberSequences
            .FirstOrDefaultAsync(x =>
                x.CompanyId == companyId &&
                x.BranchId == branchId &&
                x.DocumentTypeId == documentTypeId &&
                x.FinancialYearId == financialYearId, ct);

        if (seq is null)
        {
            seq = new DocumentNumberSequence
            {
                CompanyId = companyId,
                BranchId = branchId,
                DocumentTypeId = documentTypeId,
                FinancialYearId = financialYearId,
                Prefix = prefix,
                CurrentNumber = 0,
                Padding = 6
            };
            db.DocumentNumberSequences.Add(seq);
        }

        seq.CurrentNumber++;
        var number = $"{prefix}-{year}-{seq.CurrentNumber.ToString().PadLeft(seq.Padding, '0')}";
        return number;
    }
}

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;
    private readonly IReportService _reports;
    public DashboardService(AppDbContext db, IPermissionService permissions, IReportService reports)
    {
        _db = db;
        _permissions = permissions;
        _reports = reports;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var payables = await _db.PayableBalances.SumAsync(x => (decimal?)x.Balance, ct) ?? 0;
        var receivables = await _db.ReceivableBalances.SumAsync(x => (decimal?)x.Balance, ct) ?? 0;
        var today = DateTime.UtcNow.Date;
        var purchases = await _db.PurchaseInvoices
            .Where(x => x.InvoiceDate == today && x.Status == DocumentStatus.Posted)
            .SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0;
        var sales = await _db.SalesInvoices
            .Where(x => x.InvoiceDate == today && x.Status == DocumentStatus.Posted)
            .SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0;

        var inventoryValue = await (
            from s in _db.WarehouseStocks
            join p in _db.Products on s.ProductId equals p.Id
            select (decimal?)(s.Quantity * p.AverageCost)
        ).SumAsync(ct) ?? 0;

        var pendingStatuses = new[] { DocumentStatus.Submitted, DocumentStatus.Verified };

        var pendingOrders = await _db.PurchaseOrders.AsNoTracking()
            .Include(x => x.Party)
            .Where(x => pendingStatuses.Contains(x.Status))
            .OrderByDescending(x => x.OrderDate)
            .Take(10)
            .Select(x => new PendingApprovalDto(x.Id, x.OrderNo, "PurchaseOrder", x.Status.ToString(), x.OrderDate, x.Party!.Name, x.GrandTotal))
            .ToListAsync(ct);

        var pendingReceipts = await _db.GoodsReceipts.AsNoTracking()
            .Include(x => x.Party)
            .Where(x => pendingStatuses.Contains(x.Status))
            .OrderByDescending(x => x.ReceiptDate)
            .Take(10)
            .Select(x => new PendingApprovalDto(x.Id, x.ReceiptNo, "GoodsReceipt", x.Status.ToString(), x.ReceiptDate, x.Party!.Name, 0))
            .ToListAsync(ct);

        var pendingInvoices = await _db.PurchaseInvoices.AsNoTracking()
            .Include(x => x.Party)
            .Where(x => pendingStatuses.Contains(x.Status) || x.Status == DocumentStatus.Approved)
            .OrderByDescending(x => x.InvoiceDate)
            .Take(10)
            .Select(x => new PendingApprovalDto(x.Id, x.InvoiceNo, "PurchaseInvoice", x.Status.ToString(), x.InvoiceDate, x.Party!.Name, x.GrandTotal))
            .ToListAsync(ct);

        var pending = pendingOrders.Concat(pendingReceipts).Concat(pendingInvoices)
            .OrderByDescending(x => x.Date)
            .Take(12)
            .ToList();

        var recentOrders = await _db.PurchaseOrders.AsNoTracking()
            .Include(x => x.Party)
            .OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => new RecentDocumentDto(x.Id, x.OrderNo, "PurchaseOrder", x.Status.ToString(), x.OrderDate, x.Party!.Name, x.GrandTotal))
            .ToListAsync(ct);

        var recentReceipts = await _db.GoodsReceipts.AsNoTracking()
            .Include(x => x.Party)
            .OrderByDescending(x => x.ReceiptDate).ThenByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => new RecentDocumentDto(x.Id, x.ReceiptNo, "GoodsReceipt", x.Status.ToString(), x.ReceiptDate, x.Party!.Name, 0))
            .ToListAsync(ct);

        var recentInvoices = await _db.PurchaseInvoices.AsNoTracking()
            .Include(x => x.Party)
            .OrderByDescending(x => x.InvoiceDate).ThenByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => new RecentDocumentDto(x.Id, x.InvoiceNo, "PurchaseInvoice", x.Status.ToString(), x.InvoiceDate, x.Party!.Name, x.GrandTotal))
            .ToListAsync(ct);

        var recent = recentOrders.Concat(recentReceipts).Concat(recentInvoices)
            .OrderByDescending(x => x.Date)
            .Take(12)
            .ToList();

        return new DashboardSummaryDto(sales, purchases, inventoryValue, receivables, payables, Array.Empty<VoucherListDto>(), pending, recent);
    }

    public async Task<ExecutiveDashboardDto> GetExecutiveAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var f = (from ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)).Date;
        var t = (to ?? DateTime.UtcNow.Date).Date;
        var sections = new List<ReportSectionDto>();

        async Task<bool> Can(string code) =>
            await _permissions.HasAsync(code, ct) || await _permissions.HasAsync("Reports.Executive.View", ct);

        if (await Can("Reports.Sales.View") || await Can("Reports.Executive.View"))
        {
            var sales = await _db.SalesInvoices.Where(x => x.Status == DocumentStatus.Posted && x.InvoiceDate >= f && x.InvoiceDate <= t)
                .SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0;
            var collections = await _db.CustomerPayments.Where(x => x.Status == DocumentStatus.Posted && x.PaymentDate >= f && x.PaymentDate <= t)
                .SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
            var ar = await _db.ReceivableBalances.SumAsync(x => (decimal?)x.Balance, ct) ?? 0;
            sections.Add(new ReportSectionDto("sales", "Sales",
            [
                new("sales", "Sales (invoices)", sales, null, "/reports?domain=sales"),
                new("collections", "Collections", collections, null, "/reports?domain=sales&tab=collections"),
                new("ar", "Receivables", ar, null, "/reports?domain=finance&tab=ar")
            ]));
        }

        if (await Can("Reports.Procurement.View") || await Can("Reports.Executive.View"))
        {
            var purchases = await _db.PurchaseInvoices.Where(x => x.Status == DocumentStatus.Posted && x.InvoiceDate >= f && x.InvoiceDate <= t)
                .SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0;
            var ap = await _db.PayableBalances.SumAsync(x => (decimal?)x.Balance, ct) ?? 0;
            sections.Add(new ReportSectionDto("procurement", "Procurement",
            [
                new("purchases", "Purchases", purchases, null, "/reports?domain=procurement"),
                new("ap", "Payables", ap, null, "/reports?domain=finance&tab=ap")
            ]));
        }

        if (await Can("Reports.Inventory.View") || await Can("Reports.Executive.View") || await _permissions.HasAsync("Inventory.Reports.View", ct))
        {
            var inv = await (
                from s in _db.WarehouseStocks
                join p in _db.Products on s.ProductId equals p.Id
                select (decimal?)(s.Quantity * p.AverageCost)
            ).SumAsync(ct) ?? 0;
            var low = await _db.WarehouseStocks.Include(x => x.Product)
                .CountAsync(x => x.Quantity <= x.Product!.ReorderLevel, ct);
            sections.Add(new ReportSectionDto("inventory", "Inventory",
            [
                new("value", "Stock value", inv, null, "/reports?domain=inventory"),
                new("low", "Low stock SKUs", low, "count", "/reports?domain=inventory")
            ]));
        }

        if (await Can("Reports.Finance.View") || await Can("Reports.Executive.View") || await _permissions.HasAsync("Finance.Reports.View", ct))
        {
            var pl = await _reports.GetProfitLossAsync(f, t, ct);
            var cash = await _db.CashBankAccounts.CountAsync(ct);
            sections.Add(new ReportSectionDto("finance", "Finance",
            [
                new("revenue", "Revenue", pl.Revenue, null, "/reports?domain=finance&tab=pl"),
                new("expenses", "Expenses", pl.Expenses + pl.CostOfSales, null, "/reports?domain=finance&tab=pl"),
                new("profit", "Net profit", pl.NetProfit, null, "/reports?domain=finance&tab=pl"),
                new("cashbanks", "Cash/Bank accounts", cash, "count", "/finance")
            ]));
        }

        if (await Can("Reports.HR.View") || await Can("Reports.Executive.View"))
        {
            var emps = await _db.Employees.CountAsync(x => x.IsActive, ct);
            var payroll = await _db.PayrollRuns.Where(x => x.Status == DocumentStatus.Posted && x.RunDate >= f && x.RunDate <= t)
                .SumAsync(x => (decimal?)x.NetTotal, ct) ?? 0;
            sections.Add(new ReportSectionDto("hr", "HR",
            [
                new("employees", "Active employees", emps, "count", "/reports?domain=hr"),
                new("payroll", "Payroll net", payroll, null, "/reports?domain=hr&tab=payroll")
            ]));
        }

        if (await Can("Reports.Production.View") || await Can("Reports.Executive.View"))
        {
            var produced = await _db.ProductionOrders.Where(x => x.OrderDate >= f && x.OrderDate <= t)
                .SumAsync(x => (decimal?)x.QtyCompleted, ct) ?? 0;
            var cost = await _db.ProductionOrders.Where(x => x.OrderDate >= f && x.OrderDate <= t)
                .SumAsync(x => (decimal?)x.TotalCost, ct) ?? 0;
            sections.Add(new ReportSectionDto("production", "Production",
            [
                new("qty", "Produced qty", produced, null, "/reports?domain=production"),
                new("cost", "Production cost", cost, null, "/reports?domain=production")
            ]));
        }

        if (await Can("Reports.Farm.View") || await Can("Reports.Executive.View"))
        {
            var flocks = await _db.Flocks.CountAsync(x => x.Status == FlockStatus.Active, ct);
            var mortality = await _db.MortalityRecords.Where(x => x.Date >= f && x.Date <= t).SumAsync(x => (int?)x.Qty, ct) ?? 0;
            sections.Add(new ReportSectionDto("farm", "Farm",
            [
                new("flocks", "Active flocks", flocks, "count", "/reports?domain=farm"),
                new("mortality", "Mortality qty", mortality, "count", "/reports?domain=farm")
            ]));
        }

        if (await Can("Reports.Operations.View") || await Can("Reports.Executive.View"))
        {
            var net = await _db.Weighments.Where(x => x.Status == WeighmentStatus.Completed && x.CreatedAt.Date >= f && x.CreatedAt.Date <= t)
                .SumAsync(x => (decimal?)(x.NetWeight ?? 0), ct) ?? 0;
            var tickets = await _db.Weighments.CountAsync(x => x.CreatedAt.Date >= f && x.CreatedAt.Date <= t, ct);
            sections.Add(new ReportSectionDto("operations", "Operations",
            [
                new("weighments", "Weighments", tickets, "count", "/reports?domain=operations"),
                new("netWeight", "Net weight", net, "kg", "/reports?domain=operations")
            ]));
        }

        if (await Can("Reports.POS.View") || await Can("Reports.Executive.View"))
        {
            var pos = await _db.PosSales.Where(x => x.Status == DocumentStatus.Posted && x.SaleDate >= f && x.SaleDate <= t)
                .SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0;
            var variance = await _db.PosShifts.Where(x => x.Status == PosShiftStatus.Closed && x.ClosedAt != null && x.ClosedAt.Value.Date >= f && x.ClosedAt.Value.Date <= t)
                .SumAsync(x => (decimal?)(x.Variance ?? 0), ct) ?? 0;
            sections.Add(new ReportSectionDto("pos", "POS",
            [
                new("sales", "POS sales", pos, null, "/reports?domain=pos"),
                new("variance", "Cash variance", variance, null, "/reports?domain=pos&tab=shifts")
            ]));
        }

        return new ExecutiveDashboardDto(f, t, sections);
    }
}

public class AccountService : IAccountService
{
    private readonly AppDbContext _db;
    public AccountService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AccountDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Accounts.OrderBy(x => x.Code)
            .Select(x => new AccountDto(x.Id, x.Code, x.Name, x.AccountType, x.ParentId, x.IsPostable, x.OpeningBalance, x.Level))
            .ToListAsync(ct);

    public async Task<AccountDto> CreateAsync(AccountUpsertDto dto, CancellationToken ct = default)
    {
        var a = new Account
        {
            Code = dto.Code, Name = dto.Name, AccountType = dto.AccountType,
            ParentId = dto.ParentId, IsPostable = dto.IsPostable, OpeningBalance = dto.OpeningBalance,
            Level = dto.ParentId is null ? 1 : 2
        };
        _db.Accounts.Add(a);
        await _db.SaveChangesAsync(ct);
        return new AccountDto(a.Id, a.Code, a.Name, a.AccountType, a.ParentId, a.IsPostable, a.OpeningBalance, a.Level);
    }
}

public class PartyService : IPartyService
{
    private readonly AppDbContext _db;
    public PartyService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PartyDto>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var q = _db.Parties.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.Name.Contains(search) || x.Code.Contains(search));
        return await q.OrderBy(x => x.Name)
            .Select(x => new PartyDto(x.Id, x.Code, x.Name, x.PartyType, x.Phone, x.Email, x.City, x.Address,
                x.CustomerProfile != null ? x.CustomerProfile.CreditLimit
                    : x.SupplierProfile != null ? x.SupplierProfile.CreditLimit : 0,
                0, x.IsActive))
            .ToListAsync(ct);
    }

    public async Task<PartyDto> CreateAsync(PartyUpsertDto dto, CancellationToken ct = default)
    {
        var p = new Party
        {
            Code = dto.Code, Name = dto.Name, PartyType = dto.PartyType,
            Phone = dto.Phone, Email = dto.Email, City = dto.City, Address = dto.Address
        };
        _db.Parties.Add(p);
        if (dto.PartyType is PartyType.Supplier or PartyType.Both)
        {
            _db.SupplierProfiles.Add(new SupplierProfile
            {
                Party = p, CreditLimit = dto.CreditLimit, PaymentTerms = "Net 30"
            });
        }
        if (dto.PartyType is PartyType.Customer or PartyType.Both)
        {
            _db.CustomerProfiles.Add(new CustomerProfile
            {
                Party = p, CreditLimit = dto.CreditLimit, PaymentTerms = "Net 30"
            });
        }
        await _db.SaveChangesAsync(ct);
        return new PartyDto(p.Id, p.Code, p.Name, p.PartyType, p.Phone, p.Email, p.City, p.Address, dto.CreditLimit, 0, true);
    }

    public async Task<PartyDto?> UpdateAsync(Guid id, PartyUpsertDto dto, CancellationToken ct = default)
    {
        var p = await _db.Parties
            .Include(x => x.SupplierProfile)
            .Include(x => x.CustomerProfile)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;
        p.Code = dto.Code; p.Name = dto.Name; p.PartyType = dto.PartyType;
        p.Phone = dto.Phone; p.Email = dto.Email; p.City = dto.City; p.Address = dto.Address;
        if (dto.PartyType is PartyType.Supplier or PartyType.Both)
        {
            if (p.SupplierProfile is null)
                _db.SupplierProfiles.Add(new SupplierProfile { PartyId = p.Id, CreditLimit = dto.CreditLimit });
            else
                p.SupplierProfile.CreditLimit = dto.CreditLimit;
        }
        if (dto.PartyType is PartyType.Customer or PartyType.Both)
        {
            if (p.CustomerProfile is null)
                _db.CustomerProfiles.Add(new CustomerProfile { PartyId = p.Id, CreditLimit = dto.CreditLimit });
            else
                p.CustomerProfile.CreditLimit = dto.CreditLimit;
        }
        await _db.SaveChangesAsync(ct);
        return new PartyDto(p.Id, p.Code, p.Name, p.PartyType, p.Phone, p.Email, p.City, p.Address, dto.CreditLimit, 0, p.IsActive);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _db.Parties.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return false;
        p.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    public ProductService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var q = _db.Products.Include(x => x.Category).Include(x => x.UnitOfMeasure).Include(x => x.Stocks).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.Name.Contains(search) || x.Code.Contains(search));
        return await q.OrderBy(x => x.Name).Select(x => new ProductDto(
            x.Id, x.Code, x.Name, x.Category != null ? x.Category.Name : null,
            x.UnitOfMeasure != null ? x.UnitOfMeasure.Code : "BAG",
            x.SalePrice, x.PurchasePrice, x.Stocks.Sum(s => s.Quantity),
            x.ReorderLevel, x.IsActive, x.IsRawMaterial, x.IsFinishedGood, x.AverageCost)).ToListAsync(ct);
    }

    public async Task<ProductDto> CreateAsync(ProductUpsertDto dto, CancellationToken ct = default)
    {
        var uom = await _db.UnitsOfMeasure.FirstOrDefaultAsync(x => x.Code == dto.Unit, ct)
            ?? await _db.UnitsOfMeasure.FirstAsync(ct);
        ProductCategory? cat = null;
        if (!string.IsNullOrWhiteSpace(dto.Category))
        {
            cat = await _db.ProductCategories.FirstOrDefaultAsync(x => x.Name == dto.Category, ct);
            if (cat is null)
            {
                cat = new ProductCategory { Name = dto.Category };
                _db.ProductCategories.Add(cat);
                await _db.SaveChangesAsync(ct);
            }
        }
        var p = new Product
        {
            Code = dto.Code, Name = dto.Name, CategoryId = cat?.Id, UnitOfMeasureId = uom.Id,
            SalePrice = dto.SalePrice, PurchasePrice = dto.PurchasePrice,
            AverageCost = dto.PurchasePrice,
            ReorderLevel = dto.ReorderLevel, IsRawMaterial = dto.IsRawMaterial, IsFinishedGood = dto.IsFinishedGood
        };
        _db.Products.Add(p);
        await _db.SaveChangesAsync(ct);
        return new ProductDto(p.Id, p.Code, p.Name, cat?.Name, uom.Code, p.SalePrice, p.PurchasePrice, 0, p.ReorderLevel, true, p.IsRawMaterial, p.IsFinishedGood, p.AverageCost);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, ProductUpsertDto dto, CancellationToken ct = default)
    {
        var p = await _db.Products.Include(x => x.Category).Include(x => x.UnitOfMeasure).Include(x => x.Stocks)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;
        p.Code = dto.Code; p.Name = dto.Name; p.SalePrice = dto.SalePrice; p.PurchasePrice = dto.PurchasePrice;
        p.ReorderLevel = dto.ReorderLevel; p.IsRawMaterial = dto.IsRawMaterial; p.IsFinishedGood = dto.IsFinishedGood;
        await _db.SaveChangesAsync(ct);
        return new ProductDto(p.Id, p.Code, p.Name, p.Category?.Name, p.UnitOfMeasure?.Code ?? dto.Unit,
            p.SalePrice, p.PurchasePrice, p.Stocks.Sum(s => s.Quantity), p.ReorderLevel, p.IsActive, p.IsRawMaterial, p.IsFinishedGood, p.AverageCost);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return false;
        p.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class CompanyService : ICompanyService
{
    private readonly AppDbContext _db;
    public CompanyService(AppDbContext db) => _db = db;

    public async Task<CompanyDto?> GetAsync(CancellationToken ct = default)
    {
        var c = await _db.Companies.FirstOrDefaultAsync(ct);
        return c is null ? null : Map(c);
    }

    public async Task<CompanyDto?> UpdateAsync(CompanyUpdateDto dto, CancellationToken ct = default)
    {
        var c = await _db.Companies.FirstOrDefaultAsync(ct);
        if (c is null) return null;
        c.Name = dto.Name; c.ShortName = dto.ShortName; c.Address = dto.Address; c.City = dto.City;
        c.Country = dto.Country; c.Phone = dto.Phone; c.Email = dto.Email; c.Currency = dto.Currency;
        c.Ntn = dto.Ntn; c.Industry = dto.Industry;
        c.DefaultSaleTaxPercent = dto.DefaultSaleTaxPercent;
        c.DefaultPurchaseTaxPercent = dto.DefaultPurchaseTaxPercent;
        await _db.SaveChangesAsync(ct);
        return Map(c);
    }

    private static CompanyDto Map(Company c) =>
        new(c.Id, c.Name, c.ShortName, c.Address, c.City, c.Country, c.Phone, c.Email, c.Currency, c.Ntn, c.Industry,
            c.DefaultSaleTaxPercent, c.DefaultPurchaseTaxPercent);
}

public class BrandService : IBrandService
{
    private readonly AppDbContext _db;
    public BrandService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<BrandDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Brands.OrderBy(x => x.Name).Select(x => new BrandDto(x.Id, x.Code, x.Name, !x.IsDeleted)).ToListAsync(ct);

    public async Task<BrandDto> CreateAsync(BrandUpsertDto dto, CancellationToken ct = default)
    {
        var b = new Brand { Code = dto.Code, Name = dto.Name };
        _db.Brands.Add(b);
        await _db.SaveChangesAsync(ct);
        return new BrandDto(b.Id, b.Code, b.Name, dto.IsActive);
    }
}

public class WarehouseService : IWarehouseService
{
    private readonly AppDbContext _db;
    public WarehouseService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<WarehouseDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Warehouses.OrderBy(x => x.Name)
            .Select(x => new WarehouseDto(x.Id, x.Code, x.Name, x.Location, x.IsDefault)).ToListAsync(ct);

    public async Task<WarehouseDto> CreateAsync(WarehouseUpsertDto dto, CancellationToken ct = default)
    {
        var w = new Warehouse { Code = dto.Code, Name = dto.Name, Location = dto.Location, IsDefault = dto.IsDefault };
        _db.Warehouses.Add(w);
        await _db.SaveChangesAsync(ct);
        return new WarehouseDto(w.Id, w.Code, w.Name, w.Location, w.IsDefault);
    }

    public async Task<WarehouseDto?> UpdateAsync(Guid id, WarehouseUpsertDto dto, CancellationToken ct = default)
    {
        var w = await _db.Warehouses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (w is null) return null;
        w.Code = dto.Code; w.Name = dto.Name; w.Location = dto.Location; w.IsDefault = dto.IsDefault;
        await _db.SaveChangesAsync(ct);
        return new WarehouseDto(w.Id, w.Code, w.Name, w.Location, w.IsDefault);
    }
}

public class UserAdminService : IUserAdminService
{
    private readonly AppDbContext _db;
    public UserAdminService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken ct = default) =>
        await _db.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .Select(x => new UserDto(x.Id, x.Email, x.FullName,
                x.UserRoles.Select(r => r.Role!.Code).FirstOrDefault() ?? "User", x.IsPlatformAdmin))
            .ToListAsync(ct);

    public async Task<UserDto> CreateAsync(UserCreateDto dto, CancellationToken ct = default)
    {
        var user = new User
        {
            Email = dto.Email.Trim().ToLowerInvariant(),
            FullName = dto.FullName,
            PasswordHash = AuthService.HashPassword(dto.Password)
        };
        _db.Users.Add(user);
        var role = await _db.Roles.FirstOrDefaultAsync(x => x.Code == dto.Role || x.Name == dto.Role, ct);
        if (role is null)
        {
            role = new Role { Code = dto.Role, Name = dto.Role };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync(ct);
        }
        _db.UserRoles.Add(new UserRole { User = user, RoleId = role.Id });
        await _db.SaveChangesAsync(ct);
        return new UserDto(user.Id, user.Email, user.FullName, role.Code, user.IsPlatformAdmin);
    }

    public async Task<UserDto?> UpdateAsync(Guid id, UserUpdateDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (user is null) return null;
        user.FullName = dto.FullName;
        user.IsActive = dto.IsActive;
        await _db.SaveChangesAsync(ct);
        var role = user.UserRoles.Select(r => r.Role?.Code).FirstOrDefault() ?? dto.Role;
        return new UserDto(user.Id, user.Email, user.FullName, role, user.IsPlatformAdmin);
    }
}

public class PurchaseService : IPurchaseService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly IDocumentWorkflowService _workflow;
    public PurchaseService(AppDbContext db, IAuditService audit, IDocumentWorkflowService workflow)
    {
        _db = db;
        _audit = audit;
        _workflow = workflow;
    }

    private async Task<(Company company, Branch branch, FinancialYear fy)> ScopeAsync(CancellationToken ct)
    {
        var company = await _db.Companies.FirstAsync(ct);
        var branch = await _db.Branches.FirstAsync(x => x.IsDefault || true, ct);
        var fy = await _db.FinancialYears.FirstAsync(x => x.IsCurrent, ct);
        return (company, branch, fy);
    }

    private async Task<DocumentType> DocTypeAsync(string code, CancellationToken ct) =>
        await _db.DocumentTypes.FirstAsync(x => x.Code == code, ct);

    public async Task<IReadOnlyList<PurchaseOrderDto>> GetOrdersAsync(CancellationToken ct = default) =>
        await _db.PurchaseOrders.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(x => x.OrderDate)
            .Select(x => new PurchaseOrderDto(x.Id, x.OrderNo, x.OrderDate, x.PartyId, x.Party!.Name, x.WarehouseId, x.Status,
                x.SubTotal, x.Tax, x.GrandTotal, x.Remarks,
                x.Lines.Select(l => new InvoiceLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList(), null))
            .ToListAsync(ct);

    public async Task<PurchaseOrderDto?> GetOrderByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = (await GetOrdersAsync(ct)).FirstOrDefault(x => x.Id == id);
        if (dto is null) return null;
        var entity = await _db.PurchaseOrders.AsNoTracking().FirstAsync(x => x.Id == id, ct);
        var workflow = await DocumentWorkflowActorsResolver.ResolveAsync(
            _db, nameof(PurchaseOrder), id, entity.DocumentId, entity.CreatedBy, entity.CreatedAt, ct);
        return dto with { Workflow = workflow };
    }

    public async Task<PurchaseOrderDto> CreateOrderAsync(PurchaseOrderCreateDto dto, CancellationToken ct = default)
    {
        var (company, branch, fy) = await ScopeAsync(ct);
        var dt = await DocTypeAsync("PO", ct);
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var no = await DocumentNumbering.NextAsync(_db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "PO", fy.StartDate.Year, ct);
        var sub = dto.Lines.Sum(l => l.Qty * l.Rate);
        var document = new Document { DocumentTypeId = dt.Id, CompanyId = company.Id, BranchId = branch.Id, FinancialYearId = fy.Id, DocumentNo = no, DocumentDate = dto.Date.Date, PartyId = dto.PartyId, Narration = dto.Remarks };
        var po = new PurchaseOrder
        {
            OrderNo = no, OrderDate = dto.Date.Date, PartyId = dto.PartyId, WarehouseId = dto.WarehouseId,
            Document = document, Status = DocumentStatus.Draft,
            SubTotal = sub, Tax = dto.Tax, GrandTotal = sub + dto.Tax, Remarks = dto.Remarks
        };
        var lineNo = 1;
        foreach (var l in dto.Lines)
        {
            po.Lines.Add(new PurchaseOrderLine
            {
                LineNo = lineNo++, ProductId = l.ProductId, Qty = l.Qty, Rate = l.Rate, Amount = l.Qty * l.Rate
            });
        }
        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await _audit.WriteAsync(nameof(PurchaseOrder), po.Id, "Created", null, po.Status.ToString(), ct);
        if (dto.Post) await RunQuickPathAsync(po.Id, "order", ct);
        return (await GetOrdersAsync(ct)).First(x => x.Id == po.Id);
    }

    public async Task<IReadOnlyList<GoodsReceiptDto>> GetReceiptsAsync(CancellationToken ct = default) =>
        await _db.GoodsReceipts.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(x => x.ReceiptDate)
            .Select(x => new GoodsReceiptDto(x.Id, x.ReceiptNo, x.ReceiptDate, x.PartyId, x.Party!.Name, x.WarehouseId,
                x.PurchaseOrderId, x.Status, x.Remarks,
                x.Lines.Select(l => new InvoiceLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList(), null))
            .ToListAsync(ct);

    public async Task<GoodsReceiptDto?> GetReceiptByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = (await GetReceiptsAsync(ct)).FirstOrDefault(x => x.Id == id);
        if (dto is null) return null;
        var entity = await _db.GoodsReceipts.AsNoTracking().FirstAsync(x => x.Id == id, ct);
        var workflow = await DocumentWorkflowActorsResolver.ResolveAsync(
            _db, nameof(GoodsReceipt), id, entity.DocumentId, entity.CreatedBy, entity.CreatedAt, ct);
        return dto with { Workflow = workflow };
    }

    public async Task<GoodsReceiptDto> CreateReceiptAsync(GoodsReceiptCreateDto dto, CancellationToken ct = default)
    {
        var (company, branch, fy) = await ScopeAsync(ct);
        var dt = await DocTypeAsync("GRN", ct);
        if (dto.PurchaseOrderId is Guid orderId)
        {
            var po = await _db.PurchaseOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == orderId, ct)
                ?? throw new InvalidOperationException("Purchase order not found.");
            if (po.PartyId != dto.PartyId) throw new InvalidOperationException("GRN supplier must match its purchase order.");
            foreach (var line in dto.Lines)
            {
                var poLine = po.Lines.FirstOrDefault(x => x.ProductId == line.ProductId)
                    ?? throw new InvalidOperationException("GRN product is not on the purchase order.");
                if (line.Qty <= 0 || line.Qty > poLine.Qty - poLine.ReceivedQty)
                    throw new InvalidOperationException($"GRN quantity exceeds remaining purchase-order quantity for product {line.ProductId}.");
            }
        }
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var no = await DocumentNumbering.NextAsync(_db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "GRN", fy.StartDate.Year, ct);
        var document = new Document { DocumentTypeId = dt.Id, CompanyId = company.Id, BranchId = branch.Id, FinancialYearId = fy.Id, DocumentNo = no, DocumentDate = dto.Date.Date, PartyId = dto.PartyId, Narration = dto.Remarks };
        var grn = new GoodsReceipt
        {
            ReceiptNo = no, ReceiptDate = dto.Date.Date, PartyId = dto.PartyId, WarehouseId = dto.WarehouseId,
            PurchaseOrderId = dto.PurchaseOrderId,
            Document = document, Status = DocumentStatus.Draft,
            Remarks = dto.Remarks
        };
        var lineNo = 1;
        foreach (var l in dto.Lines)
        {
            grn.Lines.Add(new GoodsReceiptLine
            {
                LineNo = lineNo++, ProductId = l.ProductId, PurchaseOrderLineId = dto.PurchaseOrderId is Guid poId ? await _db.PurchaseOrderLines.Where(x => x.PurchaseOrderId == poId && x.ProductId == l.ProductId).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct) : null, Qty = l.Qty, Rate = l.Rate, Amount = l.Qty * l.Rate
            });
        }
        _db.GoodsReceipts.Add(grn);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await _audit.WriteAsync(nameof(GoodsReceipt), grn.Id, "Created", null, grn.Status.ToString(), ct);
        if (dto.Post) await RunQuickPathAsync(grn.Id, "receipt", ct);
        return (await GetReceiptsAsync(ct)).First(x => x.Id == grn.Id);
    }

    public async Task<IReadOnlyList<PurchaseInvoiceDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.PurchaseInvoices.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(x => x.InvoiceDate)
            .Select(x => new PurchaseInvoiceDto(x.Id, x.InvoiceNo, x.InvoiceDate, x.PartyId, x.Party!.Name, x.Status,
                x.SubTotal, x.Discount, x.Tax, x.GrandTotal, x.Remarks,
                x.Lines.Select(l => new InvoiceLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList(), null))
            .ToListAsync(ct);

    public async Task<PurchaseInvoiceDto?> GetInvoiceByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = (await GetAllAsync(ct)).FirstOrDefault(x => x.Id == id);
        if (dto is null) return null;
        var entity = await _db.PurchaseInvoices.AsNoTracking().FirstAsync(x => x.Id == id, ct);
        var workflow = await DocumentWorkflowActorsResolver.ResolveAsync(
            _db, nameof(PurchaseInvoice), id, entity.DocumentId, entity.CreatedBy, entity.CreatedAt, ct);
        return dto with { Workflow = workflow };
    }

    public async Task<PurchaseInvoiceDto> CreateAsync(PurchaseInvoiceCreateDto dto, CancellationToken ct = default)
    {
        var (company, branch, fy) = await ScopeAsync(ct);
        var dt = await DocTypeAsync("PI", ct);
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var no = await DocumentNumbering.NextAsync(_db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "PI", fy.StartDate.Year, ct);
        var sub = dto.Lines.Sum(l => l.Qty * l.Rate);
        var grand = sub - dto.Discount + dto.Tax;
        var document = new Document { DocumentTypeId = dt.Id, CompanyId = company.Id, BranchId = branch.Id, FinancialYearId = fy.Id, DocumentNo = no, DocumentDate = dto.Date.Date, PartyId = dto.PartyId, Narration = dto.Remarks };
        var pi = new PurchaseInvoice
        {
            InvoiceNo = no, InvoiceDate = dto.Date.Date, PartyId = dto.PartyId,
            GoodsReceiptId = dto.GoodsReceiptId, PurchaseOrderId = dto.PurchaseOrderId,
            Document = document, Status = DocumentStatus.Draft,
            SubTotal = sub, Discount = dto.Discount, Tax = dto.Tax, GrandTotal = grand, Remarks = dto.Remarks
        };
        var lineNo = 1;
        foreach (var l in dto.Lines)
        {
            pi.Lines.Add(new PurchaseInvoiceLine
            {
                LineNo = lineNo++, ProductId = l.ProductId, Qty = l.Qty, Rate = l.Rate, Amount = l.Qty * l.Rate
            });
        }
        _db.PurchaseInvoices.Add(pi);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await _audit.WriteAsync(nameof(PurchaseInvoice), pi.Id, "Created", null, pi.Status.ToString(), ct);
        if (dto.Post) await RunQuickPathAsync(pi.Id, "invoice", ct);
        return (await GetAllAsync(ct)).First(x => x.Id == pi.Id);
    }

    public async Task<PayableBalanceDto?> GetPayableAsync(Guid partyId, CancellationToken ct = default)
    {
        var bal = await _db.PayableBalances.Include(x => x.Party)
            .FirstOrDefaultAsync(x => x.PartyId == partyId, ct);
        return bal is null ? null : new PayableBalanceDto(bal.PartyId, bal.Party?.Name, bal.Balance);
    }

    public async Task<PurchaseOrderDto?> UpdateOrderAsync(Guid id, PurchaseOrderCreateDto dto, CancellationToken ct = default)
    {
        var po = await _db.PurchaseOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (po is null) return null;
        if (po.Status != DocumentStatus.Draft) throw new InvalidOperationException("Only draft purchase orders can be edited.");
        po.OrderDate = dto.Date.Date; po.PartyId = dto.PartyId; po.WarehouseId = dto.WarehouseId; po.Tax = dto.Tax; po.Remarks = dto.Remarks;
        _db.PurchaseOrderLines.RemoveRange(po.Lines);
        po.Lines = dto.Lines.Select((x, i) => new PurchaseOrderLine { LineNo = i + 1, ProductId = x.ProductId, Qty = x.Qty, Rate = x.Rate, Amount = x.Qty * x.Rate }).ToList();
        po.SubTotal = po.Lines.Sum(x => x.Amount); po.GrandTotal = po.SubTotal + po.Tax;
        await _db.SaveChangesAsync(ct); await _audit.WriteAsync(nameof(PurchaseOrder), id, "Updated", null, "Draft updated", ct);
        return (await GetOrdersAsync(ct)).First(x => x.Id == id);
    }

    public Task<PurchaseOrderDto?> TransitionOrderAsync(Guid id, string action, CancellationToken ct = default) =>
        _workflow.TransitionOrderAsync(id, action, null, ct);

    public Task<GoodsReceiptDto?> TransitionReceiptAsync(Guid id, string action, CancellationToken ct = default) =>
        _workflow.TransitionReceiptAsync(id, action, null, ct);

    public Task<PurchaseInvoiceDto?> TransitionInvoiceAsync(Guid id, string action, CancellationToken ct = default) =>
        _workflow.TransitionInvoiceAsync(id, action, null, ct);

    public async Task<IReadOnlyList<AuditLogDto>> GetAuditAsync(string entityName, Guid id, CancellationToken ct = default) =>
        await _db.AuditLogs.Where(x => x.EntityName == entityName && x.EntityId == id.ToString()).OrderByDescending(x => x.Timestamp)
            .Select(x => new AuditLogDto(x.Id, x.EntityName, x.EntityId, x.Action, x.OldValues, x.NewValues, x.UserId, x.Timestamp, x.IpAddress)).ToListAsync(ct);

    private async Task RunQuickPathAsync(Guid id, string kind, CancellationToken ct)
    {
        var code = kind switch
        {
            "order" => "PO",
            "receipt" => "GRN",
            "invoice" => "PI",
            _ => throw new InvalidOperationException($"Unknown quick-path kind '{kind}'.")
        };
        var docType = await DocTypeAsync(code, ct);
        async Task Step(Func<Task> action)
        {
            await action();
        }

        if (kind == "order")
        {
            await Step(() => TransitionOrderAsync(id, "submit", ct));
            if (docType.RequiresVerification) await Step(() => TransitionOrderAsync(id, "verify", ct));
            if (docType.RequiresApproval) await Step(() => TransitionOrderAsync(id, "approve", ct));
            await Step(() => TransitionOrderAsync(id, "post", ct));
        }
        else if (kind == "receipt")
        {
            await Step(() => TransitionReceiptAsync(id, "submit", ct));
            if (docType.RequiresVerification) await Step(() => TransitionReceiptAsync(id, "verify", ct));
            if (docType.RequiresApproval) await Step(() => TransitionReceiptAsync(id, "approve", ct));
            await Step(() => TransitionReceiptAsync(id, "post", ct));
        }
        else
        {
            await Step(() => TransitionInvoiceAsync(id, "submit", ct));
            if (docType.RequiresVerification) await Step(() => TransitionInvoiceAsync(id, "verify", ct));
            if (docType.RequiresApproval) await Step(() => TransitionInvoiceAsync(id, "approve", ct));
            await Step(() => TransitionInvoiceAsync(id, "post", ct));
        }
    }
}
