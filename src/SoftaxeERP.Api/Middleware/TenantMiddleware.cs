using System.Security.Claims;
using SoftaxeERP.Application.Interfaces;

namespace SoftaxeERP.Api.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantContext tenant)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
            var userClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value;
            if (Guid.TryParse(tenantClaim, out var tid))
            {
                Guid? uid = Guid.TryParse(userClaim, out var u) ? u : null;
                tenant.Set(tid, uid);
            }
        }
        await _next(context);
    }
}
