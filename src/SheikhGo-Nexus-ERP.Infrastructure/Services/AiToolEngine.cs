using SheikhGoNexusERP.Application.Interfaces;

namespace SheikhGoNexusERP.Infrastructure.Services;

public class AiToolEngine(
    IBiReportService bi,
    IReportService finance,
    IInventoryService inventory,
    IInventoryForecastService forecasts,
    IPermissionService permissions) : IAiToolEngine
{
    public async Task<AiToolResult> ExecuteAsync(AiIntentResolution intent, CancellationToken ct = default)
    {
        try
        {
            return intent.Intent switch
            {
                AiIntents.SalesSummary => await SalesAsync(intent, ct),
                AiIntents.CustomerOutstanding => await ArAsync(intent, ct),
                AiIntents.InventoryBalance => await InvAsync(ct),
                AiIntents.InventoryDemandForecast => await ForecastAsync(ct),
                AiIntents.PurchaseSummary => await PurchaseAsync(intent, ct),
                AiIntents.SupplierOutstanding => await ApAsync(intent, ct),
                AiIntents.TrialBalance => await TbAsync(intent, ct),
                AiIntents.ProfitLoss => await PlAsync(intent, ct),
                AiIntents.BalanceSheet => await BsAsync(intent, ct),
                _ => new AiToolResult("none", "Unsupported", null, Error: "No tool for intent.")
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            var perm = ex.Message.Replace("Missing permission: ", "", StringComparison.OrdinalIgnoreCase).Trim();
            return new AiToolResult("denied", "Access denied", null, Denied: true, DeniedPermission: perm);
        }
    }

    private async Task EnsureFinanceAsync(CancellationToken ct)
    {
        if (await permissions.HasAsync("Reports.Finance.View", ct)) return;
        if (await permissions.HasAsync("Finance.Reports.View", ct)) return;
        await permissions.EnsureAsync("Reports.Finance.View", ct);
    }

    private async Task EnsureInventoryAsync(CancellationToken ct)
    {
        if (await permissions.HasAsync("Reports.Inventory.View", ct)) return;
        if (await permissions.HasAsync("Inventory.Reports.View", ct)) return;
        if (await permissions.HasAsync("Reports.Predictive.View", ct)) return;
        await permissions.EnsureAsync("Reports.Inventory.View", ct);
    }

    private async Task<AiToolResult> ForecastAsync(CancellationToken ct)
    {
        // Service itself also Ensures predictive/inventory
        var report = await forecasts.GetDemandForecastAsync(null, null, null, null, null, ct);
        var top = report.Lines
            .OrderBy(l => l.StockOutRiskDays ?? 9999)
            .ThenByDescending(l => l.RecommendedOrderQty)
            .Take(15)
            .Select(l => new
            {
                l.Code,
                l.Name,
                l.OnHand,
                l.AvgDailyUsage,
                l.LeadTimeDays,
                l.SafetyProxy,
                l.StockOutRiskDays,
                l.RecommendedOrderQty,
                l.AnomalyPct,
                l.Confidence,
                l.DataDays,
                Horizons = l.Horizons,
                l.Message
            })
            .ToList();
        var payload = new
        {
            report.AsOf,
            report.PredictionId,
            LineCount = report.Lines.Count,
            TopRisk = top,
            Note = "Numbers are statistical FACTS. To prepare POs, ask the replenishment agent — this forecast does not create documents."
        };
        return new("getInventoryDemandForecast", "Inventory demand forecast", payload);
    }

    private async Task<AiToolResult> SalesAsync(AiIntentResolution intent, CancellationToken ct)
    {
        var data = await bi.SalesSummaryAsync(intent.From, intent.To, ct);
        return new("getSalesSummary", "Sales summary", data);
    }

    private async Task<AiToolResult> PurchaseAsync(AiIntentResolution intent, CancellationToken ct)
    {
        var data = await bi.PurchaseSummaryAsync(intent.From, intent.To, ct);
        return new("getPurchaseSummary", "Purchase summary", data);
    }

    private async Task<AiToolResult> ArAsync(AiIntentResolution intent, CancellationToken ct)
    {
        await EnsureFinanceAsync(ct);
        var aging = await finance.GetReceivablesAgingAsync(intent.AsOf, ct);
        var rows = aging.Rows;
        if (intent.AmountThreshold is decimal min)
            rows = rows.Where(r => r.Total >= min).ToList();
        var top = rows.OrderByDescending(r => r.Total).Take(15).ToList();
        var payload = new
        {
            aging.Kind,
            aging.AsOf,
            GrandTotal = rows.Sum(r => r.Total),
            Filtered = intent.AmountThreshold.HasValue,
            AmountThreshold = intent.AmountThreshold,
            TopCustomers = top.Select(r => new { r.PartyCode, r.PartyName, r.Total, r.Current, r.Days1To30, r.Days31To60, r.Days61To90, r.Days90Plus })
        };
        return new("getCustomerOutstanding", "Customer outstanding (AR)", payload);
    }

    private async Task<AiToolResult> ApAsync(AiIntentResolution intent, CancellationToken ct)
    {
        await EnsureFinanceAsync(ct);
        var aging = await finance.GetPayablesAgingAsync(intent.AsOf, ct);
        var rows = aging.Rows;
        if (intent.AmountThreshold is decimal min)
            rows = rows.Where(r => r.Total >= min).ToList();
        var top = rows.OrderByDescending(r => r.Total).Take(15).ToList();
        var payload = new
        {
            aging.Kind,
            aging.AsOf,
            GrandTotal = rows.Sum(r => r.Total),
            Filtered = intent.AmountThreshold.HasValue,
            AmountThreshold = intent.AmountThreshold,
            TopSuppliers = top.Select(r => new { r.PartyCode, r.PartyName, r.Total })
        };
        return new("getSupplierOutstanding", "Supplier outstanding (AP)", payload);
    }

    private async Task<AiToolResult> InvAsync(CancellationToken ct)
    {
        await EnsureInventoryAsync(ct);
        var lines = await inventory.GetValuationAsync(null, ct);
        var total = lines.Sum(x => x.Value);
        var top = lines.OrderByDescending(x => x.Value).Take(20)
            .Select(x => new { x.Code, x.Name, x.Qty, x.Value, x.WarehouseCode }).ToList();
        return new("getInventoryBalance", "Inventory valuation", new { TotalValue = total, LineCount = lines.Count, TopLines = top });
    }

    private async Task<AiToolResult> TbAsync(AiIntentResolution intent, CancellationToken ct)
    {
        await EnsureFinanceAsync(ct);
        var tb = await finance.GetTrialBalanceAsync(intent.AsOf, ct);
        var payload = new
        {
            tb.AsOf,
            tb.TotalDebit,
            tb.TotalCredit,
            Balanced = Math.Abs(tb.TotalDebit - tb.TotalCredit) < 0.05m,
            LineCount = tb.Lines.Count,
            Sample = tb.Lines.Take(25).Select(l => new { l.Code, l.Name, l.Debit, l.Credit })
        };
        return new("getTrialBalance", "Trial balance", payload);
    }

    private async Task<AiToolResult> PlAsync(AiIntentResolution intent, CancellationToken ct)
    {
        await EnsureFinanceAsync(ct);
        var pl = await finance.GetProfitLossAsync(intent.From, intent.To, ct);
        return new("getProfitLoss", "Profit & loss", new
        {
            pl.From,
            pl.To,
            pl.Revenue,
            pl.CostOfSales,
            pl.GrossProfit,
            pl.Expenses,
            pl.NetProfit
        });
    }

    private async Task<AiToolResult> BsAsync(AiIntentResolution intent, CancellationToken ct)
    {
        await EnsureFinanceAsync(ct);
        var bs = await finance.GetBalanceSheetAsync(intent.AsOf, ct);
        return new("getBalanceSheet", "Balance sheet", new
        {
            bs.AsOf,
            bs.TotalAssets,
            bs.TotalLiabilities,
            bs.TotalEquity,
            Balanced = Math.Abs(bs.TotalAssets - (bs.TotalLiabilities + bs.TotalEquity)) < 0.05m
        });
    }
}
