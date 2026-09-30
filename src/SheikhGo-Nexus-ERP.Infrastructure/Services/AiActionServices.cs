using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Entities;
using SheikhGoNexusERP.Domain.Enums;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

public class AiEntityResolver(AppDbContext db) : IAiEntityResolver
{
    public async Task<Party> ResolvePartyAsync(string hint, PartyType? type, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(hint))
            throw new InvalidOperationException("Party name or code is required.");
        var q = db.Parties.AsNoTracking().Where(x => x.IsActive);
        if (type is PartyType t) q = q.Where(x => x.PartyType == t);
        var matches = await MatchAsync(q, hint, x => x.Code, x => x.Name, ct);
        if (matches.Count == 0) throw new InvalidOperationException($"No party matched '{hint}'.");
        if (matches.Count > 1) throw new InvalidOperationException($"Ambiguous party '{hint}' — refine the name or code.");
        return matches[0];
    }

    public async Task<Product> ResolveProductAsync(string hint, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(hint))
            throw new InvalidOperationException("Product name or code is required.");
        var matches = await MatchAsync(db.Products.AsNoTracking().Where(x => x.IsActive), hint, x => x.Code, x => x.Name, ct);
        if (matches.Count == 0) throw new InvalidOperationException($"No product matched '{hint}'.");
        if (matches.Count > 1) throw new InvalidOperationException($"Ambiguous product '{hint}' — refine the name or code.");
        return matches[0];
    }

    public async Task<Warehouse> ResolveWarehouseAsync(string? hint, bool preferDefault, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(hint))
        {
            var def = await db.Warehouses.AsNoTracking().FirstOrDefaultAsync(x => x.IsDefault, ct)
                      ?? await db.Warehouses.AsNoTracking().FirstOrDefaultAsync(ct);
            return def ?? throw new InvalidOperationException("No warehouse found.");
        }
        var matches = await MatchAsync(db.Warehouses.AsNoTracking(), hint, x => x.Code, x => x.Name, ct);
        if (matches.Count == 0) throw new InvalidOperationException($"No warehouse matched '{hint}'.");
        if (matches.Count > 1) throw new InvalidOperationException($"Ambiguous warehouse '{hint}'.");
        return matches[0];
    }

    public async Task<Account> ResolveAccountAsync(string hint, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(hint))
            throw new InvalidOperationException("Account name or code is required.");
        // Prefer postable; fall back to any non-deleted account if name/code matches (COA parents are non-postable).
        var postable = await MatchAsync(
            db.Accounts.AsNoTracking().Where(x => x.IsPostable),
            hint, x => x.Code, x => x.Name, ct);
        if (postable.Count == 1) return postable[0];
        if (postable.Count > 1) throw new InvalidOperationException($"Ambiguous account '{hint}'.");

        var any = await MatchAsync(db.Accounts.AsNoTracking(), hint, x => x.Code, x => x.Name, ct);
        if (any.Count == 0) throw new InvalidOperationException($"No account matched '{hint}'.");
        if (any.Count > 1) throw new InvalidOperationException($"Ambiguous account '{hint}'.");
        if (!any[0].IsPostable)
            throw new InvalidOperationException($"Account '{hint}' is not postable.");
        return any[0];
    }

    private static async Task<List<T>> MatchAsync<T>(
        IQueryable<T> query,
        string hint,
        Func<T, string> code,
        Func<T, string> name,
        CancellationToken ct) where T : class
    {
        var list = await query.ToListAsync(ct);
        var h = hint.Trim();
        var exact = list.Where(x =>
            code(x).Equals(h, StringComparison.OrdinalIgnoreCase) ||
            name(x).Equals(h, StringComparison.OrdinalIgnoreCase)).ToList();
        if (exact.Count > 0) return exact;
        return list.Where(x =>
            code(x).Contains(h, StringComparison.OrdinalIgnoreCase) ||
            name(x).Contains(h, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}

public class AiActionPlanner(
    AppDbContext db,
    ITenantContext tenant,
    IPermissionService permissions,
    IAiEntityResolver entities,
    IAuditService audit) : IAiActionPlanner
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task<AiActionPreviewDto> ProposeAsync(Guid conversationId, AiIntentResolution resolution, string rawMessage, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Ai.Action.Propose", ct);
        EnsureScope(resolution.ActionSlots);

        var (actionType, permission) = MapIntent(resolution.Intent);
        await permissions.EnsureAsync(permission, ct);

        var payload = await BuildPayloadAsync(actionType, resolution, ct);
        var preview = BuildPreview(Guid.Empty, actionType, AiActionStatuses.Pending, permission, payload, []);

        var entity = new AiPendingAction
        {
            ConversationId = conversationId,
            UserId = tenant.UserId!.Value,
            CompanyId = tenant.CompanyId!.Value,
            BranchId = tenant.BranchId!.Value,
            FinancialYearId = tenant.FinancialYearId!.Value,
            ActionType = actionType,
            Status = AiActionStatuses.Pending,
            PermissionRequired = permission,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOpts),
            PreviewJson = JsonSerializer.Serialize(preview, JsonOpts),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };
        db.AiPendingActions.Add(entity);
        await db.SaveChangesAsync(ct);

        preview = BuildPreview(entity.Id, actionType, AiActionStatuses.Pending, permission, payload, []);
        entity.PreviewJson = JsonSerializer.Serialize(preview, JsonOpts);
        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(nameof(AiPendingAction), entity.Id, "AiActionPropose", null,
            JsonSerializer.Serialize(new { actionType, permission, success = true, companyId = tenant.CompanyId, branchId = tenant.BranchId }, JsonOpts), ct);

        return preview;
    }

    public async Task<AiActionPreviewDto> ProposePayloadAsync(
        Guid conversationId,
        string actionType,
        string permissionRequired,
        object payload,
        Guid? agentRunId = null,
        Guid? agentStepId = null,
        CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Ai.Action.Propose", ct);
        EnsureScope(null);
        await permissions.EnsureAsync(permissionRequired, ct);

        var preview = BuildPreview(Guid.Empty, actionType, AiActionStatuses.Pending, permissionRequired, payload, []);
        var entity = new AiPendingAction
        {
            ConversationId = conversationId,
            UserId = tenant.UserId!.Value,
            CompanyId = tenant.CompanyId!.Value,
            BranchId = tenant.BranchId!.Value,
            FinancialYearId = tenant.FinancialYearId!.Value,
            ActionType = actionType,
            Status = AiActionStatuses.Pending,
            PermissionRequired = permissionRequired,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOpts),
            PreviewJson = JsonSerializer.Serialize(preview, JsonOpts),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            AgentRunId = agentRunId,
            AgentStepId = agentStepId
        };
        db.AiPendingActions.Add(entity);
        await db.SaveChangesAsync(ct);

        preview = BuildPreview(entity.Id, actionType, AiActionStatuses.Pending, permissionRequired, payload, []);
        entity.PreviewJson = JsonSerializer.Serialize(preview, JsonOpts);
        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(nameof(AiPendingAction), entity.Id, "AiActionPropose", null,
            JsonSerializer.Serialize(new { actionType, permission = permissionRequired, success = true, agentRunId }, JsonOpts), ct);
        return preview;
    }

    private void EnsureScope(AiActionSlots? slots)
    {
        if (tenant.UserId is null) throw new UnauthorizedAccessException("User context required.");
        if (tenant.CompanyId is null || tenant.BranchId is null || tenant.FinancialYearId is null)
            throw new UnauthorizedAccessException("Company, branch, and financial year context are required for AI actions.");

        if (slots?.ForcedCompanyId is Guid fc && fc != tenant.CompanyId)
            throw new UnauthorizedAccessException("Cross-company AI actions are not allowed.");
        if (slots?.ForcedBranchId is Guid fb && fb != tenant.BranchId)
            throw new UnauthorizedAccessException("Cross-branch AI actions are not allowed.");
    }

    private static (string ActionType, string Permission) MapIntent(string intent) => intent switch
    {
        AiIntents.CreatePurchaseOrderDraft => (AiActionTypes.PurchaseOrderDraft, "Procurement.PurchaseOrder.Create"),
        AiIntents.CreateSalesOrderDraft => (AiActionTypes.SalesOrderDraft, "Sales.Order.Create"),
        AiIntents.CreateStockTransferDraft => (AiActionTypes.StockTransferDraft, "Inventory.Transfer.Create"),
        AiIntents.CreateCustomerPaymentDraft => (AiActionTypes.CustomerPaymentDraft, "Sales.Payment.Create"),
        AiIntents.CreateSupplierPaymentDraft => (AiActionTypes.SupplierPaymentDraft, "Finance.SupplierPayment.Create"),
        AiIntents.CreateJournalEntryDraft => (AiActionTypes.JournalEntryDraft, "Finance.Journal.Create"),
        _ => throw new InvalidOperationException($"Intent '{intent}' is not an action intent.")
    };

    private async Task<object> BuildPayloadAsync(string actionType, AiIntentResolution resolution, CancellationToken ct)
    {
        var slots = resolution.ActionSlots ?? new AiActionSlots();
        var today = DateTime.UtcNow.Date;

        return actionType switch
        {
            AiActionTypes.PurchaseOrderDraft => await BuildPoAsync(slots, today, ct),
            AiActionTypes.SalesOrderDraft => await BuildSoAsync(slots, today, ct),
            AiActionTypes.StockTransferDraft => await BuildStAsync(slots, today, ct),
            AiActionTypes.CustomerPaymentDraft => await BuildCpAsync(slots, today, ct),
            AiActionTypes.SupplierPaymentDraft => await BuildSpayAsync(slots, today, ct),
            AiActionTypes.JournalEntryDraft => await BuildJvAsync(slots, today, ct),
            _ => throw new InvalidOperationException($"Unknown action type {actionType}")
        };
    }

    private async Task<PurchaseOrderCreateDto> BuildPoAsync(AiActionSlots slots, DateTime today, CancellationToken ct)
    {
        var party = await entities.ResolvePartyAsync(slots.PartyHint ?? "Demo Supplier", PartyType.Supplier, ct);
        var product = await entities.ResolveProductAsync(slots.ProductHint ?? "Demo Product", ct);
        var qty = slots.Qty ?? 1m;
        var rate = slots.Rate ?? (product.PurchasePrice > 0 ? product.PurchasePrice : product.AverageCost);
        var line = new InvoiceLineDto(null, product.Id, product.Name, qty, rate, qty * rate);
        return new PurchaseOrderCreateDto(today, party.Id, null, 0m, "AI draft", false, [line]);
    }

    private async Task<SalesOrderCreateDto> BuildSoAsync(AiActionSlots slots, DateTime today, CancellationToken ct)
    {
        var party = await entities.ResolvePartyAsync(slots.PartyHint ?? "Demo Customer", PartyType.Customer, ct);
        var product = await entities.ResolveProductAsync(slots.ProductHint ?? "Demo Product", ct);
        var qty = slots.Qty ?? 1m;
        var rate = slots.Rate ?? (product.SalePrice > 0 ? product.SalePrice : product.AverageCost);
        var line = new InvoiceLineDto(null, product.Id, product.Name, qty, rate, qty * rate);
        return new SalesOrderCreateDto(today, party.Id, null, 0m, "AI draft", false, [line]);
    }

    private async Task<StockTransferCreateDto> BuildStAsync(AiActionSlots slots, DateTime today, CancellationToken ct)
    {
        var from = await entities.ResolveWarehouseAsync(slots.FromWarehouseHint, true, ct);
        var to = await entities.ResolveWarehouseAsync(slots.ToWarehouseHint ?? "Branch Warehouse", false, ct);
        if (from.Id == to.Id)
        {
            to = await db.Warehouses.AsNoTracking().FirstOrDefaultAsync(x => x.Id != from.Id, ct)
                 ?? throw new InvalidOperationException("Need two distinct warehouses for a stock transfer.");
        }
        var product = await entities.ResolveProductAsync(slots.ProductHint ?? "Demo Product", ct);
        var qty = slots.Qty ?? 1m;
        return new StockTransferCreateDto(today, from.Id, to.Id, "AI draft", false,
            [new StockTransferLineDto(null, product.Id, product.Name, qty)]);
    }

    private async Task<CustomerPaymentCreateDto> BuildCpAsync(AiActionSlots slots, DateTime today, CancellationToken ct)
    {
        var party = await entities.ResolvePartyAsync(slots.PartyHint ?? "Demo Customer", PartyType.Customer, ct);
        var amount = slots.Amount ?? slots.Qty ?? throw new InvalidOperationException("Payment amount is required.");
        var apps = await FifoSalesAppsAsync(party.Id, amount, ct);
        if (apps.Count == 0) throw new InvalidOperationException("No posted outstanding sales invoices for this customer.");
        return new CustomerPaymentCreateDto(today, party.Id, amount, "AI draft", false, apps);
    }

    private async Task<SupplierPaymentCreateDto> BuildSpayAsync(AiActionSlots slots, DateTime today, CancellationToken ct)
    {
        var party = await entities.ResolvePartyAsync(slots.PartyHint ?? "Demo Supplier", PartyType.Supplier, ct);
        var amount = slots.Amount ?? slots.Qty ?? throw new InvalidOperationException("Payment amount is required.");
        var apps = await FifoPurchaseAppsAsync(party.Id, amount, ct);
        if (apps.Count == 0) throw new InvalidOperationException("No posted outstanding purchase invoices for this supplier.");
        return new SupplierPaymentCreateDto(today, party.Id, amount, "AI draft", false, apps);
    }

    private async Task<JournalEntryCreateDto> BuildJvAsync(AiActionSlots slots, DateTime today, CancellationToken ct)
    {
        var amount = slots.Amount ?? slots.Qty ?? 100m;
        var debitHint = slots.DebitAccountHint ?? "Inventory";
        var creditHint = slots.CreditAccountHint ?? "Accounts Payable";
        var debit = await entities.ResolveAccountAsync(debitHint, ct);
        var credit = await entities.ResolveAccountAsync(creditHint, ct);
        if (debit.Id == credit.Id) throw new InvalidOperationException("Debit and credit accounts must differ.");
        return new JournalEntryCreateDto(today, slots.Narration ?? "AI draft journal", false,
        [
            new JournalLineDto(null, debit.Id, debit.Code, debit.Name, amount, 0m, null),
            new JournalLineDto(null, credit.Id, credit.Code, credit.Name, 0m, amount, null)
        ]);
    }

    private async Task<List<PaymentApplicationDto>> FifoSalesAppsAsync(Guid partyId, decimal amount, CancellationToken ct)
    {
        var invoices = await db.SalesInvoices.AsNoTracking()
            .Where(x => x.PartyId == partyId && x.Status == DocumentStatus.Posted && x.GrandTotal > x.PaidAmount)
            .OrderBy(x => x.InvoiceDate).ThenBy(x => x.CreatedAt)
            .ToListAsync(ct);
        var remaining = amount;
        var apps = new List<PaymentApplicationDto>();
        foreach (var inv in invoices)
        {
            if (remaining <= 0) break;
            var outstanding = inv.GrandTotal - inv.PaidAmount;
            var apply = Math.Min(outstanding, remaining);
            apps.Add(new PaymentApplicationDto(null, inv.Id, inv.InvoiceNo, apply));
            remaining -= apply;
        }
        return apps;
    }

    private async Task<List<SupplierPaymentApplicationDto>> FifoPurchaseAppsAsync(Guid partyId, decimal amount, CancellationToken ct)
    {
        var invoices = await db.PurchaseInvoices.AsNoTracking()
            .Where(x => x.PartyId == partyId && x.Status == DocumentStatus.Posted && x.GrandTotal > x.PaidAmount)
            .OrderBy(x => x.InvoiceDate).ThenBy(x => x.CreatedAt)
            .ToListAsync(ct);
        var remaining = amount;
        var apps = new List<SupplierPaymentApplicationDto>();
        foreach (var inv in invoices)
        {
            if (remaining <= 0) break;
            var outstanding = inv.GrandTotal - inv.PaidAmount;
            var apply = Math.Min(outstanding, remaining);
            apps.Add(new SupplierPaymentApplicationDto(null, inv.Id, inv.InvoiceNo, apply));
            remaining -= apply;
        }
        return apps;
    }

    internal static AiActionPreviewDto BuildPreview(
        Guid id, string actionType, string status, string permission, object payload, IReadOnlyList<string> errors,
        Guid? resultDocumentId = null, string? resultDocumentNo = null)
    {
        var fields = new List<AiActionFieldDto>();
        string summary = actionType;

        switch (payload)
        {
            case PurchaseOrderCreateDto po:
                summary = $"Purchase Order Draft — party {po.PartyId}, lines {po.Lines.Count}";
                var pol = po.Lines.FirstOrDefault();
                fields.Add(new("partyId", "Supplier Id", po.PartyId.ToString()));
                fields.Add(new("product", "Product", pol?.ProductName ?? pol?.ProductId.ToString(), false));
                fields.Add(new("qty", "Quantity", (pol?.Qty ?? 0).ToString("0.####"), true, "number"));
                fields.Add(new("rate", "Rate", (pol?.Rate ?? 0).ToString("0.####"), true, "number"));
                fields.Add(new("status", "Status", "DRAFT"));
                break;
            case SalesOrderCreateDto so:
                summary = $"Sales Order Draft — party {so.PartyId}, lines {so.Lines.Count}";
                var sol = so.Lines.FirstOrDefault();
                fields.Add(new("partyId", "Customer Id", so.PartyId.ToString()));
                fields.Add(new("product", "Product", sol?.ProductName ?? sol?.ProductId.ToString(), false));
                fields.Add(new("qty", "Quantity", (sol?.Qty ?? 0).ToString("0.####"), true, "number"));
                fields.Add(new("rate", "Rate", (sol?.Rate ?? 0).ToString("0.####"), true, "number"));
                fields.Add(new("status", "Status", "DRAFT"));
                break;
            case StockTransferCreateDto st:
                summary = $"Stock Transfer Draft — {st.FromWarehouseId} → {st.ToWarehouseId}";
                var stl = st.Lines.FirstOrDefault();
                fields.Add(new("fromWarehouseId", "From Warehouse", st.FromWarehouseId.ToString()));
                fields.Add(new("toWarehouseId", "To Warehouse", st.ToWarehouseId.ToString()));
                fields.Add(new("product", "Product", stl?.ProductName ?? stl?.ProductId.ToString()));
                fields.Add(new("qty", "Quantity", (stl?.Qty ?? 0).ToString("0.####"), true, "number"));
                fields.Add(new("status", "Status", "DRAFT"));
                break;
            case CustomerPaymentCreateDto cp:
                summary = $"Customer Payment Draft — {cp.Amount:0.####}";
                fields.Add(new("partyId", "Customer Id", cp.PartyId.ToString()));
                fields.Add(new("amount", "Amount", cp.Amount.ToString("0.####"), true, "number"));
                fields.Add(new("applications", "Applications", cp.Applications.Count.ToString()));
                fields.Add(new("status", "Status", "DRAFT"));
                break;
            case SupplierPaymentCreateDto sp:
                summary = $"Supplier Payment Draft — {sp.Amount:0.####}";
                fields.Add(new("partyId", "Supplier Id", sp.PartyId.ToString()));
                fields.Add(new("amount", "Amount", sp.Amount.ToString("0.####"), true, "number"));
                fields.Add(new("applications", "Applications", sp.Applications.Count.ToString()));
                fields.Add(new("status", "Status", "DRAFT"));
                break;
            case JournalEntryCreateDto jv:
                summary = $"Journal Entry Draft — {jv.Lines.Count} lines";
                fields.Add(new("postingDate", "Posting Date", jv.PostingDate.ToString("yyyy-MM-dd")));
                fields.Add(new("narration", "Narration", jv.Narration));
                fields.Add(new("lines", "Lines", string.Join("; ", jv.Lines.Select(l => $"{l.AccountCode} Dr {l.Debit} Cr {l.Credit}"))));
                fields.Add(new("status", "Status", "DRAFT"));
                break;
        }

        return new AiActionPreviewDto(id, actionType, status, permission, summary, fields, errors, resultDocumentId, resultDocumentNo);
    }

    internal static object DeserializePayload(string actionType, string json)
    {
        return actionType switch
        {
            AiActionTypes.PurchaseOrderDraft => JsonSerializer.Deserialize<PurchaseOrderCreateDto>(json, JsonOpts)!,
            AiActionTypes.SalesOrderDraft => JsonSerializer.Deserialize<SalesOrderCreateDto>(json, JsonOpts)!,
            AiActionTypes.StockTransferDraft => JsonSerializer.Deserialize<StockTransferCreateDto>(json, JsonOpts)!,
            AiActionTypes.CustomerPaymentDraft => JsonSerializer.Deserialize<CustomerPaymentCreateDto>(json, JsonOpts)!,
            AiActionTypes.SupplierPaymentDraft => JsonSerializer.Deserialize<SupplierPaymentCreateDto>(json, JsonOpts)!,
            AiActionTypes.JournalEntryDraft => JsonSerializer.Deserialize<JournalEntryCreateDto>(json, JsonOpts)!,
            _ => throw new InvalidOperationException($"Unknown action type {actionType}")
        };
    }
}

public class AiActionExecutor(
    AppDbContext db,
    ITenantContext tenant,
    IPermissionService permissions,
    IPurchaseService purchases,
    ISalesService sales,
    IStockTransferService transfers,
    IFinanceService finance,
    IAuditService audit) : IAiActionExecutor
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task<AiActionPreviewDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Ai.Chat.View", ct);
        var entity = await LoadOwnedAsync(id, ct);
        if (entity is null) return null;
        ExpireIfNeeded(entity);
        var payload = AiActionPlanner.DeserializePayload(entity.ActionType, entity.PayloadJson);
        return AiActionPlanner.BuildPreview(entity.Id, entity.ActionType, entity.Status, entity.PermissionRequired, payload, [], entity.ResultDocumentId, entity.ResultDocumentNo);
    }

    public async Task<AiActionPreviewDto> UpdateAsync(Guid id, AiActionUpdateRequest request, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Ai.Action.Propose", ct);
        RejectScopeOverride(request.CompanyId, request.BranchId);
        var entity = await LoadOwnedAsync(id, ct) ?? throw new InvalidOperationException("Action not found.");
        EnsurePending(entity);
        await permissions.EnsureAsync(entity.PermissionRequired, ct);

        var payload = ApplyUpdate(entity.ActionType, AiActionPlanner.DeserializePayload(entity.ActionType, entity.PayloadJson), request);
        entity.PayloadJson = JsonSerializer.Serialize(payload, JsonOpts);
        var preview = AiActionPlanner.BuildPreview(entity.Id, entity.ActionType, entity.Status, entity.PermissionRequired, payload, []);
        entity.PreviewJson = JsonSerializer.Serialize(preview, JsonOpts);
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return preview;
    }

    public async Task<AiActionConfirmResponse> ConfirmAsync(Guid id, AiActionConfirmRequest? request, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Ai.Action.Confirm", ct);
        RejectScopeOverride(request?.CompanyId, request?.BranchId);

        var entity = await LoadOwnedAsync(id, ct) ?? throw new InvalidOperationException("Action not found.");
        EnsurePending(entity);
        EnsureScopeMatch(entity);
        await permissions.EnsureAsync(entity.PermissionRequired, ct);

        try
        {
            var payload = AiActionPlanner.DeserializePayload(entity.ActionType, entity.PayloadJson);
            var (docId, docNo) = await ExecuteCreateAsync(entity.ActionType, payload, ct);
            entity.Status = AiActionStatuses.Confirmed;
            entity.ResultDocumentId = docId;
            entity.ResultDocumentNo = docNo;
            entity.ErrorMessage = null;
            entity.UpdatedAt = DateTime.UtcNow;
            var preview = AiActionPlanner.BuildPreview(entity.Id, entity.ActionType, entity.Status, entity.PermissionRequired, payload, [], docId, docNo);
            entity.PreviewJson = JsonSerializer.Serialize(preview, JsonOpts);
            await db.SaveChangesAsync(ct);

            await audit.WriteAsync(nameof(AiPendingAction), entity.Id, "AiActionConfirm", null,
                JsonSerializer.Serialize(new { entity.ActionType, entity.PermissionRequired, success = true, documentNo = docNo, documentId = docId }, JsonOpts), ct);

            return new AiActionConfirmResponse(entity.Id, entity.Status, docId, docNo,
                $"Created draft {docNo}. AI did not post or approve the document.");
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            entity.Status = AiActionStatuses.Failed;
            entity.ErrorMessage = ex.Message;
            entity.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync(nameof(AiPendingAction), entity.Id, "AiActionConfirm", null,
                JsonSerializer.Serialize(new { entity.ActionType, success = false, error = ex.Message }, JsonOpts), ct);
            throw new InvalidOperationException(ex.Message);
        }
    }

    public async Task<AiActionPreviewDto> CancelAsync(Guid id, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Ai.Action.Propose", ct);
        var entity = await LoadOwnedAsync(id, ct) ?? throw new InvalidOperationException("Action not found.");
        EnsurePending(entity);
        entity.Status = AiActionStatuses.Cancelled;
        entity.UpdatedAt = DateTime.UtcNow;
        var payload = AiActionPlanner.DeserializePayload(entity.ActionType, entity.PayloadJson);
        var preview = AiActionPlanner.BuildPreview(entity.Id, entity.ActionType, entity.Status, entity.PermissionRequired, payload, []);
        entity.PreviewJson = JsonSerializer.Serialize(preview, JsonOpts);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(AiPendingAction), entity.Id, "AiActionCancel", null,
            JsonSerializer.Serialize(new { entity.ActionType, success = true }, JsonOpts), ct);
        return preview;
    }

    private async Task<(Guid Id, string No)> ExecuteCreateAsync(string actionType, object payload, CancellationToken ct)
    {
        switch (actionType)
        {
            case AiActionTypes.PurchaseOrderDraft:
            {
                var dto = ForceDraft((PurchaseOrderCreateDto)payload);
                var created = await purchases.CreateOrderAsync(dto, ct);
                return (created.Id, created.OrderNo);
            }
            case AiActionTypes.SalesOrderDraft:
            {
                var dto = ForceDraft((SalesOrderCreateDto)payload);
                var created = await sales.CreateOrderAsync(dto, ct);
                return (created.Id, created.OrderNo);
            }
            case AiActionTypes.StockTransferDraft:
            {
                var dto = ForceDraft((StockTransferCreateDto)payload);
                var created = await transfers.CreateAsync(dto, ct);
                return (created.Id, created.TransferNo);
            }
            case AiActionTypes.CustomerPaymentDraft:
            {
                var dto = ForceDraft((CustomerPaymentCreateDto)payload);
                var created = await sales.CreatePaymentAsync(dto, ct);
                return (created.Id, created.PaymentNo);
            }
            case AiActionTypes.SupplierPaymentDraft:
            {
                var dto = ForceDraft((SupplierPaymentCreateDto)payload);
                var created = await finance.CreateSupplierPaymentAsync(dto, ct);
                return (created.Id, created.PaymentNo);
            }
            case AiActionTypes.JournalEntryDraft:
            {
                var dto = ForceDraft((JournalEntryCreateDto)payload);
                var created = await finance.CreateJournalAsync(dto, ct);
                return (created.Id, created.JournalNo);
            }
            default:
                throw new InvalidOperationException($"Unknown action type {actionType}");
        }
    }

    private static PurchaseOrderCreateDto ForceDraft(PurchaseOrderCreateDto d) =>
        d with { Post = false };
    private static SalesOrderCreateDto ForceDraft(SalesOrderCreateDto d) =>
        d with { Post = false };
    private static StockTransferCreateDto ForceDraft(StockTransferCreateDto d) =>
        d with { Post = false };
    private static CustomerPaymentCreateDto ForceDraft(CustomerPaymentCreateDto d) =>
        d with { Post = false };
    private static SupplierPaymentCreateDto ForceDraft(SupplierPaymentCreateDto d) =>
        d with { Post = false };
    private static JournalEntryCreateDto ForceDraft(JournalEntryCreateDto d) =>
        d with { Post = false };

    private static object ApplyUpdate(string actionType, object payload, AiActionUpdateRequest request)
    {
        switch (actionType)
        {
            case AiActionTypes.PurchaseOrderDraft:
            {
                var po = (PurchaseOrderCreateDto)payload;
                var lines = request.Lines ?? po.Lines.Select(l =>
                {
                    var qty = request.Qty ?? l.Qty;
                    var rate = request.Rate ?? l.Rate;
                    return l with { Qty = qty, Rate = rate, Amount = qty * rate };
                }).ToList();
                return po with { Lines = lines, Tax = request.Tax ?? po.Tax, Remarks = request.Remarks ?? po.Remarks, Post = false };
            }
            case AiActionTypes.SalesOrderDraft:
            {
                var so = (SalesOrderCreateDto)payload;
                var lines = request.Lines ?? so.Lines.Select(l =>
                {
                    var qty = request.Qty ?? l.Qty;
                    var rate = request.Rate ?? l.Rate;
                    return l with { Qty = qty, Rate = rate, Amount = qty * rate };
                }).ToList();
                return so with { Lines = lines, Tax = request.Tax ?? so.Tax, Remarks = request.Remarks ?? so.Remarks, Post = false };
            }
            case AiActionTypes.StockTransferDraft:
            {
                var st = (StockTransferCreateDto)payload;
                var lines = st.Lines.Select(l => l with { Qty = request.Qty ?? l.Qty }).ToList();
                return st with
                {
                    Lines = lines,
                    FromWarehouseId = request.FromWarehouseId ?? st.FromWarehouseId,
                    ToWarehouseId = request.ToWarehouseId ?? st.ToWarehouseId,
                    Remarks = request.Remarks ?? st.Remarks,
                    Post = false
                };
            }
            case AiActionTypes.CustomerPaymentDraft:
            {
                var cp = (CustomerPaymentCreateDto)payload;
                var amount = request.Amount ?? cp.Amount;
                var apps = request.Applications ?? cp.Applications;
                return cp with { Amount = amount, Applications = apps, Remarks = request.Remarks ?? cp.Remarks, Post = false };
            }
            case AiActionTypes.SupplierPaymentDraft:
            {
                var sp = (SupplierPaymentCreateDto)payload;
                var amount = request.Amount ?? sp.Amount;
                var apps = request.SupplierApplications ?? sp.Applications;
                return sp with { Amount = amount, Applications = apps, Remarks = request.Remarks ?? sp.Remarks, Post = false };
            }
            case AiActionTypes.JournalEntryDraft:
            {
                var jv = (JournalEntryCreateDto)payload;
                return jv with
                {
                    Narration = request.Remarks ?? jv.Narration,
                    Lines = request.JournalLines ?? jv.Lines,
                    Post = false
                };
            }
            default:
                throw new InvalidOperationException($"Unknown action type {actionType}");
        }
    }

    private void RejectScopeOverride(Guid? companyId, Guid? branchId)
    {
        if (companyId is Guid c && tenant.CompanyId is Guid tc && c != tc)
            throw new UnauthorizedAccessException("Cross-company AI actions are not allowed.");
        if (branchId is Guid b && tenant.BranchId is Guid tb && b != tb)
            throw new UnauthorizedAccessException("Cross-branch AI actions are not allowed.");
        if (companyId is Guid c2 && tenant.CompanyId is null)
            throw new UnauthorizedAccessException("Company context required.");
        if (branchId is Guid b2 && tenant.BranchId is null)
            throw new UnauthorizedAccessException("Branch context required.");
    }

    private void EnsureScopeMatch(AiPendingAction entity)
    {
        if (tenant.CompanyId != entity.CompanyId)
            throw new UnauthorizedAccessException("Cross-company AI actions are not allowed.");
        if (tenant.BranchId != entity.BranchId)
            throw new UnauthorizedAccessException("Cross-branch AI actions are not allowed.");
        if (tenant.FinancialYearId != entity.FinancialYearId)
            throw new UnauthorizedAccessException("Financial year mismatch for AI action.");
    }

    private void EnsurePending(AiPendingAction entity)
    {
        ExpireIfNeeded(entity);
        if (entity.Status != AiActionStatuses.Pending)
            throw new InvalidOperationException($"Action is {entity.Status} and cannot be modified.");
    }

    private void ExpireIfNeeded(AiPendingAction entity)
    {
        if (entity.Status == AiActionStatuses.Pending && entity.ExpiresAt < DateTime.UtcNow)
            entity.Status = AiActionStatuses.Expired;
    }

    private async Task<AiPendingAction?> LoadOwnedAsync(Guid id, CancellationToken ct)
    {
        if (tenant.UserId is null) return null;
        return await db.AiPendingActions.FirstOrDefaultAsync(x => x.Id == id && x.UserId == tenant.UserId, ct);
    }
}
