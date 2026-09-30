using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Enums;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

public class ReportService(AppDbContext db) : IReportService
{
    public async Task<LedgerReportDto> GetLedgerAsync(Guid accountId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(x => x.Id == accountId, ct)
            ?? throw new InvalidOperationException("Account not found.");

        var openingDebit = await db.JournalEntryLines
            .Where(l => l.AccountId == accountId && l.JournalEntry!.Status == DocumentStatus.Posted && l.JournalEntry.PostingDate < from.Date)
            .SumAsync(l => (decimal?)l.Debit, ct) ?? 0;
        var openingCredit = await db.JournalEntryLines
            .Where(l => l.AccountId == accountId && l.JournalEntry!.Status == DocumentStatus.Posted && l.JournalEntry.PostingDate < from.Date)
            .SumAsync(l => (decimal?)l.Credit, ct) ?? 0;
        var opening = OpeningFor(account.AccountType, openingDebit, openingCredit);

        var rows = await db.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.AccountId == accountId &&
                        l.JournalEntry!.Status == DocumentStatus.Posted &&
                        l.JournalEntry.PostingDate >= from.Date &&
                        l.JournalEntry.PostingDate <= to.Date)
            .OrderBy(l => l.JournalEntry!.PostingDate).ThenBy(l => l.JournalEntry!.JournalNo).ThenBy(l => l.LineNo)
            .Select(l => new { l.JournalEntry!.PostingDate, l.JournalEntry.JournalNo, l.JournalEntry.Narration, l.Debit, l.Credit })
            .ToListAsync(ct);

        var balance = opening;
        var lines = new List<LedgerLineDto>();
        foreach (var r in rows)
        {
            balance = RunningBalance(account.AccountType, balance, r.Debit, r.Credit);
            lines.Add(new LedgerLineDto(r.PostingDate, r.JournalNo, "JV", r.Narration, r.Debit, r.Credit, balance));
        }

        return new LedgerReportDto(account.Id, account.Code, account.Name, from.Date, to.Date, opening, balance, lines);
    }

    public async Task<TrialBalanceReportDto> GetTrialBalanceAsync(DateTime asOf, CancellationToken ct = default)
    {
        var accounts = await db.Accounts.Where(x => x.IsPostable).OrderBy(x => x.Code).ToListAsync(ct);
        var aggs = await db.JournalEntryLines
            .Where(l => l.JournalEntry!.Status == DocumentStatus.Posted && l.JournalEntry.PostingDate <= asOf.Date)
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .ToListAsync(ct);
        var map = aggs.ToDictionary(x => x.AccountId);

        var lines = new List<TrialBalanceLineDto>();
        decimal totalDr = 0, totalCr = 0;
        foreach (var a in accounts)
        {
            map.TryGetValue(a.Id, out var m);
            var dr = m?.Debit ?? 0;
            var cr = m?.Credit ?? 0;
            if (dr == 0 && cr == 0 && a.OpeningBalance == 0) continue;
            var netDr = dr > cr ? dr - cr : 0;
            var netCr = cr > dr ? cr - dr : 0;
            if (a.OpeningBalance != 0)
            {
                if (IsDebitNormal(a.AccountType))
                {
                    if (a.OpeningBalance > 0) netDr += a.OpeningBalance;
                    else netCr += -a.OpeningBalance;
                }
                else
                {
                    if (a.OpeningBalance > 0) netCr += a.OpeningBalance;
                    else netDr += -a.OpeningBalance;
                }
            }
            var bal = OpeningFor(a.AccountType, dr + (IsDebitNormal(a.AccountType) && a.OpeningBalance > 0 ? a.OpeningBalance : 0),
                cr + (!IsDebitNormal(a.AccountType) && a.OpeningBalance > 0 ? a.OpeningBalance : 0));
            lines.Add(new TrialBalanceLineDto(a.Id, a.Code, a.Name, a.AccountType.ToString(), netDr, netCr, bal));
            totalDr += netDr;
            totalCr += netCr;
        }

        return new TrialBalanceReportDto(asOf.Date, totalDr, totalCr, lines);
    }

    public async Task<ProfitLossReportDto> GetProfitLossAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var cogsAccountIds = await ResolveCogsAccountIdsAsync(ct);
        var accounts = await db.Accounts.Where(x => x.IsPostable &&
            (x.AccountType == AccountType.Revenue || x.AccountType == AccountType.Expense)).ToListAsync(ct);
        var aggs = await db.JournalEntryLines
            .Where(l => l.JournalEntry!.Status == DocumentStatus.Posted &&
                        l.JournalEntry.PostingDate >= from.Date &&
                        l.JournalEntry.PostingDate <= to.Date)
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .ToListAsync(ct);
        var map = aggs.ToDictionary(x => x.AccountId);

        var revenueLines = new List<ProfitLossLineDto>();
        var cogsLines = new List<ProfitLossLineDto>();
        var expenseLines = new List<ProfitLossLineDto>();
        decimal revenue = 0, cogs = 0, expenses = 0;

        foreach (var a in accounts.OrderBy(x => x.Code))
        {
            map.TryGetValue(a.Id, out var m);
            var amount = a.AccountType == AccountType.Revenue
                ? (m?.Credit ?? 0) - (m?.Debit ?? 0)
                : (m?.Debit ?? 0) - (m?.Credit ?? 0);
            if (amount == 0) continue;
            if (a.AccountType == AccountType.Revenue)
            {
                revenue += amount;
                revenueLines.Add(new ProfitLossLineDto(a.Code, a.Name, amount));
            }
            else if (cogsAccountIds.Contains(a.Id) || a.Code == "5100")
            {
                cogs += amount;
                cogsLines.Add(new ProfitLossLineDto(a.Code, a.Name, amount));
            }
            else
            {
                expenses += amount;
                expenseLines.Add(new ProfitLossLineDto(a.Code, a.Name, amount));
            }
        }

        var gross = revenue - cogs;
        var net = gross - expenses;
        return new ProfitLossReportDto(from.Date, to.Date, revenue, cogs, gross, expenses, net, revenueLines, cogsLines, expenseLines);
    }

    public async Task<BalanceSheetReportDto> GetBalanceSheetAsync(DateTime asOf, CancellationToken ct = default)
    {
        var accounts = await db.Accounts.Where(x => x.IsPostable).OrderBy(x => x.Code).ToListAsync(ct);
        var aggs = await db.JournalEntryLines
            .Where(l => l.JournalEntry!.Status == DocumentStatus.Posted && l.JournalEntry.PostingDate <= asOf.Date)
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .ToListAsync(ct);
        var map = aggs.ToDictionary(x => x.AccountId);

        var assets = new List<BalanceSheetLineDto>();
        var liabilities = new List<BalanceSheetLineDto>();
        var equity = new List<BalanceSheetLineDto>();
        decimal totalA = 0, totalL = 0, totalE = 0;

        var fyStart = await db.FinancialYears.Where(x => x.IsCurrent)
            .Select(x => (DateTime?)x.StartDate).FirstOrDefaultAsync(ct)
            ?? new DateTime(asOf.Year, 1, 1);
        if (fyStart > asOf.Date) fyStart = new DateTime(asOf.Year, 1, 1);
        var pl = await GetProfitLossAsync(fyStart, asOf, ct);

        foreach (var a in accounts)
        {
            if (a.AccountType is AccountType.Revenue or AccountType.Expense) continue;
            map.TryGetValue(a.Id, out var m);
            var bal = OpeningFor(a.AccountType, (m?.Debit ?? 0) + (IsDebitNormal(a.AccountType) ? a.OpeningBalance : 0),
                (m?.Credit ?? 0) + (!IsDebitNormal(a.AccountType) ? a.OpeningBalance : 0));
            if (bal == 0) continue;
            var line = new BalanceSheetLineDto(a.Code, a.Name, bal);
            switch (a.AccountType)
            {
                case AccountType.Asset:
                    assets.Add(line); totalA += bal; break;
                case AccountType.Liability:
                    liabilities.Add(line); totalL += bal; break;
                case AccountType.Equity:
                    equity.Add(line); totalE += bal; break;
            }
        }

        if (pl.NetProfit != 0)
        {
            equity.Add(new BalanceSheetLineDto("RE", "Retained Earnings (FYTD)", pl.NetProfit));
            totalE += pl.NetProfit;
        }

        return new BalanceSheetReportDto(asOf.Date, totalA, totalL, totalE, assets, liabilities, equity);
    }

    public async Task<CashFlowReportDto> GetCashFlowAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var cashBankIds = await db.CashBankAccounts.Where(x => x.IsActive).Select(x => x.Id).ToListAsync(ct);
        var glIds = await db.CashBankAccounts.Where(x => x.IsActive).Select(x => x.GlAccountId).Distinct().ToListAsync(ct);

        var openingReceipts = await db.CashBankTransactions
            .Where(x => cashBankIds.Contains(x.CashBankAccountId) && x.Status == DocumentStatus.Posted && x.PostingDate < from.Date && x.TxnType == CashBankTxnType.Receipt)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        var openingPayments = await db.CashBankTransactions
            .Where(x => cashBankIds.Contains(x.CashBankAccountId) && x.Status == DocumentStatus.Posted && x.PostingDate < from.Date && x.TxnType == CashBankTxnType.Payment)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        var opening = openingReceipts - openingPayments;

        var period = await db.CashBankTransactions
            .Include(x => x.CashBankAccount)
            .Where(x => cashBankIds.Contains(x.CashBankAccountId) && x.Status == DocumentStatus.Posted
                        && x.PostingDate >= from.Date && x.PostingDate <= to.Date)
            .ToListAsync(ct);

        var inflows = period.Where(x => x.TxnType == CashBankTxnType.Receipt).Sum(x => x.Amount);
        var outflows = period.Where(x => x.TxnType == CashBankTxnType.Payment).Sum(x => x.Amount);
        var transfers = period.Where(x => x.TxnType == CashBankTxnType.Transfer).Sum(x => x.Amount);

        // Also include GL movements on cash/bank accounts not already reflected (journals)
        var glIn = await db.JournalEntryLines
            .Where(l => glIds.Contains(l.AccountId) && l.JournalEntry!.Status == DocumentStatus.Posted
                        && l.JournalEntry.PostingDate >= from.Date && l.JournalEntry.PostingDate <= to.Date)
            .SumAsync(l => (decimal?)l.Debit, ct) ?? 0;
        var glOut = await db.JournalEntryLines
            .Where(l => glIds.Contains(l.AccountId) && l.JournalEntry!.Status == DocumentStatus.Posted
                        && l.JournalEntry.PostingDate >= from.Date && l.JournalEntry.PostingDate <= to.Date)
            .SumAsync(l => (decimal?)l.Credit, ct) ?? 0;

        // Prefer cash-bank book as operational cash flow; use max of cashbook vs GL delta awareness in lines
        var lines = new List<CashFlowLineDto>
        {
            new("Inflow", "Cash/Bank receipts", inflows),
            new("Outflow", "Cash/Bank payments", outflows),
            new("Transfer", "Internal transfers", transfers),
            new("GL", "Cash/Bank GL debits (period)", glIn),
            new("GL", "Cash/Bank GL credits (period)", glOut)
        };

        var net = inflows - outflows;
        var closing = opening + net;
        return new CashFlowReportDto(from.Date, to.Date, opening, inflows, outflows, net, closing, lines);
    }

    public async Task<AgingReportDto> GetReceivablesAgingAsync(DateTime? asOf = null, CancellationToken ct = default)
    {
        var date = (asOf ?? DateTime.UtcNow).Date;
        var invoices = await db.SalesInvoices
            .Include(x => x.Party)
            .Where(x => x.Status == DocumentStatus.Posted && x.GrandTotal > x.PaidAmount)
            .ToListAsync(ct);
        var rows = invoices.GroupBy(x => x.PartyId).Select(g =>
        {
            var party = g.First().Party!;
            decimal cur = 0, d30 = 0, d60 = 0, d90 = 0, d90p = 0;
            foreach (var inv in g)
            {
                var outstanding = inv.GrandTotal - inv.PaidAmount;
                var age = (date - inv.InvoiceDate.Date).Days;
                if (age <= 0) cur += outstanding;
                else if (age <= 30) d30 += outstanding;
                else if (age <= 60) d60 += outstanding;
                else if (age <= 90) d90 += outstanding;
                else d90p += outstanding;
            }
            return new AgingBucketDto(party.Code, party.Name, cur, d30, d60, d90, d90p, cur + d30 + d60 + d90 + d90p);
        }).ToList();
        return new AgingReportDto("AR", date, rows.Sum(r => r.Total), rows);
    }

    public async Task<AgingReportDto> GetPayablesAgingAsync(DateTime? asOf = null, CancellationToken ct = default)
    {
        var date = (asOf ?? DateTime.UtcNow).Date;
        var invoices = await db.PurchaseInvoices
            .Include(x => x.Party)
            .Where(x => x.Status == DocumentStatus.Posted && x.GrandTotal > x.PaidAmount)
            .ToListAsync(ct);
        var rows = invoices.GroupBy(x => x.PartyId).Select(g =>
        {
            var party = g.First().Party!;
            decimal cur = 0, d30 = 0, d60 = 0, d90 = 0, d90p = 0;
            foreach (var inv in g)
            {
                var outstanding = inv.GrandTotal - inv.PaidAmount;
                var age = (date - inv.InvoiceDate.Date).Days;
                if (age <= 0) cur += outstanding;
                else if (age <= 30) d30 += outstanding;
                else if (age <= 60) d60 += outstanding;
                else if (age <= 90) d90 += outstanding;
                else d90p += outstanding;
            }
            return new AgingBucketDto(party.Code, party.Name, cur, d30, d60, d90, d90p, cur + d30 + d60 + d90 + d90p);
        }).ToList();
        return new AgingReportDto("AP", date, rows.Sum(r => r.Total), rows);
    }

    private async Task<HashSet<Guid>> ResolveCogsAccountIdsAsync(CancellationToken ct)
    {
        var ids = await db.GlAccountMappings
            .Where(x => x.IsActive && x.MappingKey == GlMappingKeys.Cogs)
            .Select(x => x.AccountId)
            .ToListAsync(ct);
        if (ids.Count == 0)
        {
            var cogs = await db.Accounts.Where(x => x.Code == "5100").Select(x => x.Id).ToListAsync(ct);
            ids.AddRange(cogs);
        }
        return ids.ToHashSet();
    }

    private static bool IsDebitNormal(AccountType t) => t is AccountType.Asset or AccountType.Expense;

    private static decimal OpeningFor(AccountType type, decimal debit, decimal credit) =>
        IsDebitNormal(type) ? debit - credit : credit - debit;

    private static decimal RunningBalance(AccountType type, decimal current, decimal debit, decimal credit) =>
        IsDebitNormal(type) ? current + debit - credit : current + credit - debit;
}
