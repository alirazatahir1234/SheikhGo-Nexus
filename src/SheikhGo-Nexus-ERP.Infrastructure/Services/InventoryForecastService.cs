using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Entities;
using SheikhGoNexusERP.Domain.Enums;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

public class InventoryForecastService(
    AppDbContext db,
    ITenantContext tenant,
    IPermissionService permissions,
    IAuditService audit) : IInventoryForecastService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly InventoryTxnSourceType[] DemandSources =
    [
        InventoryTxnSourceType.Delivery,
        InventoryTxnSourceType.ProductionConsumption,
        InventoryTxnSourceType.FarmFeedIssue,
        InventoryTxnSourceType.FarmMedication
    ];
    private static readonly int[] Horizons = [7, 14, 21, 30];
    private const int LookbackDays = 28;

    public async Task<InventoryDemandForecastReportDto> GetDemandForecastAsync(
        Guid? productId = null,
        Guid? warehouseId = null,
        DateTime? asOf = null,
        Guid? companyId = null,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        await EnsurePredictiveAsync(ct);
        RejectScopeOverride(companyId, branchId);

        var asOfDate = (asOf ?? DateTime.UtcNow).Date;
        var company = tenant.CompanyId;
        var branch = tenant.BranchId;
        var from = asOfDate.AddDays(-(LookbackDays + 21)); // extra for anomaly baseline

        var warehouseIds = await ResolveWarehouseIdsAsync(warehouseId, branch, ct);

        var stockQ = db.WarehouseStocks.AsNoTracking().Include(x => x.Product).AsQueryable();
        if (warehouseIds is not null)
            stockQ = stockQ.Where(x => warehouseIds.Contains(x.WarehouseId));
        if (productId is Guid pid)
            stockQ = stockQ.Where(x => x.ProductId == pid);

        var stockRows = await stockQ.Where(x => x.Product != null && x.Product.IsActive).ToListAsync(ct);
        var byProduct = stockRows
            .GroupBy(x => x.ProductId)
            .Select(g =>
            {
                var p = g.First().Product!;
                return new
                {
                    ProductId = g.Key,
                    p.Code,
                    p.Name,
                    OnHand = g.Sum(x => x.Quantity),
                    p.ReorderLevel
                };
            })
            .OrderBy(x => x.Code)
            .ToList();

        var txnQ = db.InventoryTransactions.AsNoTracking()
            .Where(x => x.TxnDate >= from && x.TxnDate <= asOfDate && DemandSources.Contains(x.SourceType));
        if (company is Guid c) txnQ = txnQ.Where(x => x.CompanyId == c);
        if (branch is Guid b) txnQ = txnQ.Where(x => x.BranchId == b);
        if (warehouseIds is not null) txnQ = txnQ.Where(x => warehouseIds.Contains(x.WarehouseId));
        if (productId is Guid pOnly) txnQ = txnQ.Where(x => x.ProductId == pOnly);

        var txns = await txnQ.Select(x => new { x.ProductId, x.TxnDate, x.QtyOut }).ToListAsync(ct);

        var leadByProduct = await ComputeLeadTimesAsync(productId, ct);
        var tenantLead = leadByProduct.Values.Count > 0
            ? Median(leadByProduct.Values.Select(x => (decimal)x).ToList())
            : 7m;
        var defaultLead = Math.Max(1, (int)Math.Round(tenantLead));

        var onOrderByProduct = await ComputeOnOrderAsync(productId, ct);

        var lines = new List<InventoryDemandForecastLineDto>();
        foreach (var p in byProduct)
        {
            var productTxns = txns.Where(t => t.ProductId == p.ProductId).ToList();
            var daily = new Dictionary<DateTime, decimal>();
            for (var d = asOfDate.AddDays(-LookbackDays + 1); d <= asOfDate; d = d.AddDays(1))
                daily[d] = 0m;
            foreach (var t in productTxns.Where(t => t.TxnDate >= asOfDate.AddDays(-LookbackDays + 1)))
            {
                var day = t.TxnDate.Date;
                if (daily.ContainsKey(day))
                    daily[day] += t.QtyOut;
            }

            var activeDays = daily.Count(kv => kv.Value > 0);
            var avgDaily = daily.Values.Sum() / LookbackDays;
            var confidence = activeDays >= 21 ? "High"
                : activeDays >= 7 ? "Medium"
                : activeDays >= 3 ? "Low"
                : "InsufficientData";

            var lead = leadByProduct.TryGetValue(p.ProductId, out var lt) ? lt : defaultLead;
            var safety = Math.Max(p.ReorderLevel, avgDaily * lead);
            var onOrder = onOrderByProduct.GetValueOrDefault(p.ProductId, 0m);

            string? message = null;
            int? stockOutDays = null;
            var horizons = new List<ForecastHorizonPointDto>();
            decimal recommended = 0m;
            decimal? anomalyPct = null;

            if (confidence == "InsufficientData")
            {
                message = "Insufficient demand history for a reliable forecast (need at least 3 active demand days in the lookback window).";
                foreach (var h in Horizons)
                    horizons.Add(new ForecastHorizonPointDto(h, p.OnHand));
            }
            else
            {
                foreach (var h in Horizons)
                {
                    var projected = p.OnHand - avgDaily * h;
                    horizons.Add(new ForecastHorizonPointDto(h, Math.Round(projected, 4)));
                }

                for (var d = 1; d <= 90; d++)
                {
                    var projected = p.OnHand - avgDaily * d;
                    if (projected <= safety)
                    {
                        stockOutDays = d;
                        break;
                    }
                }

                recommended = Math.Max(0, Math.Round(avgDaily * (lead + 7) + safety - p.OnHand - onOrder, 0));

                // Anomaly: last 7 vs prior 21
                var last7 = productTxns.Where(t => t.TxnDate > asOfDate.AddDays(-7) && t.TxnDate <= asOfDate).Sum(t => t.QtyOut);
                var prior21 = productTxns.Where(t => t.TxnDate > asOfDate.AddDays(-28) && t.TxnDate <= asOfDate.AddDays(-7)).Sum(t => t.QtyOut);
                var baselineDaily = prior21 / 21m;
                var recentDaily = last7 / 7m;
                if (baselineDaily > 0)
                {
                    var pct = (recentDaily - baselineDaily) / baselineDaily * 100m;
                    if (Math.Abs(pct) >= 30m)
                        anomalyPct = Math.Round(pct, 1);
                }
            }

            lines.Add(new InventoryDemandForecastLineDto(
                p.ProductId, p.Code, p.Name, p.OnHand, p.ReorderLevel,
                Math.Round(avgDaily, 4), lead, Math.Round(safety, 4), onOrder,
                horizons, stockOutDays, recommended, anomalyPct, confidence, activeDays, message));
        }

        // Prefer riskiest first for chat/report
        lines = lines
            .OrderBy(x => x.Confidence == "InsufficientData" ? 1 : 0)
            .ThenBy(x => x.StockOutRiskDays ?? 999)
            .ThenByDescending(x => x.RecommendedOrderQty)
            .ThenBy(x => x.Code)
            .ToList();

        var report = new InventoryDemandForecastReportDto(asOfDate, company, branch, lines);
        var predictionId = await PersistPredictionAsync(report, productId, ct);
        return report with { PredictionId = predictionId };
    }

    private async Task EnsurePredictiveAsync(CancellationToken ct)
    {
        if (await permissions.HasAsync("Reports.Predictive.View", ct)) return;
        if (await permissions.HasAsync("Reports.Inventory.View", ct)) return;
        if (await permissions.HasAsync("Inventory.Reports.View", ct)) return;
        await permissions.EnsureAsync("Reports.Predictive.View", ct);
    }

    private void RejectScopeOverride(Guid? companyId, Guid? branchId)
    {
        if (companyId is Guid c && tenant.CompanyId is Guid tc && c != tc)
            throw new UnauthorizedAccessException("Cross-company AI actions are not allowed.");
        if (branchId is Guid b && tenant.BranchId is Guid tb && b != tb)
            throw new UnauthorizedAccessException("Cross-branch AI actions are not allowed.");
        if (companyId is Guid && tenant.CompanyId is null)
            throw new UnauthorizedAccessException("Company context required.");
        if (branchId is Guid && tenant.BranchId is null)
            throw new UnauthorizedAccessException("Branch context required.");
    }

    private async Task<List<Guid>?> ResolveWarehouseIdsAsync(Guid? warehouseId, Guid? branchId, CancellationToken ct)
    {
        if (warehouseId is Guid wid) return [wid];
        if (branchId is Guid bid)
        {
            return await db.Warehouses.AsNoTracking()
                .Where(w => w.BranchId == bid)
                .Select(w => w.Id)
                .ToListAsync(ct);
        }
        return null;
    }

    private async Task<Dictionary<Guid, int>> ComputeLeadTimesAsync(Guid? productId, CancellationToken ct)
    {
        var q =
            from grn in db.GoodsReceipts.AsNoTracking()
            where grn.PurchaseOrderId != null && grn.Status == DocumentStatus.Posted
            join po in db.PurchaseOrders.AsNoTracking() on grn.PurchaseOrderId equals po.Id
            join line in db.GoodsReceiptLines.AsNoTracking() on grn.Id equals line.GoodsReceiptId
            select new { line.ProductId, Days = (grn.ReceiptDate.Date - po.OrderDate.Date).TotalDays };

        if (productId is Guid pid)
            q = q.Where(x => x.ProductId == pid);

        var rows = await q.ToListAsync(ct);
        var result = new Dictionary<Guid, int>();
        foreach (var g in rows.Where(x => x.Days >= 0 && x.Days < 365).GroupBy(x => x.ProductId))
        {
            var days = g.Select(x => (decimal)x.Days).ToList();
            result[g.Key] = Math.Max(1, (int)Math.Round(Median(days)));
        }
        return result;
    }

    private async Task<Dictionary<Guid, decimal>> ComputeOnOrderAsync(Guid? productId, CancellationToken ct)
    {
        var q =
            from line in db.PurchaseOrderLines.AsNoTracking()
            join po in db.PurchaseOrders.AsNoTracking() on line.PurchaseOrderId equals po.Id
            where po.Status != DocumentStatus.Cancelled
            select new { line.ProductId, Remaining = line.Qty - line.ReceivedQty };

        if (productId is Guid pid)
            q = q.Where(x => x.ProductId == pid);

        var rows = await q.Where(x => x.Remaining > 0).ToListAsync(ct);
        return rows.GroupBy(x => x.ProductId).ToDictionary(g => g.Key, g => g.Sum(x => x.Remaining));
    }

    private async Task<Guid> PersistPredictionAsync(InventoryDemandForecastReportDto report, Guid? productId, CancellationToken ct)
    {
        if (tenant.UserId is null)
            return Guid.Empty;

        var summaryLines = report.Lines.Take(50).ToList();
        var entity = new AiPrediction
        {
            UserId = tenant.UserId.Value,
            CompanyId = tenant.CompanyId,
            BranchId = tenant.BranchId,
            PredictionType = "InventoryDemandForecast",
            ProductId = productId,
            HorizonDays = 30,
            Confidence = summaryLines.Count == 0 ? "InsufficientData"
                : summaryLines.Any(l => l.Confidence == "High") ? "High"
                : summaryLines.Any(l => l.Confidence == "Medium") ? "Medium"
                : summaryLines.Any(l => l.Confidence == "Low") ? "Low"
                : "InsufficientData",
            ResultJson = JsonSerializer.Serialize(new
            {
                report.AsOf,
                lineCount = report.Lines.Count,
                lines = summaryLines
            }, JsonOpts)
        };
        db.AiPredictions.Add(entity);
        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(nameof(AiPrediction), entity.Id, "AiPrediction", null,
            JsonSerializer.Serialize(new
            {
                success = true,
                entity.PredictionType,
                entity.Confidence,
                lineCount = report.Lines.Count,
                companyId = tenant.CompanyId,
                branchId = tenant.BranchId
            }, JsonOpts), ct);

        return entity.Id;
    }

    private static decimal Median(List<decimal> values)
    {
        if (values.Count == 0) return 0;
        var sorted = values.OrderBy(x => x).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2m : sorted[mid];
    }
}
