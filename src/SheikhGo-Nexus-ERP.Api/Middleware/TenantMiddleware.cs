using System.Security.Claims;
using SheikhGoNexusERP.Application.Interfaces;

namespace SheikhGoNexusERP.Api.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantContext tenant)
    {
        var clientIp = ResolveClientIp(context);
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
            var userClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value;
            if (Guid.TryParse(tenantClaim, out var tid))
            {
                Guid? uid = Guid.TryParse(userClaim, out var u) ? u : null;
                Guid? companyId = Guid.TryParse(context.User.FindFirst("company_id")?.Value, out var c) ? c : null;
                Guid? branchId = Guid.TryParse(context.User.FindFirst("branch_id")?.Value, out var b) ? b : null;
                Guid? fyId = Guid.TryParse(context.User.FindFirst("financial_year_id")?.Value, out var f) ? f : null;
                tenant.Set(tid, uid, clientIp, companyId, branchId, fyId);
            }
        }
        await _next(context);
    }

    private static string? ResolveClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first)) return first;
        }
        return context.Connection.RemoteIpAddress?.ToString();
    }
}
