using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Entities;
using SheikhGoNexusERP.Domain.Enums;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

public class AiAgentOrchestrator(
    AppDbContext db,
    ITenantContext tenant,
    IPermissionService permissions,
    IInventoryService inventory,
    IAiActionPlanner planner,
    IAiActionExecutor executor,
    IAuditService audit) : IAiAgentOrchestrator
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task<AiAgentRunDto> StartAsync(Guid conversationId, AiIntentResolution resolution, string goal, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Ai.Agent.Run", ct);
        EnsureScope(resolution.ActionSlots);

        if (resolution.Intent != AiIntents.RunInventoryReplenishmentAgent)
            throw new InvalidOperationException("Only the Inventory Replenishment agent is available in Phase 10C.");

        var run = new AiAgentRun
        {
            UserId = tenant.UserId!.Value,
            CompanyId = tenant.CompanyId!.Value,
            BranchId = tenant.BranchId!.Value,
            FinancialYearId = tenant.FinancialYearId!.Value,
            ConversationId = conversationId,
            AgentType = AiAgentTypes.InventoryReplenishment,
            Goal = string.IsNullOrWhiteSpace(goal) ? "Replenish products below minimum stock" : goal.Trim(),
            Status = AiAgentRunStatuses.Running,
            StartedAt = DateTime.UtcNow,
            PlanJson = "{}"
        };
        db.AiAgentRuns.Add(run);
        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(nameof(AiAgentRun), run.Id, "AiAgentStart", null,
            JsonSerializer.Serialize(new { run.AgentType, run.Goal, success = true }, JsonOpts), ct);

        try
        {
            await RunReplenishmentAsync(run, conversationId, ct);
        }
        catch (UnauthorizedAccessException ex)
        {
            run.Status = AiAgentRunStatuses.Failed;
            run.FailureReason = ex.Message;
            run.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            throw;
        }
        catch (Exception ex)
        {
            run.Status = AiAgentRunStatuses.Failed;
            run.FailureReason = ex.Message;
            run.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            throw new InvalidOperationException(ex.Message);
        }

        return await MapRunAsync(run.Id, ct) ?? throw new InvalidOperationException("Agent run missing after start.");
    }

    public async Task<IReadOnlyList<AiAgentRunDto>> ListAsync(CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Ai.Agent.Run", ct);
        if (tenant.UserId is null) return [];
        var ids = await db.AiAgentRuns.AsNoTracking()
            .Where(x => x.UserId == tenant.UserId)
            .OrderByDescending(x => x.StartedAt)
            .Select(x => x.Id)
            .Take(30)
            .ToListAsync(ct);
        var list = new List<AiAgentRunDto>();
        foreach (var id in ids)
        {
            var dto = await MapRunAsync(id, ct);
            if (dto is not null) list.Add(dto);
        }
        return list;
    }

    public async Task<AiAgentRunDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Ai.Agent.Run", ct);
        return await MapRunAsync(id, ct);
    }

    public async Task<AiAgentConfirmResponse> ConfirmAsync(Guid id, AiAgentConfirmRequest? request, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Ai.Agent.Confirm", ct);
        RejectScopeOverride(request?.CompanyId, request?.BranchId);

        var run = await LoadOwnedRunAsync(id, ct) ?? throw new InvalidOperationException("Agent run not found.");
        if (run.Status != AiAgentRunStatuses.WaitingConfirmation)
            throw new InvalidOperationException($"Agent run is {run.Status} and cannot be confirmed.");

        EnsureRunScope(run);

        var pendingIds = await db.AiPendingActions
            .Where(x => x.AgentRunId == run.Id && x.Status == AiActionStatuses.Pending)
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.Id)
            .ToListAsync(ct);

        var results = new List<AiActionConfirmResponse>();
        foreach (var pendingId in pendingIds)
        {
            var result = await executor.ConfirmAsync(pendingId, new AiActionConfirmRequest(request?.CompanyId, request?.BranchId), ct);
            results.Add(result);
        }

        // Mark prepare steps succeeded; run verify step
        var prepareSteps = await db.AiAgentSteps
            .Where(x => x.AgentRunId == run.Id && x.Status == AiAgentStepStatuses.WaitingConfirm)
            .ToListAsync(ct);
        foreach (var step in prepareSteps)
        {
            step.Status = AiAgentStepStatuses.Succeeded;
            step.ExecutedAt = DateTime.UtcNow;
        }

        var verify = await EnsureStepAsync(run, 4, "verifyCreated", requiresConfirm: false, ct);
        verify.Status = AiAgentStepStatuses.Running;
        var docs = results.Select(r => new { r.DocumentNo, r.DocumentId, r.Status }).ToList();
        verify.OutputJson = JsonSerializer.Serialize(docs, JsonOpts);
        verify.Status = AiAgentStepStatuses.Succeeded;
        verify.ExecutedAt = DateTime.UtcNow;

        run.Status = AiAgentRunStatuses.Completed;
        run.CompletedAt = DateTime.UtcNow;
        run.PlanJson = JsonSerializer.Serialize(new
        {
            confirmed = true,
            documents = docs
        }, JsonOpts);
        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(nameof(AiAgentRun), run.Id, "AiAgentConfirm", null,
            JsonSerializer.Serialize(new { success = true, count = results.Count, documents = docs }, JsonOpts), ct);

        return new AiAgentConfirmResponse(run.Id, run.Status, results,
            results.Count == 0
                ? "No pending actions to confirm."
                : $"Created {results.Count} draft purchase order(s). AI did not post or approve.");
    }

    public async Task<AiAgentRunDto> CancelAsync(Guid id, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Ai.Agent.Cancel", ct);
        var run = await LoadOwnedRunAsync(id, ct) ?? throw new InvalidOperationException("Agent run not found.");
        if (run.Status is AiAgentRunStatuses.Completed or AiAgentRunStatuses.Cancelled)
            throw new InvalidOperationException($"Agent run is already {run.Status}.");

        var pendings = await db.AiPendingActions
            .Where(x => x.AgentRunId == run.Id && x.Status == AiActionStatuses.Pending)
            .ToListAsync(ct);
        foreach (var p in pendings)
        {
            try { await executor.CancelAsync(p.Id, ct); }
            catch { /* already cancelled/expired */ }
        }

        var openSteps = await db.AiAgentSteps
            .Where(x => x.AgentRunId == run.Id &&
                        (x.Status == AiAgentStepStatuses.WaitingConfirm || x.Status == AiAgentStepStatuses.Pending || x.Status == AiAgentStepStatuses.Running))
            .ToListAsync(ct);
        foreach (var s in openSteps)
        {
            s.Status = AiAgentStepStatuses.Skipped;
            s.ExecutedAt = DateTime.UtcNow;
        }

        run.Status = AiAgentRunStatuses.Cancelled;
        run.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(nameof(AiAgentRun), run.Id, "AiAgentCancel", null,
            JsonSerializer.Serialize(new { success = true }, JsonOpts), ct);

        return (await MapRunAsync(run.Id, ct))!;
    }

    private async Task RunReplenishmentAsync(AiAgentRun run, Guid conversationId, CancellationToken ct)
    {
        // Step 1: read low stock
        var step1 = await EnsureStepAsync(run, 1, "readLowStock", false, ct);
        step1.Status = AiAgentStepStatuses.Running;
        await db.SaveChangesAsync(ct);

        await EnsureInventoryViewAsync(ct);
        var stock = await inventory.GetStockAsync(null, ct);
        var shortages = stock
            .Where(x => x.ReorderLevel > 0 && x.StockQty <= x.ReorderLevel)
            .GroupBy(x => x.ProductId)
            .Select(g =>
            {
                var first = g.First();
                var qty = g.Sum(x => x.StockQty);
                var reorder = first.ReorderLevel;
                var shortage = Math.Max(0, reorder - qty);
                return new ShortageLine(first.ProductId, first.Code, first.Name, qty, reorder, shortage);
            })
            .Where(x => x.ShortageQty > 0)
            .OrderBy(x => x.Code)
            .ToList();

        step1.InputJson = "{}";
        step1.OutputJson = JsonSerializer.Serialize(shortages, JsonOpts);
        step1.Status = AiAgentStepStatuses.Succeeded;
        step1.ExecutedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await AuditStepAsync(run.Id, step1, ct);

        if (shortages.Count == 0)
        {
            var skip2 = await EnsureStepAsync(run, 2, "resolveSuppliers", false, ct);
            skip2.Status = AiAgentStepStatuses.Skipped;
            skip2.OutputJson = "[]";
            skip2.ExecutedAt = DateTime.UtcNow;
            var skip3 = await EnsureStepAsync(run, 3, "preparePurchaseOrders", true, ct);
            skip3.Status = AiAgentStepStatuses.Skipped;
            skip3.OutputJson = "[]";
            skip3.ExecutedAt = DateTime.UtcNow;
            var skip4 = await EnsureStepAsync(run, 4, "verifyCreated", false, ct);
            skip4.Status = AiAgentStepStatuses.Skipped;
            skip4.ExecutedAt = DateTime.UtcNow;
            run.Status = AiAgentRunStatuses.Completed;
            run.CompletedAt = DateTime.UtcNow;
            run.PlanJson = JsonSerializer.Serialize(new { shortages = Array.Empty<object>(), proposedOrders = Array.Empty<object>(), message = "No products below minimum stock." }, JsonOpts);
            await db.SaveChangesAsync(ct);
            return;
        }

        // Step 2: resolve suppliers
        var step2 = await EnsureStepAsync(run, 2, "resolveSuppliers", false, ct);
        step2.Status = AiAgentStepStatuses.Running;
        await db.SaveChangesAsync(ct);

        var defaultSupplier = await db.Parties.AsNoTracking()
            .Where(x => x.IsActive && x.PartyType == PartyType.Supplier)
            .OrderBy(x => x.Code)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No active supplier found to prepare purchase orders.");

        var resolved = new List<ResolvedShortage>();
        foreach (var s in shortages)
        {
            var supplierId = await ResolveLastSupplierAsync(s.ProductId, ct) ?? defaultSupplier.Id;
            var party = await db.Parties.AsNoTracking().FirstAsync(x => x.Id == supplierId, ct);
            var product = await db.Products.AsNoTracking().FirstAsync(x => x.Id == s.ProductId, ct);
            var rate = product.PurchasePrice > 0 ? product.PurchasePrice : product.AverageCost;
            resolved.Add(new ResolvedShortage(s, party.Id, party.Name, rate));
        }

        step2.OutputJson = JsonSerializer.Serialize(resolved.Select(r => new
        {
            r.Shortage.ProductId,
            r.Shortage.Code,
            r.Shortage.Name,
            r.Shortage.ShortageQty,
            r.SupplierId,
            r.SupplierName
        }), JsonOpts);
        step2.Status = AiAgentStepStatuses.Succeeded;
        step2.ExecutedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await AuditStepAsync(run.Id, step2, ct);

        // Step 3: prepare PO drafts (confirmation required)
        await permissions.EnsureAsync("Ai.Action.Propose", ct);
        await permissions.EnsureAsync("Procurement.PurchaseOrder.Create", ct);

        var step3 = await EnsureStepAsync(run, 3, "preparePurchaseOrders", true, ct);
        step3.Status = AiAgentStepStatuses.Running;
        await db.SaveChangesAsync(ct);

        var warehouse = await db.Warehouses.AsNoTracking().FirstOrDefaultAsync(x => x.IsDefault, ct)
                        ?? await db.Warehouses.AsNoTracking().FirstOrDefaultAsync(ct);
        var today = DateTime.UtcNow.Date;
        var proposed = new List<object>();
        var pendingPreviews = new List<AiActionPreviewDto>();

        foreach (var group in resolved.GroupBy(x => x.SupplierId))
        {
            var supplier = group.First();
            var lines = group.Select(g =>
            {
                var qty = g.Shortage.ShortageQty;
                var rate = g.Rate;
                return new InvoiceLineDto(null, g.Shortage.ProductId, g.Shortage.Name, qty, rate, qty * rate);
            }).ToList();

            var payload = new PurchaseOrderCreateDto(
                today,
                group.Key,
                warehouse?.Id,
                0m,
                "AI agent replenishment",
                false,
                lines);

            // Create a WaitingConfirm step row per PO for LinkedPendingActionId
            var poStep = new AiAgentStep
            {
                AgentRunId = run.Id,
                StepNo = 3,
                ToolName = "preparePurchaseOrders",
                Status = AiAgentStepStatuses.WaitingConfirm,
                RequiresConfirmation = true,
                InputJson = JsonSerializer.Serialize(new { supplierId = group.Key, lineCount = lines.Count }, JsonOpts)
            };
            db.AiAgentSteps.Add(poStep);
            await db.SaveChangesAsync(ct);

            var preview = await planner.ProposePayloadAsync(
                conversationId,
                AiActionTypes.PurchaseOrderDraft,
                "Procurement.PurchaseOrder.Create",
                payload,
                run.Id,
                poStep.Id,
                ct);

            poStep.LinkedPendingActionId = preview.Id;
            poStep.OutputJson = JsonSerializer.Serialize(preview, JsonOpts);
            poStep.ExecutedAt = DateTime.UtcNow;
            pendingPreviews.Add(preview);
            proposed.Add(new
            {
                pendingActionId = preview.Id,
                supplierId = group.Key,
                supplierName = supplier.SupplierName,
                lines = lines.Select(l => $"{l.ProductName}: {l.Qty:0.####} @ {l.Rate:0.####}"),
                totalQty = lines.Sum(l => l.Qty)
            });
        }

        // Mark template step3 as succeeded summary; per-PO steps hold WaitingConfirm
        step3.Status = AiAgentStepStatuses.Succeeded;
        step3.RequiresConfirmation = true;
        step3.OutputJson = JsonSerializer.Serialize(proposed, JsonOpts);
        step3.ExecutedAt = DateTime.UtcNow;

        run.Status = AiAgentRunStatuses.WaitingConfirmation;
        run.PlanJson = JsonSerializer.Serialize(new
        {
            shortages,
            proposedOrders = proposed
        }, JsonOpts);
        await db.SaveChangesAsync(ct);
        await AuditStepAsync(run.Id, step3, ct);
    }

    private async Task<Guid?> ResolveLastSupplierAsync(Guid productId, CancellationToken ct)
    {
        var fromPo = await db.PurchaseOrders.AsNoTracking()
            .Where(po => po.Lines.Any(l => l.ProductId == productId))
            .OrderByDescending(po => po.OrderDate).ThenByDescending(po => po.CreatedAt)
            .Select(po => (Guid?)po.PartyId)
            .FirstOrDefaultAsync(ct);
        if (fromPo is Guid) return fromPo;

        var fromPi = await (
            from line in db.PurchaseInvoiceLines.AsNoTracking()
            join inv in db.PurchaseInvoices.AsNoTracking() on line.PurchaseInvoiceId equals inv.Id
            where line.ProductId == productId
            orderby inv.InvoiceDate descending, inv.CreatedAt descending
            select (Guid?)inv.PartyId
        ).FirstOrDefaultAsync(ct);
        return fromPi;
    }

    private async Task EnsureInventoryViewAsync(CancellationToken ct)
    {
        if (await permissions.HasAsync("Reports.Inventory.View", ct)) return;
        if (await permissions.HasAsync("Inventory.Reports.View", ct)) return;
        await permissions.EnsureAsync("Reports.Inventory.View", ct);
    }

    private async Task<AiAgentStep> EnsureStepAsync(AiAgentRun run, int stepNo, string tool, bool requiresConfirm, CancellationToken ct)
    {
        var existing = await db.AiAgentSteps.FirstOrDefaultAsync(
            x => x.AgentRunId == run.Id && x.StepNo == stepNo && x.ToolName == tool && x.LinkedPendingActionId == null, ct);
        if (existing is not null) return existing;
        var step = new AiAgentStep
        {
            AgentRunId = run.Id,
            StepNo = stepNo,
            ToolName = tool,
            Status = AiAgentStepStatuses.Pending,
            RequiresConfirmation = requiresConfirm
        };
        db.AiAgentSteps.Add(step);
        await db.SaveChangesAsync(ct);
        return step;
    }

    private Task AuditStepAsync(Guid runId, AiAgentStep step, CancellationToken ct) =>
        audit.WriteAsync(nameof(AiAgentRun), runId, "AiAgentStep", null,
            JsonSerializer.Serialize(new { step.StepNo, step.ToolName, step.Status, success = step.Status == AiAgentStepStatuses.Succeeded }, JsonOpts), ct);

    private void EnsureScope(AiActionSlots? slots)
    {
        if (tenant.UserId is null) throw new UnauthorizedAccessException("User context required.");
        if (tenant.CompanyId is null || tenant.BranchId is null || tenant.FinancialYearId is null)
            throw new UnauthorizedAccessException("Company, branch, and financial year context are required for AI agents.");
        if (slots?.ForcedCompanyId is Guid fc && fc != tenant.CompanyId)
            throw new UnauthorizedAccessException("Cross-company AI actions are not allowed.");
        if (slots?.ForcedBranchId is Guid fb && fb != tenant.BranchId)
            throw new UnauthorizedAccessException("Cross-branch AI actions are not allowed.");
    }

    private void EnsureRunScope(AiAgentRun run)
    {
        if (tenant.CompanyId != run.CompanyId)
            throw new UnauthorizedAccessException("Cross-company AI actions are not allowed.");
        if (tenant.BranchId != run.BranchId)
            throw new UnauthorizedAccessException("Cross-branch AI actions are not allowed.");
        if (tenant.FinancialYearId != run.FinancialYearId)
            throw new UnauthorizedAccessException("Financial year mismatch for AI agent.");
    }

    private void RejectScopeOverride(Guid? companyId, Guid? branchId)
    {
        if (companyId is Guid c && tenant.CompanyId is Guid tc && c != tc)
            throw new UnauthorizedAccessException("Cross-company AI actions are not allowed.");
        if (branchId is Guid b && tenant.BranchId is Guid tb && b != tb)
            throw new UnauthorizedAccessException("Cross-branch AI actions are not allowed.");
    }

    private async Task<AiAgentRun?> LoadOwnedRunAsync(Guid id, CancellationToken ct)
    {
        if (tenant.UserId is null) return null;
        return await db.AiAgentRuns.FirstOrDefaultAsync(x => x.Id == id && x.UserId == tenant.UserId, ct);
    }

    private async Task<AiAgentRunDto?> MapRunAsync(Guid id, CancellationToken ct)
    {
        if (tenant.UserId is null) return null;
        var run = await db.AiAgentRuns.AsNoTracking()
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == tenant.UserId, ct);
        if (run is null) return null;

        var pending = await db.AiPendingActions.AsNoTracking()
            .Where(x => x.AgentRunId == run.Id)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

        var pendingDtos = pending.Select(p =>
        {
            try
            {
                var payload = AiActionPlanner.DeserializePayload(p.ActionType, p.PayloadJson);
                return AiActionPlanner.BuildPreview(p.Id, p.ActionType, p.Status, p.PermissionRequired, payload, [], p.ResultDocumentId, p.ResultDocumentNo);
            }
            catch
            {
                return new AiActionPreviewDto(p.Id, p.ActionType, p.Status, p.PermissionRequired, p.ActionType, [], [], p.ResultDocumentId, p.ResultDocumentNo);
            }
        }).ToList();

        var proposed = new List<AiAgentProposedPoDto>();
        foreach (var p in pending.Where(x => x.ActionType == AiActionTypes.PurchaseOrderDraft))
        {
            try
            {
                var po = JsonSerializer.Deserialize<PurchaseOrderCreateDto>(p.PayloadJson, JsonOpts)!;
                var party = await db.Parties.AsNoTracking().FirstOrDefaultAsync(x => x.Id == po.PartyId, ct);
                proposed.Add(new AiAgentProposedPoDto(
                    p.Id,
                    po.PartyId,
                    party?.Name ?? po.PartyId.ToString(),
                    po.Lines.Select(l => $"{l.ProductName ?? l.ProductId.ToString()}: {l.Qty:0.####}").ToList(),
                    po.Lines.Sum(l => l.Qty)));
            }
            catch { /* ignore */ }
        }

        // Prefer summary steps (no LinkedPendingActionId) for the step list UI
        var stepDtos = run.Steps
            .Where(s => s.LinkedPendingActionId is null || s.ToolName != "preparePurchaseOrders")
            .GroupBy(s => new { s.StepNo, s.ToolName })
            .Select(g => g.OrderByDescending(x => x.ExecutedAt).First())
            .OrderBy(s => s.StepNo)
            .Select(s => new AiAgentStepDto(
                s.Id, s.StepNo, s.ToolName, s.Status, s.RequiresConfirmation, s.LinkedPendingActionId,
                Truncate(s.OutputJson), s.ExecutedAt))
            .ToList();

        // If we filtered out all prepare steps, include one WaitingConfirm aggregate
        if (!stepDtos.Any(s => s.ToolName == "preparePurchaseOrders") &&
            run.Steps.Any(s => s.ToolName == "preparePurchaseOrders"))
        {
            var prep = run.Steps.Where(s => s.ToolName == "preparePurchaseOrders").OrderBy(s => s.CreatedAt).First();
            var waiting = run.Steps.Any(s => s.ToolName == "preparePurchaseOrders" && s.Status == AiAgentStepStatuses.WaitingConfirm);
            stepDtos.Add(new AiAgentStepDto(prep.Id, 3, "preparePurchaseOrders",
                waiting ? AiAgentStepStatuses.WaitingConfirm : prep.Status,
                true, null, $"{pending.Count} PO draft(s)", prep.ExecutedAt));
            stepDtos = stepDtos.OrderBy(s => s.StepNo).ToList();
        }

        var summary = run.Status switch
        {
            AiAgentRunStatuses.WaitingConfirmation => $"{proposed.Count} purchase order draft(s) ready — confirmation required.",
            AiAgentRunStatuses.Completed when proposed.Count == 0 && (run.PlanJson.Contains("No products") || run.PlanJson.Contains("shortages\":[]")) =>
                "No products below minimum stock.",
            AiAgentRunStatuses.Completed => $"Completed — {pending.Count(p => p.Status == AiActionStatuses.Confirmed)} draft PO(s) created.",
            AiAgentRunStatuses.Failed => run.FailureReason ?? "Agent failed.",
            AiAgentRunStatuses.Cancelled => "Agent run cancelled.",
            _ => run.Goal
        };

        return new AiAgentRunDto(
            run.Id, run.AgentType, run.Goal, run.Status, summary, stepDtos, proposed, pendingDtos,
            run.StartedAt, run.CompletedAt, run.FailureReason);
    }

    private static string? Truncate(string? json) =>
        string.IsNullOrEmpty(json) ? null : json.Length <= 160 ? json : json[..157] + "...";

    private sealed record ShortageLine(Guid ProductId, string Code, string Name, decimal OnHand, decimal ReorderLevel, decimal ShortageQty);
    private sealed record ResolvedShortage(ShortageLine Shortage, Guid SupplierId, string SupplierName, decimal Rate);
}
