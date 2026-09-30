using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Entities;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

public class AiAssistantService(
    AppDbContext db,
    ITenantContext tenant,
    IPermissionService permissions,
    IAiIntentResolver intents,
    IAiToolEngine tools,
    IAiActionPlanner actionPlanner,
    IAiActionExecutor actionExecutor,
    IAiAgentOrchestrator agents,
    ILlmProvider llm,
    IAuditService audit) : IAiAssistantService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<AiChatResponse> ChatAsync(AiChatRequest request, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Ai.Chat.View", ct);
        if (string.IsNullOrWhiteSpace(request.Message))
            throw new InvalidOperationException("Message is required.");
        if (tenant.UserId is null)
            throw new UnauthorizedAccessException("User context required.");

        var conversation = await GetOrCreateConversationAsync(request.ConversationId, request.Message, ct);

        db.AiMessages.Add(new AiMessage
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = request.Message.Trim()
        });
        await db.SaveChangesAsync(ct);

        var resolution = intents.Resolve(request.Message);
        var sources = new List<AiChatSourceDto>();

        if (resolution.Intent == AiIntents.DeniedUnsafe)
        {
            const string answer =
                "I cannot help with that request. Nexus AI never runs SQL, never bypasses permissions or confirmation, and never posts or approves documents.";
            await PersistAssistantAsync(conversation, answer, resolution.Intent, null, ct);
            await AuditAsync(conversation.Id, resolution.Intent, Array.Empty<string>(), true, null, ct);
            return new AiChatResponse(conversation.Id, resolution.Intent, answer, sources, Denied: true, Kind: "Denied");
        }

        if (resolution.Intent == AiIntents.Unknown)
        {
            const string answer =
                "I can help with read-only reports, inventory demand forecasts, draft actions (confirm before create), and the Inventory Replenishment agent (low stock → PO drafts → your confirmation).";
            await PersistAssistantAsync(conversation, answer, resolution.Intent, null, ct);
            await AuditAsync(conversation.Id, resolution.Intent, Array.Empty<string>(), true, null, ct);
            return new AiChatResponse(conversation.Id, resolution.Intent, answer, sources, Kind: "Clarification");
        }

        if (resolution.Intent == AiIntents.AgentNotAvailable)
        {
            const string answer =
                "That agent is not available in Phase 10C. Only the Inventory Replenishment agent is enabled (find low stock and prepare purchase order drafts for confirmation).";
            await PersistAssistantAsync(conversation, answer, resolution.Intent, null, ct);
            await AuditAsync(conversation.Id, resolution.Intent, Array.Empty<string>(), true, null, ct);
            return new AiChatResponse(conversation.Id, resolution.Intent, answer, sources, Kind: "Clarification");
        }

        if (AiIntents.IsAgentIntent(resolution.Intent))
        {
            try
            {
                var run = await agents.StartAsync(conversation.Id, resolution, request.Message, ct);
                var kind = run.Status == AiAgentRunStatuses.WaitingConfirmation ? "AgentWaitingConfirm"
                    : run.Status == AiAgentRunStatuses.Completed ? "AgentCompleted"
                    : "AgentPlan";
                var answer =
                    $"Agent: {run.AgentType}\n{run.Summary}\n\nSteps: {string.Join(" → ", run.Steps.Select(s => s.ToolName))}\n\n" +
                    (run.Status == AiAgentRunStatuses.WaitingConfirmation
                        ? "CONFIRMATION REQUIRED — use Confirm All to create draft POs via existing Nexus APIs (never auto-post)."
                        : run.Status == AiAgentRunStatuses.Completed
                            ? "No further action needed."
                            : "Review the agent plan.");
                await PersistAssistantAsync(conversation, answer, resolution.Intent, null, ct);
                await AuditAsync(conversation.Id, resolution.Intent, [run.AgentType], true, null, ct);
                return new AiChatResponse(conversation.Id, resolution.Intent, answer, sources, Kind: kind, Agent: run);
            }
            catch (UnauthorizedAccessException ex)
            {
                var perm = ExtractPermission(ex.Message);
                var answer = $"Access denied. {ex.Message}";
                await PersistAssistantAsync(conversation, answer, resolution.Intent, null, ct);
                await AuditAsync(conversation.Id, resolution.Intent, Array.Empty<string>(), false, perm, ct);
                return new AiChatResponse(conversation.Id, resolution.Intent, answer, sources, Denied: true, DeniedPermission: perm, Kind: "Denied");
            }
            catch (InvalidOperationException ex)
            {
                var answer = $"Agent could not complete: {ex.Message}";
                await PersistAssistantAsync(conversation, answer, resolution.Intent, null, ct);
                await AuditAsync(conversation.Id, resolution.Intent, Array.Empty<string>(), false, null, ct);
                return new AiChatResponse(conversation.Id, resolution.Intent, answer, sources, Kind: "Clarification");
            }
        }

        if (AiIntents.IsActionIntent(resolution.Intent))
        {
            try
            {
                var preview = await actionPlanner.ProposeAsync(conversation.Id, resolution, request.Message, ct);
                var answer =
                    $"{preview.Summary}\n\nStatus: {preview.Status}\nPermission: {preview.PermissionRequired}\n\nReview the draft below. Only Confirm & Create will call Nexus APIs (always as Draft — never post/approve).";
                await PersistAssistantAsync(conversation, answer, resolution.Intent, null, ct, preview.Id);
                await AuditAsync(conversation.Id, resolution.Intent, [preview.ActionType], true, null, ct);
                return new AiChatResponse(conversation.Id, resolution.Intent, answer, sources, Kind: "DraftAction", Action: preview);
            }
            catch (UnauthorizedAccessException ex)
            {
                var perm = ExtractPermission(ex.Message);
                var answer = $"Access denied. {ex.Message}";
                await PersistAssistantAsync(conversation, answer, resolution.Intent, null, ct);
                await AuditAsync(conversation.Id, resolution.Intent, Array.Empty<string>(), false, perm, ct);
                return new AiChatResponse(conversation.Id, resolution.Intent, answer, sources, Denied: true, DeniedPermission: perm, Kind: "Denied");
            }
            catch (InvalidOperationException ex)
            {
                var answer = $"Could not prepare draft: {ex.Message}";
                await PersistAssistantAsync(conversation, answer, resolution.Intent, null, ct);
                await AuditAsync(conversation.Id, resolution.Intent, Array.Empty<string>(), false, null, ct);
                return new AiChatResponse(conversation.Id, resolution.Intent, answer, sources, Kind: "Clarification");
            }
        }

        await permissions.EnsureAsync("Ai.Chat.Query", ct);
        var toolResult = await tools.ExecuteAsync(resolution, ct);

        if (toolResult.Denied)
        {
            var answer = $"Access denied. You need permission `{toolResult.DeniedPermission}` to run this report.";
            sources.Add(new AiChatSourceDto(toolResult.ToolName, toolResult.Label));
            await PersistAssistantAsync(conversation, answer, resolution.Intent, toolResult, ct);
            await AuditAsync(conversation.Id, resolution.Intent, [toolResult.ToolName], false, toolResult.DeniedPermission, ct);
            return new AiChatResponse(conversation.Id, resolution.Intent, answer, sources, Denied: true, DeniedPermission: toolResult.DeniedPermission, Kind: "Denied");
        }

        if (!string.IsNullOrEmpty(toolResult.Error) || toolResult.Data is null)
        {
            var answer = toolResult.Error ?? "Tool returned no data.";
            await PersistAssistantAsync(conversation, answer, resolution.Intent, toolResult, ct);
            await AuditAsync(conversation.Id, resolution.Intent, [toolResult.ToolName], false, null, ct);
            return new AiChatResponse(conversation.Id, resolution.Intent, answer, sources, Kind: "Answer");
        }

        sources.Add(new AiChatSourceDto(toolResult.ToolName, toolResult.Label));
        var factsJson = JsonSerializer.Serialize(toolResult.Data, JsonOpts);
        var messages = new List<LlmChatMessage>
        {
            new("system",
                "You are SheikhGo-Nexus-ERP assistant. Use ONLY the FACTS JSON. Label sections FACT and INTERPRETATION. Never invent numbers or forecast figures beyond FACTS. Never generate SQL. Never mention Softaxe. Never post or approve documents. Forecast recommendations do not create purchase orders."),
            new("user",
                $"Intent: {resolution.Intent}\nPeriod: {resolution.From:yyyy-MM-dd} to {resolution.To:yyyy-MM-dd}\nAsOf: {resolution.AsOf:yyyy-MM-dd}\nFACTS JSON:\n{factsJson}")
        };
        var llmAnswer = await llm.CompleteAsync(messages, ct);

        await PersistAssistantAsync(conversation, llmAnswer, resolution.Intent, toolResult, ct);
        await AuditAsync(conversation.Id, resolution.Intent, [toolResult.ToolName], true, null, ct);
        return new AiChatResponse(conversation.Id, resolution.Intent, llmAnswer, sources, Kind: "Answer");
    }

    public Task<AiActionPreviewDto?> GetActionAsync(Guid id, CancellationToken ct = default) =>
        actionExecutor.GetAsync(id, ct);

    public Task<AiActionPreviewDto> UpdateActionAsync(Guid id, AiActionUpdateRequest request, CancellationToken ct = default) =>
        actionExecutor.UpdateAsync(id, request, ct);

    public Task<AiActionConfirmResponse> ConfirmActionAsync(Guid id, AiActionConfirmRequest? request = null, CancellationToken ct = default) =>
        actionExecutor.ConfirmAsync(id, request, ct);

    public Task<AiActionPreviewDto> CancelActionAsync(Guid id, CancellationToken ct = default) =>
        actionExecutor.CancelAsync(id, ct);

    public Task<IReadOnlyList<AiAgentRunDto>> ListAgentRunsAsync(CancellationToken ct = default) =>
        agents.ListAsync(ct);

    public Task<AiAgentRunDto?> GetAgentRunAsync(Guid id, CancellationToken ct = default) =>
        agents.GetAsync(id, ct);

    public Task<AiAgentConfirmResponse> ConfirmAgentRunAsync(Guid id, AiAgentConfirmRequest? request = null, CancellationToken ct = default) =>
        agents.ConfirmAsync(id, request, ct);

    public Task<AiAgentRunDto> CancelAgentRunAsync(Guid id, CancellationToken ct = default) =>
        agents.CancelAsync(id, ct);

    public async Task<IReadOnlyList<AiConversationListItemDto>> ListConversationsAsync(CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Ai.Chat.View", ct);
        if (tenant.UserId is null) return [];
        return await db.AiConversations.AsNoTracking()
            .Where(x => x.UserId == tenant.UserId)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Select(x => new AiConversationListItemDto(x.Id, x.Title, x.CreatedAt, x.UpdatedAt ?? x.CreatedAt))
            .Take(50)
            .ToListAsync(ct);
    }

    public async Task<AiConversationDetailDto?> GetConversationAsync(Guid id, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Ai.Chat.View", ct);
        if (tenant.UserId is null) return null;
        var conv = await db.AiConversations.AsNoTracking()
            .Include(x => x.Messages)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == tenant.UserId, ct);
        if (conv is null) return null;
        var msgs = conv.Messages.OrderBy(m => m.CreatedAt)
            .Select(m => new AiMessageDto(m.Id, m.Role, m.Content, m.Intent, m.CreatedAt)).ToList();
        return new AiConversationDetailDto(conv.Id, conv.Title, msgs);
    }

    private async Task<AiConversation> GetOrCreateConversationAsync(Guid? id, string message, CancellationToken ct)
    {
        if (id is Guid cid)
        {
            var existing = await db.AiConversations.FirstOrDefaultAsync(x => x.Id == cid && x.UserId == tenant.UserId, ct);
            if (existing is not null) return existing;
        }

        var title = message.Trim();
        if (title.Length > 80) title = title[..77] + "...";
        var conv = new AiConversation
        {
            UserId = tenant.UserId!.Value,
            CompanyId = tenant.CompanyId,
            BranchId = tenant.BranchId,
            Title = string.IsNullOrWhiteSpace(title) ? "New chat" : title
        };
        db.AiConversations.Add(conv);
        await db.SaveChangesAsync(ct);
        return conv;
    }

    private async Task PersistAssistantAsync(
        AiConversation conversation, string answer, string intent, AiToolResult? tool, CancellationToken ct, Guid? actionId = null)
    {
        object? meta = tool is null
            ? (actionId is null ? null : new { actionId })
            : new { tool.ToolName, tool.Label, tool.Denied, tool.DeniedPermission, actionId };
        db.AiMessages.Add(new AiMessage
        {
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = answer,
            Intent = intent,
            ToolsJson = meta is null ? null : JsonSerializer.Serialize(meta, JsonOpts)
        });
        conversation.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private Task AuditAsync(Guid conversationId, string intent, IReadOnlyList<string> toolsUsed, bool success, string? denied, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new
        {
            intent,
            tools = toolsUsed,
            success,
            deniedPermission = denied,
            companyId = tenant.CompanyId,
            branchId = tenant.BranchId,
            financialYearId = tenant.FinancialYearId
        }, JsonOpts);
        return audit.WriteAsync(nameof(AiConversation), conversationId, "AiQuery", null, payload, ct);
    }

    private static string? ExtractPermission(string message)
    {
        const string prefix = "Missing permission: ";
        if (message.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return message[prefix.Length..].Trim();
        return message;
    }
}
