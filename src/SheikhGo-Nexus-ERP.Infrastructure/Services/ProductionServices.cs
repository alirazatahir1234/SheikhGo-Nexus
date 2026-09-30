using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Entities;
using SheikhGoNexusERP.Domain.Enums;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

public class ProductionService(
    AppDbContext db,
    IAuditService audit,
    IInventoryPostingService inventory,
    IAccountingPostingService accounting,
    IPermissionService permissions) : IProductionService
{
    public async Task<IReadOnlyList<BomDto>> GetBomsAsync(CancellationToken ct = default)
    {
        var rows = await db.BillOfMaterials
            .Include(x => x.FinishedProduct)
            .Include(x => x.Lines).ThenInclude(l => l.ComponentProduct)
            .Include(x => x.Lines).ThenInclude(l => l.Uom)
            .OrderBy(x => x.Code).ThenByDescending(x => x.Version)
            .ToListAsync(ct);
        return rows.Select(MapBom).ToList();
    }

    public async Task<BomDto?> GetBomByIdAsync(Guid id, CancellationToken ct = default)
    {
        var bom = await db.BillOfMaterials
            .Include(x => x.FinishedProduct)
            .Include(x => x.Lines).ThenInclude(l => l.ComponentProduct)
            .Include(x => x.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return bom is null ? null : MapBom(bom);
    }

    public async Task<BomDto> CreateBomAsync(BomUpsertDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Production.BOM.Create", ct);
        _ = await db.Products.FirstOrDefaultAsync(x => x.Id == dto.FinishedProductId, ct)
            ?? throw new InvalidOperationException("Finished product not found.");
        if (dto.Lines is null || dto.Lines.Count == 0)
            throw new InvalidOperationException("BOM requires component lines.");

        var bom = new BillOfMaterial
        {
            Code = dto.Code.Trim().ToUpperInvariant(),
            Name = dto.Name.Trim(),
            FinishedProductId = dto.FinishedProductId,
            Version = dto.Version <= 0 ? 1 : dto.Version,
            EffectiveFrom = (dto.EffectiveFrom ?? DateTime.UtcNow).Date,
            EffectiveTo = dto.EffectiveTo?.Date,
            ScrapPercent = dto.ScrapPercent,
            IsActive = dto.IsActive
        };
        var lineNo = 1;
        foreach (var l in dto.Lines)
        {
            bom.Lines.Add(new BillOfMaterialLine
            {
                LineNo = l.LineNo > 0 ? l.LineNo : lineNo++,
                ComponentProductId = l.ComponentProductId,
                QtyPer = l.QtyPer,
                UomId = l.UomId,
                ScrapPercent = l.ScrapPercent
            });
        }
        db.BillOfMaterials.Add(bom);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(BillOfMaterial), bom.Id, "Created", null, bom.Code, ct);
        return (await GetBomByIdAsync(bom.Id, ct))!;
    }

    public async Task<IReadOnlyList<ProductionOrderDto>> GetOrdersAsync(CancellationToken ct = default)
    {
        var rows = await db.ProductionOrders
            .Include(x => x.Bom).Include(x => x.FinishedProduct).Include(x => x.Warehouse)
            .Include(x => x.Components).ThenInclude(c => c.Product)
            .Include(x => x.Outputs).ThenInclude(o => o.Product)
            .Include(x => x.Variances).ThenInclude(v => v.Product)
            .OrderByDescending(x => x.OrderDate).ToListAsync(ct);
        return rows.Select(MapOrder).ToList();
    }

    public async Task<ProductionOrderDto?> GetOrderByIdAsync(Guid id, CancellationToken ct = default)
    {
        var o = await LoadOrderAsync(id, ct);
        return o is null ? null : MapOrder(o);
    }

    public async Task<ProductionOrderDto> CreateOrderAsync(ProductionOrderCreateDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Production.Order.Create", ct);
        if (dto.QtyOrdered <= 0) throw new InvalidOperationException("QtyOrdered must be positive.");

        var bom = await db.BillOfMaterials.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == dto.BomId && x.IsActive, ct)
            ?? throw new InvalidOperationException("Active BOM not found.");
        _ = await db.Warehouses.FirstOrDefaultAsync(x => x.Id == dto.WarehouseId, ct)
            ?? throw new InvalidOperationException("Warehouse not found.");

        var company = await db.Companies.FirstAsync(ct);
        var branch = await db.Branches.FirstAsync(x => x.IsDefault || true, ct);
        var fy = await db.FinancialYears.FirstAsync(x => x.IsCurrent, ct);
        var dt = await db.DocumentTypes.FirstAsync(x => x.Code == "MO", ct);
        var no = await DocumentNumbering.NextAsync(db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "MO", fy.StartDate.Year, ct);

        var doc = new Document
        {
            DocumentTypeId = dt.Id, CompanyId = company.Id, BranchId = branch.Id, FinancialYearId = fy.Id,
            DocumentNo = no, DocumentDate = (dto.OrderDate ?? DateTime.UtcNow).Date, Narration = dto.Remarks
        };

        var order = new ProductionOrder
        {
            Document = doc,
            OrderNo = no,
            OrderDate = (dto.OrderDate ?? DateTime.UtcNow).Date,
            BomId = bom.Id,
            FinishedProductId = bom.FinishedProductId,
            WarehouseId = dto.WarehouseId,
            QtyOrdered = dto.QtyOrdered,
            LaborCost = dto.LaborCost,
            OverheadCost = dto.OverheadCost,
            Status = ProductionOrderStatus.Draft,
            DocumentStatus = DocumentStatus.Draft,
            Remarks = dto.Remarks
        };

        var lineNo = 1;
        foreach (var bl in bom.Lines.OrderBy(x => x.LineNo))
        {
            var scrapFactor = 1m + ((bl.ScrapPercent + bom.ScrapPercent) / 100m);
            var planned = Math.Round(bl.QtyPer * dto.QtyOrdered * scrapFactor, 4);
            order.Components.Add(new ProductionOrderComponent
            {
                LineNo = lineNo++,
                ProductId = bl.ComponentProductId,
                PlannedQty = planned,
                ActualQty = planned
            });
        }

        db.ProductionOrders.Add(order);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(ProductionOrder), order.Id, "Created", null, no, ct);
        return (await GetOrderByIdAsync(order.Id, ct))!;
    }

    public async Task<ProductionOrderDto?> AddVarianceAsync(Guid id, ProductionVarianceCreateDto dto, CancellationToken ct = default)
    {
        var order = await db.ProductionOrders.Include(x => x.Variances).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (order is null) return null;
        if (order.Status is ProductionOrderStatus.Completed or ProductionOrderStatus.Closed or ProductionOrderStatus.Cancelled)
            throw new InvalidOperationException("Cannot add variance to a completed/closed order.");

        if (!Enum.TryParse<ProductionVarianceKind>(dto.Kind, true, out var kind))
            throw new InvalidOperationException("Kind must be Wastage or ByProduct.");
        if (dto.Qty <= 0) throw new InvalidOperationException("Qty must be positive.");
        _ = await db.Products.FirstOrDefaultAsync(x => x.Id == dto.ProductId, ct)
            ?? throw new InvalidOperationException("Product not found.");

        order.Variances.Add(new ProductionVarianceLine
        {
            Kind = kind,
            ProductId = dto.ProductId,
            Qty = dto.Qty,
            Narration = dto.Narration
        });
        await db.SaveChangesAsync(ct);
        return await GetOrderByIdAsync(id, ct);
    }

    public async Task<ProductionOrderDto?> TransitionAsync(Guid id, string action, CancellationToken ct = default)
    {
        var order = await LoadOrderAsync(id, ct);
        if (order is null) return null;

        var actionKey = action.Trim().ToLowerInvariant();
        await EnsurePermAsync(actionKey, ct);

        switch (actionKey)
        {
            case "submit":
                EnsureStatus(order, ProductionOrderStatus.Draft);
                order.Status = ProductionOrderStatus.Draft;
                order.DocumentStatus = DocumentStatus.Submitted;
                SyncDoc(order);
                break;
            case "approve":
                if (order.DocumentStatus is not (DocumentStatus.Draft or DocumentStatus.Submitted))
                    throw new InvalidOperationException($"Cannot approve from {order.DocumentStatus}.");
                order.Status = ProductionOrderStatus.Approved;
                order.DocumentStatus = DocumentStatus.Approved;
                SyncDoc(order);
                break;
            case "release":
                if (order.Status is not (ProductionOrderStatus.Approved or ProductionOrderStatus.Draft))
                    throw new InvalidOperationException($"Cannot release from {order.Status}.");
                if (order.DocumentStatus == DocumentStatus.Draft)
                {
                    order.DocumentStatus = DocumentStatus.Approved;
                    SyncDoc(order);
                }
                order.Status = ProductionOrderStatus.Released;
                break;
            case "start":
                if (order.Status is not (ProductionOrderStatus.Released or ProductionOrderStatus.Approved))
                    throw new InvalidOperationException($"Cannot start from {order.Status}.");
                order.Status = ProductionOrderStatus.InProduction;
                break;
            case "complete":
            case "post":
                await CompleteAsync(order, ct);
                break;
            case "close":
                if (order.Status != ProductionOrderStatus.Completed)
                    throw new InvalidOperationException("Only completed orders can be closed.");
                order.Status = ProductionOrderStatus.Closed;
                break;
            case "cancel":
                if (order.Status is ProductionOrderStatus.Completed or ProductionOrderStatus.Closed)
                    throw new InvalidOperationException("Cannot cancel a completed/closed order.");
                order.Status = ProductionOrderStatus.Cancelled;
                order.DocumentStatus = DocumentStatus.Cancelled;
                SyncDoc(order);
                break;
            default:
                throw new InvalidOperationException($"Unknown action '{action}'.");
        }

        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(ProductionOrder), order.Id, actionKey, null, order.Status.ToString(), ct);
        return await GetOrderByIdAsync(id, ct);
    }

    public async Task<ProductionCostingDto?> GetCostingAsync(Guid orderId, CancellationToken ct = default)
    {
        var o = await db.ProductionOrders.FirstOrDefaultAsync(x => x.Id == orderId, ct);
        return o is null ? null : new ProductionCostingDto(o.Id, o.OrderNo, o.MaterialCost, o.LaborCost, o.OverheadCost, o.TotalCost, o.UnitCost, o.QtyCompleted);
    }

    private async Task CompleteAsync(ProductionOrder order, CancellationToken ct)
    {
        if (order.Status is ProductionOrderStatus.Completed or ProductionOrderStatus.Closed)
            return;
        if (order.Status is not (ProductionOrderStatus.Released or ProductionOrderStatus.InProduction or ProductionOrderStatus.Approved))
            throw new InvalidOperationException($"Cannot complete from {order.Status}. Release the order first.");

        // Ensure ActualQty defaults
        foreach (var c in order.Components)
            if (c.ActualQty <= 0) c.ActualQty = c.PlannedQty;

        await inventory.PostProductionConsumptionAsync(order, ct);

        // Reload costs after IPS stamped UnitCost/TotalCost
        order = (await LoadOrderAsync(order.Id, ct))!;
        var material = order.Components.Sum(c => c.TotalCost)
            + order.Variances.Where(v => v.Kind == ProductionVarianceKind.Wastage).Sum(v => v.TotalCost);
        order.MaterialCost = material;
        order.TotalCost = material + order.LaborCost + order.OverheadCost;
        order.QtyCompleted = order.QtyOrdered;
        order.UnitCost = order.QtyCompleted > 0 ? Math.Round(order.TotalCost / order.QtyCompleted, 4) : 0;

        // Main FG output line — use DbSet.Add so pre-generated Guids insert (not UPDATE)
        var existingFg = order.Outputs.FirstOrDefault(o => !o.IsByProduct && o.ProductId == order.FinishedProductId);
        if (existingFg is null)
        {
            existingFg = new ProductionOrderOutput
            {
                ProductionOrderId = order.Id,
                ProductId = order.FinishedProductId,
                Qty = order.QtyCompleted,
                UnitCost = order.UnitCost,
                TotalCost = order.TotalCost,
                IsByProduct = false,
                LineNo = 1
            };
            db.ProductionOrderOutputs.Add(existingFg);
        }
        else
        {
            existingFg.Qty = order.QtyCompleted;
            existingFg.UnitCost = order.UnitCost;
            existingFg.TotalCost = order.TotalCost;
        }

        // By-product outputs from variances
        var lineNo = order.Outputs.Count + 1;
        foreach (var bp in order.Variances.Where(v => v.Kind == ProductionVarianceKind.ByProduct))
        {
            if (order.Outputs.Any(o => o.IsByProduct && o.ProductId == bp.ProductId)) continue;
            if (db.ProductionOrderOutputs.Local.Any(o => o.ProductionOrderId == order.Id && o.IsByProduct && o.ProductId == bp.ProductId))
                continue;
            db.ProductionOrderOutputs.Add(new ProductionOrderOutput
            {
                ProductionOrderId = order.Id,
                ProductId = bp.ProductId,
                Qty = bp.Qty,
                UnitCost = bp.UnitCost,
                TotalCost = bp.TotalCost,
                IsByProduct = true,
                LineNo = lineNo++
            });
        }

        await db.SaveChangesAsync(ct);

        await inventory.PostProductionOutputAsync(order, ct);
        await accounting.PostProductionIssueAsync(order, ct);
        await accounting.PostProductionReceiptAsync(order, ct);

        order.Status = ProductionOrderStatus.Completed;
        order.DocumentStatus = DocumentStatus.Posted;
        SyncDoc(order);
        await db.SaveChangesAsync(ct);
    }

    private void SyncDoc(ProductionOrder order)
    {
        if (order.DocumentId is Guid did)
        {
            var doc = db.Documents.FirstOrDefault(x => x.Id == did);
            if (doc is not null) doc.Status = order.DocumentStatus;
        }
        else if (order.Document is not null)
        {
            order.Document.Status = order.DocumentStatus;
        }
    }

    private static void EnsureStatus(ProductionOrder order, ProductionOrderStatus expected)
    {
        if (order.Status != expected)
            throw new InvalidOperationException($"Expected status {expected}, was {order.Status}.");
    }

    private async Task EnsurePermAsync(string action, CancellationToken ct)
    {
        var code = action switch
        {
            "approve" => "Production.Order.Approve",
            "release" => "Production.Order.Release",
            "start" => "Production.Order.Start",
            "complete" or "post" => "Production.Order.Complete",
            "close" => "Production.Order.Close",
            _ => "Production.Order.Create"
        };
        await permissions.EnsureAsync(code, ct);
    }

    private async Task<ProductionOrder?> LoadOrderAsync(Guid id, CancellationToken ct) =>
        await db.ProductionOrders
            .Include(x => x.Bom).Include(x => x.FinishedProduct).Include(x => x.Warehouse)
            .Include(x => x.Components).ThenInclude(c => c.Product)
            .Include(x => x.Outputs).ThenInclude(o => o.Product)
            .Include(x => x.Variances).ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    private static BomDto MapBom(BillOfMaterial b) => new(
        b.Id, b.Code, b.Name, b.FinishedProductId, b.FinishedProduct?.Name, b.Version,
        b.EffectiveFrom, b.EffectiveTo, b.ScrapPercent, b.IsActive,
        b.Lines.OrderBy(l => l.LineNo).Select(l => new BomLineDto(
            l.Id, l.ComponentProductId, l.ComponentProduct?.Name, l.QtyPer, l.UomId, l.Uom?.Code, l.ScrapPercent, l.LineNo)).ToList());

    private static ProductionOrderDto MapOrder(ProductionOrder o) => new(
        o.Id, o.OrderNo, o.OrderDate, o.BomId, o.Bom?.Code, o.FinishedProductId, o.FinishedProduct?.Name,
        o.WarehouseId, o.Warehouse?.Name, o.QtyOrdered, o.QtyCompleted,
        o.Status.ToString(), o.DocumentStatus, o.MaterialCost, o.LaborCost, o.OverheadCost,
        o.TotalCost, o.UnitCost, o.Remarks,
        o.Components.Select(c => new ProductionComponentDto(c.Id, c.ProductId, c.Product?.Code, c.Product?.Name, c.PlannedQty, c.ActualQty, c.UnitCost, c.TotalCost)).ToList(),
        o.Outputs.Select(x => new ProductionOutputDto(x.Id, x.ProductId, x.Product?.Code, x.Product?.Name, x.Qty, x.UnitCost, x.TotalCost, x.IsByProduct)).ToList(),
        o.Variances.Select(v => new ProductionVarianceDto(v.Id, v.Kind.ToString(), v.ProductId, v.Product?.Code, v.Product?.Name, v.Qty, v.UnitCost, v.TotalCost, v.Narration)).ToList());
}
