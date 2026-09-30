using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Entities;
using SheikhGoNexusERP.Domain.Enums;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

public class FinanceService(
    AppDbContext db,
    IAuditService audit,
    IAccountingPostingService accounting,
    IDocumentWorkflowService workflow) : IFinanceService
{
    public async Task<IReadOnlyList<JournalEntryDto>> GetJournalsAsync(CancellationToken ct = default) =>
        await db.JournalEntries.Include(x => x.Lines).ThenInclude(l => l.Account)
            .OrderByDescending(x => x.PostingDate).ThenByDescending(x => x.CreatedAt)
            .Select(x => new JournalEntryDto(
                x.Id, x.JournalNo, x.PostingDate, x.SourceType, x.SourceEntityId, x.Status, x.Narration,
                x.TotalDebit, x.TotalCredit,
                x.Lines.OrderBy(l => l.LineNo).Select(l => new JournalLineDto(
                    l.Id, l.AccountId, l.Account!.Code, l.Account.Name, l.Debit, l.Credit, l.Description, l.PartyId, l.ProductId)).ToList()))
            .ToListAsync(ct);

    public async Task<JournalEntryDto?> GetJournalByIdAsync(Guid id, CancellationToken ct = default) =>
        (await GetJournalsAsync(ct)).FirstOrDefault(x => x.Id == id);

    public Task<JournalEntryDto> CreateJournalAsync(JournalEntryCreateDto dto, CancellationToken ct = default) =>
        accounting.CreateManualJournalAsync(dto, ct);

    public async Task<JournalEntryDto?> PostJournalAsync(Guid id, CancellationToken ct = default)
    {
        var je = await db.JournalEntries.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (je is null) return null;
        if (je.Status == DocumentStatus.Posted) return await GetJournalByIdAsync(id, ct);
        if (je.Status != DocumentStatus.Draft && je.Status != DocumentStatus.Approved)
            throw new InvalidOperationException("Only draft/approved journals can be posted.");

        await accounting.EnsurePeriodOpenAsync(je.PostingDate, je.CompanyId, je.FinancialYearId, ct);
        if (Math.Abs(je.TotalDebit - je.TotalCredit) > 0.01m)
            throw new InvalidOperationException("Journal is not balanced.");

        je.Status = DocumentStatus.Posted;
        if (je.DocumentId is Guid docId)
        {
            var doc = await db.Documents.FirstOrDefaultAsync(x => x.Id == docId, ct);
            if (doc is not null) doc.Status = DocumentStatus.Posted;
        }
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(JournalEntry), id, "Posted", null, je.JournalNo, ct);
        return await GetJournalByIdAsync(id, ct);
    }

    public async Task<IReadOnlyList<CashBankAccountDto>> GetCashBankAccountsAsync(CancellationToken ct = default) =>
        await db.CashBankAccounts.Include(x => x.GlAccount).OrderBy(x => x.Code)
            .Select(x => new CashBankAccountDto(x.Id, x.Code, x.Name, x.AccountKind, x.GlAccountId, x.GlAccount!.Code, x.IsActive, x.IsDefault))
            .ToListAsync(ct);

    public async Task<CashBankAccountDto> CreateCashBankAccountAsync(CashBankAccountUpsertDto dto, CancellationToken ct = default)
    {
        if (dto.IsDefault)
        {
            var existing = await db.CashBankAccounts.Where(x => x.IsDefault).ToListAsync(ct);
            foreach (var e in existing) e.IsDefault = false;
        }
        var entity = new CashBankAccount
        {
            Code = dto.Code,
            Name = dto.Name,
            AccountKind = dto.AccountKind,
            GlAccountId = dto.GlAccountId,
            IsDefault = dto.IsDefault
        };
        db.CashBankAccounts.Add(entity);
        await db.SaveChangesAsync(ct);
        return (await GetCashBankAccountsAsync(ct)).First(x => x.Id == entity.Id);
    }

    public async Task<IReadOnlyList<CashBankTxnDto>> GetCashBankTransactionsAsync(CancellationToken ct = default) =>
        await db.CashBankTransactions.Include(x => x.CashBankAccount).Include(x => x.Party)
            .OrderByDescending(x => x.PostingDate)
            .Select(x => new CashBankTxnDto(
                x.Id, x.CashBankAccountId, x.CashBankAccount!.Name, x.OppositeCashBankAccountId,
                x.PartyId, x.Party != null ? x.Party.Name : null, x.Amount, x.TxnType, x.PostingDate, x.Status, x.Narration, x.JournalEntryId))
            .ToListAsync(ct);

    public async Task<CashBankTxnDto> CreateCashBankTransactionAsync(CashBankTxnCreateDto dto, CancellationToken ct = default)
    {
        if (dto.Amount <= 0) throw new InvalidOperationException("Amount must be positive.");
        var cashBank = await db.CashBankAccounts.FirstOrDefaultAsync(x => x.Id == dto.CashBankAccountId, ct)
            ?? throw new InvalidOperationException("Cash/bank account not found.");

        var txn = new CashBankTransaction
        {
            CashBankAccountId = dto.CashBankAccountId,
            OppositeCashBankAccountId = dto.OppositeCashBankAccountId,
            PartyId = dto.PartyId,
            Amount = dto.Amount,
            TxnType = dto.TxnType,
            PostingDate = dto.PostingDate.Date,
            Status = DocumentStatus.Draft,
            Narration = dto.Narration
        };
        db.CashBankTransactions.Add(txn);
        await db.SaveChangesAsync(ct);

        if (dto.Post)
        {
            var gl = await db.Accounts.FirstAsync(x => x.Id == cashBank.GlAccountId, ct);
            var clearing = dto.TxnType == CashBankTxnType.Transfer && dto.OppositeCashBankAccountId is Guid oppId
                ? (await db.CashBankAccounts.Include(x => x.GlAccount).FirstAsync(x => x.Id == oppId, ct)).GlAccountId
                : (await db.Accounts.FirstAsync(x => x.Code == (dto.TxnType == CashBankTxnType.Receipt ? "3100" : "5200"), ct)).Id;

            // Simple transfer or generic receipt/payment against Capital/Expense for manual cash ops
            List<JournalLineDto> lines;
            if (dto.TxnType == CashBankTxnType.Transfer && dto.OppositeCashBankAccountId is Guid)
            {
                lines =
                [
                    new(null, clearing, null, null, dto.Amount, 0, dto.Narration),
                    new(null, gl.Id, null, null, 0, dto.Amount, dto.Narration)
                ];
                if (dto.TxnType == CashBankTxnType.Transfer)
                {
                    // Dr destination, Cr source — destination is opposite
                    lines =
                    [
                        new(null, clearing, null, null, dto.Amount, 0, "Transfer in"),
                        new(null, gl.Id, null, null, 0, dto.Amount, "Transfer out")
                    ];
                }
            }
            else if (dto.TxnType == CashBankTxnType.Receipt)
            {
                lines =
                [
                    new(null, gl.Id, null, null, dto.Amount, 0, dto.Narration),
                    new(null, clearing, null, null, 0, dto.Amount, dto.Narration)
                ];
            }
            else
            {
                lines =
                [
                    new(null, clearing, null, null, dto.Amount, 0, dto.Narration),
                    new(null, gl.Id, null, null, 0, dto.Amount, dto.Narration)
                ];
            }

            var je = await accounting.CreateManualJournalAsync(new JournalEntryCreateDto(dto.PostingDate, dto.Narration, true, lines), ct);
            // Fix source type: mark as CashBank by updating
            var entity = await db.JournalEntries.FirstAsync(x => x.Id == je.Id, ct);
            entity.SourceType = JournalSourceType.CashBank;
            entity.SourceEntityId = txn.Id;
            txn.JournalEntryId = je.Id;
            txn.Status = DocumentStatus.Posted;
            await db.SaveChangesAsync(ct);
        }

        return (await GetCashBankTransactionsAsync(ct)).First(x => x.Id == txn.Id);
    }

    public async Task<IReadOnlyList<SupplierPaymentDto>> GetSupplierPaymentsAsync(CancellationToken ct = default) =>
        await db.SupplierPayments.Include(x => x.Party).Include(x => x.Applications).ThenInclude(a => a.PurchaseInvoice)
            .OrderByDescending(x => x.PaymentDate)
            .Select(x => new SupplierPaymentDto(x.Id, x.PaymentNo, x.PaymentDate, x.PartyId, x.Party!.Name, x.CashBankAccountId, x.Amount, x.Status, x.Remarks,
                x.Applications.Select(a => new SupplierPaymentApplicationDto(a.Id, a.PurchaseInvoiceId, a.PurchaseInvoice!.InvoiceNo, a.AmountApplied)).ToList(), null))
            .ToListAsync(ct);

    public async Task<SupplierPaymentDto?> GetSupplierPaymentByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = (await GetSupplierPaymentsAsync(ct)).FirstOrDefault(x => x.Id == id);
        if (dto is null) return null;
        var entity = await db.SupplierPayments.AsNoTracking().FirstAsync(x => x.Id == id, ct);
        var actors = await DocumentWorkflowActorsResolver.ResolveAsync(
            db, nameof(SupplierPayment), id, entity.DocumentId, entity.CreatedBy, entity.CreatedAt, ct);
        return dto with { Workflow = actors };
    }

    public async Task<SupplierPaymentDto> CreateSupplierPaymentAsync(SupplierPaymentCreateDto dto, CancellationToken ct = default)
    {
        if (dto.Applications.Count == 0)
            throw new InvalidOperationException("Payment must apply to at least one invoice.");
        var appliedTotal = dto.Applications.Sum(a => a.AmountApplied);
        if (appliedTotal <= 0 || appliedTotal > dto.Amount)
            throw new InvalidOperationException("Applied amount must be positive and not exceed payment amount.");

        foreach (var app in dto.Applications)
        {
            var invoice = await db.PurchaseInvoices.FirstOrDefaultAsync(x => x.Id == app.PurchaseInvoiceId, ct)
                ?? throw new InvalidOperationException($"Purchase invoice {app.PurchaseInvoiceId} not found.");
            if (invoice.Status != DocumentStatus.Posted)
                throw new InvalidOperationException("Payment can only apply to posted invoices.");
            if (invoice.PartyId != dto.PartyId)
                throw new InvalidOperationException("Payment supplier must match invoice supplier.");
            var outstanding = invoice.GrandTotal - invoice.PaidAmount;
            if (app.AmountApplied <= 0 || app.AmountApplied > outstanding)
                throw new InvalidOperationException($"Applied amount exceeds outstanding for invoice {invoice.InvoiceNo}.");
        }

        var company = await db.Companies.FirstAsync(ct);
        var branch = await db.Branches.FirstAsync(x => x.IsDefault || true, ct);
        var fy = await db.FinancialYears.FirstAsync(x => x.IsCurrent, ct);
        var dt = await db.DocumentTypes.FirstAsync(x => x.Code == "SPAY", ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var no = await DocumentNumbering.NextAsync(db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "SPAY", fy.StartDate.Year, ct);
        var document = new Document
        {
            DocumentTypeId = dt.Id, CompanyId = company.Id, BranchId = branch.Id, FinancialYearId = fy.Id,
            DocumentNo = no, DocumentDate = dto.Date.Date, PartyId = dto.PartyId, Narration = dto.Remarks
        };
        var payment = new SupplierPayment
        {
            PaymentNo = no, PaymentDate = dto.Date.Date, PartyId = dto.PartyId,
            CashBankAccountId = dto.CashBankAccountId, Amount = dto.Amount,
            Document = document, Status = DocumentStatus.Draft, Remarks = dto.Remarks
        };
        foreach (var app in dto.Applications)
        {
            payment.Applications.Add(new SupplierPaymentApplication
            {
                PurchaseInvoiceId = app.PurchaseInvoiceId,
                AmountApplied = app.AmountApplied
            });
        }
        db.SupplierPayments.Add(payment);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await audit.WriteAsync(nameof(SupplierPayment), payment.Id, "Created", null, payment.Status.ToString(), ct);
        if (dto.Post)
        {
            await workflow.TransitionSupplierPaymentAsync(payment.Id, "submit", null, ct);
            await workflow.TransitionSupplierPaymentAsync(payment.Id, "post", null, ct);
        }
        return (await GetSupplierPaymentsAsync(ct)).First(x => x.Id == payment.Id);
    }

    public Task<SupplierPaymentDto?> TransitionSupplierPaymentAsync(Guid id, string action, CancellationToken ct = default) =>
        workflow.TransitionSupplierPaymentAsync(id, action, null, ct);

    public async Task EnsureMonthlyPeriodsAsync(CancellationToken ct = default)
    {
        var fy = await db.FinancialYears.FirstOrDefaultAsync(x => x.IsCurrent, ct);
        if (fy is null) return;
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

    public async Task<FiscalPeriodDto?> ClosePeriodAsync(Guid periodId, CancellationToken ct = default)
    {
        var p = await db.FiscalPeriods.FirstOrDefaultAsync(x => x.Id == periodId, ct);
        if (p is null) return null;
        p.IsClosed = true;
        await db.SaveChangesAsync(ct);
        return new FiscalPeriodDto(p.Id, p.FinancialYearId, p.Name, p.PeriodNumber, p.StartDate, p.EndDate, p.IsClosed);
    }

    public async Task<FiscalPeriodDto?> OpenPeriodAsync(Guid periodId, CancellationToken ct = default)
    {
        var p = await db.FiscalPeriods.FirstOrDefaultAsync(x => x.Id == periodId, ct);
        if (p is null) return null;
        p.IsClosed = false;
        await db.SaveChangesAsync(ct);
        return new FiscalPeriodDto(p.Id, p.FinancialYearId, p.Name, p.PeriodNumber, p.StartDate, p.EndDate, p.IsClosed);
    }
}
