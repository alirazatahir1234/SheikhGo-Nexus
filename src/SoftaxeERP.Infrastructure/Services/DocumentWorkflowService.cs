using Microsoft.EntityFrameworkCore;
using SoftaxeERP.Application.DTOs;
using SoftaxeERP.Application.Interfaces;
using SoftaxeERP.Domain.Entities;
using SoftaxeERP.Domain.Enums;
using SoftaxeERP.Infrastructure.Persistence;

namespace SoftaxeERP.Infrastructure.Services;

/// <summary>
/// Shared document engine: Draft → Submitted → (Verified?) → (Approved?) → Posted (or Cancelled).
/// Steps depend on DocumentType.RequiresVerification / RequiresApproval.
/// Posting side-effects stay domain-specific (GRN → stock, PI → payable, PO → status only).
/// </summary>
public class DocumentWorkflowService(
    AppDbContext db,
    IAuditService audit,
    IPermissionService permissions,
    ITenantContext tenant) : IDocumentWorkflowService
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

        await EnsureActionPermissionAsync("PurchaseInvoice", action, ct);
        var wasPosted = pi.Status == DocumentStatus.Posted;
        await TransitionCoreAsync(pi, pi.DocumentId, nameof(PurchaseInvoice), "PI", pi.InvoiceNo, action, remarks, ct);

        if (!wasPosted && pi.Status == DocumentStatus.Posted)
            await ApplyPurchaseInvoicePostingAsync(pi, ct);

        return await MapInvoiceAsync(id, ct);
    }

    private async Task EnsureActionPermissionAsync(string docKind, string action, CancellationToken ct)
    {
        var key = action.Trim().ToLowerInvariant();
        var suffix = key switch
        {
            "submit" => "Submit",
            "verify" or "approve" => "Approve",
            "post" => "Post",
            "cancel" => "Edit",
            _ => throw new InvalidOperationException($"Unknown workflow action '{action}'.")
        };
        await permissions.EnsureAsync($"Procurement.{docKind}.{suffix}", ct);
    }

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

            var stock = await db.WarehouseStocks
                .FirstOrDefaultAsync(x => x.ProductId == line.ProductId && x.WarehouseId == grn.WarehouseId, ct);
            if (stock is null)
            {
                stock = new WarehouseStock { ProductId = line.ProductId, WarehouseId = grn.WarehouseId };
                db.WarehouseStocks.Add(stock);
            }
            stock.Quantity += line.Qty;
        }

        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(GoodsReceipt), grn.Id, "PostedEffects", null, "WarehouseStock increased", ct);
    }

    private async Task ApplyPurchaseInvoicePostingAsync(PurchaseInvoice pi, CancellationToken ct)
    {
        var balance = await db.PayableBalances.FirstOrDefaultAsync(x => x.PartyId == pi.PartyId, ct);
        if (balance is null)
        {
            balance = new PayableBalance { PartyId = pi.PartyId };
            db.PayableBalances.Add(balance);
        }
        balance.Balance += pi.GrandTotal;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(PurchaseInvoice), pi.Id, "PostedEffects", null, $"Payable +{pi.GrandTotal}", ct);
    }

    private Task<PurchaseOrderDto?> MapOrderAsync(Guid id, CancellationToken ct) =>
        db.PurchaseOrders.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product)
            .Where(x => x.Id == id)
            .Select(x => new PurchaseOrderDto(x.Id, x.OrderNo, x.OrderDate, x.PartyId, x.Party!.Name, x.WarehouseId, x.Status,
                x.SubTotal, x.Tax, x.GrandTotal, x.Remarks,
                x.Lines.Select(l => new InvoiceLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList()))
            .FirstOrDefaultAsync(ct);

    private Task<GoodsReceiptDto?> MapReceiptAsync(Guid id, CancellationToken ct) =>
        db.GoodsReceipts.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product)
            .Where(x => x.Id == id)
            .Select(x => new GoodsReceiptDto(x.Id, x.ReceiptNo, x.ReceiptDate, x.PartyId, x.Party!.Name, x.WarehouseId,
                x.PurchaseOrderId, x.Status, x.Remarks,
                x.Lines.Select(l => new InvoiceLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList()))
            .FirstOrDefaultAsync(ct);

    private Task<PurchaseInvoiceDto?> MapInvoiceAsync(Guid id, CancellationToken ct) =>
        db.PurchaseInvoices.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product)
            .Where(x => x.Id == id)
            .Select(x => new PurchaseInvoiceDto(x.Id, x.InvoiceNo, x.InvoiceDate, x.PartyId, x.Party!.Name, x.Status,
                x.SubTotal, x.Discount, x.Tax, x.GrandTotal, x.Remarks,
                x.Lines.Select(l => new InvoiceLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList()))
            .FirstOrDefaultAsync(ct);
}
