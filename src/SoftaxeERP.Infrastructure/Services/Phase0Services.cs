using Microsoft.EntityFrameworkCore;
using SoftaxeERP.Application.DTOs;
using SoftaxeERP.Application.Interfaces;
using SoftaxeERP.Domain.Entities;
using SoftaxeERP.Domain.Enums;
using SoftaxeERP.Infrastructure.Persistence;

namespace SoftaxeERP.Infrastructure.Services;

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
    public DashboardService(AppDbContext db) => _db = db;

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var payables = await _db.PayableBalances.SumAsync(x => (decimal?)x.Balance, ct) ?? 0;
        var purchases = await _db.PurchaseInvoices
            .Where(x => x.InvoiceDate == DateTime.UtcNow.Date && x.Status == DocumentStatus.Posted)
            .SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0;
        return new DashboardSummaryDto(0, purchases, 0, payables, Array.Empty<VoucherListDto>());
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
                x.SupplierProfile != null ? x.SupplierProfile.CreditLimit : 0, 0, x.IsActive))
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
        await _db.SaveChangesAsync(ct);
        return new PartyDto(p.Id, p.Code, p.Name, p.PartyType, p.Phone, p.Email, p.City, p.Address, dto.CreditLimit, 0, true);
    }

    public async Task<PartyDto?> UpdateAsync(Guid id, PartyUpsertDto dto, CancellationToken ct = default)
    {
        var p = await _db.Parties.Include(x => x.SupplierProfile).FirstOrDefaultAsync(x => x.Id == id, ct);
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
            x.ReorderLevel, x.IsActive, x.IsRawMaterial, x.IsFinishedGood)).ToListAsync(ct);
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
            ReorderLevel = dto.ReorderLevel, IsRawMaterial = dto.IsRawMaterial, IsFinishedGood = dto.IsFinishedGood
        };
        _db.Products.Add(p);
        await _db.SaveChangesAsync(ct);
        return new ProductDto(p.Id, p.Code, p.Name, cat?.Name, uom.Code, p.SalePrice, p.PurchasePrice, 0, p.ReorderLevel, true, p.IsRawMaterial, p.IsFinishedGood);
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
            p.SalePrice, p.PurchasePrice, p.Stocks.Sum(s => s.Quantity), p.ReorderLevel, p.IsActive, p.IsRawMaterial, p.IsFinishedGood);
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

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _db;
    public InventoryService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<StockDto>> GetStockAsync(CancellationToken ct = default)
    {
        return await _db.Products.Include(x => x.UnitOfMeasure).Include(x => x.Stocks)
            .Select(x => new StockDto(
                x.Id, x.Code, x.Name, x.UnitOfMeasure != null ? x.UnitOfMeasure.Code : "BAG",
                x.Stocks.Sum(s => s.Quantity), x.ReorderLevel,
                x.Stocks.Sum(s => s.Quantity) <= x.ReorderLevel))
            .ToListAsync(ct);
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
    public PurchaseService(AppDbContext db) => _db = db;

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
                x.Lines.Select(l => new InvoiceLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList()))
            .ToListAsync(ct);

    public async Task<PurchaseOrderDto> CreateOrderAsync(PurchaseOrderCreateDto dto, CancellationToken ct = default)
    {
        var (company, branch, fy) = await ScopeAsync(ct);
        var dt = await DocTypeAsync("PO", ct);
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var no = await DocumentNumbering.NextAsync(_db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "PO", fy.StartDate.Year, ct);
        var sub = dto.Lines.Sum(l => l.Qty * l.Rate);
        var po = new PurchaseOrder
        {
            OrderNo = no, OrderDate = dto.Date.Date, PartyId = dto.PartyId, WarehouseId = dto.WarehouseId,
            Status = dto.Post ? DocumentStatus.Posted : DocumentStatus.Draft,
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
        return (await GetOrdersAsync(ct)).First(x => x.Id == po.Id);
    }

    public async Task<IReadOnlyList<GoodsReceiptDto>> GetReceiptsAsync(CancellationToken ct = default) =>
        await _db.GoodsReceipts.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(x => x.ReceiptDate)
            .Select(x => new GoodsReceiptDto(x.Id, x.ReceiptNo, x.ReceiptDate, x.PartyId, x.Party!.Name, x.WarehouseId,
                x.PurchaseOrderId, x.Status, x.Remarks,
                x.Lines.Select(l => new InvoiceLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList()))
            .ToListAsync(ct);

    public async Task<GoodsReceiptDto> CreateReceiptAsync(GoodsReceiptCreateDto dto, CancellationToken ct = default)
    {
        var (company, branch, fy) = await ScopeAsync(ct);
        var dt = await DocTypeAsync("GRN", ct);
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var no = await DocumentNumbering.NextAsync(_db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "GRN", fy.StartDate.Year, ct);
        var grn = new GoodsReceipt
        {
            ReceiptNo = no, ReceiptDate = dto.Date.Date, PartyId = dto.PartyId, WarehouseId = dto.WarehouseId,
            PurchaseOrderId = dto.PurchaseOrderId,
            Status = dto.Post ? DocumentStatus.Posted : DocumentStatus.Draft,
            Remarks = dto.Remarks
        };
        var lineNo = 1;
        foreach (var l in dto.Lines)
        {
            grn.Lines.Add(new GoodsReceiptLine
            {
                LineNo = lineNo++, ProductId = l.ProductId, Qty = l.Qty, Rate = l.Rate, Amount = l.Qty * l.Rate
            });
        }
        _db.GoodsReceipts.Add(grn);

        // GRN owns stock increase
        if (dto.Post)
        {
            foreach (var l in dto.Lines)
            {
                var stock = await _db.WarehouseStocks
                    .FirstOrDefaultAsync(x => x.ProductId == l.ProductId && x.WarehouseId == dto.WarehouseId, ct);
                if (stock is null)
                {
                    stock = new WarehouseStock { ProductId = l.ProductId, WarehouseId = dto.WarehouseId, Quantity = 0 };
                    _db.WarehouseStocks.Add(stock);
                }
                stock.Quantity += l.Qty;

                if (dto.PurchaseOrderId is Guid poId)
                {
                    var poLine = await _db.PurchaseOrderLines
                        .FirstOrDefaultAsync(x => x.PurchaseOrderId == poId && x.ProductId == l.ProductId, ct);
                    if (poLine is not null) poLine.ReceivedQty += l.Qty;
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (await GetReceiptsAsync(ct)).First(x => x.Id == grn.Id);
    }

    public async Task<IReadOnlyList<PurchaseInvoiceDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.PurchaseInvoices.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(x => x.InvoiceDate)
            .Select(x => new PurchaseInvoiceDto(x.Id, x.InvoiceNo, x.InvoiceDate, x.PartyId, x.Party!.Name, x.Status,
                x.SubTotal, x.Discount, x.Tax, x.GrandTotal, x.Remarks,
                x.Lines.Select(l => new InvoiceLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList()))
            .ToListAsync(ct);

    public async Task<PurchaseInvoiceDto> CreateAsync(PurchaseInvoiceCreateDto dto, CancellationToken ct = default)
    {
        var (company, branch, fy) = await ScopeAsync(ct);
        var dt = await DocTypeAsync("PI", ct);
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var no = await DocumentNumbering.NextAsync(_db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "PI", fy.StartDate.Year, ct);
        var sub = dto.Lines.Sum(l => l.Qty * l.Rate);
        var grand = sub - dto.Discount + dto.Tax;
        var pi = new PurchaseInvoice
        {
            InvoiceNo = no, InvoiceDate = dto.Date.Date, PartyId = dto.PartyId,
            GoodsReceiptId = dto.GoodsReceiptId, PurchaseOrderId = dto.PurchaseOrderId,
            Status = dto.Post ? DocumentStatus.Posted : DocumentStatus.Draft,
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

        // PI owns AP only — NEVER stock
        if (dto.Post)
        {
            var bal = await _db.PayableBalances.FirstOrDefaultAsync(x => x.PartyId == dto.PartyId, ct);
            if (bal is null)
            {
                bal = new PayableBalance { PartyId = dto.PartyId, Balance = 0 };
                _db.PayableBalances.Add(bal);
            }
            bal.Balance += grand;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (await GetAllAsync(ct)).First(x => x.Id == pi.Id);
    }

    public async Task<PayableBalanceDto?> GetPayableAsync(Guid partyId, CancellationToken ct = default)
    {
        var bal = await _db.PayableBalances.Include(x => x.Party)
            .FirstOrDefaultAsync(x => x.PartyId == partyId, ct);
        return bal is null ? null : new PayableBalanceDto(bal.PartyId, bal.Party?.Name, bal.Balance);
    }
}
