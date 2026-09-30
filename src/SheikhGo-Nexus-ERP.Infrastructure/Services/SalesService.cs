using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Entities;
using SheikhGoNexusERP.Domain.Enums;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

public class SalesService : ISalesService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly IDocumentWorkflowService _workflow;

    public SalesService(AppDbContext db, IAuditService audit, IDocumentWorkflowService workflow)
    {
        _db = db;
        _audit = audit;
        _workflow = workflow;
    }

    private async Task<(Company company, Branch branch, FinancialYear fy)> ScopeAsync(CancellationToken ct)
    {
        var company = await _db.Companies.FirstAsync(ct);
        var branch = await _db.Branches.FirstAsync(x => x.IsDefault || true, ct);
        var fy = await _db.FinancialYears.FirstAsync(x => x.IsCurrent, ct);
        return (company, branch, fy);
    }

    private async Task<DocumentType> DocTypeAsync(string code, CancellationToken ct) =>
        await _db.DocumentTypes.FirstAsync(x => x.Code == code, ct);

    // ── Sales Orders ────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SalesOrderDto>> GetOrdersAsync(CancellationToken ct = default) =>
        await _db.SalesOrders.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(x => x.OrderDate)
            .Select(x => new SalesOrderDto(x.Id, x.OrderNo, x.OrderDate, x.PartyId, x.Party!.Name, x.WarehouseId, x.Status,
                x.SubTotal, x.Tax, x.GrandTotal, x.Remarks,
                x.Lines.Select(l => new InvoiceLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList(), null))
            .ToListAsync(ct);

    public async Task<SalesOrderDto?> GetOrderByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = (await GetOrdersAsync(ct)).FirstOrDefault(x => x.Id == id);
        if (dto is null) return null;
        var entity = await _db.SalesOrders.AsNoTracking().FirstAsync(x => x.Id == id, ct);
        var workflow = await DocumentWorkflowActorsResolver.ResolveAsync(
            _db, nameof(SalesOrder), id, entity.DocumentId, entity.CreatedBy, entity.CreatedAt, ct);
        return dto with { Workflow = workflow };
    }

    public async Task<SalesOrderDto> CreateOrderAsync(SalesOrderCreateDto dto, CancellationToken ct = default)
    {
        var (company, branch, fy) = await ScopeAsync(ct);
        var dt = await DocTypeAsync("SO", ct);
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var no = await DocumentNumbering.NextAsync(_db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "SO", fy.StartDate.Year, ct);
        var sub = dto.Lines.Sum(l => l.Qty * l.Rate);
        var document = new Document
        {
            DocumentTypeId = dt.Id, CompanyId = company.Id, BranchId = branch.Id, FinancialYearId = fy.Id,
            DocumentNo = no, DocumentDate = dto.Date.Date, PartyId = dto.PartyId, Narration = dto.Remarks
        };
        var so = new SalesOrder
        {
            OrderNo = no, OrderDate = dto.Date.Date, PartyId = dto.PartyId, WarehouseId = dto.WarehouseId,
            Document = document, Status = DocumentStatus.Draft,
            SubTotal = sub, Tax = dto.Tax, GrandTotal = sub + dto.Tax, Remarks = dto.Remarks
        };
        var lineNo = 1;
        foreach (var l in dto.Lines)
        {
            so.Lines.Add(new SalesOrderLine
            {
                LineNo = lineNo++, ProductId = l.ProductId, Qty = l.Qty, Rate = l.Rate, Amount = l.Qty * l.Rate
            });
        }
        _db.SalesOrders.Add(so);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await _audit.WriteAsync(nameof(SalesOrder), so.Id, "Created", null, so.Status.ToString(), ct);
        if (dto.Post) await RunQuickPathAsync(so.Id, "order", ct);
        return (await GetOrdersAsync(ct)).First(x => x.Id == so.Id);
    }

    public async Task<SalesOrderDto?> UpdateOrderAsync(Guid id, SalesOrderCreateDto dto, CancellationToken ct = default)
    {
        var so = await _db.SalesOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (so is null) return null;
        if (so.Status != DocumentStatus.Draft) throw new InvalidOperationException("Only draft sales orders can be edited.");
        so.OrderDate = dto.Date.Date; so.PartyId = dto.PartyId; so.WarehouseId = dto.WarehouseId; so.Tax = dto.Tax; so.Remarks = dto.Remarks;
        _db.SalesOrderLines.RemoveRange(so.Lines);
        so.Lines = dto.Lines.Select((x, i) => new SalesOrderLine
        {
            LineNo = i + 1, ProductId = x.ProductId, Qty = x.Qty, Rate = x.Rate, Amount = x.Qty * x.Rate
        }).ToList();
        so.SubTotal = so.Lines.Sum(x => x.Amount); so.GrandTotal = so.SubTotal + so.Tax;
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(nameof(SalesOrder), id, "Updated", null, "Draft updated", ct);
        return (await GetOrdersAsync(ct)).First(x => x.Id == id);
    }

    public Task<SalesOrderDto?> TransitionOrderAsync(Guid id, string action, CancellationToken ct = default) =>
        _workflow.TransitionSalesOrderAsync(id, action, null, ct);

    // ── Delivery Orders ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<DeliveryOrderDto>> GetDeliveriesAsync(CancellationToken ct = default) =>
        await _db.DeliveryOrders.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(x => x.DeliveryDate)
            .Select(x => new DeliveryOrderDto(x.Id, x.DeliveryNo, x.DeliveryDate, x.PartyId, x.Party!.Name, x.WarehouseId,
                x.SalesOrderId, x.Status, x.Remarks,
                x.Lines.Select(l => new InvoiceLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList(), null))
            .ToListAsync(ct);

    public async Task<DeliveryOrderDto?> GetDeliveryByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = (await GetDeliveriesAsync(ct)).FirstOrDefault(x => x.Id == id);
        if (dto is null) return null;
        var entity = await _db.DeliveryOrders.AsNoTracking().FirstAsync(x => x.Id == id, ct);
        var workflow = await DocumentWorkflowActorsResolver.ResolveAsync(
            _db, nameof(DeliveryOrder), id, entity.DocumentId, entity.CreatedBy, entity.CreatedAt, ct);
        return dto with { Workflow = workflow };
    }

    public async Task<DeliveryOrderDto> CreateDeliveryAsync(DeliveryOrderCreateDto dto, CancellationToken ct = default)
    {
        var so = await _db.SalesOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == dto.SalesOrderId, ct)
            ?? throw new InvalidOperationException("Sales order not found.");
        if (so.Status is not DocumentStatus.Approved and not DocumentStatus.Posted)
            throw new InvalidOperationException("Delivery requires an approved sales order.");
        if (so.PartyId != dto.PartyId)
            throw new InvalidOperationException("Delivery customer must match its sales order.");

        foreach (var line in dto.Lines)
        {
            var soLine = so.Lines.FirstOrDefault(x => x.ProductId == line.ProductId)
                ?? throw new InvalidOperationException("Delivery product is not on the sales order.");
            if (line.Qty <= 0 || line.Qty > soLine.Qty - soLine.DeliveredQty)
                throw new InvalidOperationException($"Delivery quantity exceeds remaining sales-order quantity for product {line.ProductId}.");
        }

        var (company, branch, fy) = await ScopeAsync(ct);
        var dt = await DocTypeAsync("DO", ct);
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var no = await DocumentNumbering.NextAsync(_db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "DO", fy.StartDate.Year, ct);
        var document = new Document
        {
            DocumentTypeId = dt.Id, CompanyId = company.Id, BranchId = branch.Id, FinancialYearId = fy.Id,
            DocumentNo = no, DocumentDate = dto.Date.Date, PartyId = dto.PartyId, Narration = dto.Remarks
        };
        var delivery = new DeliveryOrder
        {
            DeliveryNo = no, DeliveryDate = dto.Date.Date, PartyId = dto.PartyId, WarehouseId = dto.WarehouseId,
            SalesOrderId = dto.SalesOrderId, Document = document, Status = DocumentStatus.Draft, Remarks = dto.Remarks
        };
        var lineNo = 1;
        foreach (var l in dto.Lines)
        {
            delivery.Lines.Add(new DeliveryOrderLine
            {
                LineNo = lineNo++,
                ProductId = l.ProductId,
                SalesOrderLineId = so.Lines.First(x => x.ProductId == l.ProductId).Id,
                Qty = l.Qty,
                Rate = l.Rate,
                Amount = l.Qty * l.Rate
            });
        }
        _db.DeliveryOrders.Add(delivery);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await _audit.WriteAsync(nameof(DeliveryOrder), delivery.Id, "Created", null, delivery.Status.ToString(), ct);
        if (dto.Post) await RunQuickPathAsync(delivery.Id, "delivery", ct);
        return (await GetDeliveriesAsync(ct)).First(x => x.Id == delivery.Id);
    }

    public Task<DeliveryOrderDto?> TransitionDeliveryAsync(Guid id, string action, CancellationToken ct = default) =>
        _workflow.TransitionDeliveryOrderAsync(id, action, null, ct);

    // ── Sales Invoices ──────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SalesInvoiceDocDto>> GetInvoicesAsync(CancellationToken ct = default) =>
        await _db.SalesInvoices.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(x => x.InvoiceDate)
            .Select(x => new SalesInvoiceDocDto(x.Id, x.InvoiceNo, x.InvoiceDate, x.PartyId, x.Party!.Name,
                x.DeliveryOrderId, x.SalesOrderId, x.Status,
                x.SubTotal, x.Discount, x.Tax, x.GrandTotal, x.PaidAmount, x.Remarks,
                x.Lines.Select(l => new InvoiceLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList(), null))
            .ToListAsync(ct);

    public async Task<SalesInvoiceDocDto?> GetInvoiceByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = (await GetInvoicesAsync(ct)).FirstOrDefault(x => x.Id == id);
        if (dto is null) return null;
        var entity = await _db.SalesInvoices.AsNoTracking().FirstAsync(x => x.Id == id, ct);
        var workflow = await DocumentWorkflowActorsResolver.ResolveAsync(
            _db, nameof(SalesInvoice), id, entity.DocumentId, entity.CreatedBy, entity.CreatedAt, ct);
        return dto with { Workflow = workflow };
    }

    public async Task<SalesInvoiceDocDto> CreateInvoiceAsync(SalesInvoiceDocCreateDto dto, CancellationToken ct = default)
    {
        if (dto.DeliveryOrderId is Guid doId)
        {
            var delivery = await _db.DeliveryOrders.FirstOrDefaultAsync(x => x.Id == doId, ct)
                ?? throw new InvalidOperationException("Delivery order not found.");
            if (delivery.Status != DocumentStatus.Posted)
                throw new InvalidOperationException("Sales invoice requires a posted delivery order.");
            if (delivery.PartyId != dto.PartyId)
                throw new InvalidOperationException("Invoice customer must match its delivery order.");
        }

        var (company, branch, fy) = await ScopeAsync(ct);
        var dt = await DocTypeAsync("SI", ct);
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var no = await DocumentNumbering.NextAsync(_db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "SI", fy.StartDate.Year, ct);
        var sub = dto.Lines.Sum(l => l.Qty * l.Rate);
        var grand = sub - dto.Discount + dto.Tax;
        var document = new Document
        {
            DocumentTypeId = dt.Id, CompanyId = company.Id, BranchId = branch.Id, FinancialYearId = fy.Id,
            DocumentNo = no, DocumentDate = dto.Date.Date, PartyId = dto.PartyId, Narration = dto.Remarks
        };
        var invoice = new SalesInvoice
        {
            InvoiceNo = no, InvoiceDate = dto.Date.Date, PartyId = dto.PartyId,
            DeliveryOrderId = dto.DeliveryOrderId, SalesOrderId = dto.SalesOrderId,
            Document = document, Status = DocumentStatus.Draft,
            SubTotal = sub, Discount = dto.Discount, Tax = dto.Tax, GrandTotal = grand, Remarks = dto.Remarks
        };
        var lineNo = 1;
        foreach (var l in dto.Lines)
        {
            invoice.Lines.Add(new SalesInvoiceLine
            {
                LineNo = lineNo++, ProductId = l.ProductId, Qty = l.Qty, Rate = l.Rate, Amount = l.Qty * l.Rate
            });
        }
        _db.SalesInvoices.Add(invoice);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await _audit.WriteAsync(nameof(SalesInvoice), invoice.Id, "Created", null, invoice.Status.ToString(), ct);
        if (dto.Post) await RunQuickPathAsync(invoice.Id, "invoice", ct);
        return (await GetInvoicesAsync(ct)).First(x => x.Id == invoice.Id);
    }

    public Task<SalesInvoiceDocDto?> TransitionInvoiceAsync(Guid id, string action, CancellationToken ct = default) =>
        _workflow.TransitionSalesInvoiceAsync(id, action, null, ct);

    // ── Customer Payments ───────────────────────────────────────────────────

    public async Task<IReadOnlyList<CustomerPaymentDto>> GetPaymentsAsync(CancellationToken ct = default) =>
        await _db.CustomerPayments.Include(x => x.Party).Include(x => x.Applications).ThenInclude(a => a.SalesInvoice)
            .OrderByDescending(x => x.PaymentDate)
            .Select(x => new CustomerPaymentDto(x.Id, x.PaymentNo, x.PaymentDate, x.PartyId, x.Party!.Name, x.Amount, x.Status, x.Remarks,
                x.Applications.Select(a => new PaymentApplicationDto(a.Id, a.SalesInvoiceId, a.SalesInvoice!.InvoiceNo, a.AmountApplied)).ToList(), null))
            .ToListAsync(ct);

    public async Task<CustomerPaymentDto?> GetPaymentByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = (await GetPaymentsAsync(ct)).FirstOrDefault(x => x.Id == id);
        if (dto is null) return null;
        var entity = await _db.CustomerPayments.AsNoTracking().FirstAsync(x => x.Id == id, ct);
        var workflow = await DocumentWorkflowActorsResolver.ResolveAsync(
            _db, nameof(CustomerPayment), id, entity.DocumentId, entity.CreatedBy, entity.CreatedAt, ct);
        return dto with { Workflow = workflow };
    }

    public async Task<CustomerPaymentDto> CreatePaymentAsync(CustomerPaymentCreateDto dto, CancellationToken ct = default)
    {
        if (dto.Applications.Count == 0)
            throw new InvalidOperationException("Payment must apply to at least one invoice.");
        var appliedTotal = dto.Applications.Sum(a => a.AmountApplied);
        if (appliedTotal <= 0 || appliedTotal > dto.Amount)
            throw new InvalidOperationException("Applied amount must be positive and not exceed payment amount.");

        foreach (var app in dto.Applications)
        {
            var invoice = await _db.SalesInvoices.FirstOrDefaultAsync(x => x.Id == app.SalesInvoiceId, ct)
                ?? throw new InvalidOperationException($"Sales invoice {app.SalesInvoiceId} not found.");
            if (invoice.Status != DocumentStatus.Posted)
                throw new InvalidOperationException("Payment can only apply to posted invoices.");
            if (invoice.PartyId != dto.PartyId)
                throw new InvalidOperationException("Payment customer must match invoice customer.");
            var outstanding = invoice.GrandTotal - invoice.PaidAmount;
            if (app.AmountApplied <= 0 || app.AmountApplied > outstanding)
                throw new InvalidOperationException($"Applied amount exceeds outstanding for invoice {invoice.InvoiceNo}.");
        }

        var (company, branch, fy) = await ScopeAsync(ct);
        var dt = await DocTypeAsync("CP", ct);
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var no = await DocumentNumbering.NextAsync(_db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "CP", fy.StartDate.Year, ct);
        var document = new Document
        {
            DocumentTypeId = dt.Id, CompanyId = company.Id, BranchId = branch.Id, FinancialYearId = fy.Id,
            DocumentNo = no, DocumentDate = dto.Date.Date, PartyId = dto.PartyId, Narration = dto.Remarks
        };
        var payment = new CustomerPayment
        {
            PaymentNo = no, PaymentDate = dto.Date.Date, PartyId = dto.PartyId, Amount = dto.Amount,
            CashBankAccountId = dto.CashBankAccountId,
            Document = document, Status = DocumentStatus.Draft, Remarks = dto.Remarks
        };
        foreach (var app in dto.Applications)
        {
            payment.Applications.Add(new CustomerPaymentApplication
            {
                SalesInvoiceId = app.SalesInvoiceId,
                AmountApplied = app.AmountApplied
            });
        }
        _db.CustomerPayments.Add(payment);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await _audit.WriteAsync(nameof(CustomerPayment), payment.Id, "Created", null, payment.Status.ToString(), ct);
        if (dto.Post) await RunQuickPathAsync(payment.Id, "payment", ct);
        return (await GetPaymentsAsync(ct)).First(x => x.Id == payment.Id);
    }

    public Task<CustomerPaymentDto?> TransitionPaymentAsync(Guid id, string action, CancellationToken ct = default) =>
        _workflow.TransitionCustomerPaymentAsync(id, action, null, ct);

    public async Task<ReceivableBalanceDto?> GetReceivableAsync(Guid partyId, CancellationToken ct = default)
    {
        var bal = await _db.ReceivableBalances.Include(x => x.Party)
            .FirstOrDefaultAsync(x => x.PartyId == partyId, ct);
        return bal is null ? null : new ReceivableBalanceDto(bal.PartyId, bal.Party?.Name, bal.Balance);
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetAuditAsync(string entityName, Guid id, CancellationToken ct = default) =>
        await _db.AuditLogs.Where(x => x.EntityName == entityName && x.EntityId == id.ToString()).OrderByDescending(x => x.Timestamp)
            .Select(x => new AuditLogDto(x.Id, x.EntityName, x.EntityId, x.Action, x.OldValues, x.NewValues, x.UserId, x.Timestamp, x.IpAddress)).ToListAsync(ct);

    private async Task RunQuickPathAsync(Guid id, string kind, CancellationToken ct)
    {
        var code = kind switch
        {
            "order" => "SO",
            "delivery" => "DO",
            "invoice" => "SI",
            "payment" => "CP",
            _ => throw new InvalidOperationException($"Unknown quick-path kind '{kind}'.")
        };
        var docType = await DocTypeAsync(code, ct);

        if (kind == "order")
        {
            await TransitionOrderAsync(id, "submit", ct);
            if (docType.RequiresVerification) await TransitionOrderAsync(id, "verify", ct);
            if (docType.RequiresApproval) await TransitionOrderAsync(id, "approve", ct);
            await TransitionOrderAsync(id, "post", ct);
        }
        else if (kind == "delivery")
        {
            await TransitionDeliveryAsync(id, "submit", ct);
            if (docType.RequiresVerification) await TransitionDeliveryAsync(id, "verify", ct);
            if (docType.RequiresApproval) await TransitionDeliveryAsync(id, "approve", ct);
            await TransitionDeliveryAsync(id, "post", ct);
        }
        else if (kind == "invoice")
        {
            await TransitionInvoiceAsync(id, "submit", ct);
            if (docType.RequiresVerification) await TransitionInvoiceAsync(id, "verify", ct);
            if (docType.RequiresApproval) await TransitionInvoiceAsync(id, "approve", ct);
            await TransitionInvoiceAsync(id, "post", ct);
        }
        else
        {
            await TransitionPaymentAsync(id, "submit", ct);
            if (docType.RequiresVerification) await TransitionPaymentAsync(id, "verify", ct);
            if (docType.RequiresApproval) await TransitionPaymentAsync(id, "approve", ct);
            await TransitionPaymentAsync(id, "post", ct);
        }
    }
}
