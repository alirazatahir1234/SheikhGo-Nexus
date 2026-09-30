using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Enums;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

public class BiReportService(AppDbContext db, IPermissionService permissions, IReportService reports) : IBiReportService
{
    public Task EnsureAsync(string permission, CancellationToken ct = default) =>
        permissions.EnsureAsync(permission, ct);

    private static NamedReportDto R(string title, DateTime? from, DateTime? to, DateTime? asOf, IEnumerable<NamedAmountRowDto> rows) =>
        new(title, from, to, asOf, rows.Sum(x => x.Amount), rows.ToList(), null);

    private static NamedAmountRowDto Row(string code, string name, decimal amount, decimal? qty = null, string? extra = null) =>
        new(code, name, amount, qty, extra);

    public async Task<NamedReportDto> SalesSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Sales.View", ct);
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var yearStart = new DateTime(today.Year, 1, 1);
        var posted = db.SalesInvoices.Where(x => x.Status == DocumentStatus.Posted);
        var day = await posted.Where(x => x.InvoiceDate == today).SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0;
        var month = await posted.Where(x => x.InvoiceDate >= monthStart && x.InvoiceDate <= today).SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0;
        var year = await posted.Where(x => x.InvoiceDate >= yearStart && x.InvoiceDate <= today).SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0;
        var range = await posted.Where(x => x.InvoiceDate >= from.Date && x.InvoiceDate <= to.Date).SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0;
        return R("Sales Summary", from, to, null, [Row("TODAY", "Today", day), Row("MTD", "This month", month), Row("YTD", "This year", year), Row("RANGE", "Selected range", range)]);
    }

    public async Task<NamedReportDto> SalesByCustomerAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Sales.View", ct);
        var data = await db.SalesInvoices.Include(x => x.Party)
            .Where(x => x.Status == DocumentStatus.Posted && x.InvoiceDate >= from.Date && x.InvoiceDate <= to.Date)
            .GroupBy(x => new { x.Party!.Code, x.Party.Name })
            .Select(g => new { g.Key.Code, g.Key.Name, Amount = g.Sum(x => x.GrandTotal) })
            .OrderByDescending(x => x.Amount).ToListAsync(ct);
        return R("Sales by Customer", from, to, null, data.Select(x => Row(x.Code, x.Name, x.Amount)));
    }

    public async Task<NamedReportDto> SalesByProductAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Sales.View", ct);
        var data = await db.SalesInvoiceLines.Include(x => x.Product).Include(x => x.SalesInvoice)
            .Where(x => x.SalesInvoice!.Status == DocumentStatus.Posted && x.SalesInvoice.InvoiceDate >= from.Date && x.SalesInvoice.InvoiceDate <= to.Date)
            .GroupBy(x => new { x.Product!.Code, x.Product.Name })
            .Select(g => new { g.Key.Code, g.Key.Name, Amount = g.Sum(x => x.Amount), Qty = g.Sum(x => x.Qty) })
            .OrderByDescending(x => x.Amount).ToListAsync(ct);
        return R("Sales by Product", from, to, null, data.Select(x => Row(x.Code, x.Name, x.Amount, x.Qty)));
    }

    public async Task<NamedReportDto> SalesByCategoryAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Sales.View", ct);
        var data = await db.SalesInvoiceLines.Include(x => x.Product)!.ThenInclude(p => p!.Category).Include(x => x.SalesInvoice)
            .Where(x => x.SalesInvoice!.Status == DocumentStatus.Posted && x.SalesInvoice.InvoiceDate >= from.Date && x.SalesInvoice.InvoiceDate <= to.Date)
            .ToListAsync(ct);
        var rows = data.GroupBy(x => x.Product?.Category?.Name ?? "Uncategorized")
            .Select(g => Row(g.Key, g.Key, g.Sum(x => x.Amount), g.Sum(x => x.Qty)))
            .OrderByDescending(x => x.Amount);
        return R("Sales by Category", from, to, null, rows);
    }

    public async Task<NamedReportDto> SalesByBranchAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Sales.View", ct);
        var data = await db.SalesInvoices.Include(x => x.Document)!.ThenInclude(d => d!.Branch)
            .Where(x => x.Status == DocumentStatus.Posted && x.InvoiceDate >= from.Date && x.InvoiceDate <= to.Date)
            .ToListAsync(ct);
        var rows = data.GroupBy(x => x.Document?.Branch?.Code ?? "MAIN")
            .Select(g => Row(g.Key, g.Key, g.Sum(x => x.GrandTotal)))
            .OrderByDescending(x => x.Amount);
        return R("Sales by Branch", from, to, null, rows);
    }

    public async Task<NamedReportDto> SalesTrendAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Sales.View", ct);
        var data = await db.SalesInvoices
            .Where(x => x.Status == DocumentStatus.Posted && x.InvoiceDate >= from.Date && x.InvoiceDate <= to.Date)
            .GroupBy(x => x.InvoiceDate.Date)
            .Select(g => new { Day = g.Key, Amount = g.Sum(x => x.GrandTotal) })
            .OrderBy(x => x.Day).ToListAsync(ct);
        return R("Sales Trend", from, to, null, data.Select(x => Row(x.Day.ToString("yyyy-MM-dd"), x.Day.ToString("dd MMM"), x.Amount)));
    }

    public async Task<NamedReportDto> CollectionsAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Sales.View", ct);
        var data = await db.CustomerPayments.Include(x => x.Party)
            .Where(x => x.Status == DocumentStatus.Posted && x.PaymentDate >= from.Date && x.PaymentDate <= to.Date)
            .GroupBy(x => new { x.Party!.Code, x.Party.Name })
            .Select(g => new { g.Key.Code, g.Key.Name, Amount = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Amount).ToListAsync(ct);
        return R("Collection Analysis", from, to, null, data.Select(x => Row(x.Code, x.Name, x.Amount)));
    }

    public async Task<NamedReportDto> PurchaseSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Procurement.View", ct);
        var total = await db.PurchaseInvoices.Where(x => x.Status == DocumentStatus.Posted && x.InvoiceDate >= from.Date && x.InvoiceDate <= to.Date)
            .SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0;
        var grn = await db.GoodsReceipts.CountAsync(x => x.Status == DocumentStatus.Posted && x.ReceiptDate >= from.Date && x.ReceiptDate <= to.Date, ct);
        return R("Purchase Summary", from, to, null, [Row("PI", "Posted purchase invoices", total), Row("GRN", "Posted GRNs (count)", grn)]);
    }

    public async Task<NamedReportDto> PurchaseBySupplierAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Procurement.View", ct);
        var data = await db.PurchaseInvoices.Include(x => x.Party)
            .Where(x => x.Status == DocumentStatus.Posted && x.InvoiceDate >= from.Date && x.InvoiceDate <= to.Date)
            .GroupBy(x => new { x.Party!.Code, x.Party.Name })
            .Select(g => new { g.Key.Code, g.Key.Name, Amount = g.Sum(x => x.GrandTotal) })
            .OrderByDescending(x => x.Amount).ToListAsync(ct);
        return R("Purchase by Supplier", from, to, null, data.Select(x => Row(x.Code, x.Name, x.Amount)));
    }

    public async Task<NamedReportDto> PurchaseByProductAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Procurement.View", ct);
        var data = await db.PurchaseInvoiceLines.Include(x => x.Product).Include(x => x.PurchaseInvoice)
            .Where(x => x.PurchaseInvoice!.Status == DocumentStatus.Posted && x.PurchaseInvoice.InvoiceDate >= from.Date && x.PurchaseInvoice.InvoiceDate <= to.Date)
            .GroupBy(x => new { x.Product!.Code, x.Product.Name })
            .Select(g => new { g.Key.Code, g.Key.Name, Amount = g.Sum(x => x.Amount), Qty = g.Sum(x => x.Qty) })
            .OrderByDescending(x => x.Amount).ToListAsync(ct);
        return R("Purchase by Product", from, to, null, data.Select(x => Row(x.Code, x.Name, x.Amount, x.Qty)));
    }

    public async Task<NamedReportDto> SupplierOutstandingAsync(DateTime? asOf = null, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Procurement.View", ct);
        var aging = await reports.GetPayablesAgingAsync(asOf, ct);
        return R("Supplier Outstanding", null, null, aging.AsOf, aging.Rows.Select(r => Row(r.PartyCode, r.PartyName, r.Total)));
    }

    public async Task<NamedReportDto> PurchasePriceAnalysisAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Procurement.View", ct);
        var data = await db.PurchaseInvoiceLines.Include(x => x.Product).Include(x => x.PurchaseInvoice)
            .Where(x => x.PurchaseInvoice!.Status == DocumentStatus.Posted && x.PurchaseInvoice.InvoiceDate >= from.Date && x.PurchaseInvoice.InvoiceDate <= to.Date && x.Qty > 0)
            .GroupBy(x => new { x.Product!.Code, x.Product.Name })
            .Select(g => new { g.Key.Code, g.Key.Name, Amount = g.Sum(x => x.Amount), Qty = g.Sum(x => x.Qty) })
            .ToListAsync(ct);
        return R("Purchase Price Analysis", from, to, null, data.Select(x => Row(x.Code, x.Name, x.Amount / x.Qty, x.Qty, "avg rate")));
    }

    public async Task<NamedReportDto> GrnVsInvoiceAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Procurement.View", ct);
        var grnQty = await db.GoodsReceiptLines.Include(x => x.GoodsReceipt)
            .Where(x => x.GoodsReceipt!.Status == DocumentStatus.Posted && x.GoodsReceipt.ReceiptDate >= from.Date && x.GoodsReceipt.ReceiptDate <= to.Date)
            .SumAsync(x => (decimal?)x.Qty, ct) ?? 0;
        var piQty = await db.PurchaseInvoiceLines.Include(x => x.PurchaseInvoice)
            .Where(x => x.PurchaseInvoice!.Status == DocumentStatus.Posted && x.PurchaseInvoice.InvoiceDate >= from.Date && x.PurchaseInvoice.InvoiceDate <= to.Date)
            .SumAsync(x => (decimal?)x.Qty, ct) ?? 0;
        return R("GRN vs Invoice", from, to, null, [Row("GRN", "GRN qty", grnQty, grnQty), Row("PI", "Invoice qty", piQty, piQty), Row("VAR", "Variance (GRN-PI)", grnQty - piQty)]);
    }

    public async Task<NamedReportDto> PurchaseTrendAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Procurement.View", ct);
        var data = await db.PurchaseInvoices
            .Where(x => x.Status == DocumentStatus.Posted && x.InvoiceDate >= from.Date && x.InvoiceDate <= to.Date)
            .GroupBy(x => x.InvoiceDate.Date)
            .Select(g => new { Day = g.Key, Amount = g.Sum(x => x.GrandTotal) })
            .OrderBy(x => x.Day).ToListAsync(ct);
        return R("Purchase Trend", from, to, null, data.Select(x => Row(x.Day.ToString("yyyy-MM-dd"), x.Day.ToString("dd MMM"), x.Amount)));
    }

    public async Task<NamedReportDto> HrEmployeeSummaryAsync(CancellationToken ct = default)
    {
        await EnsureAsync("Reports.HR.View", ct);
        var active = await db.Employees.CountAsync(x => x.IsActive, ct);
        var inactive = await db.Employees.CountAsync(x => !x.IsActive, ct);
        var salary = await db.Employees.Where(x => x.IsActive).SumAsync(x => (decimal?)x.BasicSalary, ct) ?? 0;
        return R("Employee Summary", null, null, DateTime.UtcNow.Date, [Row("ACT", "Active", active), Row("INA", "Inactive", inactive), Row("BASIC", "Basic salary total", salary)]);
    }

    public async Task<NamedReportDto> HrAttendanceAsync(int year, int month, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.HR.View", ct);
        var data = await db.Attendances.Where(x => x.Date.Year == year && x.Date.Month == month)
            .GroupBy(x => x.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync(ct);
        return R($"Attendance {year}-{month:00}", new DateTime(year, month, 1), new DateTime(year, month, DateTime.DaysInMonth(year, month)), null,
            data.Select(x => Row(x.Status.ToString(), x.Status.ToString(), x.Count)));
    }

    public async Task<NamedReportDto> HrLeaveAsync(int? year = null, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.HR.View", ct);
        var y = year ?? DateTime.UtcNow.Year;
        var data = await db.LeaveRequests.Include(x => x.LeaveType).Where(x => x.FromDate.Year == y)
            .GroupBy(x => x.LeaveType!.Name).Select(g => new { Name = g.Key, Days = g.Sum(x => x.Days) }).ToListAsync(ct);
        return R($"Leave {y}", new DateTime(y, 1, 1), new DateTime(y, 12, 31), null, data.Select(x => Row(x.Name, x.Name, x.Days)));
    }

    public async Task<NamedReportDto> HrPayrollAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.HR.View", ct);
        var data = await db.PayrollRuns.Where(x => x.Status == DocumentStatus.Posted && x.RunDate >= from.Date && x.RunDate <= to.Date)
            .Select(x => new { x.RunNo, x.RunDate, x.NetTotal, x.GrossTotal }).ToListAsync(ct);
        return R("Payroll", from, to, null, data.Select(x => Row(x.RunNo, x.RunDate.ToString("yyyy-MM"), x.NetTotal, null, $"Gross={x.GrossTotal}")));
    }

    public async Task<NamedReportDto> HrDepartmentCostAsync(CancellationToken ct = default)
    {
        await EnsureAsync("Reports.HR.View", ct);
        var data = await db.Employees.Include(x => x.Department).Where(x => x.IsActive).ToListAsync(ct);
        var rows = data.GroupBy(x => x.Department?.Name ?? "Unassigned")
            .Select(g => Row(g.Key, g.Key, g.Sum(x => x.BasicSalary), g.Count()));
        return R("Department Cost", null, null, DateTime.UtcNow.Date, rows);
    }

    public async Task<NamedReportDto> HrOvertimeAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.HR.View", ct);
        var data = await db.Attendances.Include(x => x.Employee)
            .Where(x => x.Date >= from.Date && x.Date <= to.Date && x.OvertimeHours > 0)
            .GroupBy(x => new { x.Employee!.EmployeeCode, x.Employee.Name })
            .Select(g => new { g.Key.EmployeeCode, g.Key.Name, Hours = g.Sum(x => x.OvertimeHours ?? 0) })
            .OrderByDescending(x => x.Hours).ToListAsync(ct);
        return R("Overtime", from, to, null, data.Select(x => Row(x.EmployeeCode, x.Name, x.Hours)));
    }

    public async Task<NamedReportDto> ProductionSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Production.View", ct);
        var orders = await db.ProductionOrders.Where(x => x.OrderDate >= from.Date && x.OrderDate <= to.Date).ToListAsync(ct);
        var planned = orders.Sum(x => x.QtyOrdered);
        var produced = orders.Sum(x => x.QtyCompleted);
        var wastage = Math.Max(0, planned - produced);
        var yieldPct = planned == 0 ? 0 : Math.Round(produced / planned * 100m, 2);
        return R("Production Summary", from, to, null,
        [
            Row("PLANNED", "Planned", planned, planned),
            Row("PRODUCED", "Produced", produced, produced),
            Row("WASTE", "Wastage", wastage, wastage),
            Row("YIELD", "Yield %", yieldPct)
        ]);
    }

    public async Task<NamedReportDto> ProductionOrderStatusAsync(CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Production.View", ct);
        var data = await db.ProductionOrders.GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Qty = g.Sum(x => x.QtyOrdered) }).ToListAsync(ct);
        return R("Production Order Status", null, null, DateTime.UtcNow.Date, data.Select(x => Row(x.Status.ToString(), x.Status.ToString(), x.Count, x.Qty)));
    }

    public async Task<NamedReportDto> ProductionConsumptionAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Production.View", ct);
        var data = await db.ProductionOrderComponents.Include(x => x.Product).Include(x => x.ProductionOrder)
            .Where(x => x.ProductionOrder!.OrderDate >= from.Date && x.ProductionOrder.OrderDate <= to.Date)
            .GroupBy(x => new { x.Product!.Code, x.Product.Name })
            .Select(g => new { g.Key.Code, g.Key.Name, Amount = g.Sum(x => x.TotalCost), Qty = g.Sum(x => x.ActualQty) })
            .OrderByDescending(x => x.Amount).ToListAsync(ct);
        return R("Material Consumption", from, to, null, data.Select(x => Row(x.Code, x.Name, x.Amount, x.Qty)));
    }

    public async Task<NamedReportDto> ProductionCostAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Production.View", ct);
        var mat = await db.ProductionOrders.Where(x => x.OrderDate >= from.Date && x.OrderDate <= to.Date).SumAsync(x => (decimal?)x.MaterialCost, ct) ?? 0;
        var lab = await db.ProductionOrders.Where(x => x.OrderDate >= from.Date && x.OrderDate <= to.Date).SumAsync(x => (decimal?)x.LaborCost, ct) ?? 0;
        var oh = await db.ProductionOrders.Where(x => x.OrderDate >= from.Date && x.OrderDate <= to.Date).SumAsync(x => (decimal?)x.OverheadCost, ct) ?? 0;
        return R("Production Cost", from, to, null, [Row("MAT", "Material", mat), Row("LAB", "Labor", lab), Row("OH", "Overhead", oh), Row("TOT", "Total", mat + lab + oh)]);
    }

    public Task<NamedReportDto> ProductionYieldAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        ProductionSummaryAsync(from, to, ct);

    public async Task<NamedReportDto> ProductionFgAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Production.View", ct);
        var data = await db.ProductionOrderOutputs.Include(x => x.Product).Include(x => x.ProductionOrder)
            .Where(x => x.ProductionOrder!.OrderDate >= from.Date && x.ProductionOrder.OrderDate <= to.Date)
            .GroupBy(x => new { x.Product!.Code, x.Product.Name })
            .Select(g => new { g.Key.Code, g.Key.Name, Amount = g.Sum(x => x.TotalCost), Qty = g.Sum(x => x.Qty) })
            .OrderByDescending(x => x.Qty).ToListAsync(ct);
        return R("Finished Goods", from, to, null, data.Select(x => Row(x.Code, x.Name, x.Amount, x.Qty)));
    }

    public async Task<NamedReportDto> ProductionWipAsync(CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Production.View", ct);
        var statuses = new[] { ProductionOrderStatus.Released, ProductionOrderStatus.InProduction };
        var data = await db.ProductionOrders.Include(x => x.FinishedProduct).Where(x => statuses.Contains(x.Status)).ToListAsync(ct);
        return R("WIP", null, null, DateTime.UtcNow.Date,
            data.Select(x => Row(x.OrderNo, x.FinishedProduct?.Name ?? "", x.TotalCost, x.QtyOrdered - x.QtyCompleted, x.Status.ToString())));
    }

    public async Task<NamedReportDto> FarmFlockSummaryAsync(CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Farm.View", ct);
        var data = await db.Flocks.Include(x => x.Farm).ToListAsync(ct);
        return R("Flock Summary", null, null, DateTime.UtcNow.Date,
            data.Select(x => Row(x.BatchNo, x.Farm?.Name ?? "", x.QtyCurrent, x.QtyPlaced, x.Status.ToString())));
    }

    public async Task<NamedReportDto> FarmMortalityAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Farm.View", ct);
        var data = await db.MortalityRecords.Include(x => x.Flock)
            .Where(x => x.Date >= from.Date && x.Date <= to.Date)
            .GroupBy(x => x.Flock!.BatchNo)
            .Select(g => new { Batch = g.Key, Qty = g.Sum(x => x.Qty) }).ToListAsync(ct);
        return R("Mortality", from, to, null, data.Select(x => Row(x.Batch, x.Batch, x.Qty)));
    }

    public async Task<NamedReportDto> FarmFeedAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Farm.View", ct);
        var data = await db.FeedIssueLines.Include(x => x.FeedIssue).Include(x => x.Product)
            .Where(x => x.FeedIssue!.Status == DocumentStatus.Posted && x.FeedIssue.IssueDate >= from.Date && x.FeedIssue.IssueDate <= to.Date)
            .GroupBy(x => new { x.Product!.Code, x.Product.Name })
            .Select(g => new { g.Key.Code, g.Key.Name, Amount = g.Sum(x => x.TotalCost), Qty = g.Sum(x => x.Qty) })
            .ToListAsync(ct);
        return R("Feed Consumption", from, to, null, data.Select(x => Row(x.Code, x.Name, x.Amount, x.Qty)));
    }

    public async Task<NamedReportDto> FarmExpensesAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Farm.View", ct);
        var data = await db.FarmExpenses
            .Where(x => x.Status == DocumentStatus.Posted && x.ExpenseDate >= from.Date && x.ExpenseDate <= to.Date)
            .GroupBy(x => x.Category).Select(g => new { Cat = g.Key, Amount = g.Sum(x => x.Amount) }).ToListAsync(ct);
        return R("Farm Expenses", from, to, null, data.Select(x => Row(x.Cat.ToString(), x.Cat.ToString(), x.Amount)));
    }

    public async Task<NamedReportDto> FarmPerformanceAsync(CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Farm.View", ct);
        var flocks = await db.Flocks.AsNoTracking().ToListAsync(ct);
        var feedByFlock = await db.FeedIssueLines.AsNoTracking()
            .Where(x => x.FeedIssue!.Status == DocumentStatus.Posted)
            .GroupBy(x => x.FeedIssue!.FlockId)
            .Select(g => new { FlockId = g.Key, Amount = g.Sum(x => x.TotalCost) })
            .ToDictionaryAsync(x => x.FlockId, x => x.Amount, ct);
        var expByFlock = await db.FarmExpenses.AsNoTracking()
            .Where(x => x.Status == DocumentStatus.Posted && x.FlockId != null)
            .GroupBy(x => x.FlockId!.Value)
            .Select(g => new { FlockId = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.FlockId, x => x.Amount, ct);

        var rows = flocks.Select(f =>
        {
            feedByFlock.TryGetValue(f.Id, out var feed);
            expByFlock.TryGetValue(f.Id, out var exp);
            var mortPct = f.QtyPlaced == 0 ? 0 : Math.Round((decimal)f.QtyMortality / f.QtyPlaced * 100m, 2);
            var costPer = f.QtyCurrent == 0 ? 0 : (feed + exp) / f.QtyCurrent;
            return Row(f.BatchNo, $"Mort%={mortPct}", feed + exp, f.QtyCurrent, $"cost={costPer:N2}/bird");
        }).ToList();
        return R("Farm Performance", null, null, DateTime.UtcNow.Date, rows);
    }

    public async Task<NamedReportDto> QualitySamplesAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Quality.View", ct);
        var data = await db.LabSamples.Where(x => x.SampleDate >= from.Date && x.SampleDate <= to.Date)
            .GroupBy(x => x.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync(ct);
        return R("Sample Summary", from, to, null, data.Select(x => Row(x.Status.ToString(), x.Status.ToString(), x.Count)));
    }

    public async Task<NamedReportDto> QualityResultsAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Quality.View", ct);
        var data = await db.LabTestResults.Include(x => x.TestType).Include(x => x.Sample)
            .Where(x => x.Sample!.SampleDate >= from.Date && x.Sample.SampleDate <= to.Date).ToListAsync(ct);
        return R("Test Results", from, to, null, data.Select(x => Row(x.TestType?.Code ?? "", x.TestType?.Name ?? "", x.Value, null, x.Decision.ToString())));
    }

    public async Task<NamedReportDto> QualityPassFailAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Quality.View", ct);
        var data = await db.LabTestResults.Include(x => x.Sample)
            .Where(x => x.Sample!.SampleDate >= from.Date && x.Sample.SampleDate <= to.Date)
            .GroupBy(x => x.Decision).Select(g => new { Decision = g.Key, Count = g.Count() }).ToListAsync(ct);
        return R("Pass/Fail/Hold", from, to, null, data.Select(x => Row(x.Decision.ToString(), x.Decision.ToString(), x.Count)));
    }

    public async Task<NamedReportDto> OpsWeighmentSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Operations.View", ct);
        var q = db.Weighments.Where(x => x.CreatedAt.Date >= from.Date && x.CreatedAt.Date <= to.Date);
        var count = await q.CountAsync(ct);
        var completed = await q.CountAsync(x => x.Status == WeighmentStatus.Completed, ct);
        var net = await q.Where(x => x.Status == WeighmentStatus.Completed).SumAsync(x => (decimal?)(x.NetWeight ?? 0), ct) ?? 0;
        var inward = await q.CountAsync(x => x.Direction == WeighmentDirection.Inward, ct);
        var outward = await q.CountAsync(x => x.Direction == WeighmentDirection.Outward, ct);
        return R("Weighment Summary", from, to, null,
        [
            Row("ALL", "Tickets", count), Row("DONE", "Completed", completed),
            Row("IN", "Inward", inward), Row("OUT", "Outward", outward), Row("NET", "Net weight", net, net, "kg")
        ]);
    }

    public async Task<NamedReportDto> OpsVehicleMovementAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Operations.View", ct);
        var data = await db.Weighments.Where(x => x.CreatedAt.Date >= from.Date && x.CreatedAt.Date <= to.Date)
            .GroupBy(x => x.VehicleNo)
            .Select(g => new { Vehicle = g.Key, Net = g.Sum(x => x.NetWeight ?? 0), Count = g.Count() })
            .OrderByDescending(x => x.Count).ToListAsync(ct);
        return R("Vehicle Movement", from, to, null, data.Select(x => Row(x.Vehicle, x.Vehicle, x.Net, x.Count)));
    }

    public async Task<NamedReportDto> OpsGatePassAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.Operations.View", ct);
        var data = await db.GatePasses.Where(x => x.PassDate >= from.Date && x.PassDate <= to.Date)
            .GroupBy(x => x.Direction).Select(g => new { Dir = g.Key, Count = g.Count() }).ToListAsync(ct);
        return R("Gate Pass", from, to, null, data.Select(x => Row(x.Dir.ToString(), x.Dir.ToString(), x.Count)));
    }

    public async Task<NamedReportDto> PosSalesAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.POS.View", ct);
        var total = await db.PosSales.Where(x => x.Status == DocumentStatus.Posted && x.SaleDate >= from.Date && x.SaleDate <= to.Date)
            .SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0;
        var count = await db.PosSales.CountAsync(x => x.Status == DocumentStatus.Posted && x.SaleDate >= from.Date && x.SaleDate <= to.Date, ct);
        return R("POS Sales", from, to, null, [Row("SALES", "Posted sales", total, count)]);
    }

    public async Task<NamedReportDto> PosPaymentsAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.POS.View", ct);
        var data = await db.PosPayments.Include(x => x.PosSale)
            .Where(x => x.PosSale!.Status == DocumentStatus.Posted && x.PosSale.SaleDate >= from.Date && x.PosSale.SaleDate <= to.Date)
            .GroupBy(x => x.Tender).Select(g => new { Tender = g.Key, Amount = g.Sum(x => x.Amount) }).ToListAsync(ct);
        return R("POS Payment Methods", from, to, null, data.Select(x => Row(x.Tender.ToString(), x.Tender.ToString(), x.Amount)));
    }

    public async Task<NamedReportDto> PosShiftsAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.POS.View", ct);
        var data = await db.PosShifts.Include(x => x.Terminal)
            .Where(x => x.OpenedAt.Date >= from.Date && x.OpenedAt.Date <= to.Date).ToListAsync(ct);
        return R("POS Shift Summary", from, to, null,
            data.Select(x => Row(x.ShiftNo, x.Terminal?.Code ?? "", x.ExpectedCash ?? x.OpeningCash, x.ActualCash, $"Var={x.Variance ?? 0}")));
    }

    public async Task<NamedReportDto> PosProductsAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await EnsureAsync("Reports.POS.View", ct);
        var data = await db.PosSaleLines.Include(x => x.Product).Include(x => x.PosSale)
            .Where(x => x.PosSale!.Status == DocumentStatus.Posted && x.PosSale.SaleDate >= from.Date && x.PosSale.SaleDate <= to.Date)
            .GroupBy(x => new { x.Product!.Code, x.Product.Name })
            .Select(g => new { g.Key.Code, g.Key.Name, Amount = g.Sum(x => x.Amount), Qty = g.Sum(x => x.Qty) })
            .OrderByDescending(x => x.Amount).ToListAsync(ct);
        return R("POS Product Sales", from, to, null, data.Select(x => Row(x.Code, x.Name, x.Amount, x.Qty)));
    }
}
