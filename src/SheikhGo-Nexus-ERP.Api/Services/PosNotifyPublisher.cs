using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Api.Hubs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Api.Services;

public class PosNotifyPublisher(
    IHubContext<PosNotifyHub> hub,
    ITenantContext tenant,
    AppDbContext db)
{
    public async Task ShiftClosedAsync(string shiftNo, decimal? variance, CancellationToken ct = default)
    {
        if (tenant.TenantId == Guid.Empty) return;
        await hub.Clients.Group(PosNotifyHub.GroupName(tenant.TenantId)).SendAsync(
            "posNotify",
            new PosNotifyPayload(
                "ShiftClosed",
                $"Shift {shiftNo} closed" + (variance is null ? "." : $" (variance {variance:0.##})."),
                new { shiftNo, variance }),
            ct);
    }

    public async Task LowStockAfterSaleAsync(IEnumerable<Guid> productIds, Guid warehouseId, CancellationToken ct = default)
    {
        if (tenant.TenantId == Guid.Empty) return;
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0) return;

        var lows = await db.Products.AsNoTracking()
            .Where(p => ids.Contains(p.Id) && p.ReorderLevel > 0)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.ReorderLevel,
                Qty = p.Stocks.Where(s => s.WarehouseId == warehouseId).Sum(s => s.Quantity)
            })
            .Where(x => x.Qty <= x.ReorderLevel)
            .ToListAsync(ct);

        foreach (var row in lows)
        {
            await hub.Clients.Group(PosNotifyHub.GroupName(tenant.TenantId)).SendAsync(
                "posNotify",
                new PosNotifyPayload(
                    "LowStock",
                    $"{row.Name} is low ({row.Qty:0.###} ≤ reorder {row.ReorderLevel:0.###}).",
                    new { productId = row.Id, name = row.Name, qty = row.Qty, reorderLevel = row.ReorderLevel }),
                ct);
        }
    }
}
