using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Entities;
using SheikhGoNexusERP.Domain.Enums;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

/// <summary>
/// Shared document engine: Draft → Submitted → (Verified?) → (Approved?) → Posted (or Cancelled).
/// Steps depend on DocumentType.RequiresVerification / RequiresApproval.
/// Posting side-effects stay domain-specific (GRN → stock, PI → payable, PO → status only).
/// </summary>
public class DocumentWorkflowService(
    AppDbContext db,
    IAuditService audit,
    IPermissionService permissions,
    ITenantContext tenant,
    IAccountingPostingService accounting,
    IInventoryPostingService inventory) : IDocumentWorkflowService
{
    public async Task<PurchaseOrderDto?> TransitionOrderAsync(Guid id, string action, string? remarks = null, CancellationToken ct = default)
    {
        var po = await db.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (po is null) return null;

        await EnsureActionPermissionAsync("PurchaseOrder", action, ct);
        await TransitionCoreAsync(po, po.DocumentId, nameof(PurchaseOrder), "PO", po.OrderNo, action, remarks, ct);
        return await MapOrderAsync(id, ct);
    }

    public async Task<GoodsReceiptDto?> TransitionReceiptAsync(Guid id, string action, string? remarks = null, CancellationToken ct = default)
    {
        var grn = await db.GoodsReceipts.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (grn is null) return null;

        await EnsureActionPermissionAsync("GoodsReceipt", action, ct);
        var wasPosted = grn.Status == DocumentStatus.Posted;
        await TransitionCoreAsync(grn, grn.DocumentId, nameof(GoodsReceipt), "GRN", grn.ReceiptNo, action, remarks, ct);

        if (!wasPosted && grn.Status == DocumentStatus.Posted)
            await ApplyGoodsReceiptPostingAsync(grn, ct);

        return await MapReceiptAsync(id, ct);
    }

    public async Task<PurchaseInvoiceDto?> TransitionInvoiceAsync(Guid id, string action, string? remarks = null, CancellationToken ct = default)
    {
        var pi = await db.PurchaseInvoices.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (pi is null) return null;

        await EnsureActionPermissionAsync("Procurement", "PurchaseInvoice", action, ct);
        var wasPosted = pi.Status == DocumentStatus.Posted;
        await TransitionCoreAsync(pi, pi.DocumentId, nameof(PurchaseInvoice), "PI", pi.InvoiceNo, action, remarks, ct);

        if (!wasPosted && pi.Status == DocumentStatus.Posted)
            await ApplyPurchaseInvoicePostingAsync(pi, ct);

        return await MapInvoiceAsync(id, ct);
    }

    public async Task<SalesOrderDto?> TransitionSalesOrderAsync(Guid id, string action, string? remarks = null, CancellationToken ct = default)
    {
        var so = await db.SalesOrders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (so is null) return null;

        await EnsureActionPermissionAsync("Sales", "Order", action, ct);
        await TransitionCoreAsync(so, so.DocumentId, nameof(SalesOrder), "SO", so.OrderNo, action, remarks, ct);
        return await MapSalesOrderAsync(id, ct);
    }

    public async Task<DeliveryOrderDto?> TransitionDeliveryOrderAsync(Guid id, string action, string? remarks = null, CancellationToken ct = default)
    {
        var delivery = await db.DeliveryOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (delivery is null) return null;

        await EnsureActionPermissionAsync("Sales", "Delivery", action, ct);
        var wasPosted = delivery.Status == DocumentStatus.Posted;
        await TransitionCoreAsync(delivery, delivery.DocumentId, nameof(DeliveryOrder), "DO", delivery.DeliveryNo, action, remarks, ct);

        if (!wasPosted && delivery.Status == DocumentStatus.Posted)
            await ApplyDeliveryOrderPostingAsync(delivery, ct);

        return await MapDeliveryAsync(id, ct);
    }

    public async Task<SalesInvoiceDocDto?> TransitionSalesInvoiceAsync(Guid id, string action, string? remarks = null, CancellationToken ct = default)
    {
        var invoice = await db.SalesInvoices.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (invoice is null) return null;

        await EnsureActionPermissionAsync("Sales", "Invoice", action, ct);
        var wasPosted = invoice.Status == DocumentStatus.Posted;
        await TransitionCoreAsync(invoice, invoice.DocumentId, nameof(SalesInvoice), "SI", invoice.InvoiceNo, action, remarks, ct);

        if (!wasPosted && invoice.Status == DocumentStatus.Posted)
            await ApplySalesInvoicePostingAsync(invoice, ct);

        return await MapSalesInvoiceAsync(id, ct);
    }

    public async Task<CustomerPaymentDto?> TransitionCustomerPaymentAsync(Guid id, string action, string? remarks = null, CancellationToken ct = default)
    {
        var payment = await db.CustomerPayments.Include(x => x.Applications).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (payment is null) return null;

        await EnsureActionPermissionAsync("Sales", "Payment", action, ct);
        var wasPosted = payment.Status == DocumentStatus.Posted;
        await TransitionCoreAsync(payment, payment.DocumentId, nameof(CustomerPayment), "CP", payment.PaymentNo, action, remarks, ct);

        if (!wasPosted && payment.Status == DocumentStatus.Posted)
            await ApplyCustomerPaymentPostingAsync(payment, ct);

        return await MapPaymentAsync(id, ct);
    }

    public async Task<SupplierPaymentDto?> TransitionSupplierPaymentAsync(Guid id, string action, string? remarks = null, CancellationToken ct = default)
    {
        var payment = await db.SupplierPayments.Include(x => x.Applications).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (payment is null) return null;

        await permissions.EnsureAsync(
            action.Trim().ToLowerInvariant() switch
            {
                "submit" => "Finance.SupplierPayment.Create",
                "post" => "Finance.SupplierPayment.Post",
                "cancel" => "Finance.SupplierPayment.Create",
                _ => "Finance.SupplierPayment.Post"
            }, ct);

        var wasPosted = payment.Status == DocumentStatus.Posted;
        await TransitionCoreAsync(payment, payment.DocumentId, nameof(SupplierPayment), "SPAY", payment.PaymentNo, action, remarks, ct);

        if (!wasPosted && payment.Status == DocumentStatus.Posted)
            await accounting.PostSupplierPaymentAsync(payment, ct);

        return await MapSupplierPaymentAsync(id, ct);
    }

    private async Task EnsureActionPermissionAsync(string module, string docKind, string action, CancellationToken ct)
    {
        var key = action.Trim().ToLowerInvariant();
        var suffix = key switch
        {
            "submit" => "Submit",
            "verify" => "Verify",
            "approve" => "Approve",
            "post" => "Post",
            "cancel" => "Edit",
            _ => throw new InvalidOperationException($"Unknown workflow action '{action}'.")
        };
        await permissions.EnsureAsync($"{module}.{docKind}.{suffix}", ct);
    }

    private async Task EnsureActionPermissionAsync(string docKind, string action, CancellationToken ct) =>
        await EnsureActionPermissionAsync("Procurement", docKind, action, ct);

    private async Task TransitionCoreAsync(
        dynamic documentEntity,
        Guid? documentId,
        string entityName,
        string documentTypeCode,
        string documentNo,
        string action,
        string? remarks,
        CancellationToken ct)
    {
        var docType = await db.DocumentTypes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == documentTypeCode, ct);
        var requiresVerification = docType?.RequiresVerification ?? true;
        var requiresApproval = docType?.RequiresApproval ?? true;

        var old = (DocumentStatus)documentEntity.Status;
        var actionKey = action.Trim().ToLowerInvariant();
        var next = ResolveNextStatus(old, actionKey, requiresVerification, requiresApproval, documentNo);

        documentEntity.Status = next;
        if (documentId is Guid docId)
        {
            var document = await db.Documents.FirstOrDefaultAsync(x => x.Id == docId, ct);
            if (document is not null)
            {
                document.Status = next;
                if (actionKey == "verify")
                {
                    db.Verifications.Add(new Verification
                    {
                        DocumentId = docId,
                        VerifiedByUserId = tenant.UserId,
                        VerifiedAt = DateTime.UtcNow,
                        Remarks = remarks,
                        IsVerified = true
                    });
                }
                if (actionKey == "approve")
                {
                    db.Approvals.Add(new Approval
                    {
                        DocumentId = docId,
                        ApprovedByUserId = tenant.UserId,
                        ApprovedAt = DateTime.UtcNow,
                        Remarks = remarks,
                        IsApproved = true
                    });
                }
            }
        }

        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(entityName, (Guid)documentEntity.Id, actionKey, old.ToString(), next.ToString(), ct);
    }

    internal static DocumentStatus ResolveNextStatus(
        DocumentStatus old,
        string actionKey,
        bool requiresVerification,
        bool requiresApproval,
        string documentNo)
    {
        DocumentStatus? next = (actionKey, old) switch
        {
            ("submit", DocumentStatus.Draft) => DocumentStatus.Submitted,
            ("submit", DocumentStatus.Calculated) => DocumentStatus.Submitted,

            ("verify", DocumentStatus.Submitted) when requiresVerification => DocumentStatus.Verified,

            ("approve", DocumentStatus.Submitted) when !requiresVerification && requiresApproval => DocumentStatus.Approved,
            ("approve", DocumentStatus.Verified) when requiresApproval => DocumentStatus.Approved,

            ("post", DocumentStatus.Submitted) when !requiresVerification && !requiresApproval => DocumentStatus.Posted,
            ("post", DocumentStatus.Verified) when !requiresApproval => DocumentStatus.Posted,
            ("post", DocumentStatus.Approved) => DocumentStatus.Posted,

            ("cancel", _) when old is not DocumentStatus.Posted and not DocumentStatus.Cancelled
                => DocumentStatus.Cancelled,

            _ => null
        };

        return next ?? throw new InvalidOperationException(
            $"Cannot {actionKey} a {old} document ({documentNo}) with verification={requiresVerification}, approval={requiresApproval}.");
    }

    private async Task ApplyGoodsReceiptPostingAsync(GoodsReceipt grn, CancellationToken ct)
    {
        foreach (var line in grn.Lines)
        {
            if (line.PurchaseOrderLineId is Guid poLineId)
            {
                var poLine = await db.PurchaseOrderLines.FirstAsync(x => x.Id == poLineId, ct);
                var remaining = poLine.Qty - poLine.ReceivedQty;
                if (line.Qty > remaining)
                    throw new InvalidOperationException($"GRN quantity exceeds remaining PO quantity for product {line.ProductId}.");
                poLine.ReceivedQty += line.Qty;
            }
        }
        await db.SaveChangesAsync(ct);
        await inventory.PostGoodsReceiptAsync(grn, ct);
        await audit.WriteAsync(nameof(GoodsReceipt), grn.Id, "PostedEffects", null, "Inventory ledger + stock", ct);
    }

    private async Task ApplyPurchaseInvoicePostingAsync(PurchaseInvoice pi, CancellationToken ct) =>
        await accounting.PostPurchaseInvoiceAsync(pi, ct);

    private async Task ApplyDeliveryOrderPostingAsync(DeliveryOrder delivery, CancellationToken ct)
    {
        foreach (var line in delivery.Lines)
        {
            if (line.SalesOrderLineId is Guid soLineId)
            {
                var soLine = await db.SalesOrderLines.FirstAsync(x => x.Id == soLineId, ct);
                var remaining = soLine.Qty - soLine.DeliveredQty;
                if (line.Qty > remaining)
                    throw new InvalidOperationException($"Delivery quantity exceeds remaining SO quantity for product {line.ProductId}.");
                soLine.DeliveredQty += line.Qty;
            }
        }
        await db.SaveChangesAsync(ct);
        await inventory.PostDeliveryIssueAsync(delivery, ct);
        await audit.WriteAsync(nameof(DeliveryOrder), delivery.Id, "PostedEffects", null, "Inventory ledger + stock", ct);
        await accounting.PostDeliveryCogsAsync(delivery, ct);
    }

    private async Task ApplySalesInvoicePostingAsync(SalesInvoice invoice, CancellationToken ct) =>
        await accounting.PostSalesInvoiceAsync(invoice, ct);

    private async Task ApplyCustomerPaymentPostingAsync(CustomerPayment payment, CancellationToken ct) =>
        await accounting.PostCustomerPaymentAsync(payment, ct);

    private async Task<PurchaseOrderDto?> MapOrderAsync(Guid id, CancellationToken ct)
    {
        var dto = await db.PurchaseOrders.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product)
            .Where(x => x.Id == id)
            .Select(x => new PurchaseOrderDto(x.Id, x.OrderNo, x.OrderDate, x.PartyId, x.Party!.Name, x.WarehouseId, x.Status,
                x.SubTotal, x.Tax, x.GrandTotal, x.Remarks,
                x.Lines.Select(l => new InvoiceLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList(), null))
            .FirstOrDefaultAsync(ct);
        if (dto is null) return null;
        var entity = await db.PurchaseOrders.AsNoTracking().FirstAsync(x => x.Id == id, ct);
        var workflow = await DocumentWorkflowActorsResolver.ResolveAsync(
            db, nameof(PurchaseOrder), id, entity.DocumentId, entity.CreatedBy, entity.CreatedAt, ct);
        return dto with { Workflow = workflow };
    }

    private async Task<GoodsReceiptDto?> MapReceiptAsync(Guid id, CancellationToken ct)
    {
        var dto = await db.GoodsReceipts.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product)
            .Where(x => x.Id == id)
            .Select(x => new GoodsReceiptDto(x.Id, x.ReceiptNo, x.ReceiptDate, x.PartyId, x.Party!.Name, x.WarehouseId,
                x.PurchaseOrderId, x.Status, x.Remarks,
                x.Lines.Select(l => new InvoiceLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList(), null))
            .FirstOrDefaultAsync(ct);
        if (dto is null) return null;
        var entity = await db.GoodsReceipts.AsNoTracking().FirstAsync(x => x.Id == id, ct);
        var workflow = await DocumentWorkflowActorsResolver.ResolveAsync(
            db, nameof(GoodsReceipt), id, entity.DocumentId, entity.CreatedBy, entity.CreatedAt, ct);
        return dto with { Workflow = workflow };
    }

    private async Task<PurchaseInvoiceDto?> MapInvoiceAsync(Guid id, CancellationToken ct)
    {
        var dto = await db.PurchaseInvoices.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product)
            .Where(x => x.Id == id)
            .Select(x => new PurchaseInvoiceDto(x.Id, x.InvoiceNo, x.InvoiceDate, x.PartyId, x.Party!.Name, x.Status,
                x.SubTotal, x.Discount, x.Tax, x.GrandTotal, x.Remarks,
                x.Lines.Select(l => new InvoiceLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList(), null))
            .FirstOrDefaultAsync(ct);
        if (dto is null) return null;
        var entity = await db.PurchaseInvoices.AsNoTracking().FirstAsync(x => x.Id == id, ct);
        var workflow = await DocumentWorkflowActorsResolver.ResolveAsync(
            db, nameof(PurchaseInvoice), id, entity.DocumentId, entity.CreatedBy, entity.CreatedAt, ct);
        return dto with { Workflow = workflow };
    }

    private async Task<SalesOrderDto?> MapSalesOrderAsync(Guid id, CancellationToken ct)
    {
        var dto = await db.SalesOrders.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product)
            .Where(x => x.Id == id)
            .Select(x => new SalesOrderDto(x.Id, x.OrderNo, x.OrderDate, x.PartyId, x.Party!.Name, x.WarehouseId, x.Status,
                x.SubTotal, x.Tax, x.GrandTotal, x.Remarks,
                x.Lines.Select(l => new InvoiceLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList(), null))
            .FirstOrDefaultAsync(ct);
        if (dto is null) return null;
        var entity = await db.SalesOrders.AsNoTracking().FirstAsync(x => x.Id == id, ct);
        var workflow = await DocumentWorkflowActorsResolver.ResolveAsync(
            db, nameof(SalesOrder), id, entity.DocumentId, entity.CreatedBy, entity.CreatedAt, ct);
        return dto with { Workflow = workflow };
    }

    private async Task<DeliveryOrderDto?> MapDeliveryAsync(Guid id, CancellationToken ct)
    {
        var dto = await db.DeliveryOrders.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product)
            .Where(x => x.Id == id)
            .Select(x => new DeliveryOrderDto(x.Id, x.DeliveryNo, x.DeliveryDate, x.PartyId, x.Party!.Name, x.WarehouseId,
                x.SalesOrderId, x.Status, x.Remarks,
                x.Lines.Select(l => new InvoiceLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList(), null))
            .FirstOrDefaultAsync(ct);
        if (dto is null) return null;
        var entity = await db.DeliveryOrders.AsNoTracking().FirstAsync(x => x.Id == id, ct);
        var workflow = await DocumentWorkflowActorsResolver.ResolveAsync(
            db, nameof(DeliveryOrder), id, entity.DocumentId, entity.CreatedBy, entity.CreatedAt, ct);
        return dto with { Workflow = workflow };
    }

    private async Task<SalesInvoiceDocDto?> MapSalesInvoiceAsync(Guid id, CancellationToken ct)
    {
        var dto = await db.SalesInvoices.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product)
            .Where(x => x.Id == id)
            .Select(x => new SalesInvoiceDocDto(x.Id, x.InvoiceNo, x.InvoiceDate, x.PartyId, x.Party!.Name,
                x.DeliveryOrderId, x.SalesOrderId, x.Status,
                x.SubTotal, x.Discount, x.Tax, x.GrandTotal, x.PaidAmount, x.Remarks,
                x.Lines.Select(l => new InvoiceLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList(), null))
            .FirstOrDefaultAsync(ct);
        if (dto is null) return null;
        var entity = await db.SalesInvoices.AsNoTracking().FirstAsync(x => x.Id == id, ct);
        var workflow = await DocumentWorkflowActorsResolver.ResolveAsync(
            db, nameof(SalesInvoice), id, entity.DocumentId, entity.CreatedBy, entity.CreatedAt, ct);
        return dto with { Workflow = workflow };
    }

    private async Task<CustomerPaymentDto?> MapPaymentAsync(Guid id, CancellationToken ct)
    {
        var dto = await db.CustomerPayments.Include(x => x.Party).Include(x => x.Applications).ThenInclude(a => a.SalesInvoice)
            .Where(x => x.Id == id)
            .Select(x => new CustomerPaymentDto(x.Id, x.PaymentNo, x.PaymentDate, x.PartyId, x.Party!.Name, x.Amount, x.Status, x.Remarks,
                x.Applications.Select(a => new PaymentApplicationDto(a.Id, a.SalesInvoiceId, a.SalesInvoice!.InvoiceNo, a.AmountApplied)).ToList(), null))
            .FirstOrDefaultAsync(ct);
        if (dto is null) return null;
        var entity = await db.CustomerPayments.AsNoTracking().FirstAsync(x => x.Id == id, ct);
        var workflow = await DocumentWorkflowActorsResolver.ResolveAsync(
            db, nameof(CustomerPayment), id, entity.DocumentId, entity.CreatedBy, entity.CreatedAt, ct);
        return dto with { Workflow = workflow };
    }

    private async Task<SupplierPaymentDto?> MapSupplierPaymentAsync(Guid id, CancellationToken ct)
    {
        var dto = await db.SupplierPayments.Include(x => x.Party).Include(x => x.Applications).ThenInclude(a => a.PurchaseInvoice)
            .Where(x => x.Id == id)
            .Select(x => new SupplierPaymentDto(x.Id, x.PaymentNo, x.PaymentDate, x.PartyId, x.Party!.Name, x.CashBankAccountId, x.Amount, x.Status, x.Remarks,
                x.Applications.Select(a => new SupplierPaymentApplicationDto(a.Id, a.PurchaseInvoiceId, a.PurchaseInvoice!.InvoiceNo, a.AmountApplied)).ToList(), null))
            .FirstOrDefaultAsync(ct);
        if (dto is null) return null;
        var entity = await db.SupplierPayments.AsNoTracking().FirstAsync(x => x.Id == id, ct);
        var workflow = await DocumentWorkflowActorsResolver.ResolveAsync(
            db, nameof(SupplierPayment), id, entity.DocumentId, entity.CreatedBy, entity.CreatedAt, ct);
        return dto with { Workflow = workflow };
    }
}
