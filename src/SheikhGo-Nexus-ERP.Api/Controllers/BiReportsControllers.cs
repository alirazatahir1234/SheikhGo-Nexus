using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Infrastructure.Services;

namespace SheikhGoNexusERP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public class BiReportsController(
    IReportService finance,
    IBiReportService bi,
    IInventoryService inventory,
    IInventoryForecastService forecasts,
    IPermissionService permissions) : ControllerBase
{
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

    private async Task<ActionResult<T>> GuardAsync<T>(Func<Task<T>> run)
    {
        try { return Ok(await run()); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    private async Task<IActionResult> GuardResultAsync(Func<Task<IActionResult>> run)
    {
        try { return await run(); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpGet("cash-flow")]
    [HttpGet("finance/cash-flow")]
    public Task<ActionResult<CashFlowReportDto>> CashFlow([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(async () =>
        {
            await EnsureFinanceAsync(ct);
            var f = from ?? new DateTime(DateTime.UtcNow.Year, 1, 1);
            var t = to ?? DateTime.UtcNow.Date;
            return await finance.GetCashFlowAsync(f, t, ct);
        });

    [HttpGet("finance/trial-balance")]
    public Task<ActionResult<TrialBalanceReportDto>> FinTb([FromQuery] DateTime? asOf, CancellationToken ct) =>
        GuardAsync(async () =>
        {
            await EnsureFinanceAsync(ct);
            return await finance.GetTrialBalanceAsync(asOf ?? DateTime.UtcNow.Date, ct);
        });

    [HttpGet("finance/profit-loss")]
    public Task<ActionResult<ProfitLossReportDto>> FinPl([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(async () =>
        {
            await EnsureFinanceAsync(ct);
            return await finance.GetProfitLossAsync(from ?? new DateTime(DateTime.UtcNow.Year, 1, 1), to ?? DateTime.UtcNow.Date, ct);
        });

    [HttpGet("finance/balance-sheet")]
    public Task<ActionResult<BalanceSheetReportDto>> FinBs([FromQuery] DateTime? asOf, CancellationToken ct) =>
        GuardAsync(async () =>
        {
            await EnsureFinanceAsync(ct);
            return await finance.GetBalanceSheetAsync(asOf ?? DateTime.UtcNow.Date, ct);
        });

    [HttpGet("finance/aging/receivables")]
    public Task<ActionResult<AgingReportDto>> FinAr([FromQuery] DateTime? asOf, CancellationToken ct) =>
        GuardAsync(async () =>
        {
            await EnsureFinanceAsync(ct);
            return await finance.GetReceivablesAgingAsync(asOf, ct);
        });

    [HttpGet("finance/aging/payables")]
    public Task<ActionResult<AgingReportDto>> FinAp([FromQuery] DateTime? asOf, CancellationToken ct) =>
        GuardAsync(async () =>
        {
            await EnsureFinanceAsync(ct);
            return await finance.GetPayablesAgingAsync(asOf, ct);
        });

    // Sales
    [HttpGet("sales/summary")]
    public Task<ActionResult<NamedReportDto>> SalesSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.SalesSummaryAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("sales/by-customer")]
    public Task<ActionResult<NamedReportDto>> SalesCust([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.SalesByCustomerAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("sales/by-product")]
    public Task<ActionResult<NamedReportDto>> SalesProd([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.SalesByProductAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("sales/by-category")]
    public Task<ActionResult<NamedReportDto>> SalesCat([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.SalesByCategoryAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("sales/by-branch")]
    public Task<ActionResult<NamedReportDto>> SalesBr([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.SalesByBranchAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("sales/trend")]
    public Task<ActionResult<NamedReportDto>> SalesTrend([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.SalesTrendAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("sales/collections")]
    public Task<ActionResult<NamedReportDto>> SalesColl([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.CollectionsAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));

    // Procurement
    [HttpGet("procurement/summary")]
    public Task<ActionResult<NamedReportDto>> PurSum([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.PurchaseSummaryAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("procurement/by-supplier")]
    public Task<ActionResult<NamedReportDto>> PurSup([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.PurchaseBySupplierAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("procurement/by-product")]
    public Task<ActionResult<NamedReportDto>> PurProd([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.PurchaseByProductAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("procurement/outstanding")]
    public Task<ActionResult<NamedReportDto>> PurOut([FromQuery] DateTime? asOf, CancellationToken ct) =>
        GuardAsync(() => bi.SupplierOutstandingAsync(asOf, ct));
    [HttpGet("procurement/price-analysis")]
    public Task<ActionResult<NamedReportDto>> PurPrice([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.PurchasePriceAnalysisAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("procurement/grn-vs-invoice")]
    public Task<ActionResult<NamedReportDto>> PurGrn([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.GrnVsInvoiceAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("procurement/trend")]
    public Task<ActionResult<NamedReportDto>> PurTrend([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.PurchaseTrendAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));

    // Inventory analytics
    [HttpGet("inventory/valuation")]
    public Task<ActionResult<object>> InvVal([FromQuery] Guid? warehouseId, CancellationToken ct) =>
        GuardAsync(async () =>
        {
            await EnsureInventoryAsync(ct);
            return (object)await inventory.GetValuationAsync(warehouseId, ct);
        });
    [HttpGet("inventory/movement")]
    public Task<ActionResult<object>> InvMov([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? productId, [FromQuery] Guid? warehouseId, CancellationToken ct) =>
        GuardAsync(async () =>
        {
            await EnsureInventoryAsync(ct);
            return (object)await inventory.GetMovementAsync(productId, warehouseId, from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct);
        });
    [HttpGet("inventory/slow-moving")]
    public Task<ActionResult<object>> InvSlow([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(async () =>
        {
            await EnsureInventoryAsync(ct);
            return (object)await inventory.GetSlowMovingAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct: ct);
        });
    [HttpGet("inventory/fast-moving")]
    public Task<ActionResult<object>> InvFast([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(async () =>
        {
            await EnsureInventoryAsync(ct);
            return (object)await inventory.GetFastMovingAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct: ct);
        });
    [HttpGet("inventory/aging")]
    public Task<ActionResult<object>> InvAge([FromQuery] DateTime? asOf, CancellationToken ct) =>
        GuardAsync(async () =>
        {
            await EnsureInventoryAsync(ct);
            return (object)await inventory.GetStockAgingAsync(asOf, ct);
        });
    [HttpGet("inventory/warehouse-comparison")]
    public Task<ActionResult<object>> InvWh(CancellationToken ct) =>
        GuardAsync(async () =>
        {
            await EnsureInventoryAsync(ct);
            return (object)await inventory.GetWarehouseComparisonAsync(ct);
        });
    [HttpGet("inventory/adjustments")]
    public Task<ActionResult<object>> InvAdj([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(async () =>
        {
            await EnsureInventoryAsync(ct);
            return (object)await inventory.GetAdjustmentAnalysisAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct);
        });
    [HttpGet("inventory/turnover")]
    public Task<ActionResult<object>> InvTurn([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(async () =>
        {
            await EnsureInventoryAsync(ct);
            return (object)await inventory.GetTurnoverAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct);
        });

    [HttpGet("inventory/demand-forecast")]
    public Task<ActionResult<object>> InvDemandForecast(
        [FromQuery] Guid? productId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] DateTime? asOf,
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? branchId,
        CancellationToken ct) =>
        GuardAsync(async () =>
        {
            await EnsureInventoryAsync(ct);
            return (object)await forecasts.GetDemandForecastAsync(productId, warehouseId, asOf, companyId, branchId, ct);
        });

    // HR
    [HttpGet("hr/employees")]
    public Task<ActionResult<NamedReportDto>> HrEmp(CancellationToken ct) => GuardAsync(() => bi.HrEmployeeSummaryAsync(ct));
    [HttpGet("hr/attendance")]
    public Task<ActionResult<NamedReportDto>> HrAtt([FromQuery] int? year, [FromQuery] int? month, CancellationToken ct) =>
        GuardAsync(() => bi.HrAttendanceAsync(year ?? DateTime.UtcNow.Year, month ?? DateTime.UtcNow.Month, ct));
    [HttpGet("hr/leave")]
    public Task<ActionResult<NamedReportDto>> HrLeave([FromQuery] int? year, CancellationToken ct) =>
        GuardAsync(() => bi.HrLeaveAsync(year, ct));
    [HttpGet("hr/payroll")]
    public Task<ActionResult<NamedReportDto>> HrPay([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.HrPayrollAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("hr/department-cost")]
    public Task<ActionResult<NamedReportDto>> HrDept(CancellationToken ct) => GuardAsync(() => bi.HrDepartmentCostAsync(ct));
    [HttpGet("hr/overtime")]
    public Task<ActionResult<NamedReportDto>> HrOt([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.HrOvertimeAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));

    // Production
    [HttpGet("production/summary")]
    public Task<ActionResult<NamedReportDto>> PrSum([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.ProductionSummaryAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("production/order-status")]
    public Task<ActionResult<NamedReportDto>> PrStatus(CancellationToken ct) => GuardAsync(() => bi.ProductionOrderStatusAsync(ct));
    [HttpGet("production/consumption")]
    public Task<ActionResult<NamedReportDto>> PrCons([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.ProductionConsumptionAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("production/cost")]
    public Task<ActionResult<NamedReportDto>> PrCost([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.ProductionCostAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("production/yield")]
    public Task<ActionResult<NamedReportDto>> PrYield([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.ProductionYieldAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("production/finished-goods")]
    public Task<ActionResult<NamedReportDto>> PrFg([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.ProductionFgAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("production/wip")]
    public Task<ActionResult<NamedReportDto>> PrWip(CancellationToken ct) => GuardAsync(() => bi.ProductionWipAsync(ct));

    // Farm
    [HttpGet("farm/flocks")]
    public Task<ActionResult<NamedReportDto>> FarmFlocks(CancellationToken ct) => GuardAsync(() => bi.FarmFlockSummaryAsync(ct));
    [HttpGet("farm/mortality")]
    public Task<ActionResult<NamedReportDto>> FarmMort([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.FarmMortalityAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("farm/feed")]
    public Task<ActionResult<NamedReportDto>> FarmFeed([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.FarmFeedAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("farm/expenses")]
    public Task<ActionResult<NamedReportDto>> FarmExp([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.FarmExpensesAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("farm/performance")]
    public Task<ActionResult<NamedReportDto>> FarmPerf(CancellationToken ct) => GuardAsync(() => bi.FarmPerformanceAsync(ct));

    // Quality
    [HttpGet("quality/samples")]
    public Task<ActionResult<NamedReportDto>> QSamples([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.QualitySamplesAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("quality/results")]
    public Task<ActionResult<NamedReportDto>> QResults([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.QualityResultsAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("quality/pass-fail")]
    public Task<ActionResult<NamedReportDto>> QPf([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.QualityPassFailAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));

    // Operations
    [HttpGet("operations/weighments")]
    public Task<ActionResult<NamedReportDto>> OpsWb([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.OpsWeighmentSummaryAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("operations/vehicles")]
    public Task<ActionResult<NamedReportDto>> OpsVeh([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.OpsVehicleMovementAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("operations/gate-passes")]
    public Task<ActionResult<NamedReportDto>> OpsGp([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.OpsGatePassAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));

    // POS
    [HttpGet("pos/sales")]
    public Task<ActionResult<NamedReportDto>> PosSales([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.PosSalesAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("pos/payments")]
    public Task<ActionResult<NamedReportDto>> PosPay([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.PosPaymentsAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("pos/shifts")]
    public Task<ActionResult<NamedReportDto>> PosShifts([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.PosShiftsAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));
    [HttpGet("pos/products")]
    public Task<ActionResult<NamedReportDto>> PosProd([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        GuardAsync(() => bi.PosProductsAsync(from ?? MonthStart(), to ?? DateTime.UtcNow.Date, ct));

    [HttpGet("{domain}/{name}/export")]
    public Task<IActionResult> Export(string domain, string name, [FromQuery] string format = "csv",
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] DateTime? asOf = null, CancellationToken ct = default) =>
        GuardResultAsync(async () =>
        {
            var f = from ?? MonthStart();
            var t = to ?? DateTime.UtcNow.Date;
            NamedReportDto? report = (domain.ToLowerInvariant(), name.ToLowerInvariant()) switch
            {
                ("sales", "summary") => await bi.SalesSummaryAsync(f, t, ct),
                ("sales", "by-customer") => await bi.SalesByCustomerAsync(f, t, ct),
                ("sales", "by-product") => await bi.SalesByProductAsync(f, t, ct),
                ("sales", "by-category") => await bi.SalesByCategoryAsync(f, t, ct),
                ("sales", "by-branch") => await bi.SalesByBranchAsync(f, t, ct),
                ("sales", "trend") => await bi.SalesTrendAsync(f, t, ct),
                ("sales", "collections") => await bi.CollectionsAsync(f, t, ct),
                ("procurement", "summary") => await bi.PurchaseSummaryAsync(f, t, ct),
                ("procurement", "by-supplier") => await bi.PurchaseBySupplierAsync(f, t, ct),
                ("procurement", "by-product") => await bi.PurchaseByProductAsync(f, t, ct),
                ("procurement", "outstanding") => await bi.SupplierOutstandingAsync(asOf, ct),
                ("procurement", "price-analysis") => await bi.PurchasePriceAnalysisAsync(f, t, ct),
                ("procurement", "grn-vs-invoice") => await bi.GrnVsInvoiceAsync(f, t, ct),
                ("procurement", "trend") => await bi.PurchaseTrendAsync(f, t, ct),
                ("hr", "employees") => await bi.HrEmployeeSummaryAsync(ct),
                ("hr", "attendance") => await bi.HrAttendanceAsync(DateTime.UtcNow.Year, DateTime.UtcNow.Month, ct),
                ("hr", "leave") => await bi.HrLeaveAsync(null, ct),
                ("hr", "payroll") => await bi.HrPayrollAsync(f, t, ct),
                ("hr", "department-cost") => await bi.HrDepartmentCostAsync(ct),
                ("hr", "overtime") => await bi.HrOvertimeAsync(f, t, ct),
                ("production", "summary") => await bi.ProductionSummaryAsync(f, t, ct),
                ("production", "order-status") => await bi.ProductionOrderStatusAsync(ct),
                ("production", "consumption") => await bi.ProductionConsumptionAsync(f, t, ct),
                ("production", "cost") => await bi.ProductionCostAsync(f, t, ct),
                ("production", "yield") => await bi.ProductionYieldAsync(f, t, ct),
                ("production", "finished-goods") => await bi.ProductionFgAsync(f, t, ct),
                ("production", "wip") => await bi.ProductionWipAsync(ct),
                ("farm", "flocks") => await bi.FarmFlockSummaryAsync(ct),
                ("farm", "mortality") => await bi.FarmMortalityAsync(f, t, ct),
                ("farm", "feed") => await bi.FarmFeedAsync(f, t, ct),
                ("farm", "expenses") => await bi.FarmExpensesAsync(f, t, ct),
                ("farm", "performance") => await bi.FarmPerformanceAsync(ct),
                ("quality", "samples") => await bi.QualitySamplesAsync(f, t, ct),
                ("quality", "results") => await bi.QualityResultsAsync(f, t, ct),
                ("quality", "pass-fail") => await bi.QualityPassFailAsync(f, t, ct),
                ("operations", "weighments") => await bi.OpsWeighmentSummaryAsync(f, t, ct),
                ("operations", "vehicles") => await bi.OpsVehicleMovementAsync(f, t, ct),
                ("operations", "gate-passes") => await bi.OpsGatePassAsync(f, t, ct),
                ("pos", "sales") => await bi.PosSalesAsync(f, t, ct),
                ("pos", "payments") => await bi.PosPaymentsAsync(f, t, ct),
                ("pos", "shifts") => await bi.PosShiftsAsync(f, t, ct),
                ("pos", "products") => await bi.PosProductsAsync(f, t, ct),
                _ => null
            };
            if (report is null) return NotFound(new { message = "Unknown report for export." });
            var (bytes, contentType, fileName) = ReportExportHelper.ExportNamed(report, format);
            return File(bytes, contentType, fileName);
        });

    private static DateTime MonthStart() => new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
}
