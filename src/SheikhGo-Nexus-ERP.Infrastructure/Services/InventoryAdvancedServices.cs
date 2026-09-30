using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Entities;
using SheikhGoNexusERP.Domain.Enums;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

public class InventoryReportsService(AppDbContext db) : IInventoryService
{
    public async Task<IReadOnlyList<StockDto>> GetStockAsync(Guid? warehouseId = null, CancellationToken ct = default)
    {
        var q = db.WarehouseStocks.Include(x => x.Product)!.ThenInclude(p => p!.UnitOfMeasure)
            .Include(x => x.Warehouse).AsQueryable();
        if (warehouseId is Guid wid) q = q.Where(x => x.WarehouseId == wid);

        return await q.OrderBy(x => x.Warehouse!.Code).ThenBy(x => x.Product!.Code)
            .Select(x => new StockDto(
                x.ProductId,
                x.Product!.Code,
                x.Product.Name,
                x.Product.UnitOfMeasure != null ? x.Product.UnitOfMeasure.Code : "BAG",
                x.Quantity,
                x.Product.ReorderLevel,
                x.Quantity <= x.Product.ReorderLevel,
                x.WarehouseId,
                x.Warehouse!.Code,
                x.Product.AverageCost,
                Math.Round(x.Quantity * x.Product.AverageCost, 2)))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<InventoryLedgerLineDto>> GetLedgerAsync(
        Guid? productId, Guid? warehouseId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var q = db.InventoryTransactions
            .Include(x => x.Product).Include(x => x.Warehouse)
            .Where(x => x.TxnDate >= from.Date && x.TxnDate <= to.Date);
        if (productId is Guid pid) q = q.Where(x => x.ProductId == pid);
        if (warehouseId is Guid wid) q = q.Where(x => x.WarehouseId == wid);

        var rows = await q.OrderBy(x => x.TxnDate).ThenBy(x => x.CreatedAt).ToListAsync(ct);
        var balances = new Dictionary<(Guid, Guid), decimal>();
        var result = new List<InventoryLedgerLineDto>();
        foreach (var r in rows)
        {
            var key = (r.ProductId, r.WarehouseId);
            balances.TryGetValue(key, out var bal);
            bal += r.QtyIn - r.QtyOut;
            balances[key] = bal;
            result.Add(new InventoryLedgerLineDto(
                r.TxnDate, r.SourceType.ToString(), r.Narration,
                r.ProductId, r.Product?.Code, r.WarehouseId, r.Warehouse?.Code,
                r.QtyIn, r.QtyOut, bal, r.UnitCost, r.TotalCost, r.Narration));
        }
        return result;
    }

    public async Task<IReadOnlyList<StockValuationLineDto>> GetValuationAsync(Guid? warehouseId = null, CancellationToken ct = default)
    {
        var stock = await GetStockAsync(warehouseId, ct);
        return stock.Select(s => new StockValuationLineDto(
            s.ProductId, s.Code, s.Name, s.WarehouseId ?? Guid.Empty, s.WarehouseCode ?? "",
            s.StockQty, s.AverageCost ?? 0, s.Value ?? 0)).ToList();
    }

    public async Task<IReadOnlyList<StockMovementLineDto>> GetMovementAsync(
        Guid? productId, Guid? warehouseId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var q = db.InventoryTransactions.Include(x => x.Product).Include(x => x.Warehouse).AsQueryable();
        if (productId is Guid pid) q = q.Where(x => x.ProductId == pid);
        if (warehouseId is Guid wid) q = q.Where(x => x.WarehouseId == wid);

        var all = await q.ToListAsync(ct);
        var groups = all.GroupBy(x => new { x.ProductId, x.WarehouseId, Code = x.Product!.Code, Name = x.Product.Name, Wh = x.Warehouse!.Code });
        var list = new List<StockMovementLineDto>();
        foreach (var g in groups)
        {
            var opening = g.Where(x => x.TxnDate < from.Date).Sum(x => x.QtyIn - x.QtyOut);
            var period = g.Where(x => x.TxnDate >= from.Date && x.TxnDate <= to.Date).ToList();
            var purchase = period.Where(x => x.SourceType == InventoryTxnSourceType.GoodsReceipt).Sum(x => x.QtyIn);
            var tin = period.Where(x => x.SourceType == InventoryTxnSourceType.TransferIn).Sum(x => x.QtyIn);
            var tout = period.Where(x => x.SourceType == InventoryTxnSourceType.TransferOut).Sum(x => x.QtyOut);
            var sale = period.Where(x => x.SourceType == InventoryTxnSourceType.Delivery).Sum(x => x.QtyOut);
            var adj = period.Where(x => x.SourceType is InventoryTxnSourceType.Adjustment or InventoryTxnSourceType.StockCount)
                .Sum(x => x.QtyIn - x.QtyOut);
            var closing = opening + purchase + tin - tout - sale + adj;
            list.Add(new StockMovementLineDto(
                g.Key.ProductId, g.Key.Code, g.Key.Name, g.Key.WarehouseId, g.Key.Wh,
                opening, purchase, tin, tout, sale, adj, closing));
        }
        return list.OrderBy(x => x.WarehouseCode).ThenBy(x => x.Code).ToList();
    }

    public async Task<IReadOnlyList<NamedAmountRowDto>> GetSlowMovingAsync(DateTime from, DateTime to, decimal maxQty = 5, CancellationToken ct = default)
    {
        var movement = await GetMovementAsync(null, null, from, to, ct);
        return movement
            .Where(x => x.SaleOut + x.TransferOut <= maxQty)
            .Select(x => new NamedAmountRowDto(x.Code, x.Name, x.SaleOut + x.TransferOut, x.Closing, x.WarehouseCode))
            .OrderBy(x => x.Amount).ToList();
    }

    public async Task<IReadOnlyList<NamedAmountRowDto>> GetFastMovingAsync(DateTime from, DateTime to, decimal minQty = 20, CancellationToken ct = default)
    {
        var movement = await GetMovementAsync(null, null, from, to, ct);
        return movement
            .Where(x => x.SaleOut + x.TransferOut >= minQty)
            .Select(x => new NamedAmountRowDto(x.Code, x.Name, x.SaleOut + x.TransferOut, x.Closing, x.WarehouseCode))
            .OrderByDescending(x => x.Amount).ToList();
    }

    public async Task<IReadOnlyList<StockAgingLineDto>> GetStockAgingAsync(DateTime? asOf = null, CancellationToken ct = default)
    {
        var date = (asOf ?? DateTime.UtcNow).Date;
        var stock = await GetStockAsync(null, ct);
        var lastMoves = await db.InventoryTransactions
            .GroupBy(x => new { x.ProductId, x.WarehouseId })
            .Select(g => new { g.Key.ProductId, g.Key.WarehouseId, Last = g.Max(x => x.TxnDate) })
            .ToListAsync(ct);
        var map = lastMoves.ToDictionary(x => (x.ProductId, x.WarehouseId), x => x.Last);
        var list = new List<StockAgingLineDto>();
        foreach (var s in stock.Where(x => x.StockQty > 0))
        {
            map.TryGetValue((s.ProductId, s.WarehouseId ?? Guid.Empty), out var last);
            var days = last == default ? 999 : (date - last.Date).Days;
            var bucket = days <= 30 ? "0-30" : days <= 60 ? "31-60" : days <= 90 ? "61-90" : "90+";
            list.Add(new StockAgingLineDto(s.ProductId, s.Code, s.Name, s.WarehouseId ?? Guid.Empty, s.WarehouseCode ?? "",
                s.StockQty, s.Value ?? 0, days, bucket));
        }
        return list.OrderByDescending(x => x.DaysSinceMovement).ToList();
    }

    public async Task<IReadOnlyList<WarehouseComparisonLineDto>> GetWarehouseComparisonAsync(CancellationToken ct = default)
    {
        var stock = await GetStockAsync(null, ct);
        return stock.GroupBy(x => new { x.WarehouseId, x.WarehouseCode })
            .Select(g => new WarehouseComparisonLineDto(g.Key.WarehouseId ?? Guid.Empty, g.Key.WarehouseCode ?? "",
                g.Count(), g.Sum(x => x.StockQty), g.Sum(x => x.Value ?? 0)))
            .OrderBy(x => x.WarehouseCode).ToList();
    }

    public async Task<IReadOnlyList<NamedAmountRowDto>> GetAdjustmentAnalysisAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var data = await db.InventoryTransactions.Include(x => x.Product)
            .Where(x => x.TxnDate >= from.Date && x.TxnDate <= to.Date &&
                        (x.SourceType == InventoryTxnSourceType.Adjustment || x.SourceType == InventoryTxnSourceType.StockCount))
            .GroupBy(x => new { x.ProductId, x.Product!.Code, x.Product.Name })
            .Select(g => new { g.Key.Code, g.Key.Name, Amount = g.Sum(x => x.TotalCost), Qty = g.Sum(x => x.QtyIn - x.QtyOut) })
            .ToListAsync(ct);
        return data.Select(x => new NamedAmountRowDto(x.Code, x.Name, x.Amount, x.Qty, null))
            .OrderByDescending(x => Math.Abs(x.Amount)).ToList();
    }

    public async Task<InventoryTurnoverDto> GetTurnoverAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var cogs = await db.InventoryTransactions
            .Where(x => x.TxnDate >= from.Date && x.TxnDate <= to.Date && x.SourceType == InventoryTxnSourceType.Delivery)
            .SumAsync(x => (decimal?)x.TotalCost, ct) ?? 0;
        var valuation = await GetValuationAsync(null, ct);
        var avg = valuation.Sum(x => x.Value);
        var turnover = avg == 0 ? 0 : Math.Round(cogs / avg, 4);
        return new InventoryTurnoverDto(from.Date, to.Date, cogs, avg, turnover);
    }
}

public class StockTransferService(
    AppDbContext db,
    IAuditService audit,
    IInventoryPostingService inventory,
    IPermissionService permissions,
    ITenantContext tenant) : IStockTransferService
{
    public async Task<IReadOnlyList<StockTransferDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await db.StockTransfers.Include(x => x.FromWarehouse).Include(x => x.ToWarehouse)
            .Include(x => x.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(x => x.TransferDate).ThenByDescending(x => x.TransferNo)
            .ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<StockTransferDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var x = await db.StockTransfers.Include(x => x.FromWarehouse).Include(x => x.ToWarehouse)
            .Include(x => x.Lines).ThenInclude(l => l.Product).FirstOrDefaultAsync(t => t.Id == id, ct);
        return x is null ? null : Map(x);
    }

    public async Task<StockTransferDto> CreateAsync(StockTransferCreateDto dto, CancellationToken ct = default)
    {
        if (dto.FromWarehouseId == dto.ToWarehouseId)
            throw new InvalidOperationException("From and To warehouse must differ.");
        if (dto.Lines.Count == 0) throw new InvalidOperationException("Transfer requires lines.");

        var company = await db.Companies.FirstAsync(ct);
        var branch = await db.Branches.FirstAsync(x => x.IsDefault || true, ct);
        var fy = await db.FinancialYears.FirstAsync(x => x.IsCurrent, ct);
        var dt = await db.DocumentTypes.FirstAsync(x => x.Code == "ST", ct);
        var no = await DocumentNumbering.NextAsync(db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "ST", fy.StartDate.Year, ct);

        var doc = new Document
        {
            DocumentTypeId = dt.Id, CompanyId = company.Id, BranchId = branch.Id, FinancialYearId = fy.Id,
            DocumentNo = no, DocumentDate = dto.Date.Date, Narration = dto.Remarks
        };
        var transfer = new StockTransfer
        {
            TransferNo = no, TransferDate = dto.Date.Date,
            FromWarehouseId = dto.FromWarehouseId, ToWarehouseId = dto.ToWarehouseId,
            Document = doc, Status = DocumentStatus.Draft, Remarks = dto.Remarks
        };
        foreach (var l in dto.Lines)
        {
            transfer.Lines.Add(new StockTransferLine
            {
                ProductId = l.ProductId, Qty = l.Qty,
                FromLocationId = l.FromLocationId, ToLocationId = l.ToLocationId
            });
        }
        db.StockTransfers.Add(transfer);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(StockTransfer), transfer.Id, "Created", null, transfer.Status.ToString(), ct);

        if (dto.Post)
        {
            await TransitionAsync(transfer.Id, "submit", ct);
            await TransitionAsync(transfer.Id, "approve", ct);
            await TransitionAsync(transfer.Id, "post", ct);
        }
        return (await GetByIdAsync(transfer.Id, ct))!;
    }

    public async Task<StockTransferDto?> TransitionAsync(Guid id, string action, CancellationToken ct = default)
    {
        var transfer = await db.StockTransfers.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (transfer is null) return null;

        var actionKey = action.Trim().ToLowerInvariant();
        await EnsurePermAsync(actionKey, ct);
        var dt = await db.DocumentTypes.FirstAsync(x => x.Code == "ST", ct);
        ApplyStatus(transfer, transfer.DocumentId, dt, actionKey, ct);
        await db.SaveChangesAsync(ct);

        if (actionKey == "post" && transfer.Status == DocumentStatus.Posted)
            await inventory.PostTransferAsync(transfer, ct);

        return await GetByIdAsync(id, ct);
    }

    private async Task EnsurePermAsync(string action, CancellationToken ct)
    {
        var code = action switch
        {
            "approve" => "Inventory.Transfer.Approve",
            "post" => "Inventory.Transfer.Post",
            "submit" => "Inventory.Transfer.Create",
            "cancel" => "Inventory.Transfer.Create",
            _ => "Inventory.Transfer.View"
        };
        await permissions.EnsureAsync(code, ct);
        _ = tenant;
    }

    private void ApplyStatus(StockTransfer transfer, Guid? documentId, DocumentType dt, string action, CancellationToken ct)
    {
        var old = transfer.Status;
        transfer.Status = action switch
        {
            "submit" when old == DocumentStatus.Draft => DocumentStatus.Submitted,
            "approve" when old is DocumentStatus.Submitted or DocumentStatus.Verified or DocumentStatus.Approved =>
                DocumentStatus.Approved,
            "post" when (!dt.RequiresApproval && old is DocumentStatus.Submitted or DocumentStatus.Approved)
                || (dt.RequiresApproval && old == DocumentStatus.Approved)
                || old == DocumentStatus.Approved => DocumentStatus.Posted,
            "cancel" when old != DocumentStatus.Posted => DocumentStatus.Cancelled,
            _ => throw new InvalidOperationException($"Cannot {action} transfer in status {old}.")
        };
        if (documentId is Guid did)
        {
            var doc = db.Documents.First(x => x.Id == did);
            doc.Status = transfer.Status;
        }
    }

    private static StockTransferDto Map(StockTransfer x) => new(
        x.Id, x.TransferNo, x.TransferDate, x.FromWarehouseId, x.FromWarehouse?.Name,
        x.ToWarehouseId, x.ToWarehouse?.Name, x.Status, x.Remarks,
        x.Lines.Select(l => new StockTransferLineDto(l.Id, l.ProductId, l.Product?.Name, l.Qty, l.FromLocationId, l.ToLocationId)).ToList());
}

public class StockAdjustmentService(
    AppDbContext db,
    IAuditService audit,
    IInventoryPostingService inventory,
    IAccountingPostingService accounting,
    IPermissionService permissions) : IStockAdjustmentService
{
    public async Task<IReadOnlyList<StockAdjustmentDocDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await db.StockAdjustments.Include(x => x.Warehouse).Include(x => x.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(x => x.AdjustmentDate).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<StockAdjustmentDocDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var x = await db.StockAdjustments.Include(x => x.Warehouse).Include(x => x.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        return x is null ? null : Map(x);
    }

    public async Task<StockAdjustmentDocDto> CreateAsync(StockAdjustmentDocCreateDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason)) throw new InvalidOperationException("Reason is required.");
        if (dto.Lines.Count == 0) throw new InvalidOperationException("Adjustment requires lines.");

        var company = await db.Companies.FirstAsync(ct);
        var branch = await db.Branches.FirstAsync(x => x.IsDefault || true, ct);
        var fy = await db.FinancialYears.FirstAsync(x => x.IsCurrent, ct);
        var dt = await db.DocumentTypes.FirstAsync(x => x.Code == "SA", ct);
        var no = await DocumentNumbering.NextAsync(db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "SA", fy.StartDate.Year, ct);
        var doc = new Document
        {
            DocumentTypeId = dt.Id, CompanyId = company.Id, BranchId = branch.Id, FinancialYearId = fy.Id,
            DocumentNo = no, DocumentDate = dto.Date.Date, Narration = dto.Remarks
        };
        var adj = new StockAdjustment
        {
            AdjustmentNo = no, AdjustmentDate = dto.Date.Date, WarehouseId = dto.WarehouseId,
            Document = doc, Status = DocumentStatus.Draft, Reason = dto.Reason, Remarks = dto.Remarks
        };
        foreach (var l in dto.Lines)
            adj.Lines.Add(new StockAdjustmentLine
            {
                ProductId = l.ProductId, QtyDelta = l.QtyDelta,
                WarehouseLocationId = l.WarehouseLocationId, LineReason = l.LineReason
            });
        db.StockAdjustments.Add(adj);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(StockAdjustment), adj.Id, "Created", null, dto.Reason, ct);

        if (dto.Post)
        {
            await TransitionAsync(adj.Id, "submit", ct);
            await TransitionAsync(adj.Id, "approve", ct);
            await TransitionAsync(adj.Id, "post", ct);
        }
        return (await GetByIdAsync(adj.Id, ct))!;
    }

    public async Task<StockAdjustmentDocDto?> TransitionAsync(Guid id, string action, CancellationToken ct = default)
    {
        var adj = await db.StockAdjustments.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (adj is null) return null;
        var actionKey = action.Trim().ToLowerInvariant();
        _ = permissions;
        var dt = await db.DocumentTypes.FirstAsync(x => x.Code == "SA", ct);
        var old = adj.Status;
        adj.Status = actionKey switch
        {
            "submit" when old == DocumentStatus.Draft => DocumentStatus.Submitted,
            "approve" when old is DocumentStatus.Submitted or DocumentStatus.Approved => DocumentStatus.Approved,
            "post" when old is DocumentStatus.Approved or DocumentStatus.Submitted => DocumentStatus.Posted,
            "cancel" when old != DocumentStatus.Posted => DocumentStatus.Cancelled,
            _ => throw new InvalidOperationException($"Cannot {actionKey} adjustment in status {old}.")
        };
        if (adj.DocumentId is Guid did)
        {
            var doc = await db.Documents.FirstAsync(x => x.Id == did, ct);
            doc.Status = adj.Status;
        }
        await db.SaveChangesAsync(ct);
        if (actionKey == "post" && adj.Status == DocumentStatus.Posted)
        {
            await inventory.PostAdjustmentAsync(adj, ct);
            await accounting.PostStockAdjustmentAsync(adj, ct);
        }
        return await GetByIdAsync(id, ct);
    }

    private static StockAdjustmentDocDto Map(StockAdjustment x) => new(
        x.Id, x.AdjustmentNo, x.AdjustmentDate, x.WarehouseId, x.Warehouse?.Name, x.Status,
        x.Reason, x.Remarks, x.StockCountId,
        x.Lines.Select(l => new StockAdjustmentLineDto(l.Id, l.ProductId, l.Product?.Name, l.QtyDelta, l.WarehouseLocationId, l.LineReason)).ToList());
}

public class StockCountService(
    AppDbContext db,
    IAuditService audit,
    IStockAdjustmentService adjustments) : IStockCountService
{
    public async Task<IReadOnlyList<StockCountDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await db.StockCounts.Include(x => x.Warehouse).Include(x => x.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(x => x.CountDate).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<StockCountDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var x = await db.StockCounts.Include(x => x.Warehouse).Include(x => x.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        return x is null ? null : Map(x);
    }

    public async Task<StockCountDto> CreateAsync(StockCountCreateDto dto, CancellationToken ct = default)
    {
        var company = await db.Companies.FirstAsync(ct);
        var branch = await db.Branches.FirstAsync(x => x.IsDefault || true, ct);
        var fy = await db.FinancialYears.FirstAsync(x => x.IsCurrent, ct);
        var dt = await db.DocumentTypes.FirstAsync(x => x.Code == "SC", ct);
        var no = await DocumentNumbering.NextAsync(db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "SC", fy.StartDate.Year, ct);
        var doc = new Document
        {
            DocumentTypeId = dt.Id, CompanyId = company.Id, BranchId = branch.Id, FinancialYearId = fy.Id,
            DocumentNo = no, DocumentDate = dto.Date.Date, Narration = dto.Remarks
        };
        var count = new StockCount
        {
            CountNo = no, CountDate = dto.Date.Date, WarehouseId = dto.WarehouseId,
            Document = doc, Status = DocumentStatus.Draft, Remarks = dto.Remarks
        };
        foreach (var l in dto.Lines)
        {
            count.Lines.Add(new StockCountLine
            {
                ProductId = l.ProductId,
                SystemQty = l.SystemQty,
                CountedQty = l.CountedQty,
                Variance = l.CountedQty - l.SystemQty,
                WarehouseLocationId = l.WarehouseLocationId
            });
        }
        db.StockCounts.Add(count);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(StockCount), count.Id, "Created", null, count.CountNo, ct);
        return (await GetByIdAsync(count.Id, ct))!;
    }

    public async Task<StockCountDto?> SnapshotAsync(Guid id, CancellationToken ct = default)
    {
        var count = await db.StockCounts.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (count is null) return null;
        if (count.Status == DocumentStatus.Posted)
            throw new InvalidOperationException("Cannot snapshot a posted count.");

        foreach (var line in count.Lines)
        {
            var stock = await db.WarehouseStocks.FirstOrDefaultAsync(
                x => x.ProductId == line.ProductId && x.WarehouseId == count.WarehouseId, ct);
            line.SystemQty = stock?.Quantity ?? 0;
            line.Variance = line.CountedQty - line.SystemQty;
        }
        count.SnapshotAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(StockCount), id, "Snapshot", null, count.SnapshotAt.ToString(), ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<StockCountDto?> TransitionAsync(Guid id, string action, CancellationToken ct = default)
    {
        var count = await db.StockCounts.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (count is null) return null;
        var actionKey = action.Trim().ToLowerInvariant();
        var old = count.Status;
        count.Status = actionKey switch
        {
            "submit" when old == DocumentStatus.Draft => DocumentStatus.Submitted,
            "approve" when old is DocumentStatus.Draft or DocumentStatus.Submitted or DocumentStatus.Approved => DocumentStatus.Approved,
            "post" when old is DocumentStatus.Draft or DocumentStatus.Submitted or DocumentStatus.Approved => DocumentStatus.Posted,
            "cancel" when old != DocumentStatus.Posted => DocumentStatus.Cancelled,
            _ => throw new InvalidOperationException($"Cannot {actionKey} count in status {old}.")
        };
        if (count.DocumentId is Guid did)
        {
            var doc = await db.Documents.FirstAsync(x => x.Id == did, ct);
            doc.Status = count.Status;
        }
        await db.SaveChangesAsync(ct);

        if (actionKey == "post" && count.Status == DocumentStatus.Posted)
        {
            if (count.SnapshotAt is null)
                await SnapshotAsync(id, ct);
            count = await db.StockCounts.Include(x => x.Lines).FirstAsync(x => x.Id == id, ct);
            var varianceLines = count.Lines.Where(l => l.Variance != 0)
                .Select(l => new StockAdjustmentLineDto(null, l.ProductId, null, l.Variance, l.WarehouseLocationId, "Stock count variance"))
                .ToList();
            if (varianceLines.Count > 0)
            {
                var adj = await adjustments.CreateAsync(new StockAdjustmentDocCreateDto(
                    count.CountDate, count.WarehouseId, $"Stock count {count.CountNo}", count.Remarks, true, varianceLines), ct);
                count.LinkedAdjustmentId = adj.Id;
                var sa = await db.StockAdjustments.FirstAsync(x => x.Id == adj.Id, ct);
                sa.StockCountId = count.Id;
                await db.SaveChangesAsync(ct);
                await audit.WriteAsync(nameof(StockCount), id, "PostedAdjustment", null, adj.AdjustmentNo, ct);
            }
        }
        return await GetByIdAsync(id, ct);
    }

    private static StockCountDto Map(StockCount x) => new(
        x.Id, x.CountNo, x.CountDate, x.WarehouseId, x.Warehouse?.Name, x.Status, x.SnapshotAt, x.Remarks, x.LinkedAdjustmentId,
        x.Lines.Select(l => new StockCountLineDto(l.Id, l.ProductId, l.Product?.Name, l.SystemQty, l.CountedQty, l.Variance, l.WarehouseLocationId)).ToList());
}

public class UnitConversionService(AppDbContext db) : IUnitConversionService
{
    public async Task<IReadOnlyList<UnitConversionDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.UnitConversions.Include(x => x.Product).Include(x => x.FromUom).Include(x => x.ToUom)
            .OrderBy(x => x.FromUom!.Code)
            .Select(x => new UnitConversionDto(x.Id, x.ProductId, x.Product != null ? x.Product.Name : null,
                x.FromUomId, x.FromUom!.Code, x.ToUomId, x.ToUom!.Code, x.Factor, x.IsActive))
            .ToListAsync(ct);

    public async Task<UnitConversionDto> CreateAsync(UnitConversionUpsertDto dto, CancellationToken ct = default)
    {
        if (dto.Factor <= 0) throw new InvalidOperationException("Factor must be positive.");
        if (dto.FromUomId == dto.ToUomId) throw new InvalidOperationException("UOMs must differ.");
        var e = new UnitConversion
        {
            ProductId = dto.ProductId, FromUomId = dto.FromUomId, ToUomId = dto.ToUomId,
            Factor = dto.Factor, IsActive = dto.IsActive
        };
        db.UnitConversions.Add(e);
        await db.SaveChangesAsync(ct);
        return (await GetAllAsync(ct)).First(x => x.Id == e.Id);
    }
}

public class WarehouseLocationService(AppDbContext db) : IWarehouseLocationService
{
    public async Task<IReadOnlyList<WarehouseLocationDto>> GetByWarehouseAsync(Guid warehouseId, CancellationToken ct = default) =>
        await db.WarehouseLocations.Where(x => x.WarehouseId == warehouseId).OrderBy(x => x.Code)
            .Select(x => new WarehouseLocationDto(x.Id, x.WarehouseId, x.Code, x.Name, x.IsActive))
            .ToListAsync(ct);

    public async Task<WarehouseLocationDto> CreateAsync(WarehouseLocationUpsertDto dto, CancellationToken ct = default)
    {
        var e = new WarehouseLocation
        {
            WarehouseId = dto.WarehouseId, Code = dto.Code.Trim(), Name = dto.Name.Trim(), IsActive = dto.IsActive
        };
        db.WarehouseLocations.Add(e);
        await db.SaveChangesAsync(ct);
        return new WarehouseLocationDto(e.Id, e.WarehouseId, e.Code, e.Name, e.IsActive);
    }
}
