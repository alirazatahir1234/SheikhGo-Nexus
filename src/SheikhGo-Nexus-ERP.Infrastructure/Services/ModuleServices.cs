using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Entities;
using SheikhGoNexusERP.Domain.Enums;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

public class ProductionService : IProductionService
{
    private readonly AppDbContext _db;
    private readonly NumberService _numbers;
    public ProductionService(AppDbContext db, NumberService numbers) { _db = db; _numbers = numbers; }

    public async Task<IReadOnlyList<BomDto>> GetBomsAsync(CancellationToken ct = default) =>
        await _db.Boms.Include(b => b.FinishedProduct).Include(b => b.Lines).ThenInclude(l => l.RawMaterial)
            .OrderBy(b => b.Code)
            .Select(b => new BomDto(b.Id, b.Code, b.Name, b.FinishedProductId, b.FinishedProduct!.Name, b.OutputQty, b.IsActive,
                b.Lines.Select(l => new BomLineDto(l.Id, l.RawMaterialId, l.RawMaterial!.Name, l.Qty)).ToList()))
            .ToListAsync(ct);

    public async Task<BomDto> CreateBomAsync(BomUpsertDto dto, CancellationToken ct = default)
    {
        if (dto.Lines.Count == 0) throw new InvalidOperationException("BOM needs at least one raw material line.");
        _ = await _db.Products.FirstAsync(p => p.Id == dto.FinishedProductId, ct);
        var bom = new Bom
        {
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            FinishedProductId = dto.FinishedProductId,
            OutputQty = dto.OutputQty <= 0 ? 1 : dto.OutputQty,
            IsActive = true
        };
        foreach (var l in dto.Lines)
        {
            _ = await _db.Products.FirstAsync(p => p.Id == l.RawMaterialId, ct);
            bom.Lines.Add(new BomLine { RawMaterialId = l.RawMaterialId, Qty = l.Qty });
        }
        _db.Boms.Add(bom);
        await _db.SaveChangesAsync(ct);
        return (await GetBomsAsync(ct)).First(x => x.Id == bom.Id);
    }

    public async Task<IReadOnlyList<ProductionOrderDto>> GetOrdersAsync(CancellationToken ct = default) =>
        await _db.ProductionOrders.Include(o => o.Bom)
            .OrderByDescending(o => o.Date).ThenByDescending(o => o.CreatedAt)
            .Select(o => new ProductionOrderDto(o.Id, o.OrderNo, o.BomId, o.Bom!.Name, o.Date, o.BatchQty, o.OutputQty, o.Status, o.Remarks))
            .ToListAsync(ct);

    public async Task<ProductionOrderDto> CreateOrderAsync(ProductionCreateDto dto, CancellationToken ct = default)
    {
        var bom = await _db.Boms.Include(b => b.Lines).ThenInclude(l => l.RawMaterial)
            .Include(b => b.FinishedProduct)
            .FirstAsync(b => b.Id == dto.BomId && b.IsActive, ct);
        var batch = dto.BatchQty <= 0 ? 1 : dto.BatchQty;
        var output = bom.OutputQty * batch;
        var no = await _numbers.NextAsync("PR", "PR", ct);

        if (dto.Post)
        {
            foreach (var line in bom.Lines)
            {
                var need = line.Qty * batch;
                var rm = await _db.Products.FirstAsync(p => p.Id == line.RawMaterialId, ct);
                if (rm.StockQty < need)
                    throw new InvalidOperationException($"Insufficient {rm.Name}. Need {need}, have {rm.StockQty}.");
                rm.StockQty -= need;
                _db.StockMovements.Add(new StockMovement
                {
                    ProductId = rm.Id, Date = dto.Date.Date, QtyOut = need,
                    ReferenceType = "Production", Remarks = no
                });
            }
            var fg = await _db.Products.FirstAsync(p => p.Id == bom.FinishedProductId, ct);
            fg.StockQty += output;
            _db.StockMovements.Add(new StockMovement
            {
                ProductId = fg.Id, Date = dto.Date.Date, QtyIn = output,
                ReferenceType = "Production", Remarks = no
            });
        }

        var order = new ProductionOrder
        {
            OrderNo = no, BomId = bom.Id, Date = dto.Date.Date, BatchQty = batch,
            OutputQty = output, Remarks = dto.Remarks,
            Status = dto.Post ? DocumentStatus.Posted : DocumentStatus.Draft
        };
        _db.ProductionOrders.Add(order);
        await _db.SaveChangesAsync(ct);
        return new ProductionOrderDto(order.Id, order.OrderNo, order.BomId, bom.Name, order.Date, order.BatchQty, order.OutputQty, order.Status, order.Remarks);
    }
}

public class FarmService : IFarmService
{
    private readonly AppDbContext _db;
    public FarmService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<FarmFlockDto>> GetFlocksAsync(CancellationToken ct = default) =>
        await _db.FarmFlocks.OrderByDescending(f => f.PlacementDate)
            .Select(f => new FarmFlockDto(f.Id, f.Code, f.Name, f.BirdType, f.PlacementDate, f.PlacedQty, f.CurrentQty, f.MortalityQty, f.Shed, f.Status, f.FeedConsumedKg, f.TotalExpense))
            .ToListAsync(ct);

    public async Task<FarmFlockDto> CreateFlockAsync(FarmFlockCreateDto dto, CancellationToken ct = default)
    {
        var f = new FarmFlock
        {
            Code = dto.Code.Trim(), Name = dto.Name.Trim(), BirdType = dto.BirdType,
            PlacementDate = dto.PlacementDate.Date, PlacedQty = dto.PlacedQty, CurrentQty = dto.PlacedQty,
            Shed = dto.Shed, Remarks = dto.Remarks, Status = "Active"
        };
        _db.FarmFlocks.Add(f);
        await _db.SaveChangesAsync(ct);
        return new FarmFlockDto(f.Id, f.Code, f.Name, f.BirdType, f.PlacementDate, f.PlacedQty, f.CurrentQty, f.MortalityQty, f.Shed, f.Status, f.FeedConsumedKg, f.TotalExpense);
    }

    public async Task<FarmEventDto> AddEventAsync(Guid flockId, FarmEventCreateDto dto, CancellationToken ct = default)
    {
        var f = await _db.FarmFlocks.FirstAsync(x => x.Id == flockId, ct);
        if (f.Status != "Active") throw new InvalidOperationException("Flock is closed.");

        var ev = new FarmEvent
        {
            FarmFlockId = flockId, Date = dto.Date.Date, EventType = dto.EventType,
            Qty = dto.Qty, Amount = dto.Amount, ProductId = dto.ProductId, Remarks = dto.Remarks
        };
        _db.FarmEvents.Add(ev);

        switch (dto.EventType.ToLowerInvariant())
        {
            case "mortality":
                var dead = (int)Math.Round(dto.Qty);
                f.MortalityQty += dead;
                f.CurrentQty = Math.Max(0, f.CurrentQty - dead);
                break;
            case "feed":
                f.FeedConsumedKg += dto.Qty;
                f.TotalExpense += dto.Amount;
                if (dto.ProductId.HasValue)
                {
                    var p = await _db.Products.FirstAsync(x => x.Id == dto.ProductId.Value, ct);
                    if (p.StockQty < dto.Qty) throw new InvalidOperationException($"Insufficient feed stock for {p.Name}");
                    p.StockQty -= dto.Qty;
                    _db.StockMovements.Add(new StockMovement
                    {
                        ProductId = p.Id, Date = dto.Date.Date, QtyOut = dto.Qty,
                        ReferenceType = "Farm", Remarks = $"{f.Code} feed"
                    });
                }
                break;
            case "medicine":
            case "expense":
                f.TotalExpense += dto.Amount;
                break;
        }

        await _db.SaveChangesAsync(ct);
        return new FarmEventDto(ev.Id, ev.FarmFlockId, ev.Date, ev.EventType, ev.Qty, ev.Amount, ev.ProductId, ev.Remarks);
    }

    public async Task<IReadOnlyList<FarmEventDto>> GetEventsAsync(Guid flockId, CancellationToken ct = default) =>
        await _db.FarmEvents.Where(e => e.FarmFlockId == flockId).OrderByDescending(e => e.Date)
            .Select(e => new FarmEventDto(e.Id, e.FarmFlockId, e.Date, e.EventType, e.Qty, e.Amount, e.ProductId, e.Remarks))
            .ToListAsync(ct);

    public async Task<bool> CloseFlockAsync(Guid flockId, CancellationToken ct = default)
    {
        var f = await _db.FarmFlocks.FirstOrDefaultAsync(x => x.Id == flockId, ct);
        if (f is null) return false;
        f.Status = "Closed";
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class UserAdminService : IUserAdminService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    public UserAdminService(AppDbContext db, ITenantContext tenant) { _db = db; _tenant = tenant; }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken ct = default) =>
        await _db.Users.OrderBy(u => u.FullName)
            .Select(u => new UserDto(u.Id, u.Email, u.FullName, u.Role, u.IsPlatformAdmin))
            .ToListAsync(ct);

    public async Task<UserDto> CreateAsync(UserCreateDto dto, CancellationToken ct = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email && !u.IsDeleted, ct))
            throw new InvalidOperationException("Email already exists.");
        var role = string.IsNullOrWhiteSpace(dto.Role) ? "User" : dto.Role.Trim();
        var user = new AppUser
        {
            TenantId = _tenant.TenantId,
            Email = email,
            FullName = dto.FullName.Trim(),
            Role = role,
            IsPlatformAdmin = role.Equals("PlatformAdmin", StringComparison.OrdinalIgnoreCase),
            PasswordHash = AuthService.HashPassword(dto.Password)
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return new UserDto(user.Id, user.Email, user.FullName, user.Role, user.IsPlatformAdmin);
    }

    public async Task<UserDto?> UpdateAsync(Guid id, UserUpdateDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return null;
        user.FullName = dto.FullName.Trim();
        user.Role = dto.Role.Trim();
        user.IsActive = dto.IsActive;
        user.IsPlatformAdmin = dto.Role.Equals("PlatformAdmin", StringComparison.OrdinalIgnoreCase);
        await _db.SaveChangesAsync(ct);
        return new UserDto(user.Id, user.Email, user.FullName, user.Role, user.IsPlatformAdmin);
    }
}

public class StockAdjustmentService : IStockAdjustmentService
{
    private readonly AppDbContext _db;
    private readonly NumberService _numbers;
    public StockAdjustmentService(AppDbContext db, NumberService numbers) { _db = db; _numbers = numbers; }

    public async Task<IReadOnlyList<StockAdjustmentDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.StockAdjustments.Include(a => a.Product).OrderByDescending(a => a.Date)
            .Select(a => new StockAdjustmentDto(a.Id, a.AdjustmentNo, a.Date, a.ProductId, a.Product!.Name, a.QtyChange, a.Reason, a.Remarks))
            .ToListAsync(ct);

    public async Task<StockAdjustmentDto> CreateAsync(StockAdjustmentCreateDto dto, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstAsync(p => p.Id == dto.ProductId, ct);
        var newQty = product.StockQty + dto.QtyChange;
        if (newQty < 0) throw new InvalidOperationException("Adjustment would make stock negative.");
        var no = await _numbers.NextAsync("SA", "SA", ct);
        product.StockQty = newQty;
        _db.StockMovements.Add(new StockMovement
        {
            ProductId = product.Id, Date = dto.Date.Date,
            QtyIn = dto.QtyChange > 0 ? dto.QtyChange : 0,
            QtyOut = dto.QtyChange < 0 ? Math.Abs(dto.QtyChange) : 0,
            ReferenceType = "Adjustment", Remarks = no
        });
        var adj = new StockAdjustment
        {
            AdjustmentNo = no, Date = dto.Date.Date, ProductId = product.Id,
            QtyChange = dto.QtyChange, Reason = dto.Reason, Remarks = dto.Remarks,
            Status = DocumentStatus.Posted
        };
        _db.StockAdjustments.Add(adj);
        await _db.SaveChangesAsync(ct);
        return new StockAdjustmentDto(adj.Id, adj.AdjustmentNo, adj.Date, adj.ProductId, product.Name, adj.QtyChange, adj.Reason, adj.Remarks);
    }
}

public class WarehouseService : IWarehouseService
{
    private readonly AppDbContext _db;
    public WarehouseService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<WarehouseDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Warehouses.OrderByDescending(w => w.IsDefault).ThenBy(w => w.Code)
            .Select(w => new WarehouseDto(w.Id, w.Code, w.Name, w.Location, w.IsDefault))
            .ToListAsync(ct);

    public async Task<WarehouseDto> CreateAsync(WarehouseUpsertDto dto, CancellationToken ct = default)
    {
        if (dto.IsDefault)
        {
            foreach (var w in await _db.Warehouses.Where(x => x.IsDefault).ToListAsync(ct))
                w.IsDefault = false;
        }
        var entity = new Warehouse { Code = dto.Code.Trim(), Name = dto.Name.Trim(), Location = dto.Location, IsDefault = dto.IsDefault };
        _db.Warehouses.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new WarehouseDto(entity.Id, entity.Code, entity.Name, entity.Location, entity.IsDefault);
    }

    public async Task<WarehouseDto?> UpdateAsync(Guid id, WarehouseUpsertDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Warehouses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return null;
        if (dto.IsDefault)
        {
            foreach (var w in await _db.Warehouses.Where(x => x.IsDefault && x.Id != id).ToListAsync(ct))
                w.IsDefault = false;
        }
        entity.Code = dto.Code.Trim();
        entity.Name = dto.Name.Trim();
        entity.Location = dto.Location;
        entity.IsDefault = dto.IsDefault;
        await _db.SaveChangesAsync(ct);
        return new WarehouseDto(entity.Id, entity.Code, entity.Name, entity.Location, entity.IsDefault);
    }
}

public class PartySettlementService : IPartySettlementService
{
    private readonly AppDbContext _db;
    private readonly NumberService _numbers;
    private readonly IVoucherService _vouchers;

    public PartySettlementService(AppDbContext db, NumberService numbers, IVoucherService vouchers)
    {
        _db = db;
        _numbers = numbers;
        _vouchers = vouchers;
    }

    public async Task<IReadOnlyList<PartySettlementDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.PartySettlements.Include(x => x.Party).OrderByDescending(x => x.Date)
            .Select(x => new PartySettlementDto(x.Id, x.DocNo, x.Date, x.PartyId, x.Party!.Name, x.SettlementType, x.Mode, x.Amount, x.Remarks))
            .ToListAsync(ct);

    public async Task<PartySettlementDto> CreateAsync(PartySettlementCreateDto dto, CancellationToken ct = default)
    {
        if (dto.Amount <= 0) throw new InvalidOperationException("Amount must be greater than zero.");
        var isReceipt = string.Equals(dto.SettlementType, "Receipt", StringComparison.OrdinalIgnoreCase);
        var isCash = string.Equals(dto.Mode, "Cash", StringComparison.OrdinalIgnoreCase);
        var party = await _db.Parties.FirstAsync(x => x.Id == dto.PartyId, ct);
        var prefix = isReceipt ? (isCash ? "CR" : "BR") : (isCash ? "CP" : "BP");
        var no = await _numbers.NextAsync(prefix, prefix, ct);

        var cashOrBank = await SaleService.EnsureSystemAccount(_db,
            isCash ? "1300" : "1400",
            isCash ? "Cash in Hand" : "Bank",
            AccountType.Asset, ct);
        var contra = await SaleService.EnsureSystemAccount(_db,
            isReceipt ? "1100" : "2100",
            isReceipt ? "Accounts Receivable" : "Accounts Payable",
            isReceipt ? AccountType.Asset : AccountType.Liability, ct);

        var voucherType = isReceipt
            ? (isCash ? VoucherType.CR : VoucherType.BR)
            : (isCash ? VoucherType.CP : VoucherType.BP);

        VoucherLineDto[] lines = isReceipt
            ?
            [
                new VoucherLineDto(null, cashOrBank.Id, null, dto.Amount, 0, party.Name),
                new VoucherLineDto(null, contra.Id, null, 0, dto.Amount, no)
            ]
            :
            [
                new VoucherLineDto(null, contra.Id, null, dto.Amount, 0, party.Name),
                new VoucherLineDto(null, cashOrBank.Id, null, 0, dto.Amount, no)
            ];

        var voucher = await _vouchers.CreateAsync(
            new VoucherCreateDto(voucherType, dto.Date, $"{dto.SettlementType} {no} - {party.Name}", true, lines), ct);

        var entity = new PartySettlement
        {
            DocNo = no,
            Date = dto.Date.Date,
            PartyId = party.Id,
            SettlementType = isReceipt ? "Receipt" : "Payment",
            Mode = isCash ? "Cash" : "Bank",
            Amount = dto.Amount,
            Remarks = dto.Remarks,
            VoucherId = voucher.Id
        };
        _db.PartySettlements.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new PartySettlementDto(entity.Id, entity.DocNo, entity.Date, entity.PartyId, party.Name,
            entity.SettlementType, entity.Mode, entity.Amount, entity.Remarks);
    }
}
