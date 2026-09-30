using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Entities;
using SheikhGoNexusERP.Domain.Enums;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

/// <summary>
/// Central GL posting. Domain workflows call this instead of writing journals directly.
/// Inventory valuation: Weighted Average, perpetual (PI updates avg cost; DO posts COGS).
/// </summary>
public class AccountingPostingService(
    AppDbContext db,
    IAuditService audit) : IAccountingPostingService
{
    private const decimal Tolerance = 0.01m;

    public async Task EnsurePeriodOpenAsync(DateTime postingDate, Guid companyId, Guid financialYearId, CancellationToken ct = default)
    {
        var fy = await db.FinancialYears.FirstOrDefaultAsync(x => x.Id == financialYearId, ct)
            ?? throw new InvalidOperationException("Financial year not found.");
        if (fy.IsClosed)
            throw new InvalidOperationException("Cannot post into a closed financial year.");

        await EnsureMonthlyPeriodsInternalAsync(fy, ct);

        var period = await db.FiscalPeriods
            .FirstOrDefaultAsync(x =>
                x.FinancialYearId == financialYearId &&
                x.StartDate <= postingDate.Date &&
                x.EndDate >= postingDate.Date, ct)
            ?? throw new InvalidOperationException($"No fiscal period covers {postingDate:yyyy-MM-dd}.");

        if (period.IsClosed)
            throw new InvalidOperationException($"Fiscal period '{period.Name}' is closed.");
    }

    public async Task PostSalesInvoiceAsync(SalesInvoice invoice, CancellationToken ct = default)
    {
        if (await AlreadyPostedAsync(JournalSourceType.SalesInvoice, invoice.Id, ct)) return;

        var ar = await AccountByCodeAsync("1100", ct);
        var sales = await AccountByCodeAsync("4100", ct);
        var amount = invoice.GrandTotal;
        var lines = new List<(Guid AccountId, decimal Debit, decimal Credit, string? Desc, Guid? PartyId, Guid? ProductId)>
        {
            (ar.Id, amount, 0, $"SI {invoice.InvoiceNo}", invoice.PartyId, null),
            (sales.Id, 0, amount, $"SI {invoice.InvoiceNo}", invoice.PartyId, null)
        };

        await CreatePostedJournalAsync(
            JournalSourceType.SalesInvoice, invoice.Id, invoice.InvoiceDate,
            $"Sales invoice {invoice.InvoiceNo}", lines, ct);

        var balance = await db.ReceivableBalances.FirstOrDefaultAsync(x => x.PartyId == invoice.PartyId, ct);
        if (balance is null)
        {
            balance = new ReceivableBalance { PartyId = invoice.PartyId };
            db.ReceivableBalances.Add(balance);
        }
        balance.Balance += invoice.GrandTotal;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(SalesInvoice), invoice.Id, "PostedEffects", null, $"AR +{invoice.GrandTotal}; GL posted", ct);
    }

    public async Task PostCustomerPaymentAsync(CustomerPayment payment, CancellationToken ct = default)
    {
        if (await AlreadyPostedAsync(JournalSourceType.CustomerPayment, payment.Id, ct)) return;

        payment = await db.CustomerPayments.Include(x => x.Applications).FirstAsync(x => x.Id == payment.Id, ct);
        var applied = payment.Applications.Sum(a => a.AmountApplied);
        foreach (var app in payment.Applications)
        {
            var invoice = await db.SalesInvoices.FirstAsync(x => x.Id == app.SalesInvoiceId, ct);
            var outstanding = invoice.GrandTotal - invoice.PaidAmount;
            if (app.AmountApplied > outstanding)
                throw new InvalidOperationException($"Payment exceeds outstanding for invoice {invoice.InvoiceNo}.");
            invoice.PaidAmount += app.AmountApplied;
        }

        var cashBank = await ResolveCashBankAsync(payment.CashBankAccountId, ct);
        var cashGl = await db.Accounts.FirstAsync(x => x.Id == cashBank.GlAccountId, ct);
        var ar = await AccountByCodeAsync("1100", ct);

        var lines = new List<(Guid AccountId, decimal Debit, decimal Credit, string? Desc, Guid? PartyId, Guid? ProductId)>
        {
            (cashGl.Id, applied, 0, $"CP {payment.PaymentNo}", payment.PartyId, null),
            (ar.Id, 0, applied, $"CP {payment.PaymentNo}", payment.PartyId, null)
        };

        var je = await CreatePostedJournalAsync(
            JournalSourceType.CustomerPayment, payment.Id, payment.PaymentDate,
            $"Customer payment {payment.PaymentNo}", lines, ct);

        var balance = await db.ReceivableBalances.FirstOrDefaultAsync(x => x.PartyId == payment.PartyId, ct)
            ?? throw new InvalidOperationException("No receivable balance for this customer.");
        if (balance.Balance < applied)
            throw new InvalidOperationException("Receivable balance is less than payment application.");
        balance.Balance -= applied;

        db.CashBankTransactions.Add(new CashBankTransaction
        {
            CashBankAccountId = cashBank.Id,
            PartyId = payment.PartyId,
            Amount = applied,
            TxnType = CashBankTxnType.Receipt,
            PostingDate = payment.PaymentDate,
            JournalEntryId = je.Id,
            Status = DocumentStatus.Posted,
            Narration = $"Customer payment {payment.PaymentNo}"
        });

        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(CustomerPayment), payment.Id, "PostedEffects", null, $"AR -{applied}; GL posted", ct);
    }

    public async Task PostPurchaseInvoiceAsync(PurchaseInvoice invoice, CancellationToken ct = default)
    {
        if (await AlreadyPostedAsync(JournalSourceType.PurchaseInvoice, invoice.Id, ct)) return;

        invoice = await db.PurchaseInvoices.Include(x => x.Lines).FirstAsync(x => x.Id == invoice.Id, ct);
        var inventory = await AccountByCodeAsync("1200", ct);
        var ap = await AccountByCodeAsync("2100", ct);
        var amount = invoice.GrandTotal;

        var lines = new List<(Guid AccountId, decimal Debit, decimal Credit, string? Desc, Guid? PartyId, Guid? ProductId)>
        {
            (inventory.Id, amount, 0, $"PI {invoice.InvoiceNo}", invoice.PartyId, null),
            (ap.Id, 0, amount, $"PI {invoice.InvoiceNo}", invoice.PartyId, null)
        };

        await CreatePostedJournalAsync(
            JournalSourceType.PurchaseInvoice, invoice.Id, invoice.InvoiceDate,
            $"Purchase invoice {invoice.InvoiceNo}", lines, ct);

        // Weighted average cost update (qty on hand before invoice add is current stock)
        foreach (var line in invoice.Lines)
        {
            var product = await db.Products.FirstAsync(x => x.Id == line.ProductId, ct);
            var onHand = await db.WarehouseStocks.Where(x => x.ProductId == line.ProductId).SumAsync(x => (decimal?)x.Quantity, ct) ?? 0m;
            // Stock already includes GRN qty; for avg cost treat invoice qty as the purchase layer
            var oldQty = Math.Max(0, onHand - line.Qty);
            var oldAvg = product.AverageCost > 0 ? product.AverageCost : product.PurchasePrice;
            var newQty = oldQty + line.Qty;
            if (newQty > 0)
                product.AverageCost = Math.Round((oldQty * oldAvg + line.Qty * line.Rate) / newQty, 4);
            else
                product.AverageCost = line.Rate;
        }

        var balance = await db.PayableBalances.FirstOrDefaultAsync(x => x.PartyId == invoice.PartyId, ct);
        if (balance is null)
        {
            balance = new PayableBalance { PartyId = invoice.PartyId };
            db.PayableBalances.Add(balance);
        }
        balance.Balance += invoice.GrandTotal;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(PurchaseInvoice), invoice.Id, "PostedEffects", null, $"AP +{invoice.GrandTotal}; Inventory GL; avg cost", ct);
    }

    public async Task PostSupplierPaymentAsync(SupplierPayment payment, CancellationToken ct = default)
    {
        if (await AlreadyPostedAsync(JournalSourceType.SupplierPayment, payment.Id, ct)) return;

        payment = await db.SupplierPayments.Include(x => x.Applications).FirstAsync(x => x.Id == payment.Id, ct);
        var applied = payment.Applications.Sum(a => a.AmountApplied);
        foreach (var app in payment.Applications)
        {
            var invoice = await db.PurchaseInvoices.FirstAsync(x => x.Id == app.PurchaseInvoiceId, ct);
            var outstanding = invoice.GrandTotal - invoice.PaidAmount;
            if (app.AmountApplied > outstanding)
                throw new InvalidOperationException($"Payment exceeds outstanding for invoice {invoice.InvoiceNo}.");
            invoice.PaidAmount += app.AmountApplied;
        }

        var cashBank = await ResolveCashBankAsync(payment.CashBankAccountId, ct);
        var cashGl = await db.Accounts.FirstAsync(x => x.Id == cashBank.GlAccountId, ct);
        var ap = await AccountByCodeAsync("2100", ct);

        var lines = new List<(Guid AccountId, decimal Debit, decimal Credit, string? Desc, Guid? PartyId, Guid? ProductId)>
        {
            (ap.Id, applied, 0, $"SPAY {payment.PaymentNo}", payment.PartyId, null),
            (cashGl.Id, 0, applied, $"SPAY {payment.PaymentNo}", payment.PartyId, null)
        };

        var je = await CreatePostedJournalAsync(
            JournalSourceType.SupplierPayment, payment.Id, payment.PaymentDate,
            $"Supplier payment {payment.PaymentNo}", lines, ct);

        var balance = await db.PayableBalances.FirstOrDefaultAsync(x => x.PartyId == payment.PartyId, ct)
            ?? throw new InvalidOperationException("No payable balance for this supplier.");
        if (balance.Balance < applied)
            throw new InvalidOperationException("Payable balance is less than payment application.");
        balance.Balance -= applied;

        db.CashBankTransactions.Add(new CashBankTransaction
        {
            CashBankAccountId = cashBank.Id,
            PartyId = payment.PartyId,
            Amount = applied,
            TxnType = CashBankTxnType.Payment,
            PostingDate = payment.PaymentDate,
            JournalEntryId = je.Id,
            Status = DocumentStatus.Posted,
            Narration = $"Supplier payment {payment.PaymentNo}"
        });

        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(SupplierPayment), payment.Id, "PostedEffects", null, $"AP -{applied}; GL posted", ct);
    }

    public async Task PostDeliveryCogsAsync(DeliveryOrder delivery, CancellationToken ct = default)
    {
        if (await AlreadyPostedAsync(JournalSourceType.DeliveryCogs, delivery.Id, ct)) return;

        delivery = await db.DeliveryOrders.Include(x => x.Lines).FirstAsync(x => x.Id == delivery.Id, ct);
        decimal cogsTotal = 0;
        var detailLines = new List<(Guid AccountId, decimal Debit, decimal Credit, string? Desc, Guid? PartyId, Guid? ProductId)>();
        var cogsAcct = await AccountByCodeAsync("5100", ct);
        var invAcct = await AccountByCodeAsync("1200", ct);

        foreach (var line in delivery.Lines)
        {
            var product = await db.Products.FirstAsync(x => x.Id == line.ProductId, ct);
            var unitCost = product.AverageCost > 0 ? product.AverageCost : product.PurchasePrice;
            var amount = Math.Round(unitCost * line.Qty, 2);
            cogsTotal += amount;
            detailLines.Add((cogsAcct.Id, amount, 0, $"COGS {delivery.DeliveryNo}", delivery.PartyId, line.ProductId));
            detailLines.Add((invAcct.Id, 0, amount, $"COGS {delivery.DeliveryNo}", delivery.PartyId, line.ProductId));
        }

        if (cogsTotal <= 0)
        {
            await audit.WriteAsync(nameof(DeliveryOrder), delivery.Id, "PostedEffects", null, "COGS skipped (zero cost)", ct);
            return;
        }

        // Collapse to two summary lines for cleaner journals
        var lines = new List<(Guid AccountId, decimal Debit, decimal Credit, string? Desc, Guid? PartyId, Guid? ProductId)>
        {
            (cogsAcct.Id, cogsTotal, 0, $"COGS {delivery.DeliveryNo}", delivery.PartyId, null),
            (invAcct.Id, 0, cogsTotal, $"COGS {delivery.DeliveryNo}", delivery.PartyId, null)
        };

        await CreatePostedJournalAsync(
            JournalSourceType.DeliveryCogs, delivery.Id, delivery.DeliveryDate,
            $"COGS delivery {delivery.DeliveryNo}", lines, ct);
        await audit.WriteAsync(nameof(DeliveryOrder), delivery.Id, "PostedEffects", null, $"COGS {cogsTotal}; Inventory GL -", ct);
    }

    public async Task PostStockAdjustmentAsync(StockAdjustment adjustment, CancellationToken ct = default)
    {
        if (await AlreadyPostedAsync(JournalSourceType.StockAdjustment, adjustment.Id, ct)) return;

        adjustment = await db.StockAdjustments.Include(x => x.Lines).FirstAsync(x => x.Id == adjustment.Id, ct);
        var invAcct = await AccountByCodeAsync("1200", ct);
        var adjAcct = await AccountByCodeAsync("5600", ct);
        decimal netDrInv = 0, netCrInv = 0;

        foreach (var line in adjustment.Lines)
        {
            if (line.QtyDelta == 0) continue;
            var product = await db.Products.FirstAsync(x => x.Id == line.ProductId, ct);
            var unitCost = product.AverageCost > 0 ? product.AverageCost : product.PurchasePrice;
            var amount = Math.Round(unitCost * Math.Abs(line.QtyDelta), 2);
            if (line.QtyDelta < 0)
                netCrInv += amount; // write-off: Cr Inventory
            else
                netDrInv += amount; // gain: Dr Inventory
        }

        if (netDrInv == 0 && netCrInv == 0) return;

        // Net inventory movement vs adjustment expense
        var net = netDrInv - netCrInv;
        var jeLines = new List<(Guid AccountId, decimal Debit, decimal Credit, string? Desc, Guid? PartyId, Guid? ProductId)>();
        if (net > 0)
        {
            jeLines.Add((invAcct.Id, net, 0, $"Adj {adjustment.AdjustmentNo}", null, null));
            jeLines.Add((adjAcct.Id, 0, net, $"Adj {adjustment.AdjustmentNo}", null, null));
        }
        else
        {
            var abs = Math.Abs(net);
            jeLines.Add((adjAcct.Id, abs, 0, $"Adj {adjustment.AdjustmentNo}", null, null));
            jeLines.Add((invAcct.Id, 0, abs, $"Adj {adjustment.AdjustmentNo}", null, null));
        }

        await CreatePostedJournalAsync(
            JournalSourceType.StockAdjustment, adjustment.Id, adjustment.AdjustmentDate,
            $"Stock adjustment {adjustment.AdjustmentNo}: {adjustment.Reason}", jeLines, ct);
        await audit.WriteAsync(nameof(StockAdjustment), adjustment.Id, "PostedEffects", null, "Inventory GL adjustment", ct);
    }

    public async Task PostPayrollRunAsync(PayrollRun run, CancellationToken ct = default)
    {
        if (await AlreadyPostedAsync(JournalSourceType.PayrollRun, run.Id, ct)) return;

        run = await db.PayrollRuns
            .Include(x => x.Employees).ThenInclude(e => e.Lines).ThenInclude(l => l.PayrollComponent)
            .FirstAsync(x => x.Id == run.Id, ct);

        if (run.NetTotal <= 0 && run.GrossTotal <= 0)
            throw new InvalidOperationException("Payroll run has no amounts to post.");

        var expense = await AccountByMappingAsync(GlMappingKeys.PayrollExpense, ct);
        var salaryPayable = await AccountByMappingAsync(GlMappingKeys.SalaryPayable, ct);
        var taxPayable = await AccountByMappingAsync(GlMappingKeys.TaxPayable, ct);
        var otherPayable = await AccountByMappingAsync(GlMappingKeys.OtherPayable, ct);

        decimal taxTotal = 0, otherTotal = 0;
        foreach (var emp in run.Employees)
        {
            foreach (var line in emp.Lines.Where(l => l.ComponentType == PayrollComponentType.Deduction))
            {
                var key = line.PayrollComponent?.GlMappingKey;
                if (string.Equals(key, GlMappingKeys.TaxPayable, StringComparison.OrdinalIgnoreCase))
                    taxTotal += line.Amount;
                else
                    otherTotal += line.Amount;
            }
        }

        var lines = new List<(Guid AccountId, decimal Debit, decimal Credit, string? Desc, Guid? PartyId, Guid? ProductId)>
        {
            (expense.Id, run.GrossTotal, 0, $"Payroll {run.RunNo}", null, null),
            (salaryPayable.Id, 0, run.NetTotal, $"Payroll {run.RunNo} net", null, null)
        };
        if (taxTotal > 0)
            lines.Add((taxPayable.Id, 0, taxTotal, $"Payroll {run.RunNo} tax", null, null));
        if (otherTotal > 0)
            lines.Add((otherPayable.Id, 0, otherTotal, $"Payroll {run.RunNo} other", null, null));

        await CreatePostedJournalAsync(
            JournalSourceType.PayrollRun, run.Id, run.RunDate,
            $"Payroll run {run.RunNo}", lines, ct);
        await audit.WriteAsync(nameof(PayrollRun), run.Id, "PostedEffects", null,
            $"Gross {run.GrossTotal}; Net payable {run.NetTotal}", ct);
    }

    public async Task PostSalaryPaymentAsync(SalaryPayment payment, CancellationToken ct = default)
    {
        if (await AlreadyPostedAsync(JournalSourceType.SalaryPayment, payment.Id, ct)) return;

        payment = await db.SalaryPayments.FirstAsync(x => x.Id == payment.Id, ct);
        var salaryPayable = await AccountByMappingAsync(GlMappingKeys.SalaryPayable, ct);
        var cashBank = await ResolveCashBankAsync(payment.CashBankAccountId, ct);
        var cashGl = await db.Accounts.FirstAsync(x => x.Id == cashBank.GlAccountId, ct);

        var lines = new List<(Guid AccountId, decimal Debit, decimal Credit, string? Desc, Guid? PartyId, Guid? ProductId)>
        {
            (salaryPayable.Id, payment.Amount, 0, $"Salary pay {payment.PaymentNo}", null, null),
            (cashGl.Id, 0, payment.Amount, $"Salary pay {payment.PaymentNo}", null, null)
        };

        await CreatePostedJournalAsync(
            JournalSourceType.SalaryPayment, payment.Id, payment.PaymentDate,
            $"Salary payment {payment.PaymentNo}", lines, ct);
        await audit.WriteAsync(nameof(SalaryPayment), payment.Id, "PostedEffects", null,
            $"Paid {payment.Amount} from {cashBank.Code}", ct);
    }

    public async Task PostProductionIssueAsync(ProductionOrder order, CancellationToken ct = default)
    {
        if (await AlreadyPostedAsync(JournalSourceType.ProductionIssue, order.Id, ct)) return;

        order = await db.ProductionOrders
            .Include(x => x.Components)
            .Include(x => x.Variances)
            .FirstAsync(x => x.Id == order.Id, ct);

        var material = order.Components.Sum(c => c.TotalCost)
            + order.Variances.Where(v => v.Kind == ProductionVarianceKind.Wastage).Sum(v => v.TotalCost);
        if (material <= 0) return;

        var wip = await AccountByMappingAsync(GlMappingKeys.WorkInProcess, ct);
        var inv = await AccountByMappingAsync(GlMappingKeys.Inventory, ct);
        var lines = new List<(Guid AccountId, decimal Debit, decimal Credit, string? Desc, Guid? PartyId, Guid? ProductId)>
        {
            (wip.Id, material, 0, $"MO issue {order.OrderNo}", null, null),
            (inv.Id, 0, material, $"MO issue {order.OrderNo}", null, null)
        };
        await CreatePostedJournalAsync(
            JournalSourceType.ProductionIssue, order.Id, order.OrderDate,
            $"Production issue {order.OrderNo}", lines, ct);
        await audit.WriteAsync(nameof(ProductionOrder), order.Id, "PostedIssue", null, $"WIP +{material}", ct);
    }

    public async Task PostProductionReceiptAsync(ProductionOrder order, CancellationToken ct = default)
    {
        if (await AlreadyPostedAsync(JournalSourceType.ProductionReceipt, order.Id, ct)) return;

        order = await db.ProductionOrders
            .Include(x => x.Outputs)
            .FirstAsync(x => x.Id == order.Id, ct);

        var total = order.TotalCost > 0
            ? order.TotalCost
            : order.Outputs.Where(o => !o.IsByProduct).Sum(o => o.TotalCost);
        if (total <= 0) return;

        var wip = await AccountByMappingAsync(GlMappingKeys.WorkInProcess, ct);
        var inv = await AccountByMappingAsync(GlMappingKeys.Inventory, ct);
        var lines = new List<(Guid AccountId, decimal Debit, decimal Credit, string? Desc, Guid? PartyId, Guid? ProductId)>
        {
            (inv.Id, total, 0, $"MO receipt {order.OrderNo}", null, null),
            (wip.Id, 0, total, $"MO receipt {order.OrderNo}", null, null)
        };
        await CreatePostedJournalAsync(
            JournalSourceType.ProductionReceipt, order.Id, order.OrderDate,
            $"Production receipt {order.OrderNo}", lines, ct);

        // Weighted-average FG cost (same formula as purchase invoice)
        foreach (var output in order.Outputs.Where(o => !o.IsByProduct && o.Qty > 0))
        {
            var product = await db.Products.FirstAsync(x => x.Id == output.ProductId, ct);
            var stocks = await db.WarehouseStocks.Where(x => x.ProductId == product.Id).ToListAsync(ct);
            var onHand = stocks.Sum(s => s.Quantity);
            var qtyBefore = onHand - output.Qty;
            if (qtyBefore < 0) qtyBefore = 0;
            var oldAvg = product.AverageCost > 0 ? product.AverageCost : product.PurchasePrice;
            var prodUnit = output.UnitCost > 0 ? output.UnitCost : order.UnitCost;
            if (qtyBefore + output.Qty > 0)
            {
                product.AverageCost = Math.Round(
                    ((qtyBefore * oldAvg) + (output.Qty * prodUnit)) / (qtyBefore + output.Qty), 4);
            }
            else
            {
                product.AverageCost = prodUnit;
            }
        }
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(ProductionOrder), order.Id, "PostedReceipt", null, $"FG +{total}; avg updated", ct);
    }

    public async Task PostFarmExpenseAsync(FarmExpense expense, CancellationToken ct = default)
    {
        if (await AlreadyPostedAsync(JournalSourceType.FarmExpense, expense.Id, ct)) return;
        if (expense.Amount <= 0) return;

        var farmExp = await AccountByMappingAsync(GlMappingKeys.FarmExpense, ct);
        Guid creditAccountId;
        if (expense.CashBankAccountId is Guid cbaId)
        {
            var cashBank = await ResolveCashBankAsync(cbaId, ct);
            creditAccountId = cashBank.GlAccountId;
        }
        else
        {
            creditAccountId = (await AccountByMappingAsync(GlMappingKeys.OtherPayable, ct)).Id;
        }

        var lines = new List<(Guid AccountId, decimal Debit, decimal Credit, string? Desc, Guid? PartyId, Guid? ProductId)>
        {
            (farmExp.Id, expense.Amount, 0, $"FEX {expense.ExpenseNo}", null, null),
            (creditAccountId, 0, expense.Amount, $"FEX {expense.ExpenseNo}", null, null)
        };
        await CreatePostedJournalAsync(
            JournalSourceType.FarmExpense, expense.Id, expense.ExpenseDate,
            $"Farm expense {expense.ExpenseNo}", lines, ct);
        await audit.WriteAsync(nameof(FarmExpense), expense.Id, "PostedEffects", null, $"Expense {expense.Amount}", ct);
    }

    private async Task<Account> AccountByMappingAsync(string mappingKey, CancellationToken ct)
    {
        var map = await db.GlAccountMappings
            .Include(x => x.Account)
            .FirstOrDefaultAsync(x => x.MappingKey == mappingKey && x.IsActive, ct)
            ?? throw new InvalidOperationException($"GL mapping '{mappingKey}' is not configured.");
        if (map.Account is null || !map.Account.IsPostable)
            throw new InvalidOperationException($"GL mapping '{mappingKey}' points to an invalid account.");
        return map.Account;
    }

    public async Task<JournalEntryDto> CreateManualJournalAsync(JournalEntryCreateDto dto, CancellationToken ct = default)
    {
        var mapped = dto.Lines.Select(l => (l.AccountId, l.Debit, l.Credit, l.Description, l.PartyId, l.ProductId)).ToList();
        ValidateBalanced(mapped);
        var status = dto.Post ? DocumentStatus.Posted : DocumentStatus.Draft;
        var je = await CreateJournalCoreAsync(
            JournalSourceType.Manual, null, dto.PostingDate, dto.Narration ?? "Manual journal", mapped, status, ct);
        return await MapJournalAsync(je.Id, ct) ?? throw new InvalidOperationException("Journal not found after create.");
    }

    private async Task<JournalEntry> CreatePostedJournalAsync(
        JournalSourceType sourceType,
        Guid sourceEntityId,
        DateTime postingDate,
        string narration,
        List<(Guid AccountId, decimal Debit, decimal Credit, string? Desc, Guid? PartyId, Guid? ProductId)> lines,
        CancellationToken ct)
    {
        ValidateBalanced(lines);
        return await CreateJournalCoreAsync(sourceType, sourceEntityId, postingDate, narration, lines, DocumentStatus.Posted, ct);
    }

    private async Task<JournalEntry> CreateJournalCoreAsync(
        JournalSourceType sourceType,
        Guid? sourceEntityId,
        DateTime postingDate,
        string narration,
        List<(Guid AccountId, decimal Debit, decimal Credit, string? Desc, Guid? PartyId, Guid? ProductId)> lines,
        DocumentStatus status,
        CancellationToken ct)
    {
        var company = await db.Companies.FirstAsync(ct);
        var branch = await db.Branches.FirstAsync(x => x.IsDefault || true, ct);
        var fy = await db.FinancialYears.FirstAsync(x => x.IsCurrent, ct);
        await EnsurePeriodOpenAsync(postingDate, company.Id, fy.Id, ct);

        var period = await db.FiscalPeriods.FirstAsync(x =>
            x.FinancialYearId == fy.Id && x.StartDate <= postingDate.Date && x.EndDate >= postingDate.Date, ct);

        var dt = await db.DocumentTypes.FirstAsync(x => x.Code == "JV", ct);
        var no = await DocumentNumbering.NextAsync(db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "JV", fy.StartDate.Year, ct);

        var totalDebit = lines.Sum(l => l.Debit);
        var totalCredit = lines.Sum(l => l.Credit);

        var document = new Document
        {
            DocumentTypeId = dt.Id,
            CompanyId = company.Id,
            BranchId = branch.Id,
            FinancialYearId = fy.Id,
            DocumentNo = no,
            DocumentDate = postingDate.Date,
            Narration = narration,
            Status = status
        };

        var je = new JournalEntry
        {
            Document = document,
            CompanyId = company.Id,
            BranchId = branch.Id,
            FinancialYearId = fy.Id,
            FiscalPeriodId = period.Id,
            JournalNo = no,
            PostingDate = postingDate.Date,
            SourceType = sourceType,
            SourceEntityId = sourceEntityId,
            Status = status,
            Narration = narration,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit
        };

        var lineNo = 1;
        foreach (var l in lines)
        {
            je.Lines.Add(new JournalEntryLine
            {
                LineNo = lineNo++,
                AccountId = l.AccountId,
                Debit = l.Debit,
                Credit = l.Credit,
                Description = l.Desc,
                PartyId = l.PartyId,
                ProductId = l.ProductId
            });
        }

        db.JournalEntries.Add(je);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(JournalEntry), je.Id, status == DocumentStatus.Posted ? "Posted" : "Created", null, je.JournalNo, ct);
        return je;
    }

    private static void ValidateBalanced(List<(Guid AccountId, decimal Debit, decimal Credit, string? Desc, Guid? PartyId, Guid? ProductId)> lines)
    {
        var debit = lines.Sum(l => l.Debit);
        var credit = lines.Sum(l => l.Credit);
        if (Math.Abs(debit - credit) > Tolerance)
            throw new InvalidOperationException($"Journal not balanced: Debit {debit} != Credit {credit}.");
        if (debit <= 0)
            throw new InvalidOperationException("Journal must have positive amounts.");
        foreach (var l in lines)
        {
            if (l.Debit < 0 || l.Credit < 0 || (l.Debit > 0 && l.Credit > 0))
                throw new InvalidOperationException("Each journal line must have either debit or credit, not both.");
        }
    }

    private async Task<bool> AlreadyPostedAsync(JournalSourceType sourceType, Guid sourceEntityId, CancellationToken ct) =>
        await db.JournalEntries.AnyAsync(x =>
            x.SourceType == sourceType &&
            x.SourceEntityId == sourceEntityId &&
            x.Status == DocumentStatus.Posted, ct);

    private async Task<Account> AccountByCodeAsync(string code, CancellationToken ct) =>
        await db.Accounts.FirstOrDefaultAsync(x => x.Code == code && x.IsPostable, ct)
        ?? throw new InvalidOperationException($"System account {code} not found. Ensure Chart of Accounts is seeded.");

    private async Task<CashBankAccount> ResolveCashBankAsync(Guid? cashBankAccountId, CancellationToken ct)
    {
        if (cashBankAccountId is Guid id)
            return await db.CashBankAccounts.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new InvalidOperationException("Cash/bank account not found.");
        return await db.CashBankAccounts.FirstOrDefaultAsync(x => x.IsDefault, ct)
            ?? await db.CashBankAccounts.FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No cash/bank account configured.");
    }

    private async Task EnsureMonthlyPeriodsInternalAsync(FinancialYear fy, CancellationToken ct)
    {
        if (await db.FiscalPeriods.AnyAsync(x => x.FinancialYearId == fy.Id, ct)) return;
        for (var m = 1; m <= 12; m++)
        {
            var start = new DateTime(fy.StartDate.Year, m, 1);
            var end = start.AddMonths(1).AddDays(-1);
            if (end > fy.EndDate) end = fy.EndDate;
            if (start > fy.EndDate) break;
            db.FiscalPeriods.Add(new FiscalPeriod
            {
                FinancialYearId = fy.Id,
                Name = start.ToString("MMMM yyyy"),
                PeriodNumber = m,
                StartDate = start,
                EndDate = end
            });
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task<JournalEntryDto?> MapJournalAsync(Guid id, CancellationToken ct) =>
        await db.JournalEntries.Include(x => x.Lines).ThenInclude(l => l.Account)
            .Where(x => x.Id == id)
            .Select(x => new JournalEntryDto(
                x.Id, x.JournalNo, x.PostingDate, x.SourceType, x.SourceEntityId, x.Status, x.Narration,
                x.TotalDebit, x.TotalCredit,
                x.Lines.OrderBy(l => l.LineNo).Select(l => new JournalLineDto(
                    l.Id, l.AccountId, l.Account!.Code, l.Account.Name, l.Debit, l.Credit, l.Description, l.PartyId, l.ProductId)).ToList()))
            .FirstOrDefaultAsync(ct);
}
