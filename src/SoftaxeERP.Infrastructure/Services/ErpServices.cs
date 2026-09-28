using Microsoft.EntityFrameworkCore;
using SoftaxeERP.Application.DTOs;
using SoftaxeERP.Application.Interfaces;
using SoftaxeERP.Domain.Entities;
using SoftaxeERP.Domain.Enums;
using SoftaxeERP.Infrastructure.Persistence;

namespace SoftaxeERP.Infrastructure.Services;

/// <summary>Phase 0 document number allocator. The sequence row is locked for the transaction.</summary>
public sealed class NumberService
{
    private readonly AppDbContext _db;
    public NumberService(AppDbContext db) => _db = db;

    public async Task<string> NextNumberAsync(Guid companyId, Guid? branchId, Guid documentTypeId,
        Guid financialYearId, string prefix, CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var sequence = await _db.DocumentNumberSequences
            .FromSqlInterpolated($"""
                SELECT * FROM [documents].[DocumentNumberSequences] WITH (UPDLOCK, HOLDLOCK)
                WHERE [CompanyId] = {companyId} AND (([BranchId] = {branchId}) OR ([BranchId] IS NULL AND {branchId} IS NULL))
                  AND [DocumentTypeId] = {documentTypeId} AND [FinancialYearId] = {financialYearId}
                """)
            .FirstOrDefaultAsync(ct);
        if (sequence is null)
        {
            sequence = new DocumentNumberSequence
            {
                CompanyId = companyId, BranchId = branchId, DocumentTypeId = documentTypeId,
                FinancialYearId = financialYearId, Prefix = prefix, CurrentNumber = 0
            };
            _db.DocumentNumberSequences.Add(sequence);
        }
        sequence.CurrentNumber++;
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return $"{prefix}-{sequence.CurrentNumber.ToString().PadLeft(sequence.Padding, '0')}";
    }
}

public sealed class DashboardService : IDashboardService
{
    public Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default) =>
        Task.FromResult(new DashboardSummaryDto(0, 0, 0, 0, Array.Empty<VoucherListDto>()));
}

public sealed class AccountService : IAccountService
{
    private readonly AppDbContext _db;
    public AccountService(AppDbContext db) => _db = db;
    public async Task<IReadOnlyList<AccountDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Accounts.OrderBy(x => x.Code).Select(x => new AccountDto(x.Id, x.Code, x.Name, x.AccountType, x.ParentId, x.IsPostable, x.OpeningBalance, x.Level)).ToListAsync(ct);
    public async Task<AccountDto> CreateAsync(AccountUpsertDto dto, CancellationToken ct = default)
    {
        var level = dto.ParentId is null ? 1 : (await _db.Accounts.FirstAsync(x => x.Id == dto.ParentId, ct)).Level + 1;
        var entity = new Account { Code = dto.Code.Trim(), Name = dto.Name.Trim(), AccountType = dto.AccountType, ParentId = dto.ParentId, IsPostable = dto.IsPostable, OpeningBalance = dto.OpeningBalance, Level = level };
        _db.Accounts.Add(entity); await _db.SaveChangesAsync(ct);
        return new(entity.Id, entity.Code, entity.Name, entity.AccountType, entity.ParentId, entity.IsPostable, entity.OpeningBalance, entity.Level);
    }
}

public sealed class PartyService : IPartyService
{
    private readonly AppDbContext _db;
    public PartyService(AppDbContext db) => _db = db;
    public async Task<IReadOnlyList<PartyDto>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var query = _db.Parties.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Code.Contains(search) || x.Name.Contains(search));
        return await query.OrderBy(x => x.Name).Select(x => ToDto(x)).ToListAsync(ct);
    }
    public async Task<PartyDto> CreateAsync(PartyUpsertDto dto, CancellationToken ct = default)
    {
        var entity = Map(dto); _db.Parties.Add(entity);
        if (dto.PartyType is PartyType.Supplier or PartyType.Both) _db.SupplierProfiles.Add(new SupplierProfile { Party = entity, CreditLimit = dto.CreditLimit });
        await _db.SaveChangesAsync(ct); return ToDto(entity);
    }
    public async Task<PartyDto?> UpdateAsync(Guid id, PartyUpsertDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Parties.Include(x => x.SupplierProfile).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return null;
        entity.Code = dto.Code.Trim(); entity.Name = dto.Name.Trim(); entity.PartyType = dto.PartyType; entity.Phone = dto.Phone; entity.Email = dto.Email; entity.City = dto.City; entity.Address = dto.Address;
        if (dto.PartyType is PartyType.Supplier or PartyType.Both && entity.SupplierProfile is null) _db.SupplierProfiles.Add(new SupplierProfile { PartyId = entity.Id, CreditLimit = dto.CreditLimit });
        await _db.SaveChangesAsync(ct); return ToDto(entity);
    }
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) { var x = await _db.Parties.FindAsync([id], ct); if (x is null) return false; x.IsDeleted = true; await _db.SaveChangesAsync(ct); return true; }
    private static Party Map(PartyUpsertDto x) => new() { Code = x.Code.Trim(), Name = x.Name.Trim(), PartyType = x.PartyType, Phone = x.Phone, Email = x.Email, City = x.City, Address = x.Address };
    private static PartyDto ToDto(Party x) => new(x.Id, x.Code, x.Name, x.PartyType, x.Phone, x.Email, x.City, x.Address, x.SupplierProfile?.CreditLimit ?? 0, 0, x.IsActive);
}

public sealed class ProductService : IProductService
{
    private readonly AppDbContext _db;
    public ProductService(AppDbContext db) => _db = db;
    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var q = _db.Products.Include(x => x.Category).Include(x => x.UnitOfMeasure).Include(x => x.Stocks).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.Code.Contains(search) || x.Name.Contains(search));
        return (await q.OrderBy(x => x.Name).ToListAsync(ct)).Select(ToDto).ToList();
    }
    public async Task<ProductDto> CreateAsync(ProductUpsertDto dto, CancellationToken ct = default)
    {
        var category = await EnsureCategory(dto.Category, ct); var uom = await EnsureUom(dto.Unit, ct);
        var e = new Product { Code = dto.Code.Trim(), Name = dto.Name.Trim(), CategoryId = category?.Id, UnitOfMeasureId = uom.Id, SalePrice = dto.SalePrice, PurchasePrice = dto.PurchasePrice, ReorderLevel = dto.ReorderLevel, IsRawMaterial = dto.IsRawMaterial, IsFinishedGood = dto.IsFinishedGood };
        _db.Products.Add(e); await _db.SaveChangesAsync(ct); return ToDto(e);
    }
    public async Task<ProductDto?> UpdateAsync(Guid id, ProductUpsertDto dto, CancellationToken ct = default)
    {
        var e = await _db.Products.Include(x => x.Category).Include(x => x.UnitOfMeasure).Include(x => x.Stocks).FirstOrDefaultAsync(x => x.Id == id, ct); if (e is null) return null;
        e.Code = dto.Code.Trim(); e.Name = dto.Name.Trim(); e.SalePrice = dto.SalePrice; e.PurchasePrice = dto.PurchasePrice; e.ReorderLevel = dto.ReorderLevel; e.IsRawMaterial = dto.IsRawMaterial; e.IsFinishedGood = dto.IsFinishedGood;
        e.CategoryId = (await EnsureCategory(dto.Category, ct))?.Id; e.UnitOfMeasureId = (await EnsureUom(dto.Unit, ct)).Id;
        await _db.SaveChangesAsync(ct); return ToDto(e);
    }
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) { var e = await _db.Products.FindAsync([id], ct); if (e is null) return false; e.IsDeleted = true; await _db.SaveChangesAsync(ct); return true; }
    private async Task<ProductCategory?> EnsureCategory(string? name, CancellationToken ct) { if (string.IsNullOrWhiteSpace(name)) return null; var e = await _db.ProductCategories.FirstOrDefaultAsync(x => x.Name == name, ct); if (e is not null) return e; e = new() { Name = name.Trim() }; _db.ProductCategories.Add(e); await _db.SaveChangesAsync(ct); return e; }
    private async Task<UnitOfMeasure> EnsureUom(string name, CancellationToken ct) { var e = await _db.UnitsOfMeasure.FirstOrDefaultAsync(x => x.Code == name, ct); if (e is not null) return e; e = new() { Code = name.Trim(), Name = name.Trim() }; _db.UnitsOfMeasure.Add(e); await _db.SaveChangesAsync(ct); return e; }
    private static ProductDto ToDto(Product x) => new(x.Id, x.Code, x.Name, x.Category?.Name, x.UnitOfMeasure?.Code ?? "", x.SalePrice, x.PurchasePrice, x.Stocks.Sum(s => s.Quantity), x.ReorderLevel, x.IsActive, x.IsRawMaterial, x.IsFinishedGood);
}

public sealed class InventoryService : IInventoryService
{
    private readonly AppDbContext _db;
    public InventoryService(AppDbContext db) => _db = db;
    public async Task<IReadOnlyList<StockDto>> GetStockAsync(CancellationToken ct = default) =>
        (await _db.Products.Include(x => x.UnitOfMeasure).Include(x => x.Stocks).OrderBy(x => x.Name).ToListAsync(ct))
        .Select(x => new StockDto(x.Id, x.Code, x.Name, x.UnitOfMeasure?.Code ?? "", x.Stocks.Sum(s => s.Quantity), x.ReorderLevel, x.Stocks.Sum(s => s.Quantity) <= x.ReorderLevel)).ToList();
}

public class NumberService
{
    private readonly AppDbContext _db;
    public NumberService(AppDbContext db) => _db = db;

    public async Task<string> NextAsync(string documentType, string prefix, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var series = await _db.NumberSeries.FirstOrDefaultAsync(x => x.DocumentType == documentType && x.Year == year, ct);
        if (series is null)
        {
            series = new NumberSeries { DocumentType = documentType, Prefix = prefix, NextNumber = 1, Year = year };
            _db.NumberSeries.Add(series);
        }
        var no = $"{series.Prefix}-{year}-{series.NextNumber:D5}";
        series.NextNumber++;
        return no;
    }
}

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    public DashboardService(AppDbContext db) => _db = db;

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var salesToday = await _db.SaleInvoices.Where(x => x.Date == today && x.Status == DocumentStatus.Posted).SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0;
        var purchasesToday = await _db.PurchaseInvoices.Where(x => x.Date == today && x.Status == DocumentStatus.Posted).SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0;

        // Simplified AR/AP from posted sales/purchases (MVP)
        var receivables = await _db.SaleInvoices.Where(x => x.Status == DocumentStatus.Posted).SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0;
        var payables = await _db.PurchaseInvoices.Where(x => x.Status == DocumentStatus.Posted).SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0;

        var recent = await _db.Vouchers.OrderByDescending(x => x.Date).ThenByDescending(x => x.CreatedAt).Take(8)
            .Select(x => new VoucherListDto(x.Id, x.VoucherNo, x.VoucherType.ToString(), x.Date, x.Narration, x.TotalDebit, x.TotalCredit, x.Status.ToString()))
            .ToListAsync(ct);

        return new DashboardSummaryDto(salesToday, purchasesToday, receivables, payables, recent);
    }
}

public class AccountService : IAccountService
{
    private readonly AppDbContext _db;
    public AccountService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AccountDto>> GetAllAsync(CancellationToken ct = default)
    {
        var accounts = await _db.Accounts.OrderBy(x => x.Code).ToListAsync(ct);
        var posted = await _db.VoucherLines
            .Where(l => l.Voucher!.Status == DocumentStatus.Posted)
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Bal = g.Sum(x => x.Debit - x.Credit) })
            .ToListAsync(ct);
        var map = posted.ToDictionary(x => x.AccountId, x => x.Bal);

        return accounts.Select(a => new AccountDto(
            a.Id, a.Code, a.Name, a.AccountType, a.ParentId, a.IsPostable,
            a.OpeningBalance + map.GetValueOrDefault(a.Id), a.Level)).ToList();
    }

    public async Task<AccountDto> CreateAsync(AccountUpsertDto dto, CancellationToken ct = default)
    {
        var level = 1;
        if (dto.ParentId.HasValue)
        {
            var parent = await _db.Accounts.FirstAsync(x => x.Id == dto.ParentId, ct);
            level = parent.Level + 1;
        }
        var entity = new Account
        {
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            AccountType = dto.AccountType,
            ParentId = dto.ParentId,
            IsPostable = dto.IsPostable,
            OpeningBalance = dto.OpeningBalance,
            Level = level
        };
        _db.Accounts.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new AccountDto(entity.Id, entity.Code, entity.Name, entity.AccountType, entity.ParentId, entity.IsPostable, entity.OpeningBalance, entity.Level);
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
        {
            var s = search.Trim();
            q = q.Where(x => x.Name.Contains(s) || x.Code.Contains(s) || (x.City != null && x.City.Contains(s)));
        }
        return await q.OrderBy(x => x.Name)
            .Select(x => new PartyDto(x.Id, x.Code, x.Name, x.PartyType, x.Phone, x.Email, x.City, x.Address, x.CreditLimit, x.OpeningBalance, x.IsActive))
            .ToListAsync(ct);
    }

    public async Task<PartyDto> CreateAsync(PartyUpsertDto dto, CancellationToken ct = default)
    {
        var e = Map(dto);
        _db.Parties.Add(e);
        await _db.SaveChangesAsync(ct);
        return ToDto(e);
    }

    public async Task<PartyDto?> UpdateAsync(Guid id, PartyUpsertDto dto, CancellationToken ct = default)
    {
        var e = await _db.Parties.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return null;
        e.Code = dto.Code.Trim();
        e.Name = dto.Name.Trim();
        e.PartyType = dto.PartyType;
        e.Phone = dto.Phone;
        e.Email = dto.Email;
        e.City = dto.City;
        e.Address = dto.Address;
        e.CreditLimit = dto.CreditLimit;
        e.OpeningBalance = dto.OpeningBalance;
        await _db.SaveChangesAsync(ct);
        return ToDto(e);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var e = await _db.Parties.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return false;
        e.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static Party Map(PartyUpsertDto dto) => new()
    {
        Code = dto.Code.Trim(), Name = dto.Name.Trim(), PartyType = dto.PartyType,
        Phone = dto.Phone, Email = dto.Email, City = dto.City, Address = dto.Address,
        CreditLimit = dto.CreditLimit, OpeningBalance = dto.OpeningBalance
    };

    private static PartyDto ToDto(Party x) =>
        new(x.Id, x.Code, x.Name, x.PartyType, x.Phone, x.Email, x.City, x.Address, x.CreditLimit, x.OpeningBalance, x.IsActive);
}

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    public ProductService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var q = _db.Products.Include(x => x.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x => x.Name.Contains(s) || x.Code.Contains(s));
        }
        return await q.OrderBy(x => x.Name)
            .Select(x => new ProductDto(x.Id, x.Code, x.Name, x.Category != null ? x.Category.Name : null, x.Unit,
                x.SalePrice, x.PurchasePrice, x.StockQty, x.ReorderLevel, x.IsActive, x.IsRawMaterial, x.IsFinishedGood))
            .ToListAsync(ct);
    }

    public async Task<ProductDto> CreateAsync(ProductUpsertDto dto, CancellationToken ct = default)
    {
        Guid? catId = null;
        if (!string.IsNullOrWhiteSpace(dto.Category))
        {
            var cat = await _db.ProductCategories.FirstOrDefaultAsync(x => x.Name == dto.Category, ct);
            if (cat is null)
            {
                cat = new ProductCategory { Name = dto.Category.Trim() };
                _db.ProductCategories.Add(cat);
                await _db.SaveChangesAsync(ct);
            }
            catId = cat.Id;
        }

        var e = new Product
        {
            Code = dto.Code.Trim(), Name = dto.Name.Trim(), CategoryId = catId, Unit = dto.Unit,
            SalePrice = dto.SalePrice, PurchasePrice = dto.PurchasePrice, StockQty = dto.StockQty,
            ReorderLevel = dto.ReorderLevel, IsRawMaterial = dto.IsRawMaterial, IsFinishedGood = dto.IsFinishedGood
        };
        _db.Products.Add(e);
        await _db.SaveChangesAsync(ct);
        return new ProductDto(e.Id, e.Code, e.Name, dto.Category, e.Unit, e.SalePrice, e.PurchasePrice, e.StockQty, e.ReorderLevel, e.IsActive, e.IsRawMaterial, e.IsFinishedGood);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, ProductUpsertDto dto, CancellationToken ct = default)
    {
        var e = await _db.Products.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return null;
        e.Code = dto.Code.Trim();
        e.Name = dto.Name.Trim();
        e.Unit = dto.Unit;
        e.SalePrice = dto.SalePrice;
        e.PurchasePrice = dto.PurchasePrice;
        e.StockQty = dto.StockQty;
        e.ReorderLevel = dto.ReorderLevel;
        e.IsRawMaterial = dto.IsRawMaterial;
        e.IsFinishedGood = dto.IsFinishedGood;
        await _db.SaveChangesAsync(ct);
        return new ProductDto(e.Id, e.Code, e.Name, e.Category?.Name, e.Unit, e.SalePrice, e.PurchasePrice, e.StockQty, e.ReorderLevel, e.IsActive, e.IsRawMaterial, e.IsFinishedGood);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var e = await _db.Products.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return false;
        e.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class VoucherService : IVoucherService
{
    private readonly AppDbContext _db;
    private readonly NumberService _numbers;
    public VoucherService(AppDbContext db, NumberService numbers) { _db = db; _numbers = numbers; }

    public async Task<IReadOnlyList<VoucherListDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Vouchers.OrderByDescending(x => x.Date).ThenByDescending(x => x.CreatedAt)
            .Select(x => new VoucherListDto(x.Id, x.VoucherNo, x.VoucherType.ToString(), x.Date, x.Narration, x.TotalDebit, x.TotalCredit, x.Status.ToString()))
            .ToListAsync(ct);

    public async Task<VoucherDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var x = await _db.Vouchers.Include(v => v.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
        if (x is null) return null;
        return new VoucherDto(x.Id, x.VoucherNo, x.VoucherType, x.Date, x.Narration, x.Status, x.TotalDebit, x.TotalCredit,
            x.Lines.OrderBy(l => l.LineNo).Select(l => new VoucherLineDto(l.Id, l.AccountId, l.Account?.Name, l.Debit, l.Credit, l.Remarks)).ToList());
    }

    public async Task<VoucherDto> CreateAsync(VoucherCreateDto dto, CancellationToken ct = default)
    {
        var debit = dto.Lines.Sum(l => l.Debit);
        var credit = dto.Lines.Sum(l => l.Credit);
        if (debit != credit) throw new InvalidOperationException($"Voucher not balanced. Debit={debit}, Credit={credit}");
        if (dto.Lines.Count < 2) throw new InvalidOperationException("At least two lines required.");

        var prefix = dto.VoucherType.ToString();
        prefix = dto.VoucherType switch
        {
            VoucherType.CpFreight => "CP-FREIGHT",
            VoucherType.CpAd => "CP-AD",
            VoucherType.BpAd => "BP-AD",
            VoucherType.CpSl => "CP-SL",
            VoucherType.BpSl => "BP-SL",
            VoucherType.DoSales => "DO-SALE",
            VoucherType.JvRm => "JV-RM",
            VoucherType.JvAdj => "JV-ADJ",
            VoucherType.StkTrf => "STK-TRF",
            VoucherType.Sr => "SR",
            VoucherType.Pr => "PR",
            VoucherType.CpPaid => "CP-PAID",
            _ => prefix
        };
        var no = await _numbers.NextAsync(prefix, prefix, ct);
        var voucher = new Voucher
        {
            VoucherNo = no,
            VoucherType = dto.VoucherType,
            Date = dto.Date.Date,
            Narration = dto.Narration,
            Status = dto.Post ? DocumentStatus.Posted : DocumentStatus.Draft,
            TotalDebit = debit,
            TotalCredit = credit
        };
        var i = 1;
        foreach (var line in dto.Lines)
        {
            voucher.Lines.Add(new VoucherLine
            {
                AccountId = line.AccountId,
                Debit = line.Debit,
                Credit = line.Credit,
                Remarks = line.Remarks,
                LineNo = i++
            });
        }
        _db.Vouchers.Add(voucher);
        await _db.SaveChangesAsync(ct);
        return (await GetByIdAsync(voucher.Id, ct))!;
    }
}

public class SaleService : ISaleService
{
    private readonly AppDbContext _db;
    private readonly NumberService _numbers;
    private readonly IVoucherService _vouchers;
    public SaleService(AppDbContext db, NumberService numbers, IVoucherService vouchers)
    { _db = db; _numbers = numbers; _vouchers = vouchers; }

    public async Task<IReadOnlyList<SaleInvoiceDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.SaleInvoices.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(x => x.Date)
            .Select(x => new SaleInvoiceDto(x.Id, x.InvoiceNo, x.Date, x.PartyId, x.Party!.Name, x.Status, x.SubTotal, x.Discount, x.Tax, x.GrandTotal, x.Remarks,
                x.Lines.OrderBy(l => l.LineNo).Select(l => new InvoiceLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList()))
            .ToListAsync(ct);

    public async Task<SaleInvoiceDto> CreateAsync(SaleInvoiceCreateDto dto, CancellationToken ct = default)
    {
        if (dto.Lines.Count == 0) throw new InvalidOperationException("Add at least one line.");
        var party = await _db.Parties.FirstAsync(x => x.Id == dto.PartyId, ct);
        var no = await _numbers.NextAsync("SI", "SI", ct);
        var lines = new List<SaleInvoiceLine>();
        decimal sub = 0;
        var i = 1;
        foreach (var l in dto.Lines)
        {
            var product = await _db.Products.FirstAsync(x => x.Id == l.ProductId, ct);
            var amount = Math.Round(l.Qty * l.Rate, 2);
            sub += amount;
            lines.Add(new SaleInvoiceLine { ProductId = product.Id, Qty = l.Qty, Rate = l.Rate, Amount = amount, LineNo = i++ });
            if (dto.Post)
            {
                if (product.StockQty < l.Qty) throw new InvalidOperationException($"Insufficient stock for {product.Name}");
                product.StockQty -= l.Qty;
                _db.StockMovements.Add(new StockMovement
                {
                    ProductId = product.Id, Date = dto.Date.Date, QtyOut = l.Qty,
                    ReferenceType = "Sale", Remarks = no
                });
            }
        }
        var inv = new SaleInvoice
        {
            InvoiceNo = no, Date = dto.Date.Date, PartyId = party.Id,
            SubTotal = sub, Discount = dto.Discount, Tax = dto.Tax,
            GrandTotal = sub - dto.Discount + dto.Tax,
            Remarks = dto.Remarks,
            Status = dto.Post ? DocumentStatus.Posted : DocumentStatus.Draft,
            Lines = lines
        };
        _db.SaleInvoices.Add(inv);
        await _db.SaveChangesAsync(ct);

        if (dto.Post)
        {
            var ar = await EnsureSystemAccount(_db, "1100", "Accounts Receivable", AccountType.Asset, ct);
            var sales = await EnsureSystemAccount(_db, "4100", "Sales Revenue", AccountType.Revenue, ct);
            var voucher = await _vouchers.CreateAsync(new VoucherCreateDto(VoucherType.JV, dto.Date, $"Sale {no} - {party.Name}", true, new[]
            {
                new VoucherLineDto(null, ar.Id, null, inv.GrandTotal, 0, party.Name),
                new VoucherLineDto(null, sales.Id, null, 0, inv.GrandTotal, no)
            }), ct);
            inv.PostedVoucherId = voucher.Id;
            await _db.SaveChangesAsync(ct);
        }

        return (await GetAllAsync(ct)).First(x => x.Id == inv.Id);
    }

    internal static async Task<Account> EnsureSystemAccount(AppDbContext db, string code, string name, AccountType type, CancellationToken ct)
    {
        var acc = await db.Accounts.FirstOrDefaultAsync(x => x.Code == code, ct);
        if (acc != null) return acc;
        acc = new Account { Code = code, Name = name, AccountType = type, IsPostable = true, IsSystem = true, Level = 1 };
        db.Accounts.Add(acc);
        await db.SaveChangesAsync(ct);
        return acc;
    }
}

public class PurchaseService : IPurchaseService
{
    private readonly AppDbContext _db;
    private readonly NumberService _numbers;
    private readonly IVoucherService _vouchers;
    public PurchaseService(AppDbContext db, NumberService numbers, IVoucherService vouchers)
    { _db = db; _numbers = numbers; _vouchers = vouchers; }

    public async Task<IReadOnlyList<PurchaseInvoiceDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.PurchaseInvoices.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(x => x.Date)
            .Select(x => new PurchaseInvoiceDto(x.Id, x.InvoiceNo, x.Date, x.PartyId, x.Party!.Name, x.Status, x.SubTotal, x.Discount, x.Tax, x.GrandTotal, x.Remarks,
                x.Lines.OrderBy(l => l.LineNo).Select(l => new InvoiceLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList(), null))
            .ToListAsync(ct);

    public async Task<PurchaseInvoiceDto> CreateAsync(PurchaseInvoiceCreateDto dto, CancellationToken ct = default)
    {
        if (dto.Lines.Count == 0) throw new InvalidOperationException("Add at least one line.");
        var party = await _db.Parties.FirstAsync(x => x.Id == dto.PartyId, ct);
        var no = await _numbers.NextAsync("PI", "PI", ct);
        var lines = new List<PurchaseInvoiceLine>();
        decimal sub = 0;
        var i = 1;
        foreach (var l in dto.Lines)
        {
            var product = await _db.Products.FirstAsync(x => x.Id == l.ProductId, ct);
            var amount = Math.Round(l.Qty * l.Rate, 2);
            sub += amount;
            lines.Add(new PurchaseInvoiceLine { ProductId = product.Id, Qty = l.Qty, Rate = l.Rate, Amount = amount, LineNo = i++ });
            if (dto.Post)
            {
                product.StockQty += l.Qty;
                _db.StockMovements.Add(new StockMovement
                {
                    ProductId = product.Id, Date = dto.Date.Date, QtyIn = l.Qty,
                    ReferenceType = "Purchase", Remarks = no
                });
            }
        }
        var inv = new PurchaseInvoice
        {
            InvoiceNo = no, Date = dto.Date.Date, PartyId = party.Id,
            SubTotal = sub, Discount = dto.Discount, Tax = dto.Tax,
            GrandTotal = sub - dto.Discount + dto.Tax,
            Remarks = dto.Remarks,
            Status = dto.Post ? DocumentStatus.Posted : DocumentStatus.Draft,
            Lines = lines
        };
        _db.PurchaseInvoices.Add(inv);
        await _db.SaveChangesAsync(ct);

        if (dto.Post)
        {
            var stock = await SaleService.EnsureSystemAccount(_db, "1200", "Inventory", AccountType.Asset, ct);
            var ap = await SaleService.EnsureSystemAccount(_db, "2100", "Accounts Payable", AccountType.Liability, ct);
            var voucher = await _vouchers.CreateAsync(new VoucherCreateDto(VoucherType.JV, dto.Date, $"Purchase {no} - {party.Name}", true, new[]
            {
                new VoucherLineDto(null, stock.Id, null, inv.GrandTotal, 0, no),
                new VoucherLineDto(null, ap.Id, null, 0, inv.GrandTotal, party.Name)
            }), ct);
            inv.PostedVoucherId = voucher.Id;
            await _db.SaveChangesAsync(ct);
        }

        return (await GetAllAsync(ct)).First(x => x.Id == inv.Id);
    }
}

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _db;
    public InventoryService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<StockDto>> GetStockAsync(CancellationToken ct = default) =>
        await _db.Products.Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new StockDto(x.Id, x.Code, x.Name, x.Unit, x.StockQty, x.ReorderLevel, x.StockQty <= x.ReorderLevel))
            .ToListAsync(ct);
}

public class EmployeeService : IEmployeeService
{
    private readonly AppDbContext _db;
    public EmployeeService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<EmployeeDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Employees.OrderBy(x => x.Name)
            .Select(x => new EmployeeDto(x.Id, x.Code, x.Name, x.Designation, x.Department, x.JoinDate, x.BasicSalary, x.Phone, x.IsActive))
            .ToListAsync(ct);

    public async Task<EmployeeDto> CreateAsync(EmployeeUpsertDto dto, CancellationToken ct = default)
    {
        var e = new Employee
        {
            Code = dto.Code.Trim(), Name = dto.Name.Trim(), Designation = dto.Designation,
            Department = dto.Department, JoinDate = dto.JoinDate, BasicSalary = dto.BasicSalary,
            Phone = dto.Phone, Email = dto.Email, Address = dto.Address
        };
        _db.Employees.Add(e);
        await _db.SaveChangesAsync(ct);
        return new EmployeeDto(e.Id, e.Code, e.Name, e.Designation, e.Department, e.JoinDate, e.BasicSalary, e.Phone, e.IsActive);
    }
}

public class CompanyService : ICompanyService
{
    private readonly AppDbContext _db;
    public CompanyService(AppDbContext db) => _db = db;

    public async Task<CompanyDto?> GetAsync(CancellationToken ct = default)
    {
        var c = await _db.Companies.FirstOrDefaultAsync(ct);
        return c is null ? null : new CompanyDto(c.Id, c.Name, c.ShortName, c.Address, c.City, c.Country, c.Phone, c.Email, c.Currency, c.Ntn, c.Industry,
            c.DefaultSaleTaxPercent, c.DefaultPurchaseTaxPercent, c.SyncToFbr, c.TaxModule);
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
        c.SyncToFbr = dto.SyncToFbr;
        c.TaxModule = dto.TaxModule;
        await _db.SaveChangesAsync(ct);
        return await GetAsync(ct);
    }
}
