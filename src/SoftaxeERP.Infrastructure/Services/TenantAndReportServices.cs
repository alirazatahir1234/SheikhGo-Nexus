using Microsoft.EntityFrameworkCore;
using SoftaxeERP.Application.DTOs;
using SoftaxeERP.Application.Interfaces;
using SoftaxeERP.Domain.Entities;
using SoftaxeERP.Domain.Enums;
using SoftaxeERP.Infrastructure.Persistence;

namespace SoftaxeERP.Infrastructure.Services;

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
        var tenants = await _db.Tenants.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);
        var companies = await _db.Companies.IgnoreQueryFilters()
            .Where(c => !c.IsDeleted)
            .ToListAsync(ct);
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
        var t = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return null;

        var company = await _db.Companies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == id && !c.IsDeleted, ct);
        var users = await _db.Users.IgnoreQueryFilters()
            .Where(u => u.TenantId == id && !u.IsDeleted)
            .OrderBy(u => u.FullName)
            .Select(u => new UserDto(u.Id, u.Email, u.FullName, u.Role, u.IsPlatformAdmin))
            .ToListAsync(ct);

        CompanyDto? companyDto = company is null
            ? null
            : new CompanyDto(company.Id, company.Name, company.ShortName, company.Address, company.City,
                company.Country, company.Phone, company.Email, company.Currency, company.Ntn, company.Industry,
                company.DefaultSaleTaxPercent, company.DefaultPurchaseTaxPercent, company.SyncToFbr, company.TaxModule);

        return new TenantDetailDto(
            t.Id, t.Name, t.Code, t.Slug, t.Status, t.Industry, t.IsActive,
            companyDto, TenantBootstrap.DeserializeModules(t.EnabledModulesJson), users, t.CreatedAt);
    }

    public async Task<ProvisionTenantResult> ProvisionAsync(ProvisionTenantRequest request, CancellationToken ct = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Tenants.AnyAsync(t => t.Code == code, ct))
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

            var company = new CompanyProfile
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
                CurrencySymbol = "Rs",
                Ntn = request.Ntn,
                Industry = request.Industry,
                SaleModule = modules.Contains("SALES", StringComparer.OrdinalIgnoreCase),
                PurchaseModule = modules.Contains("PURCHASE", StringComparer.OrdinalIgnoreCase),
                InventoryModule = modules.Contains("INVENTORY", StringComparer.OrdinalIgnoreCase),
                AccountsModule = modules.Contains("ACCOUNTS", StringComparer.OrdinalIgnoreCase),
                ProductionModule = modules.Contains("PRODUCTION", StringComparer.OrdinalIgnoreCase),
                PayrollModule = modules.Contains("PAYROLL", StringComparer.OrdinalIgnoreCase),
                FarmModule = modules.Contains("FARM", StringComparer.OrdinalIgnoreCase),
                TaxModule = modules.Contains("TAX", StringComparer.OrdinalIgnoreCase),
            };
            _db.Companies.Add(company);

            var admin = new AppUser
            {
                TenantId = tenantId,
                Email = email,
                FullName = request.AdminFullName.Trim(),
                Role = "Admin",
                IsPlatformAdmin = false,
                PasswordHash = AuthService.HashPassword(request.AdminPassword)
            };
            _db.Users.Add(admin);

            _db.Accounts.AddRange(TenantBootstrap.DefaultChartOfAccounts(tenantId));
            _db.Warehouses.Add(new Warehouse
            {
                TenantId = tenantId,
                Code = "WH1",
                Name = "Main Godown",
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
        var t = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == id, ct);
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
            company.ProductionModule = normalized.Contains("PRODUCTION", StringComparer.OrdinalIgnoreCase);
            company.PayrollModule = normalized.Contains("PAYROLL", StringComparer.OrdinalIgnoreCase);
            company.FarmModule = normalized.Contains("FARM", StringComparer.OrdinalIgnoreCase);
            company.TaxModule = normalized.Contains("TAX", StringComparer.OrdinalIgnoreCase);
        }

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<bool> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var t = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return false;
        t.IsActive = isActive;
        t.Status = isActive ? "Active" : "Suspended";
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class ReportService : IReportService
{
    private readonly AppDbContext _db;
    public ReportService(AppDbContext db) => _db = db;

    public async Task<LedgerReportDto> GetLedgerAsync(Guid accountId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId, ct)
            ?? throw new InvalidOperationException("Account not found.");

        from = from.Date;
        to = to.Date;

        var prior = await _db.VoucherLines
            .Where(l => l.AccountId == accountId && l.Voucher!.Status == DocumentStatus.Posted && l.Voucher.Date < from)
            .Select(l => new { l.Debit, l.Credit })
            .ToListAsync(ct);
        var opening = account.OpeningBalance + prior.Sum(x => x.Debit - x.Credit);

        var linesRaw = await _db.VoucherLines
            .Where(l => l.AccountId == accountId && l.Voucher!.Status == DocumentStatus.Posted
                        && l.Voucher.Date >= from && l.Voucher.Date <= to)
            .OrderBy(l => l.Voucher!.Date).ThenBy(l => l.Voucher!.VoucherNo).ThenBy(l => l.LineNo)
            .Select(l => new
            {
                l.Voucher!.Date,
                l.Voucher.VoucherNo,
                Type = l.Voucher.VoucherType.ToString(),
                l.Voucher.Narration,
                l.Debit,
                l.Credit
            })
            .ToListAsync(ct);

        var balance = opening;
        var lines = new List<LedgerLineDto>();
        foreach (var l in linesRaw)
        {
            balance += l.Debit - l.Credit;
            lines.Add(new LedgerLineDto(l.Date, l.VoucherNo, l.Type, l.Narration, l.Debit, l.Credit, balance));
        }

        return new LedgerReportDto(account.Id, account.Code, account.Name, from, to, opening, balance, lines);
    }

    public async Task<TrialBalanceReportDto> GetTrialBalanceAsync(DateTime asOf, CancellationToken ct = default)
    {
        asOf = asOf.Date;
        var accounts = await _db.Accounts.Where(a => a.IsPostable).OrderBy(a => a.Code).ToListAsync(ct);
        var movements = await _db.VoucherLines
            .Where(l => l.Voucher!.Status == DocumentStatus.Posted && l.Voucher.Date <= asOf)
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .ToListAsync(ct);
        var map = movements.ToDictionary(x => x.AccountId);

        var lines = new List<TrialBalanceLineDto>();
        decimal totalDr = 0, totalCr = 0;
        foreach (var a in accounts)
        {
            map.TryGetValue(a.Id, out var m);
            var debit = a.OpeningBalance > 0 ? a.OpeningBalance : 0m;
            var credit = a.OpeningBalance < 0 ? Math.Abs(a.OpeningBalance) : 0m;
            debit += m?.Debit ?? 0;
            credit += m?.Credit ?? 0;

            // Normalize to single-sided presentation for TB
            var net = debit - credit;
            var dr = net >= 0 ? net : 0;
            var cr = net < 0 ? Math.Abs(net) : 0;
            if (dr == 0 && cr == 0) continue;

            totalDr += dr;
            totalCr += cr;
            lines.Add(new TrialBalanceLineDto(a.Id, a.Code, a.Name, a.AccountType.ToString(), dr, cr, net));
        }

        return new TrialBalanceReportDto(asOf, totalDr, totalCr, lines);
    }

    public Task<AgingReportDto> GetReceivablesAgingAsync(DateTime? asOf = null, CancellationToken ct = default)
        => BuildAgingAsync(isReceivable: true, asOf ?? DateTime.UtcNow.Date, ct);

    public Task<AgingReportDto> GetPayablesAgingAsync(DateTime? asOf = null, CancellationToken ct = default)
        => BuildAgingAsync(isReceivable: false, asOf ?? DateTime.UtcNow.Date, ct);

    private async Task<AgingReportDto> BuildAgingAsync(bool isReceivable, DateTime asOf, CancellationToken ct)
    {
        asOf = asOf.Date;
        var rows = new Dictionary<Guid, AgingBucketDto>();

        if (isReceivable)
        {
            var invoices = await _db.SaleInvoices.Include(x => x.Party)
                .Where(x => x.Status == DocumentStatus.Posted && x.Date <= asOf)
                .ToListAsync(ct);
            foreach (var inv in invoices)
                Accumulate(rows, inv.PartyId, inv.Party?.Code ?? "", inv.Party?.Name ?? "", inv.Date, inv.GrandTotal, asOf);
        }
        else
        {
            var invoices = await _db.PurchaseInvoices.Include(x => x.Party)
                .Where(x => x.Status == DocumentStatus.Posted && x.Date <= asOf)
                .ToListAsync(ct);
            foreach (var inv in invoices)
                Accumulate(rows, inv.PartyId, inv.Party?.Code ?? "", inv.Party?.Name ?? "", inv.Date, inv.GrandTotal, asOf);
        }

        var list = rows.Values.OrderByDescending(x => x.Total).ToList();
        return new AgingReportDto(isReceivable ? "AR" : "AP", asOf, list.Sum(x => x.Total), list);
    }

    private static void Accumulate(
        Dictionary<Guid, AgingBucketDto> rows,
        Guid partyId, string code, string name,
        DateTime invoiceDate, decimal amount, DateTime asOf)
    {
        if (!rows.TryGetValue(partyId, out var row))
        {
            row = new AgingBucketDto(code, name, 0, 0, 0, 0, 0, 0);
            rows[partyId] = row;
        }

        var days = (asOf - invoiceDate.Date).Days;
        decimal c = row.Current, d1 = row.Days1To30, d2 = row.Days31To60, d3 = row.Days61To90, d4 = row.Days90Plus;
        if (days <= 0) c += amount;
        else if (days <= 30) d1 += amount;
        else if (days <= 60) d2 += amount;
        else if (days <= 90) d3 += amount;
        else d4 += amount;

        rows[partyId] = row with
        {
            Current = c,
            Days1To30 = d1,
            Days31To60 = d2,
            Days61To90 = d3,
            Days90Plus = d4,
            Total = c + d1 + d2 + d3 + d4
        };
    }
}
