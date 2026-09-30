using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Entities;
using SheikhGoNexusERP.Domain.Enums;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

public class InventoryPostingService(AppDbContext db, IAuditService audit, ITenantContext tenant) : IInventoryPostingService
{
    public async Task PostGoodsReceiptAsync(GoodsReceipt grn, CancellationToken ct = default)
    {
        var (companyId, branchId) = await ResolveCompanyBranchAsync(ct);
        foreach (var line in grn.Lines)
        {
            if (await AlreadyPostedAsync(InventoryTxnSourceType.GoodsReceipt, grn.Id, line.Id, grn.WarehouseId, ct))
                continue;

            var product = await db.Products.FirstAsync(x => x.Id == line.ProductId, ct);
            var unitCost = product.AverageCost > 0 ? product.AverageCost : product.PurchasePrice;
            await ApplyQtyAsync(line.ProductId, grn.WarehouseId, line.Qty, allowNegative: false, ct);
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                CompanyId = companyId,
                BranchId = branchId,
                ProductId = line.ProductId,
                WarehouseId = grn.WarehouseId,
                DocumentId = grn.DocumentId,
                SourceType = InventoryTxnSourceType.GoodsReceipt,
                SourceEntityId = grn.Id,
                SourceLineId = line.Id,
                TxnDate = grn.ReceiptDate.Date,
                QtyIn = line.Qty,
                QtyOut = 0,
                UnitCost = unitCost,
                TotalCost = Math.Round(unitCost * line.Qty, 4),
                Narration = $"GRN {grn.ReceiptNo}"
            });
        }
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(GoodsReceipt), grn.Id, "InventoryPosted", null, "Ledger In + WarehouseStock", ct);
    }

    public async Task PostDeliveryIssueAsync(DeliveryOrder delivery, CancellationToken ct = default)
    {
        var (companyId, branchId) = await ResolveCompanyBranchAsync(ct);
        foreach (var line in delivery.Lines)
        {
            if (await AlreadyPostedAsync(InventoryTxnSourceType.Delivery, delivery.Id, line.Id, delivery.WarehouseId, ct))
                continue;

            var product = await db.Products.FirstAsync(x => x.Id == line.ProductId, ct);
            var unitCost = product.AverageCost > 0 ? product.AverageCost : product.PurchasePrice;
            await ApplyQtyAsync(line.ProductId, delivery.WarehouseId, -line.Qty, allowNegative: false, ct);
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                CompanyId = companyId,
                BranchId = branchId,
                ProductId = line.ProductId,
                WarehouseId = delivery.WarehouseId,
                DocumentId = delivery.DocumentId,
                SourceType = InventoryTxnSourceType.Delivery,
                SourceEntityId = delivery.Id,
                SourceLineId = line.Id,
                TxnDate = delivery.DeliveryDate.Date,
                QtyIn = 0,
                QtyOut = line.Qty,
                UnitCost = unitCost,
                TotalCost = Math.Round(unitCost * line.Qty, 4),
                Narration = $"DO {delivery.DeliveryNo}"
            });
        }
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(DeliveryOrder), delivery.Id, "InventoryPosted", null, "Ledger Out + WarehouseStock", ct);
    }

    public async Task PostTransferAsync(StockTransfer transfer, CancellationToken ct = default)
    {
        if (transfer.FromWarehouseId == transfer.ToWarehouseId)
            throw new InvalidOperationException("Transfer warehouses must differ.");

        var (companyId, branchId) = await ResolveCompanyBranchAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var line in transfer.Lines)
            {
                if (await AlreadyPostedAsync(InventoryTxnSourceType.TransferOut, transfer.Id, line.Id, transfer.FromWarehouseId, ct))
                    continue;

                var product = await db.Products.FirstAsync(x => x.Id == line.ProductId, ct);
                var unitCost = product.AverageCost > 0 ? product.AverageCost : product.PurchasePrice;
                var total = Math.Round(unitCost * line.Qty, 4);

                await ApplyQtyAsync(line.ProductId, transfer.FromWarehouseId, -line.Qty, allowNegative: false, ct);
                await ApplyQtyAsync(line.ProductId, transfer.ToWarehouseId, line.Qty, allowNegative: true, ct);

                db.InventoryTransactions.Add(new InventoryTransaction
                {
                    CompanyId = companyId,
                    BranchId = branchId,
                    ProductId = line.ProductId,
                    WarehouseId = transfer.FromWarehouseId,
                    WarehouseLocationId = line.FromLocationId,
                    DocumentId = transfer.DocumentId,
                    SourceType = InventoryTxnSourceType.TransferOut,
                    SourceEntityId = transfer.Id,
                    SourceLineId = line.Id,
                    TxnDate = transfer.TransferDate.Date,
                    QtyIn = 0,
                    QtyOut = line.Qty,
                    UnitCost = unitCost,
                    TotalCost = total,
                    Narration = $"ST out {transfer.TransferNo}"
                });
                db.InventoryTransactions.Add(new InventoryTransaction
                {
                    CompanyId = companyId,
                    BranchId = branchId,
                    ProductId = line.ProductId,
                    WarehouseId = transfer.ToWarehouseId,
                    WarehouseLocationId = line.ToLocationId,
                    DocumentId = transfer.DocumentId,
                    SourceType = InventoryTxnSourceType.TransferIn,
                    SourceEntityId = transfer.Id,
                    SourceLineId = line.Id,
                    TxnDate = transfer.TransferDate.Date,
                    QtyIn = line.Qty,
                    QtyOut = 0,
                    UnitCost = unitCost,
                    TotalCost = total,
                    Narration = $"ST in {transfer.TransferNo}"
                });
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        await audit.WriteAsync(nameof(StockTransfer), transfer.Id, "InventoryPosted", null, "Atomic transfer Out+In", ct);
    }

    public async Task PostAdjustmentAsync(StockAdjustment adjustment, CancellationToken ct = default)
    {
        var (companyId, branchId) = await ResolveCompanyBranchAsync(ct);
        var sourceType = adjustment.StockCountId is Guid
            ? InventoryTxnSourceType.StockCount
            : InventoryTxnSourceType.Adjustment;

        foreach (var line in adjustment.Lines)
        {
            if (await AlreadyPostedAsync(sourceType, adjustment.Id, line.Id, adjustment.WarehouseId, ct))
                continue;

            var product = await db.Products.FirstAsync(x => x.Id == line.ProductId, ct);
            var unitCost = product.AverageCost > 0 ? product.AverageCost : product.PurchasePrice;
            await ApplyQtyAsync(line.ProductId, adjustment.WarehouseId, line.QtyDelta, allowNegative: line.QtyDelta > 0, ct);

            var qtyIn = line.QtyDelta > 0 ? line.QtyDelta : 0;
            var qtyOut = line.QtyDelta < 0 ? Math.Abs(line.QtyDelta) : 0;
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                CompanyId = companyId,
                BranchId = branchId,
                ProductId = line.ProductId,
                WarehouseId = adjustment.WarehouseId,
                WarehouseLocationId = line.WarehouseLocationId,
                DocumentId = adjustment.DocumentId,
                SourceType = sourceType,
                SourceEntityId = adjustment.Id,
                SourceLineId = line.Id,
                TxnDate = adjustment.AdjustmentDate.Date,
                QtyIn = qtyIn,
                QtyOut = qtyOut,
                UnitCost = unitCost,
                TotalCost = Math.Round(unitCost * Math.Abs(line.QtyDelta), 4),
                Narration = $"SA {adjustment.AdjustmentNo}: {adjustment.Reason}"
            });
        }

        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(StockAdjustment), adjustment.Id, "InventoryPosted", null, adjustment.Reason, ct);
    }

    public async Task PostProductionConsumptionAsync(ProductionOrder order, CancellationToken ct = default)
    {
        order = await db.ProductionOrders
            .Include(x => x.Components)
            .Include(x => x.Variances)
            .FirstAsync(x => x.Id == order.Id, ct);

        var (companyId, branchId) = await ResolveCompanyBranchAsync(ct);

        foreach (var line in order.Components)
        {
            if (await AlreadyPostedAsync(InventoryTxnSourceType.ProductionConsumption, order.Id, line.Id, order.WarehouseId, ct))
                continue;

            var qty = line.ActualQty > 0 ? line.ActualQty : line.PlannedQty;
            if (qty <= 0) continue;

            var product = await db.Products.FirstAsync(x => x.Id == line.ProductId, ct);
            var unitCost = product.AverageCost > 0 ? product.AverageCost : product.PurchasePrice;
            line.UnitCost = unitCost;
            line.ActualQty = qty;
            line.TotalCost = Math.Round(unitCost * qty, 4);

            await ApplyQtyAsync(line.ProductId, order.WarehouseId, -qty, allowNegative: false, ct);
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                CompanyId = companyId,
                BranchId = branchId,
                ProductId = line.ProductId,
                WarehouseId = order.WarehouseId,
                DocumentId = order.DocumentId,
                SourceType = InventoryTxnSourceType.ProductionConsumption,
                SourceEntityId = order.Id,
                SourceLineId = line.Id,
                TxnDate = order.OrderDate.Date,
                QtyIn = 0,
                QtyOut = qty,
                UnitCost = unitCost,
                TotalCost = line.TotalCost,
                Narration = $"MO consume {order.OrderNo}"
            });
        }

        foreach (var v in order.Variances.Where(x => x.Kind == ProductionVarianceKind.Wastage))
        {
            if (await AlreadyPostedAsync(InventoryTxnSourceType.ProductionConsumption, order.Id, v.Id, order.WarehouseId, ct))
                continue;
            if (v.Qty <= 0) continue;

            var product = await db.Products.FirstAsync(x => x.Id == v.ProductId, ct);
            var unitCost = product.AverageCost > 0 ? product.AverageCost : product.PurchasePrice;
            v.UnitCost = unitCost;
            v.TotalCost = Math.Round(unitCost * v.Qty, 4);

            await ApplyQtyAsync(v.ProductId, order.WarehouseId, -v.Qty, allowNegative: false, ct);
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                CompanyId = companyId,
                BranchId = branchId,
                ProductId = v.ProductId,
                WarehouseId = order.WarehouseId,
                DocumentId = order.DocumentId,
                SourceType = InventoryTxnSourceType.ProductionConsumption,
                SourceEntityId = order.Id,
                SourceLineId = v.Id,
                TxnDate = order.OrderDate.Date,
                QtyIn = 0,
                QtyOut = v.Qty,
                UnitCost = unitCost,
                TotalCost = v.TotalCost,
                Narration = $"MO wastage {order.OrderNo}"
            });
        }

        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(ProductionOrder), order.Id, "InventoryConsumption", null, "RM Out", ct);
    }

    public async Task PostProductionOutputAsync(ProductionOrder order, CancellationToken ct = default)
    {
        order = await db.ProductionOrders
            .Include(x => x.Outputs)
            .Include(x => x.Variances)
            .FirstAsync(x => x.Id == order.Id, ct);

        var (companyId, branchId) = await ResolveCompanyBranchAsync(ct);

        foreach (var line in order.Outputs)
        {
            if (await AlreadyPostedAsync(InventoryTxnSourceType.ProductionOutput, order.Id, line.Id, order.WarehouseId, ct))
                continue;
            if (line.Qty <= 0) continue;

            await ApplyQtyAsync(line.ProductId, order.WarehouseId, line.Qty, allowNegative: true, ct);
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                CompanyId = companyId,
                BranchId = branchId,
                ProductId = line.ProductId,
                WarehouseId = order.WarehouseId,
                DocumentId = order.DocumentId,
                SourceType = InventoryTxnSourceType.ProductionOutput,
                SourceEntityId = order.Id,
                SourceLineId = line.Id,
                TxnDate = order.OrderDate.Date,
                QtyIn = line.Qty,
                QtyOut = 0,
                UnitCost = line.UnitCost,
                TotalCost = line.TotalCost > 0 ? line.TotalCost : Math.Round(line.UnitCost * line.Qty, 4),
                Narration = $"MO output {order.OrderNo}"
            });
        }

        foreach (var v in order.Variances.Where(x => x.Kind == ProductionVarianceKind.ByProduct))
        {
            if (await AlreadyPostedAsync(InventoryTxnSourceType.ProductionOutput, order.Id, v.Id, order.WarehouseId, ct))
                continue;
            if (v.Qty <= 0) continue;

            var product = await db.Products.FirstAsync(x => x.Id == v.ProductId, ct);
            var unitCost = v.UnitCost > 0 ? v.UnitCost : (product.AverageCost > 0 ? product.AverageCost : 0);
            v.UnitCost = unitCost;
            v.TotalCost = Math.Round(unitCost * v.Qty, 4);

            await ApplyQtyAsync(v.ProductId, order.WarehouseId, v.Qty, allowNegative: true, ct);
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                CompanyId = companyId,
                BranchId = branchId,
                ProductId = v.ProductId,
                WarehouseId = order.WarehouseId,
                DocumentId = order.DocumentId,
                SourceType = InventoryTxnSourceType.ProductionOutput,
                SourceEntityId = order.Id,
                SourceLineId = v.Id,
                TxnDate = order.OrderDate.Date,
                QtyIn = v.Qty,
                QtyOut = 0,
                UnitCost = unitCost,
                TotalCost = v.TotalCost,
                Narration = $"MO by-product {order.OrderNo}"
            });
        }

        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(ProductionOrder), order.Id, "InventoryOutput", null, "FG In", ct);
    }

    public async Task PostFarmFeedIssueAsync(FeedIssue issue, CancellationToken ct = default)
    {
        issue = await db.FeedIssues.Include(x => x.Lines).FirstAsync(x => x.Id == issue.Id, ct);
        var (companyId, branchId) = await ResolveCompanyBranchAsync(ct);

        foreach (var line in issue.Lines)
        {
            if (await AlreadyPostedAsync(InventoryTxnSourceType.FarmFeedIssue, issue.Id, line.Id, issue.WarehouseId, ct))
                continue;
            if (line.Qty <= 0) continue;

            var product = await db.Products.FirstAsync(x => x.Id == line.ProductId, ct);
            var unitCost = product.AverageCost > 0 ? product.AverageCost : product.PurchasePrice;
            line.UnitCost = unitCost;
            line.TotalCost = Math.Round(unitCost * line.Qty, 4);

            await ApplyQtyAsync(line.ProductId, issue.WarehouseId, -line.Qty, allowNegative: false, ct);
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                CompanyId = companyId,
                BranchId = branchId,
                ProductId = line.ProductId,
                WarehouseId = issue.WarehouseId,
                DocumentId = issue.DocumentId,
                SourceType = InventoryTxnSourceType.FarmFeedIssue,
                SourceEntityId = issue.Id,
                SourceLineId = line.Id,
                TxnDate = issue.IssueDate.Date,
                QtyIn = 0,
                QtyOut = line.Qty,
                UnitCost = unitCost,
                TotalCost = line.TotalCost,
                Narration = $"FI {issue.IssueNo}"
            });
        }

        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(FeedIssue), issue.Id, "InventoryPosted", null, "Feed OUT", ct);
    }

    public async Task PostFarmMedicationAsync(FarmTreatment treatment, CancellationToken ct = default)
    {
        if (treatment.ProductId is null || treatment.WarehouseId is null || treatment.Qty <= 0) return;
        if (await AlreadyPostedAsync(InventoryTxnSourceType.FarmMedication, treatment.Id, treatment.Id, treatment.WarehouseId.Value, ct))
            return;

        var (companyId, branchId) = await ResolveCompanyBranchAsync(ct);
        var product = await db.Products.FirstAsync(x => x.Id == treatment.ProductId.Value, ct);
        var unitCost = product.AverageCost > 0 ? product.AverageCost : product.PurchasePrice;

        await ApplyQtyAsync(treatment.ProductId.Value, treatment.WarehouseId.Value, -treatment.Qty, allowNegative: false, ct);
        db.InventoryTransactions.Add(new InventoryTransaction
        {
            CompanyId = companyId,
            BranchId = branchId,
            ProductId = treatment.ProductId.Value,
            WarehouseId = treatment.WarehouseId.Value,
            DocumentId = treatment.DocumentId,
            SourceType = InventoryTxnSourceType.FarmMedication,
            SourceEntityId = treatment.Id,
            SourceLineId = treatment.Id,
            TxnDate = treatment.Date.Date,
            QtyIn = 0,
            QtyOut = treatment.Qty,
            UnitCost = unitCost,
            TotalCost = Math.Round(unitCost * treatment.Qty, 4),
            Narration = $"FMT {treatment.TreatmentNo}"
        });
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(FarmTreatment), treatment.Id, "InventoryPosted", null, "Meds OUT", ct);
    }

    private async Task ApplyQtyAsync(Guid productId, Guid warehouseId, decimal delta, bool allowNegative, CancellationToken ct)
    {
        var stock = await db.WarehouseStocks
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.WarehouseId == warehouseId, ct);
        if (stock is null)
        {
            if (delta < 0 && !allowNegative)
                throw new InvalidOperationException($"Insufficient stock for product {productId} in warehouse {warehouseId}.");
            stock = new WarehouseStock { ProductId = productId, WarehouseId = warehouseId, Quantity = 0 };
            db.WarehouseStocks.Add(stock);
        }

        if (delta < 0 && stock.QualityStatus != StockQualityStatus.Unrestricted)
            throw new InvalidOperationException($"Stock is on {stock.QualityStatus} quality status and cannot be issued.");

        var next = stock.Quantity + delta;
        if (next < 0 && !allowNegative)
            throw new InvalidOperationException($"Insufficient stock for product {productId} in warehouse {warehouseId}.");
        stock.Quantity = next;
    }

    private async Task<bool> AlreadyPostedAsync(
        InventoryTxnSourceType sourceType, Guid entityId, Guid lineId, Guid warehouseId, CancellationToken ct) =>
        await db.InventoryTransactions.AnyAsync(x =>
            x.SourceType == sourceType &&
            x.SourceEntityId == entityId &&
            x.SourceLineId == lineId &&
            x.WarehouseId == warehouseId, ct);

    private async Task<(Guid CompanyId, Guid BranchId)> ResolveCompanyBranchAsync(CancellationToken ct)
    {
        var company = await db.Companies.FirstAsync(ct);
        var branch = await db.Branches.FirstAsync(x => x.IsDefault || true, ct);
        _ = tenant;
        return (company.Id, branch.Id);
    }
}
