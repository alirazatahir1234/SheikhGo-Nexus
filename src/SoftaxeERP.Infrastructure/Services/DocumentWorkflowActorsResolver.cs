using Microsoft.EntityFrameworkCore;
using SoftaxeERP.Application.DTOs;
using SoftaxeERP.Domain.Entities;
using SoftaxeERP.Infrastructure.Persistence;

namespace SoftaxeERP.Infrastructure.Services;

/// <summary>
/// Projects Submitted/Verified/Approved/Posted actors from Document + Verification/Approval + AuditLog
/// without denormalizing columns onto Document.
/// </summary>
public static class DocumentWorkflowActorsResolver
{
    public static async Task<DocumentWorkflowActorsDto?> ResolveAsync(
        AppDbContext db,
        string entityName,
        Guid entityId,
        Guid? documentId,
        string? createdBy,
        DateTime createdAt,
        CancellationToken ct = default)
    {
        var userNames = await db.Users.AsNoTracking()
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(x => x.Id, x => x.FullName, ct);

        string? NameOf(Guid? userId) =>
            userId is Guid id && userNames.TryGetValue(id, out var n) ? n : null;

        DocumentActorDto? created = !string.IsNullOrWhiteSpace(createdBy) || createdAt != default
            ? new DocumentActorDto(null, createdBy, createdAt)
            : null;

        DocumentActorDto? verified = null;
        DocumentActorDto? approved = null;

        if (documentId is Guid docId)
        {
            var verification = await db.Verifications.AsNoTracking()
                .Where(v => v.DocumentId == docId && v.IsVerified)
                .OrderByDescending(v => v.VerifiedAt)
                .FirstOrDefaultAsync(ct);
            if (verification is not null)
            {
                verified = new DocumentActorDto(
                    verification.VerifiedByUserId,
                    NameOf(verification.VerifiedByUserId),
                    verification.VerifiedAt,
                    verification.Remarks);
            }

            var approval = await db.Approvals.AsNoTracking()
                .Where(a => a.DocumentId == docId && a.IsApproved)
                .OrderByDescending(a => a.ApprovedAt)
                .FirstOrDefaultAsync(ct);
            if (approval is not null)
            {
                approved = new DocumentActorDto(
                    approval.ApprovedByUserId,
                    NameOf(approval.ApprovedByUserId),
                    approval.ApprovedAt,
                    approval.Remarks);
            }
        }

        var audits = await db.AuditLogs.AsNoTracking()
            .Where(a => a.EntityName == entityName && a.EntityId == entityId.ToString())
            .OrderBy(a => a.Timestamp)
            .ToListAsync(ct);

        DocumentActorDto? FromAudit(string action)
        {
            var row = audits.LastOrDefault(a =>
                string.Equals(a.Action, action, StringComparison.OrdinalIgnoreCase));
            if (row is null) return null;
            return new DocumentActorDto(row.UserId, NameOf(row.UserId), row.Timestamp);
        }

        var submitted = FromAudit("submit") ?? FromAudit("Submitted");
        var posted = FromAudit("post") ?? FromAudit("Posted") ?? FromAudit("PostedEffects");

        if (created is null)
            created = FromAudit("Created");

        if (created is null && submitted is null && verified is null && approved is null && posted is null)
            return null;

        return new DocumentWorkflowActorsDto(created, submitted, verified, approved, posted);
    }
}
