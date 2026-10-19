using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SheikhGoNexusERP.Application.Interfaces;

namespace SheikhGoNexusERP.Api.Hubs;

/// <summary>
/// Tenant-scoped POS notifications (shift closed, low stock).
/// Clients join group tenant:{tenantId} on connect.
/// </summary>
[Authorize]
public class PosNotifyHub(ITenantContext tenant) : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (tenant.TenantId != Guid.Empty)
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(tenant.TenantId));
        await base.OnConnectedAsync();
    }

    public static string GroupName(Guid tenantId) => $"tenant:{tenantId}";
}

public record PosNotifyPayload(string Type, string Message, object? Data = null);
